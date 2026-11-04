using Atualizador.Windows.Models;
using Atualizador.Windows.Services;

namespace Atualizador.Windows.ViewModels;

/// <summary>
/// Atualização do próprio App Updater.
/// <para>
/// Fluxo: consulta a última release no GitHub → se houver versão nova e o app estiver instalado
/// pelo instalador, baixa <c>Atualizador-Setup.exe</c>, confere o SHA-256, executa o instalador
/// em modo silencioso e pede o encerramento do app (o instalador o reabre na versão nova).
/// Em cópias portáteis, apenas avisa e oferece o link da release.
/// </para>
/// <para>
/// A verificação roda sozinha ao abrir o app (<see cref="CheckOnStartupAsync"/>) e também pelo botão
/// "Buscar nova versão" em Configurações (<see cref="CheckCommand"/>), que pede confirmação antes de instalar.
/// </para>
/// </summary>
public sealed class AppUpdateViewModel : ViewModelBase, IDisposable
{
    private readonly NotificationService _notifications;
    private readonly Func<bool> _isPackageWorkRunning;
    private readonly IUserConfirmation _confirmation;
    private readonly AppUpdatePreferencesStore _preferences;
    private readonly CancellationTokenSource _cancellation = new();
    private bool _isChecking;
    private bool _checkOnStartup;
    private string _statusMessage;

    /// <param name="notifications">Faixa de avisos no topo da janela.</param>
    /// <param name="isPackageWorkRunning">
    /// Indica se há atualização ou instalação de pacotes em andamento. Nesse caso o app não se
    /// atualiza, porque o instalador encerraria o processo no meio da operação.
    /// </param>
    public AppUpdateViewModel(
        NotificationService notifications,
        Func<bool> isPackageWorkRunning,
        IUserConfirmation? confirmation = null,
        AppUpdatePreferencesStore? preferences = null)
    {
        _notifications = notifications;
        _isPackageWorkRunning = isPackageWorkRunning;
        _confirmation = confirmation ?? new MessageBoxUserConfirmation();
        _preferences = preferences ?? new AppUpdatePreferencesStore();
        _checkOnStartup = _preferences.LoadCheckOnStartup();
        _statusMessage = IsReleaseBuild
            ? "Clique em Buscar nova versão para verificar agora."
            : "Compilação local: a busca de novas versões fica desativada.";
        CheckCommand = new AsyncDelegateCommand(() => CheckAsync(isManual: true), () => IsReleaseBuild && !IsChecking);
    }

    /// <summary>Pedido para encerrar o app porque o instalador da nova versão já foi iniciado.</summary>
    public event EventHandler? ShutdownRequested;

    /// <summary>Texto da versão atual, exibido na barra de status e em Configurações.</summary>
    public string CurrentVersionText { get; } = IsReleaseBuild
        ? $"Versão {UpdateCheckService.Format(UpdateCheckService.CurrentVersion)}"
        : "Versão de desenvolvimento";

    /// <summary>Falso em compilações locais (versão 0.0.0.0), que não consultam o GitHub.</summary>
    private static bool IsReleaseBuild => UpdateCheckService.IsReleaseBuild(UpdateCheckService.CurrentVersion);

    public AsyncDelegateCommand CheckCommand { get; }

    public bool IsChecking
    {
        get => _isChecking;
        private set
        {
            if (SetProperty(ref _isChecking, value))
            {
                CheckCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>Resultado da última verificação, em texto para o usuário.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>Caixa "Procurar nova versão ao abrir o aplicativo". Salva na hora.</summary>
    public bool CheckOnStartup
    {
        get => _checkOnStartup;
        set
        {
            if (SetProperty(ref _checkOnStartup, value))
            {
                _preferences.SaveCheckOnStartup(value);
            }
        }
    }

    /// <summary>
    /// Verificação automática ao abrir o app: instala sem perguntar, como nas versões anteriores.
    /// Não faz nada se o usuário desligou a opção em Configurações.
    /// </summary>
    public Task CheckOnStartupAsync()
    {
        if (!CheckOnStartup)
        {
            if (IsReleaseBuild)
            {
                StatusMessage = "Busca automática de nova versão desligada.";
            }

            return Task.CompletedTask;
        }

        return CheckAsync(isManual: false);
    }

    public void Dispose()
    {
        _cancellation.Cancel();
        _cancellation.Dispose();
    }

    private async Task CheckAsync(bool isManual)
    {
        if (!IsReleaseBuild || IsChecking)
        {
            return;
        }

        var cancellationToken = _cancellation.Token;
        IsChecking = true;
        StatusMessage = "Procurando uma nova versão…";
        try
        {
            var result = await new UpdateCheckService().CheckDetailedAsync(UpdateCheckService.CurrentVersion, cancellationToken);
            switch (result.Outcome)
            {
                case UpdateCheckOutcome.UpdateAvailable when result.Release is { } release:
                    await HandleAvailableReleaseAsync(release, isManual, cancellationToken);
                    break;
                case UpdateCheckOutcome.UpToDate:
                    StatusMessage = $"Você já usa a versão mais recente (verificado às {DateTime.Now:HH:mm}).";
                    break;
                default:
                    StatusMessage = "Não foi possível consultar o GitHub. Verifique a conexão e tente de novo.";
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            // A janela foi fechada durante a verificação.
        }
        finally
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                IsChecking = false;
            }
        }
    }

    private async Task HandleAvailableReleaseAsync(AvailableRelease release, bool isManual, CancellationToken cancellationToken)
    {
        var newVersion = UpdateCheckService.Format(release.Version);

        // Cópia portátil ou instalação antiga: o app não consegue se substituir; só avisa.
        if (!AppSelfUpdateService.IsRunningInstalledCopy())
        {
            StatusMessage = $"A versão {newVersion} está disponível. Baixe-a pela página da release.";
            PublishReleaseNotification(release, isManual);
            return;
        }

        if (_isPackageWorkRunning())
        {
            StatusMessage = $"A versão {newVersion} está disponível. Termine as atualizações em andamento e busque de novo.";
            PublishReleaseNotification(release, isManual);
            return;
        }

        if (isManual && !_confirmation.Confirm(
                "Nova versão disponível",
                $"A versão {newVersion} do App Updater está disponível.\n\n"
                + "Deseja instalar agora? O aplicativo será fechado e aberto de novo ao terminar."))
        {
            StatusMessage = $"A versão {newVersion} está disponível. A instalação foi adiada.";
            return;
        }

        StatusMessage = $"Baixando a versão {newVersion}…";
        var installerPath = await new AppSelfUpdateService().DownloadInstallerAsync(release, cancellationToken);

        // Revalida: uma atualização de pacotes pode ter começado durante o download.
        if (installerPath is null || _isPackageWorkRunning() || !AppSelfUpdateService.TryStartInstaller(installerPath))
        {
            StatusMessage = $"Não foi possível baixar ou validar o instalador da versão {newVersion}.";
            PublishReleaseNotification(release, isManual);
            return;
        }

        StatusMessage = $"Instalando a versão {newVersion}…";
        ShutdownRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Mostra o aviso com o link da release. Na verificação automática não substitui um aviso
    /// já visível (por exemplo, o resultado de uma atualização de pacotes).
    /// </summary>
    private void PublishReleaseNotification(AvailableRelease release, bool isManual)
    {
        if (!isManual && _notifications.HasNotification)
        {
            return;
        }

        _notifications.Publish(new AppNotification(
            NotificationKind.Informacao,
            "Nova versão disponível",
            $"A versão {UpdateCheckService.Format(release.Version)} do App Updater foi publicada. "
            + $"Você usa a {UpdateCheckService.Format(UpdateCheckService.CurrentVersion)}.",
            "Abrir página da versão",
            release.PageUrl));
    }
}
