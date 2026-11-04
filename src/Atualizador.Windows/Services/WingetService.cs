using System.Text.RegularExpressions;
using Atualizador.Windows.Models;

namespace Atualizador.Windows.Services;

/// <summary>
/// Executa o <c>winget.exe</c> e converte a saída em modelos tipados.
/// <para>Regras de segurança aplicadas em todas as chamadas:</para>
/// <list type="bullet">
/// <item>argumentos passados um a um (<c>ArgumentList</c>), nunca uma linha de comando montada com texto do usuário;</item>
/// <item>atualização e instalação sempre por <c>--id</c> + <c>--exact</c> + <c>--source</c>, nunca pelo nome exibido;</item>
/// <item>ID e origem validados por expressão regular antes de qualquer execução;</item>
/// <item>somente a fonte <c>winget</c> é consultada (a Microsoft Store não é suportada).</item>
/// </list>
/// </summary>
public sealed partial class WingetService : IPackageUpdateService
{
    /// <summary>Consultas (versão, fontes, lista, pesquisa) costumam terminar em poucos segundos.</summary>
    private static readonly TimeSpan QueryTimeout = TimeSpan.FromSeconds(45);

    /// <summary>Instaladores grandes (IDEs, navegadores, drivers) podem levar vários minutos.</summary>
    private static readonly TimeSpan InstallTimeout = TimeSpan.FromMinutes(30);

    // Códigos de saída tratados como "precisa de administrador" e "precisa de interação",
    // para mostrar uma mensagem mais útil do que o texto bruto do WinGet.
    private const int AccessDeniedExitCode = 5;
    private const int InstallerRequiresElevationExitCode = unchecked((int)0x8A15002B);
    private const int InstallerRequiresInteractionExitCode = unchecked((int)0x8A150011);
    private const int UserCanceledUacError = 1223;

    private readonly IProcessExecutor _processExecutor;
    private readonly IExecutableLocator _locator;
    private readonly bool _isElevated;

    /// <param name="processExecutor">Executor de processos; os testes usam um falso para não tocar no sistema.</param>
    /// <param name="locator">Localiza o <c>winget.exe</c>; os testes usam um falso.</param>
    /// <param name="isElevated">Se o app está rodando como administrador (muda o diagnóstico de falhas). Os testes informam.</param>
    public WingetService(
        IProcessExecutor? processExecutor = null,
        IExecutableLocator? locator = null,
        bool? isElevated = null)
    {
        _processExecutor = processExecutor ?? new SystemProcessExecutor();
        _locator = locator ?? new WingetLocator();
        _isElevated = isElevated ?? ElevationGuard.IsElevated;
    }

    /// <summary>
    /// Lista os pacotes com atualização disponível na fonte <c>winget</c>.
    /// Antes, confere se o WinGet responde (<c>--version</c>) e se as fontes podem ser lidas,
    /// para dar uma mensagem de erro precisa em cada caso.
    /// </summary>
    /// <param name="includeUnknown">Inclui pacotes com versão desconhecida. Desligado por padrão, por segurança.</param>
    public async Task<WingetDiscoveryResult> DiscoverUpdatesAsync(
        bool includeUnknown = false,
        CancellationToken cancellationToken = default)
    {
        var wingetPath = _locator.Locate();
        if (wingetPath is null)
        {
            return DiscoveryFailure(
                WingetDiscoveryStatus.WinGetUnavailable,
                "O WinGet não foi localizado. Instale ou repare o App Installer e tente novamente.");
        }

        var versionResult = await ExecuteAsync(wingetPath, ["--version"], QueryTimeout, cancellationToken);
        if (!versionResult.Started || versionResult.Canceled || versionResult.TimedOut)
        {
            return DiscoveryFailure(
                WingetDiscoveryStatus.ExecutionFailed,
                versionResult.ErrorMessage ?? "Não foi possível iniciar o WinGet.",
                versionResult.ExitCode);
        }

        var wingetVersion = versionResult.StandardOutput.Trim();
        var sourceResult = await ExecuteAsync(
            wingetPath,
            ["source", "list", "--disable-interactivity"],
            QueryTimeout,
            cancellationToken);
        var sources = WingetSourceParser.Parse(sourceResult.StandardOutput);
        if (!sourceResult.Started || sourceResult.ExitCode != 0)
        {
            return DiscoveryFailure(
                WingetDiscoveryStatus.SourceFailed,
                "Não foi possível consultar as fontes do WinGet.",
                sourceResult.ExitCode,
                wingetVersion,
                sources);
        }

        // "list --upgrade-available" é mais estável entre versões do WinGet do que "upgrade" sem argumentos.
        var arguments = new List<string> { "list", "--upgrade-available", "--source", "winget", "--disable-interactivity" };
        if (includeUnknown)
        {
            arguments.Add("--include-unknown");
        }

        var updatesResult = await ExecuteAsync(wingetPath, arguments, QueryTimeout, cancellationToken);
        if (!updatesResult.Started || updatesResult.Canceled || updatesResult.TimedOut)
        {
            return DiscoveryFailure(
                WingetDiscoveryStatus.ExecutionFailed,
                updatesResult.ErrorMessage ?? "Não foi possível descobrir as atualizações.",
                updatesResult.ExitCode,
                wingetVersion,
                sources);
        }

        var parsed = WingetOutputParser.ParseUpgradeList(updatesResult.StandardOutput);
        var message = parsed.Packages.Count == 0
            ? "Nenhuma atualização disponível foi encontrada."
            : $"{parsed.Packages.Count} atualização(ões) encontrada(s).";

        // Código de saída diferente de zero ou linhas ignoradas no parsing = resultado parcial.
        var status = updatesResult.ExitCode == 0 && parsed.Warnings.Count == 0
            ? parsed.Packages.Count == 0 ? WingetDiscoveryStatus.NoUpdates : WingetDiscoveryStatus.Success
            : WingetDiscoveryStatus.Partial;

        return new(status, parsed.Packages, sources, wingetVersion, message, updatesResult.ExitCode, parsed.Warnings);
    }

    /// <summary>Atualiza um único pacote em modo silencioso. Nunca usa <c>--all</c>.</summary>
    public async Task<PackageUpdateResult> UpgradeAsync(
        UpdatablePackage package,
        CancellationToken cancellationToken = default)
    {
        if (!IsSafeIdentifier(package.Id) || !IsSafeIdentifier(package.Source))
        {
            return new(package, PackageUpdateStatus.Ignored, "O ID ou a origem do pacote não foi validado.", null);
        }

        if (package.IsVersionUnknown)
        {
            return new(package, PackageUpdateStatus.Ignored, "A versão instalada é desconhecida.", null);
        }

        var wingetPath = _locator.Locate();
        if (wingetPath is null)
        {
            return new(package, PackageUpdateStatus.Failed, "O WinGet não foi localizado.", null);
        }

        var processResult = await ExecuteAsync(
            wingetPath,
            BuildPackageArguments("upgrade", package.Id, package.Source),
            InstallTimeout,
            cancellationToken);

        if (processResult.Canceled)
        {
            return new(package, PackageUpdateStatus.Canceled, "A atualização foi cancelada.", null);
        }

        if (processResult.TimedOut)
        {
            return new(package, PackageUpdateStatus.Failed, "A atualização excedeu o tempo limite.", null);
        }

        if (!processResult.Started)
        {
            return new(package, PackageUpdateStatus.Failed, processResult.ErrorMessage ?? "Não foi possível iniciar o WinGet.", null);
        }

        if (processResult.ExitCode == 0)
        {
            return new(package, PackageUpdateStatus.Succeeded, "Atualização concluída.", processResult.ExitCode);
        }

        var (status, message) = ClassifyFailure(processResult, "atualização");
        return new(package, status, message, processResult.ExitCode);
    }

    /// <summary>
    /// Pesquisa aproximada no catálogo. Exige ao menos 2 caracteres para evitar listas enormes.
    /// <para>
    /// 1. Pesquisa o texto inteiro. Se o WinGet encontrar algo, todos os resultados são mantidos
    /// (ele também compara tags e apelidos que não aparecem na tabela) e só a ordem muda.
    /// </para>
    /// <para>
    /// 2. Se não encontrar nada, tenta termos mais curtos (<see cref="PackageSearch.BuildFallbackTerms"/>)
    /// e mantém só os pacotes em que todas as palavras digitadas combinam, mesmo que aproximadamente.
    /// Assim "firefox br" encontra "Mozilla Firefox (pt-BR)" e "firfox" encontra "Firefox".
    /// </para>
    /// </summary>
    public async Task<WingetSearchResult> SearchAsync(
        string query,
        string source = "winget",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            return new(WingetSearchStatus.NoPackagesFound, [], "Informe ao menos 2 caracteres para pesquisar.");
        }

        var wingetPath = _locator.Locate();
        if (wingetPath is null)
        {
            return new(
                WingetSearchStatus.WinGetUnavailable,
                [],
                "O WinGet não foi localizado. Instale ou repare o App Installer e tente novamente.");
        }

        var trimmedQuery = query.Trim();
        var exact = await SearchTermAsync(wingetPath, trimmedQuery, source, cancellationToken);
        if (exact.Failure is not null)
        {
            return exact.Failure;
        }

        if (exact.Packages.Count > 0)
        {
            var ranked = PackageSearch.Rank(trimmedQuery, exact.Packages, requireAllWords: false);
            return new(WingetSearchStatus.Success, ranked, $"{ranked.Count} aplicativo(s) encontrado(s).", exact.ExitCode);
        }

        foreach (var term in PackageSearch.BuildFallbackTerms(trimmedQuery))
        {
            var attempt = await SearchTermAsync(wingetPath, term, source, cancellationToken);
            if (attempt.Failure is not null)
            {
                return attempt.Failure;
            }

            var similar = PackageSearch.Rank(trimmedQuery, attempt.Packages, requireAllWords: true);
            if (similar.Count > 0)
            {
                return new(
                    WingetSearchStatus.Success,
                    similar,
                    $"Nenhum resultado exato para '{trimmedQuery}'. {similar.Count} aplicativo(s) parecido(s) encontrado(s).",
                    attempt.ExitCode);
            }
        }

        return new(WingetSearchStatus.NoPackagesFound, [], "Nenhum aplicativo foi encontrado para o termo informado.", exact.ExitCode);
    }

    /// <summary>Executa um único <c>winget search</c>. <c>Failure</c> só é preenchido se o WinGet não rodou até o fim.</summary>
    private async Task<(IReadOnlyList<InstallablePackage> Packages, int ExitCode, WingetSearchResult? Failure)> SearchTermAsync(
        string wingetPath,
        string term,
        string source,
        CancellationToken cancellationToken)
    {
        // O termo vai como um único argumento: o WinGet o recebe literalmente, sem interpretação de shell.
        var arguments = new List<string> { "search", term, "--disable-interactivity" };
        if (!string.IsNullOrWhiteSpace(source) && !source.Equals("Todas", StringComparison.OrdinalIgnoreCase))
        {
            arguments.Add("--source");
            arguments.Add(source);
        }

        var result = await ExecuteAsync(wingetPath, arguments, QueryTimeout, cancellationToken);
        if (!result.Started || result.Canceled || result.TimedOut)
        {
            return ([], result.ExitCode, new(
                WingetSearchStatus.ExecutionFailed,
                [],
                result.ErrorMessage ?? "A consulta ao catálogo do WinGet falhou.",
                result.ExitCode));
        }

        // "Nenhum pacote encontrado" também termina aqui: a tabela vem vazia.
        return (WingetOutputParser.ParseSearchResults(result.StandardOutput, source), result.ExitCode, null);
    }

    /// <summary>
    /// Instala um pacote em modo silencioso. Quando o instalador falha, tenta identificar se a causa
    /// foi falta de permissão de administrador ou necessidade de interação, para orientar o usuário.
    /// </summary>
    public async Task<PackageInstallResult> InstallAsync(
        InstallablePackage package,
        CancellationToken cancellationToken = default)
    {
        if (!IsSafeIdentifier(package.Id) || !IsSafeIdentifier(package.Source))
        {
            return new(package, PackageUpdateStatus.Failed, "O ID ou a origem do pacote não foi validado.", null);
        }

        var wingetPath = _locator.Locate();
        if (wingetPath is null)
        {
            return new(package, PackageUpdateStatus.Failed, "O WinGet não foi localizado.", null);
        }

        var processResult = await ExecuteAsync(
            wingetPath,
            BuildPackageArguments("install", package.Id, package.Source),
            InstallTimeout,
            cancellationToken);

        if (processResult.Canceled)
        {
            return new(package, PackageUpdateStatus.Canceled, "A instalação foi cancelada.", null);
        }

        if (processResult.TimedOut)
        {
            return new(package, PackageUpdateStatus.Failed, "A instalação excedeu o tempo limite.", null);
        }

        if (!processResult.Started)
        {
            return new(package, PackageUpdateStatus.Failed, processResult.ErrorMessage ?? "Não foi possível iniciar o WinGet.", null);
        }

        if (processResult.ExitCode == 0)
        {
            return new(package, PackageUpdateStatus.Succeeded, "Instalação concluída com sucesso.", processResult.ExitCode);
        }

        var (status, message) = ClassifyFailure(processResult, "instalação");
        return new(package, status, message, processResult.ExitCode);
    }

    /// <summary>
    /// Instala com permissão de administrador, para os poucos instaladores que exigem isso.
    /// Abre o <c>winget.exe</c> com <c>runas</c> (o Windows mostra o aviso do UAC) só para este pacote.
    /// </summary>
    public async Task<PackageInstallResult> InstallElevatedAsync(InstallablePackage package, CancellationToken cancellationToken = default)
    {
        var (status, message, exitCode) = await RunElevatedAsync("install", package.Id, package.Source, cancellationToken);
        return new PackageInstallResult(package, status, message, exitCode);
    }

    /// <summary>Atualiza com permissão de administrador (veja <see cref="InstallElevatedAsync"/>).</summary>
    public async Task<PackageUpdateResult> UpgradeElevatedAsync(UpdatablePackage package, CancellationToken cancellationToken = default)
    {
        var (status, message, exitCode) = await RunElevatedAsync("upgrade", package.Id, package.Source, cancellationToken);
        return new PackageUpdateResult(package, status, message, exitCode);
    }

    /// <summary>
    /// Executa <c>winget install/upgrade</c> elevado. Com <c>runas</c> não dá para ler a saída, só o código
    /// de saída. Os argumentos vêm de ID e origem já validados (letras, dígitos, ponto, hífen e
    /// sublinhado), então montá-los em uma linha não permite injetar outros comandos.
    /// </summary>
    private async Task<(PackageUpdateStatus Status, string Message, int? ExitCode)> RunElevatedAsync(
        string command, string id, string source, CancellationToken cancellationToken)
    {
        if (!IsSafeIdentifier(id) || !IsSafeIdentifier(source))
        {
            return (PackageUpdateStatus.Failed, "O ID ou a origem do pacote não foi validado.", null);
        }

        var wingetPath = _locator.Locate();
        if (wingetPath is null)
        {
            return (PackageUpdateStatus.Failed, "O WinGet não foi localizado.", null);
        }

        var startInfo = new System.Diagnostics.ProcessStartInfo(wingetPath)
        {
            UseShellExecute = true, // obrigatório para "runas"
            Verb = "runas",
            WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
            Arguments = string.Join(' ', BuildPackageArguments(command, id, source))
        };

        try
        {
            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process is null)
            {
                return (PackageUpdateStatus.Failed, "Não foi possível iniciar o WinGet como administrador.", null);
            }

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(InstallTimeout);
            await process.WaitForExitAsync(timeout.Token);
            return process.ExitCode == 0
                ? (PackageUpdateStatus.Succeeded, "Concluído como administrador.", process.ExitCode)
                : (PackageUpdateStatus.Failed, $"O WinGet terminou com o código 0x{process.ExitCode:X8}, mesmo como administrador.", process.ExitCode);
        }
        catch (System.ComponentModel.Win32Exception exception) when (exception.NativeErrorCode == UserCanceledUacError)
        {
            return (PackageUpdateStatus.Canceled, "A permissão de administrador não foi concedida.", null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return (PackageUpdateStatus.Failed, "A operação como administrador excedeu o tempo limite.", null);
        }
    }

    /// <summary>
    /// Traduz uma falha do WinGet em estado e mensagem úteis.
    /// <list type="bullet">
    /// <item>Códigos de acesso negado/exige elevação → <see cref="PackageUpdateStatus.RequiresElevation"/>
    /// (a tela oferece "Tentar como administrador").</item>
    /// <item>Texto citando "admin"/"elevat": se o app <b>já</b> está elevado, o problema é o contrário — o
    /// instalador não aceita rodar como administrador (caso de Discord e Spotify).</item>
    /// </list>
    /// </summary>
    private (PackageUpdateStatus Status, string Message) ClassifyFailure(ProcessExecutionResult result, string operation)
    {
        var combinedOutput = $"{result.StandardError} {result.StandardOutput}".Trim();
        var mentionsAdmin = combinedOutput.Contains("admin", StringComparison.OrdinalIgnoreCase)
            || combinedOutput.Contains("elevat", StringComparison.OrdinalIgnoreCase);

        if (result.ExitCode is AccessDeniedExitCode or InstallerRequiresElevationExitCode || (mentionsAdmin && !_isElevated))
        {
            return (PackageUpdateStatus.RequiresElevation, $"A {operation} exige privilégios de administrador.");
        }

        if (mentionsAdmin)
        {
            return (PackageUpdateStatus.Failed,
                "Este instalador não aceita rodar como administrador. " + ElevationGuard.ElevationAdvice);
        }

        if (result.ExitCode is InstallerRequiresInteractionExitCode
            || combinedOutput.Contains("interactive", StringComparison.OrdinalIgnoreCase))
        {
            return (PackageUpdateStatus.RequiresInteraction, "O instalador exige interação do usuário e não pôde ser executado em modo silencioso.");
        }

        var error = string.IsNullOrWhiteSpace(result.StandardError) ? result.StandardOutput : result.StandardError;
        return (PackageUpdateStatus.Failed, SummarizeError(error));
    }

    /// <summary>
    /// Argumentos comuns de <c>upgrade</c> e <c>install</c>: pacote exato, origem explícita, modo
    /// silencioso e aceite dos termos (o usuário já confirmou a operação na tela).
    /// </summary>
    private static string[] BuildPackageArguments(string command, string id, string source) =>
    [
        command,
        "--id", id,
        "--exact",
        "--source", source,
        "--silent",
        "--accept-source-agreements",
        "--accept-package-agreements",
        "--disable-interactivity"
    ];

    private Task<ProcessExecutionResult> ExecuteAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken) =>
        _processExecutor.ExecuteAsync(new ProcessExecutionRequest(fileName, arguments, timeout), cancellationToken);

    private static WingetDiscoveryResult DiscoveryFailure(
        WingetDiscoveryStatus status,
        string message,
        int? exitCode = null,
        string? version = null,
        IReadOnlyList<WingetSource>? sources = null) =>
        new(status, [], sources ?? [], version, message, exitCode, []);

    /// <summary>IDs do WinGet usam letras, dígitos, ponto, hífen e sublinhado (ex.: <c>Microsoft.Edge</c>).</summary>
    private static bool IsSafeIdentifier(string value) =>
        !string.IsNullOrWhiteSpace(value) && SafeIdentifierRegex().IsMatch(value);

    [GeneratedRegex(@"\A[A-Za-z0-9][A-Za-z0-9._-]*\z", RegexOptions.CultureInvariant)]
    private static partial Regex SafeIdentifierRegex();

    /// <summary>Limita a mensagem de erro a 300 caracteres para caber na tela e no histórico.</summary>
    private static string SummarizeError(string error)
    {
        var message = string.IsNullOrWhiteSpace(error)
            ? "O WinGet retornou um código de erro."
            : error.Trim();
        return message.Length <= 300 ? message : $"{message[..297]}...";
    }
}
