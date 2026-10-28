using System.Diagnostics;
using Atualizador.Windows.Models;
using Atualizador.Windows.Services;

namespace Atualizador.Windows.ViewModels;

public abstract class PageViewModel(string title, string description, string glyph) : ViewModelBase
{
    public string Title { get; } = title;

    public string Description { get; } = description;

    public string Glyph { get; } = glyph;
}

public sealed class PlaceholderPageViewModel(
    string title,
    string description,
    string glyph,
    string statusMessage) : PageViewModel(title, description, glyph)
{
    public string StatusMessage { get; } = statusMessage;
}

public sealed class SettingsPageViewModel : PageViewModel
{
    public SettingsPageViewModel(
        AppearanceService appearanceService,
        ScheduleSettingsViewModel schedule,
        StartupSettingsViewModel startup,
        TraySettingsViewModel tray,
        ExcludedPackagesSettingsViewModel excludedPackages)
        : base("Configurações", "Escolha a aparência, agendamento e gerencie aplicativos excluídos.", "⚙")
    {
        Appearance = new AppearanceSettingsViewModel(appearanceService);
        Schedule = schedule;
        Startup = startup;
        Tray = tray;
        ExcludedPackages = excludedPackages;
    }

    public AppearanceSettingsViewModel Appearance { get; }

    public ScheduleSettingsViewModel Schedule { get; }

    public StartupSettingsViewModel Startup { get; }

    public TraySettingsViewModel Tray { get; }

    public ExcludedPackagesSettingsViewModel ExcludedPackages { get; }
}

public sealed class AppearanceSettingsViewModel : ViewModelBase
{
    private static readonly AccentOption SystemAccent = new("Cor do sistema", "SYSTEM", false, true);
    private static readonly AccentOption CustomAccent = new("Personalizada", "", true, false);
    private readonly AppearanceService _appearanceService;
    private AppearanceMode _selectedMode;
    private AccentOption _selectedAccent;
    private string _customAccentHex;
    private string _validationMessage = string.Empty;

    public AppearanceSettingsViewModel(AppearanceService appearanceService)
    {
        _appearanceService = appearanceService;
        _selectedMode = appearanceService.Preferences.Mode;
        _customAccentHex = appearanceService.Preferences.AccentHex;
        _selectedAccent = AccentOptions.FirstOrDefault(option =>
            option.Hex.Equals(_customAccentHex, StringComparison.OrdinalIgnoreCase)) ?? (appearanceService.Preferences.AccentHex.Equals("SYSTEM", StringComparison.OrdinalIgnoreCase) ? SystemAccent : CustomAccent);
        ApplyCustomAccentCommand = new DelegateCommand(ApplyCustomAccent);
        RestoreDefaultsCommand = new DelegateCommand(RestoreDefaults);
    }

    public IReadOnlyList<AppearanceMode> AvailableModes { get; } = Enum.GetValues<AppearanceMode>();

    public IReadOnlyList<AccentOption> AccentOptions { get; } =
    [
        SystemAccent,
        new("Azul", "#0067C0"),
        new("Roxo", "#744DA9"),
        new("Verde", "#107C10"),
        new("Laranja", "#CA5010"),
        CustomAccent
    ];

    public AppearanceMode SelectedMode
    {
        get => _selectedMode;
        set
        {
            if (SetProperty(ref _selectedMode, value))
            {
                _appearanceService.SetMode(value);
            }
        }
    }

    public AccentOption SelectedAccent
    {
        get => _selectedAccent;
        set
        {
            if (!SetProperty(ref _selectedAccent, value))
            {
                return;
            }

            if (value.IsSystem)
            {
                CustomAccentHex = string.Empty;
                ClearValidationMessage();
                _appearanceService.SetSystemAccent();
            }
            else if (!value.IsCustom)
            {
                CustomAccentHex = value.Hex;
                ClearValidationMessage();
                _appearanceService.TrySetAccent(value.Hex, out _);
            }
        }
    }

    public string CustomAccentHex
    {
        get => _customAccentHex;
        set => SetProperty(ref _customAccentHex, value);
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public DelegateCommand ApplyCustomAccentCommand { get; }

    public DelegateCommand RestoreDefaultsCommand { get; }

    private void ApplyCustomAccent()
    {
        if (!_appearanceService.TrySetAccent(CustomAccentHex, out var errorMessage))
        {
            ValidationMessage = errorMessage;
            return;
        }

        _selectedAccent = CustomAccent;
        OnPropertyChanged(nameof(SelectedAccent));
        CustomAccentHex = _appearanceService.Preferences.AccentHex;
        ClearValidationMessage();
    }

    private void RestoreDefaults()
    {
        _appearanceService.RestoreDefaults();
        _selectedMode = _appearanceService.Preferences.Mode;
        _customAccentHex = _appearanceService.Preferences.AccentHex;
        _selectedAccent = AccentOptions[0];
        ClearValidationMessage();
        OnPropertyChanged(nameof(SelectedMode));
        OnPropertyChanged(nameof(CustomAccentHex));
        OnPropertyChanged(nameof(SelectedAccent));
    }

    private void ClearValidationMessage() => ValidationMessage = string.Empty;
}

public sealed class MainWindowViewModel : ViewModelBase, IDisposable
{
    private PageViewModel _selectedPage;
    private readonly UpdatesPageViewModel _updatesPage;
    private readonly InstallPageViewModel _installPage;
    private readonly NotificationService _notificationService;
    private readonly TraySettingsViewModel _traySettings;
    private readonly CancellationTokenSource _updateCheckCancellation = new();

    public MainWindowViewModel()
    {
        var appearanceService = new AppearanceService();
        var historyStore = new HistoryStore();
        var wingetService = new WingetService();
        var excludedPackagesStore = new ExcludedPackagesStore();
        _notificationService = new NotificationService();
        DismissNotificationCommand = new DelegateCommand(_notificationService.Clear);
        _updatesPage = new UpdatesPageViewModel(
            wingetService,
            historyStore,
            excludedPackagesStore,
            notificationService: _notificationService);
        _installPage = new InstallPageViewModel(
            wingetService,
            historyStore,
            notificationService: _notificationService);
        var scheduleStore = new ScheduleStore();
        var schedule = new ScheduleSettingsViewModel(
            scheduleStore,
            new TaskSchedulerService(),
            () => _updatesPage.SelectedPackages);
        var startup = new StartupSettingsViewModel(new StartupTaskService(), new StartupPreferencesStore());
        _traySettings = new TraySettingsViewModel(new TrayPreferencesStore());
        var excludedSettings = new ExcludedPackagesSettingsViewModel(excludedPackagesStore);
        Pages =
        [
            _updatesPage,
            _installPage,
            new HistoryPageViewModel(historyStore),
            new SettingsPageViewModel(appearanceService, schedule, startup, _traySettings, excludedSettings)
        ];
        _selectedPage = Pages[0];
        OpenNotificationLinkCommand = new DelegateCommand(OpenNotificationLink);
        _ = CheckForAppUpdateAsync(_updateCheckCancellation.Token);
    }

    public IReadOnlyList<PageViewModel> Pages { get; }

    public PageViewModel SelectedPage
    {
        get => _selectedPage;
        set => SetProperty(ref _selectedPage, value);
    }

    public NotificationService Notifications => _notificationService;

    public UpdatesPageViewModel UpdatesPage => _updatesPage;

    public InstallPageViewModel InstallPage => _installPage;

    public TraySettingsViewModel TraySettings => _traySettings;

    public DelegateCommand DismissNotificationCommand { get; }

    public DelegateCommand OpenNotificationLinkCommand { get; }

    public void Dispose()
    {
        _updateCheckCancellation.Cancel();
        _updateCheckCancellation.Dispose();
        _updatesPage.Dispose();
        _installPage.Dispose();
    }

    private async Task CheckForAppUpdateAsync(CancellationToken cancellationToken)
    {
        var release = await new UpdateCheckService().CheckAsync(UpdateCheckService.CurrentVersion, cancellationToken);
        if (release is null || cancellationToken.IsCancellationRequested || _notificationService.HasNotification)
        {
            return;
        }

        _notificationService.Publish(new AppNotification(
            NotificationKind.Informacao,
            "Nova versão disponível",
            $"A versão {UpdateCheckService.Format(release.Version)} do Atualizador foi publicada. "
            + $"Você usa a {UpdateCheckService.Format(UpdateCheckService.CurrentVersion)}.",
            "Abrir página da versão",
            release.PageUrl));
    }

    private void OpenNotificationLink()
    {
        if (_notificationService.Current?.ActionUri is { } uri)
        {
            // A URL vem validada (https, github.com) pelo UpdateCheckService; o shell só abre o navegador.
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }
    }
}
