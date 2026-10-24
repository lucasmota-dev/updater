using Atualizador.Windows.Services;

namespace Atualizador.Windows.ViewModels;

public sealed class StartupSettingsViewModel : ViewModelBase
{
    private readonly StartupTaskService _startupTaskService;
    private readonly StartupPreferencesStore _preferencesStore;
    private bool _isEnabled;
    private string _statusMessage = string.Empty;

    public StartupSettingsViewModel(
        StartupTaskService startupTaskService,
        StartupPreferencesStore preferencesStore)
    {
        _startupTaskService = startupTaskService;
        _preferencesStore = preferencesStore;
        _isEnabled = preferencesStore.Load();
        ApplyCommand = new AsyncDelegateCommand(ApplyAsync);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public AsyncDelegateCommand ApplyCommand { get; }

    private async Task ApplyAsync()
    {
        var result = await _startupTaskService.SetEnabledAsync(IsEnabled);
        if (!result.Succeeded)
        {
            StatusMessage = result.Message;
            return;
        }

        _preferencesStore.Save(IsEnabled);
        StatusMessage = IsEnabled
            ? "Inicialização com o Windows ativada."
            : "Inicialização com o Windows desativada.";
    }
}
