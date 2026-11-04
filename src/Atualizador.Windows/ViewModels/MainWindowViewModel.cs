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
    private PageViewModel _selectedPage;
    private bool _isNavigationCollapsed;

    public MainWindowViewModel()
    {
        var historyStore = new HistoryStore();
        var wingetService = new WingetService();
        var excludedPackagesStore = new ExcludedPackagesStore();

        UpdatesPage = new UpdatesPageViewModel(wingetService, historyStore, excludedPackagesStore, notificationService: _notifications);
        InstallPage = new InstallPageViewModel(wingetService, historyStore, notificationService: _notifications);
        TraySettings = new TraySettingsViewModel(new TrayPreferencesStore());
        AppUpdate = new AppUpdateViewModel(_notifications, () => UpdatesPage.IsUpdating || InstallPage.IsInstalling);

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
            new HistoryPageViewModel(historyStore),
            new SettingsPageViewModel(new AppearanceService(), schedule, startup, TraySettings, excludedSettings, AppUpdate)
        ];
        _selectedPage = Pages[0];
        _isNavigationCollapsed = _navigationPreferences.LoadIsCollapsed();

        ToggleNavigationCommand = new DelegateCommand(() => IsNavigationCollapsed = !IsNavigationCollapsed);
        DismissNotificationCommand = new DelegateCommand(_notifications.Clear);
        OpenNotificationLinkCommand = new DelegateCommand(OpenNotificationLink);

        // Tarefas em segundo plano, sem bloquear a abertura da janela:
        // atualiza a tarefa "Iniciar com o Windows" criada por versões anteriores e procura nova versão do app.
        _ = startup.RefreshTaskAsync();
        _ = AppUpdate.CheckOnStartupAsync();
    }

    /// <summary>Páginas do menu lateral, na ordem de exibição.</summary>
    public IReadOnlyList<PageViewModel> Pages { get; }

    public PageViewModel SelectedPage
    {
        get => _selectedPage;
        set => SetProperty(ref _selectedPage, value);
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
