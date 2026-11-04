using Atualizador.Windows.Services;

namespace Atualizador.Windows.ViewModels;

/// <summary>Seção "Área de notificação" de Configurações: fechar a janela esconde o app na bandeja.</summary>
public sealed class TraySettingsViewModel : ViewModelBase
{
    private readonly TrayPreferencesStore _store;
    private bool _minimizeToTray;
    private string _statusMessage = string.Empty;

    public TraySettingsViewModel(TrayPreferencesStore store)
    {
        _store = store;
        _minimizeToTray = store.Load();
        ApplyCommand = new DelegateCommand(Apply);
    }

    /// <summary>Valor da caixa de seleção. Lido pelo <see cref="TrayIconService"/> ao fechar a janela.</summary>
    public bool MinimizeToTray
    {
        get => _minimizeToTray;
        set => SetProperty(ref _minimizeToTray, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public DelegateCommand ApplyCommand { get; }

    private void Apply()
    {
        _store.Save(MinimizeToTray);
        StatusMessage = MinimizeToTray
            ? "A janela será minimizada para a bandeja ao fechar. Use Sair no menu da bandeja para encerrar."
            : "A janela será fechada normalmente.";
    }
}
