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
        StartupSettingsViewModel startup)
        : base("Configurações", "Escolha a aparência e a cor de destaque do aplicativo.", "⚙")
    {
        Appearance = new AppearanceSettingsViewModel(appearanceService);
        Schedule = schedule;
        Startup = startup;
    }

    public AppearanceSettingsViewModel Appearance { get; }

    public ScheduleSettingsViewModel Schedule { get; }

    public StartupSettingsViewModel Startup { get; }
}

public sealed class AppearanceSettingsViewModel : ViewModelBase
{
    private static readonly AccentOption CustomAccent = new("Personalizada", "", true);
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
            option.Hex.Equals(_customAccentHex, StringComparison.OrdinalIgnoreCase)) ?? CustomAccent;
        ApplyCustomAccentCommand = new DelegateCommand(ApplyCustomAccent);
        RestoreDefaultsCommand = new DelegateCommand(RestoreDefaults);
    }

    public IReadOnlyList<AppearanceMode> AvailableModes { get; } = Enum.GetValues<AppearanceMode>();

    public IReadOnlyList<AccentOption> AccentOptions { get; } =
    [
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

            if (!value.IsCustom)
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
    private readonly NotificationService _notificationService;

    public MainWindowViewModel()
    {
        var appearanceService = new AppearanceService();
        var historyStore = new HistoryStore();
        _notificationService = new NotificationService();
        DismissNotificationCommand = new DelegateCommand(_notificationService.Clear);
        _updatesPage = new UpdatesPageViewModel(new WingetService(), historyStore, notificationService: _notificationService);
        var scheduleStore = new ScheduleStore();
        var schedule = new ScheduleSettingsViewModel(
            scheduleStore,
            new TaskSchedulerService(),
            () => _updatesPage.SelectedPackages);
        var startup = new StartupSettingsViewModel(new StartupTaskService(), new StartupPreferencesStore());
        Pages =
        [
            _updatesPage,
            new HistoryPageViewModel(historyStore),
            new SettingsPageViewModel(appearanceService, schedule, startup)
        ];
        _selectedPage = Pages[0];
    }

    public IReadOnlyList<PageViewModel> Pages { get; }

    public PageViewModel SelectedPage
    {
        get => _selectedPage;
        set => SetProperty(ref _selectedPage, value);
    }

    public NotificationService Notifications => _notificationService;

    public DelegateCommand DismissNotificationCommand { get; }

    public void Dispose() => _updatesPage.Dispose();
}
