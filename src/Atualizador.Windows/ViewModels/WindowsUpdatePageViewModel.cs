using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using Atualizador.Windows.Models;
using Atualizador.Windows.Services;

namespace Atualizador.Windows.ViewModels;

/// <summary>
/// Página Windows Update: procura as atualizações do sistema (sem precisar de administrador) e
/// instala todas por meio de um processo elevado (veja <see cref="WindowsUpdateInstallLauncher"/>).
/// Ao terminar, oferece reiniciar se o Windows pedir. A instalação vira um registro no histórico.
/// </summary>
public sealed class WindowsUpdatePageViewModel : PageViewModel, IDisposable
{
    private readonly HistoryStore _historyStore;
    private readonly IUserConfirmation _confirmation;
    private readonly NotificationService? _notifications;
    private readonly CancellationTokenSource _cancellation = new();
    private bool _isSearching;
    private bool _isInstalling;
    private bool _isError;
    private bool _isRebootRequired;
    private bool _hasSearched;
    private double _progress;
    private string _statusMessage = "Clique em Procurar atualizações para consultar o Windows Update.";
    private string _progressDetail = string.Empty;

    public WindowsUpdatePageViewModel(
        HistoryStore historyStore,
        IUserConfirmation? confirmation = null,
        NotificationService? notifications = null)
        : base("Windows Update", "Procure e instale as atualizações do próprio Windows.", "")
    {
        _historyStore = historyStore;
        _confirmation = confirmation ?? new MessageBoxUserConfirmation();
        _notifications = notifications;
        SearchCommand = new AsyncDelegateCommand(SearchAsync, () => !IsBusy);
        InstallCommand = new AsyncDelegateCommand(InstallAsync, () => !IsBusy && HasUpdates);
        RestartCommand = new DelegateCommand(Restart, () => IsRebootRequired && !IsInstalling);
        OpenWindowsSettingsCommand = new DelegateCommand(OpenWindowsSettings);
    }

    public ObservableCollection<WindowsUpdateInfo> Updates { get; } = [];

    public AsyncDelegateCommand SearchCommand { get; }

    public AsyncDelegateCommand InstallCommand { get; }

    public DelegateCommand RestartCommand { get; }

    /// <summary>Abre "Configurações > Windows Update" do próprio Windows, como alternativa.</summary>
    public DelegateCommand OpenWindowsSettingsCommand { get; }

    public bool IsSearching
    {
        get => _isSearching;
        private set => SetBusyFlag(ref _isSearching, value);
    }

    /// <summary>Instalação em andamento. Também impede o app de se atualizar no meio dela.</summary>
    public bool IsInstalling
    {
        get => _isInstalling;
        private set => SetBusyFlag(ref _isInstalling, value);
    }

    public bool IsBusy => IsSearching || IsInstalling;

    public bool IsError
    {
        get => _isError;
        private set => SetProperty(ref _isError, value);
    }

    /// <summary>O Windows precisa reiniciar para concluir; mostra o botão "Reiniciar agora".</summary>
    public bool IsRebootRequired
    {
        get => _isRebootRequired;
        private set
        {
            if (SetProperty(ref _isRebootRequired, value))
            {
                RestartCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool HasUpdates => Updates.Count > 0;

    /// <summary>Progresso da instalação (0 a 100); indeterminado durante a procura e o download.</summary>
    public double Progress
    {
        get => _progress;
        private set => SetProperty(ref _progress, value);
    }

    public bool IsProgressIndeterminate => IsSearching || (IsInstalling && Progress == 0);

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string ProgressDetail
    {
        get => _progressDetail;
        private set => SetProperty(ref _progressDetail, value);
    }

    public string EmptyStateMessage => IsSearching
        ? "Consultando o Windows Update… Isso pode levar alguns minutos."
        : _hasSearched
            ? "Nenhuma atualização do Windows pendente."
            : "Clique em Procurar atualizações para consultar o Windows Update.";

    /// <summary>Procura automaticamente na primeira vez que a página é aberta nesta sessão.</summary>
    public Task SearchOnFirstOpenAsync() => _hasSearched || IsBusy ? Task.CompletedTask : SearchAsync();

    public void Dispose()
    {
        _cancellation.Cancel();
        _cancellation.Dispose();
    }

    private async Task SearchAsync()
    {
        IsSearching = true;
        IsError = false;
        StatusMessage = "Consultando o Windows Update…";
        ProgressDetail = string.Empty;
        OnPropertyChanged(nameof(EmptyStateMessage));
        try
        {
            var (result, rebootPending) = await Task.Run(() => (WindowsUpdateService.Search(), WindowsUpdateService.IsRebootPending()));
            Updates.Clear();
            foreach (var update in result.Updates)
            {
                Updates.Add(update);
            }

            IsError = !result.Succeeded;
            IsRebootRequired = rebootPending;
            StatusMessage = rebootPending
                ? result.Message + " Há um reinício pendente para concluir atualizações anteriores."
                : result.Message;
        }
        finally
        {
            _hasSearched = true;
            IsSearching = false;
            OnPropertyChanged(nameof(HasUpdates));
            OnPropertyChanged(nameof(EmptyStateMessage));
            InstallCommand.RaiseCanExecuteChanged();
        }
    }

    private async Task InstallAsync()
    {
        if (!_confirmation.Confirm(
                "Instalar atualizações do Windows",
                $"Instalar {Updates.Count} atualização(ões) do Windows?\n\n"
                + "O Windows vai pedir permissão de administrador. Não desligue o computador durante a instalação; "
                + "algumas atualizações podem exigir reinício."))
        {
            return;
        }

        var startedAt = DateTimeOffset.Now;
        IsInstalling = true;
        IsError = false;
        Progress = 0;
        StatusMessage = "Aguardando a permissão de administrador…";
        try
        {
            var reporter = new Progress<WindowsUpdateProgress>(ShowProgress);
            var final = await WindowsUpdateInstallLauncher.InstallElevatedAsync(reporter, _cancellation.Token);
            ShowProgress(final);
            IsError = final.Succeeded != true;
            IsRebootRequired = final.RebootRequired || IsRebootRequired;
            AppendHistory(final, startedAt);
            _notifications?.Publish(new(
                final.Succeeded == true ? NotificationKind.Sucesso : NotificationKind.Erro,
                "Windows Update",
                final.Message));
        }
        catch (OperationCanceledException)
        {
            // A janela foi fechada; o processo elevado continua e conclui a instalação sozinho.
        }
        finally
        {
            IsInstalling = false;
            ProgressDetail = string.Empty;
        }

        if (!_cancellation.IsCancellationRequested)
        {
            await SearchAsync();
        }
    }

    private void ShowProgress(WindowsUpdateProgress message)
    {
        StatusMessage = message.Message;
        Progress = message.Total > 0 ? (double)message.Current / message.Total * 100 : 0;
        ProgressDetail = message.Stage switch
        {
            WindowsUpdateStage.Downloading => "Baixando…",
            WindowsUpdateStage.Installing => $"Instalando {message.Current} de {message.Total}",
            _ => string.Empty
        };
        OnPropertyChanged(nameof(IsProgressIndeterminate));
    }

    private void AppendHistory(WindowsUpdateProgress final, DateTimeOffset startedAt)
    {
        var results = final.Results ?? [];
        _historyStore.Append(new HistoryEntry(
            Guid.NewGuid(),
            startedAt,
            DateTimeOffset.Now,
            HistoryOperation.WindowsUpdate,
            "Windows Update",
            results.Count,
            final.Succeeded == true
                ? HistoryEntryStatus.Sucesso
                : results.Any(result => result.Succeeded) ? HistoryEntryStatus.Parcial : HistoryEntryStatus.Falha,
            final.Message,
            results.Select(result => new HistoryPackageResult(
                result.Title,
                result.KbArticle ?? string.Empty,
                "Windows Update",
                result.Succeeded ? PackageUpdateStatus.Succeeded : PackageUpdateStatus.Failed,
                result.Message)).ToArray()));
    }

    private void Restart()
    {
        if (!_confirmation.Confirm(
                "Reiniciar o computador",
                "Reiniciar agora para concluir as atualizações?\n\nSalve o seu trabalho antes: os programas abertos serão fechados."))
        {
            return;
        }

        // Argumentos fixos, sem shell: reinicia em 5 segundos.
        var startInfo = new ProcessStartInfo("shutdown.exe") { UseShellExecute = false, CreateNoWindow = true };
        foreach (var argument in new[] { "/r", "/t", "5", "/c", "App Updater: reiniciando para concluir as atualizações do Windows." })
        {
            startInfo.ArgumentList.Add(argument);
        }

        try
        {
            using var _ = Process.Start(startInfo);
        }
        catch (Win32Exception exception)
        {
            StatusMessage = $"Não foi possível reiniciar: {exception.Message}";
            IsError = true;
        }
    }

    private static void OpenWindowsSettings()
    {
        // URI fixa do próprio Windows; o shell abre a tela Configurações.
        using var _ = Process.Start(new ProcessStartInfo("ms-settings:windowsupdate") { UseShellExecute = true });
    }

    private void SetBusyFlag(ref bool field, bool value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (!SetProperty(ref field, value, propertyName))
        {
            return;
        }

        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(IsProgressIndeterminate));
        SearchCommand.RaiseCanExecuteChanged();
        InstallCommand.RaiseCanExecuteChanged();
        RestartCommand.RaiseCanExecuteChanged();
    }
}
