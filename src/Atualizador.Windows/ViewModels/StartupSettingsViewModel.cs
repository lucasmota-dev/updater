using Atualizador.Windows.Services;

namespace Atualizador.Windows.ViewModels;

/// <summary>
/// Seção "Inicialização" de Configurações. A preferência só é salva depois que o Agendador de
/// Tarefas confirmar a criação ou a remoção da tarefa, para a tela não mostrar um estado falso.
/// </summary>
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

    /// <summary>
    /// Recria a tarefa de inicialização, se estiver ligada, para ela apontar para o executável atual
    /// e usar a ação mais recente (versões anteriores abriam a janela; hoje ela abre só na bandeja).
    /// Falhas são ignoradas: a tarefa antiga continua funcionando.
    /// </summary>
    public async Task RefreshTaskAsync()
    {
        if (!IsEnabled)
        {
            return;
        }

        try
        {
            await _startupTaskService.SetEnabledAsync(true);
        }
        catch (InvalidOperationException)
        {
            // Caminho do executável inválido; o usuário verá o erro se aplicar a opção manualmente.
        }
    }

    private async Task ApplyAsync()
    {
        ScheduledTaskResult result;
        try
        {
            result = await _startupTaskService.SetEnabledAsync(IsEnabled);
        }
        catch (InvalidOperationException exception)
        {
            StatusMessage = exception.Message;
            return;
        }

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
