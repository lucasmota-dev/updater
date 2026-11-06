using System.Diagnostics;
using Atualizador.Windows.Services;

namespace Atualizador.Windows.ViewModels;

/// <summary>
/// ViewModel da janela principal. Monta os serviços e as páginas (composição manual, sem contêiner
/// de injeção de dependência), controla a página selecionada, o menu lateral recolhível, a faixa
/// de avisos e a atualização do próprio aplicativo.
/// </summary>
public sealed class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly NotificationService _notifications = new();
    private readonly NavigationPreferencesStore _navigationPreferences = new();
    private readonly PackageOperationGate _operationGate = new();
    private PageViewModel _selectedPage;
    private bool _isNavigationCollapsed;

    public MainWindowViewModel()
    {
        var historyStore = new HistoryStore();
        var wingetService = new WingetService();
        var excludedPackagesStore = new ExcludedPackagesStore();
        var iconService = new PackageIconService(wingetService);

        // Trava compartilhada: a fila de instalação e as atualizações nunca rodam o instalador ao mesmo tempo.
        var operationGate = _operationGate;
        UpdatesPage = new UpdatesPageViewModel(wingetService, historyStore, excludedPackagesStore, notificationService: _notifications, operationGate: operationGate, iconService: iconService);
        InstallPage = new InstallPageViewModel(wingetService, historyStore, notificationService: _notifications, operationGate: operationGate, iconService: iconService);
        TraySettings = new TraySettingsViewModel(new TrayPreferencesStore());
        WindowsUpdatePage = new WindowsUpdatePageViewModel(historyStore, notifications: _notifications);
        HistoryPage = new HistoryPageViewModel(historyStore);

        // O app não se atualiza enquanto alguma instalação estiver em andamento.
        AppUpdate = new AppUpdateViewModel(
            _notifications,
            () => UpdatesPage.IsUpdating || InstallPage.IsInstalling || WindowsUpdatePage.IsInstalling);

        var schedule = new ScheduleSettingsViewModel(
            new ScheduleStore(),
            new TaskSchedulerService(),
            () => UpdatesPage.SelectedPackages);
        var startup = new StartupSettingsViewModel(new StartupTaskService(), new StartupPreferencesStore());
        var excludedSettings = new ExcludedPackagesSettingsViewModel(excludedPackagesStore);

        Pages =
        [
            UpdatesPage,
            InstallPage,
            WindowsUpdatePage,
            HistoryPage,
            new SettingsPageViewModel(new AppearanceService(), schedule, startup, TraySettings, excludedSettings),
            new AboutPageViewModel(AppUpdate)
        ];
        _selectedPage = Pages[0];
        _isNavigationCollapsed = _navigationPreferences.LoadIsCollapsed();

        ToggleNavigationCommand = new DelegateCommand(() => IsNavigationCollapsed = !IsNavigationCollapsed);
        DismissNotificationCommand = new DelegateCommand(_notifications.Clear);
        OpenNotificationLinkCommand = new DelegateCommand(OpenNotificationLink);

        // Tarefas em segundo plano, sem bloquear a abertura da janela:
        // atualiza a tarefa "Iniciar com o Windows" criada por versões anteriores, procura nova versão
        // do app e já busca as atualizações de aplicativos (a página Atualizações é a primeira).
        if (ElevationGuard.IsElevated)
        {
            // Só acontece se não foi possível reabrir sem elevação (ex.: UAC desligado).
            _notifications.Publish(new Models.AppNotification(
                Models.NotificationKind.Erro,
                "O App Updater está rodando como administrador",
                "Alguns instaladores (como o do Spotify) recusam instalar assim. " + ElevationGuard.ElevationAdvice));
        }

        _ = startup.RefreshTaskAsync();
        _ = AppUpdate.CheckOnStartupAsync();
        OnPageOpened(_selectedPage);
    }

    /// <summary>Páginas do menu lateral, na ordem de exibição.</summary>
    public IReadOnlyList<PageViewModel> Pages { get; }

    public PageViewModel SelectedPage
    {
        get => _selectedPage;
        set
        {
            if (SetProperty(ref _selectedPage, value))
            {
                OnPageOpened(value);
            }
        }
    }

    /// <summary>Menu lateral recolhido (só ícones). A escolha é lembrada entre sessões.</summary>
    public bool IsNavigationCollapsed
    {
        get => _isNavigationCollapsed;
        set
        {
            if (SetProperty(ref _isNavigationCollapsed, value))
            {
                _navigationPreferences.SaveIsCollapsed(value);
            }
        }
    }

    public NotificationService Notifications => _notifications;

    public UpdatesPageViewModel UpdatesPage { get; }

    public InstallPageViewModel InstallPage { get; }

    public WindowsUpdatePageViewModel WindowsUpdatePage { get; }

    public HistoryPageViewModel HistoryPage { get; }

    public TraySettingsViewModel TraySettings { get; }

    /// <summary>Versão atual e atualização do próprio app (barra de status e Configurações).</summary>
    public AppUpdateViewModel AppUpdate { get; }

    public DelegateCommand ToggleNavigationCommand { get; }

    public DelegateCommand DismissNotificationCommand { get; }

    public DelegateCommand OpenNotificationLinkCommand { get; }

    public void Dispose()
    {
        AppUpdate.Dispose();
        UpdatesPage.Dispose();
        InstallPage.Dispose();
        WindowsUpdatePage.Dispose();
        _operationGate.Dispose();
    }

    /// <summary>
    /// Ações automáticas ao abrir uma página: Atualizações busca de novo se a lista estiver antiga,
    /// Windows Update procura na primeira visita e Histórico recarrega os registros novos.
    /// </summary>
    private static void OnPageOpened(PageViewModel page)
    {
        switch (page)
        {
            case UpdatesPageViewModel updates:
                _ = updates.RefreshIfStaleAsync();
                break;
            case WindowsUpdatePageViewModel windowsUpdate:
                _ = windowsUpdate.SearchOnFirstOpenAsync();
                break;
            case HistoryPageViewModel history:
                history.RefreshCommand.Execute(null);
                break;
        }
    }

    private void OpenNotificationLink()
    {
        if (_notifications.Current?.ActionUri is { } uri)
        {
            // A URL vem validada (https, github.com) pelo UpdateCheckService; o shell só abre o navegador.
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }
    }
}
