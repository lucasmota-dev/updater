using Atualizador.Windows.Models;
using Atualizador.Windows.Services;

namespace Atualizador.Windows.ViewModels;

public sealed class ScheduleSettingsViewModel : ViewModelBase
{
    private readonly ScheduleStore _scheduleStore;
    private readonly TaskSchedulerService _taskSchedulerService;
    private readonly IUserConfirmation _confirmation;
    private readonly Func<IReadOnlyList<UpdatablePackage>> _selectedPackagesProvider;
    private ScheduleMode _mode;
    private int _intervalHours;
    private DayOfWeek _dayOfWeek;
    private ScheduleScope _scope;
    private string _statusMessage = string.Empty;

    public ScheduleSettingsViewModel(
        ScheduleStore scheduleStore,
        TaskSchedulerService taskSchedulerService,
        Func<IReadOnlyList<UpdatablePackage>> selectedPackagesProvider,
        IUserConfirmation? confirmation = null)
    {
        _scheduleStore = scheduleStore;
        _taskSchedulerService = taskSchedulerService;
        _selectedPackagesProvider = selectedPackagesProvider;
        _confirmation = confirmation ?? new MessageBoxUserConfirmation();
        var preferences = scheduleStore.Load();
        _mode = preferences.Mode;
        _intervalHours = preferences.IntervalHours;
        _dayOfWeek = preferences.DayOfWeek;
        _scope = preferences.Scope;
        SaveCommand = new AsyncDelegateCommand(SaveAsync, () => true);
        DisableCommand = new AsyncDelegateCommand(DisableAsync, () => Mode != ScheduleMode.Desligado);
        RemoveCommand = new AsyncDelegateCommand(RemoveAsync, () => true);
    }

    public IReadOnlyList<ScheduleMode> AvailableModes { get; } = Enum.GetValues<ScheduleMode>();

    public IReadOnlyList<int> AvailableIntervals { get; } = [1, 2, 4, 6, 12, 24, 48, 72, 168];

    public IReadOnlyList<DayOfWeek> AvailableDays { get; } = Enum.GetValues<DayOfWeek>();

    public IReadOnlyList<ScheduleScope> AvailableScopes { get; } = Enum.GetValues<ScheduleScope>();

    public AsyncDelegateCommand SaveCommand { get; }

    public AsyncDelegateCommand DisableCommand { get; }

    public AsyncDelegateCommand RemoveCommand { get; }

    public ScheduleMode Mode
    {
        get => _mode;
        set
        {
            if (SetProperty(ref _mode, value))
            {
                DisableCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public int IntervalHours
    {
        get => _intervalHours;
        set => SetProperty(ref _intervalHours, Math.Clamp(value, 1, 168));
    }

    public DayOfWeek DayOfWeek
    {
        get => _dayOfWeek;
        set => SetProperty(ref _dayOfWeek, value);
    }

    public ScheduleScope Scope
    {
        get => _scope;
        set => SetProperty(ref _scope, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    private async Task SaveAsync()
    {
        var selectedPackages = Scope == ScheduleScope.PacotesSelecionados
            ? _selectedPackagesProvider()
            : [];
        if (Scope == ScheduleScope.PacotesSelecionados && selectedPackages.Count == 0)
        {
            StatusMessage = "Selecione ao menos um pacote antes de agendar um escopo específico.";
            return;
        }

        var preferences = new SchedulePreferences(
            SchedulePreferences.CurrentVersion,
            Mode,
            IntervalHours,
            DayOfWeek,
            Scope,
            selectedPackages);
        var result = await _taskSchedulerService.CreateOrUpdateAsync(preferences);
        if (!result.Succeeded)
        {
            StatusMessage = result.Message;
            return;
        }

        _scheduleStore.Save(preferences);
        StatusMessage = "Agendamento salvo. Ele usa a conta atual e requer uma sessão compatível com WinGet/Store.";
    }

    private async Task DisableAsync()
    {
        var result = await _taskSchedulerService.DisableAsync();
        if (result.Succeeded)
        {
            var preferences = _scheduleStore.Load() with { Mode = ScheduleMode.Desligado };
            _scheduleStore.Save(preferences);
            Mode = ScheduleMode.Desligado;
            StatusMessage = "Agendamento desativado.";
        }
        else
        {
            StatusMessage = result.Message;
        }
    }

    private async Task RemoveAsync()
    {
        if (!_confirmation.Confirm("Remover agendamento", "Deseja remover o agendamento do Atualizador Windows?"))
        {
            return;
        }

        var result = await _taskSchedulerService.RemoveAsync();
        if (result.Succeeded)
        {
            var preferences = SchedulePreferences.Defaults;
            _scheduleStore.Save(preferences);
            Mode = ScheduleMode.Desligado;
            StatusMessage = "Agendamento removido.";
        }
        else
        {
            StatusMessage = result.Message;
        }
    }
}
