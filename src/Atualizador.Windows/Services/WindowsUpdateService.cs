using System.Runtime.InteropServices;
using Atualizador.Windows.Models;

namespace Atualizador.Windows.Services;

/// <summary>
/// Acesso ao Windows Update pela API oficial do Windows Update Agent (objetos COM
/// <c>Microsoft.Update.*</c>), a mesma usada pelo PowerShell e pela tela Configurações do Windows.
/// <list type="bullet">
/// <item><see cref="Search"/> funciona sem permissão de administrador;</item>
/// <item><see cref="InstallAll"/> exige administrador e só roda no processo elevado
/// (veja <see cref="WindowsUpdateInstallLauncher"/>).</item>
/// </list>
/// As chamadas são síncronas e podem levar minutos; use-as em segundo plano (<c>Task.Run</c>).
/// </summary>
public static class WindowsUpdateService
{
    /// <summary>Atualizações ainda não instaladas e não ocultadas pelo usuário (inclui drivers, como o Windows faz).</summary>
    private const string SearchCriteria = "IsInstalled=0 and IsHidden=0";

    // Valores de OperationResultCode do Windows Update Agent.
    private const int ResultSucceeded = 2;
    private const int ResultSucceededWithErrors = 3;

    // InstallationBehavior.RebootBehavior: 0 = nunca, 1 = sempre, 2 = pode pedir.
    private const int RebootNever = 0;

    /// <summary>Procura as atualizações disponíveis. Nunca lança exceção: falhas voltam em <c>Message</c>.</summary>
    public static WindowsUpdateSearchResult Search()
    {
        try
        {
            var updates = SearchComUpdates(out _);
            var list = new List<WindowsUpdateInfo>();
            for (var index = 0; index < (int)updates.Count; index++)
            {
                list.Add(ToInfo((object)updates[index]));
            }

            return new(
                true,
                list,
                list.Count == 0 ? "O Windows está atualizado." : $"{list.Count} atualização(ões) do Windows disponível(is).");
        }
        catch (Exception exception) when (exception is COMException or InvalidOperationException or UnauthorizedAccessException)
        {
            return new(false, [], $"Não foi possível consultar o Windows Update: {DescribeError(exception)}");
        }
    }

    /// <summary>Indica se o Windows já está aguardando um reinício para concluir atualizações.</summary>
    public static bool IsRebootPending()
    {
        try
        {
            dynamic systemInfo = CreateComObject("Microsoft.Update.SystemInfo");
            return (bool)systemInfo.RebootRequired;
        }
        catch (Exception exception) when (exception is COMException or InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>
    /// Procura, aceita os termos, baixa e instala todas as atualizações disponíveis, como o script
    /// de manutenção faz. Instala uma por vez, para informar o progresso e não perder as demais se uma falhar.
    /// Exige administrador. <paramref name="report"/> recebe cada etapa.
    /// </summary>
    public static WindowsUpdateProgress InstallAll(Action<WindowsUpdateProgress> report)
    {
        try
        {
            report(new(WindowsUpdateStage.Searching, 0, 0, "Procurando atualizações do Windows…"));
            var updates = SearchComUpdates(out var sessionObject);
            dynamic session = sessionObject;
            var total = (int)updates.Count;
            if (total == 0)
            {
                return Finish(report, new(WindowsUpdateStage.Finished, 0, 0, "O Windows já está atualizado.", true, IsRebootPending(), []));
            }

            for (var index = 0; index < total; index++)
            {
                dynamic update = updates[index];
                if (!(bool)update.EulaAccepted)
                {
                    update.AcceptEula();
                }
            }

            report(new(WindowsUpdateStage.Downloading, 0, total, $"Baixando {total} atualização(ões)… Isso pode levar alguns minutos."));
            dynamic downloader = session.CreateUpdateDownloader();
            downloader.Updates = updates;
            downloader.Download();

            var results = new List<WindowsUpdateItemResult>();
            var rebootRequired = false;
            for (var index = 0; index < total; index++)
            {
                object update = updates[index];
                var info = ToInfo(update);
                report(new(WindowsUpdateStage.Installing, index + 1, total, $"Instalando {info.Title} ({index + 1} de {total})…"));
                results.Add(InstallOne(sessionObject, update, info, ref rebootRequired));
            }

            var failures = results.Count(result => !result.Succeeded);
            var message = failures == 0
                ? $"{total} atualização(ões) do Windows instalada(s)."
                : $"{total - failures} de {total} atualização(ões) instalada(s); {failures} falhou(aram).";
            if (rebootRequired)
            {
                message += " Reinicie o computador para concluir.";
            }

            return Finish(report, new(WindowsUpdateStage.Finished, total, total, message, failures == 0, rebootRequired, results));
        }
        catch (Exception exception) when (exception is COMException or InvalidOperationException or UnauthorizedAccessException)
        {
            return Finish(report, new(
                WindowsUpdateStage.Finished, 0, 0,
                $"A instalação das atualizações do Windows falhou: {DescribeError(exception)}", false));
        }
    }

    private static WindowsUpdateProgress Finish(Action<WindowsUpdateProgress> report, WindowsUpdateProgress final)
    {
        report(final);
        return final;
    }

    private static WindowsUpdateItemResult InstallOne(object sessionObject, object updateObject, WindowsUpdateInfo info, ref bool rebootRequired)
    {
        dynamic session = sessionObject;
        dynamic update = updateObject;
        try
        {
            if (!(bool)update.IsDownloaded)
            {
                return new(info.Title, info.KbArticle, false, "O download não foi concluído.");
            }

            dynamic single = CreateComObject("Microsoft.Update.UpdateColl");
            single.Add(update);
            dynamic installer = session.CreateUpdateInstaller();
            installer.Updates = single;
            dynamic result = installer.Install();
            var code = (int)result.ResultCode;
            if ((bool)result.RebootRequired)
            {
                rebootRequired = true;
            }

            return code is ResultSucceeded or ResultSucceededWithErrors
                ? new(info.Title, info.KbArticle, true, "Instalada.")
                : new(info.Title, info.KbArticle, false, $"O Windows informou falha (código {code}).");
        }
        catch (COMException exception)
        {
            return new(info.Title, info.KbArticle, false, DescribeError(exception));
        }
    }

    private static dynamic SearchComUpdates(out object sessionObject)
    {
        dynamic session = CreateComObject("Microsoft.Update.Session");
        sessionObject = session;
        session.ClientApplicationID = "App Updater";
        dynamic searcher = session.CreateUpdateSearcher();
        dynamic result = searcher.Search(SearchCriteria);
        return result.Updates;
    }

    private static WindowsUpdateInfo ToInfo(object updateObject)
    {
        dynamic update = updateObject;
        string? kb = null;
        dynamic kbIds = update.KBArticleIDs;
        if ((int)kbIds.Count > 0)
        {
            kb = $"KB{kbIds[0]}";
        }

        var category = "Atualização";
        dynamic categories = update.Categories;
        if ((int)categories.Count > 0)
        {
            category = (string)categories[0].Name;
        }

        return new WindowsUpdateInfo(
            (string)update.Identity.UpdateID,
            (string)update.Title,
            kb,
            category,
            Convert.ToInt64((object)update.MaxDownloadSize, System.Globalization.CultureInfo.InvariantCulture),
            (int)update.InstallationBehavior.RebootBehavior != RebootNever);
    }

    private static dynamic CreateComObject(string progId)
    {
        var type = Type.GetTypeFromProgID(progId)
            ?? throw new InvalidOperationException("O Windows Update Agent não está disponível neste Windows.");
        return Activator.CreateInstance(type)
            ?? throw new InvalidOperationException("O Windows Update Agent não está disponível neste Windows.");
    }

    /// <summary>Mensagens para os erros mais comuns do Windows Update; os demais mostram o código.</summary>
    private static string DescribeError(Exception exception) => exception is COMException com
        ? unchecked((uint)com.HResult) switch
        {
            0x8024402C or 0x80072EFD or 0x80072EE2 => "sem conexão com os servidores do Windows Update.",
            0x80240044 or 0x80070005 => "é necessária permissão de administrador.",
            0x8024001E => "o serviço Windows Update foi interrompido.",
            var code => $"código 0x{code:X8}."
        }
        : exception.Message;
}
