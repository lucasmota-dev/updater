using System.Windows;
using Atualizador.Windows.Services;
using Atualizador.Windows.ViewModels;

namespace Atualizador.Windows.Views;

/// <summary>
/// Janela principal. O code-behind só liga o ViewModel à janela e cria o ícone da área de
/// notificação; toda a lógica de tela fica em <see cref="MainWindowViewModel"/>.
/// </summary>
public partial class MainWindow : Window, IDisposable
{
    private readonly MainWindowViewModel _viewModel;
    private readonly TrayIconService _trayIconService;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainWindowViewModel();
        DataContext = _viewModel;

        // O instalador da nova versão substitui o executável. Shutdown encerra de verdade,
        // ignorando a opção "minimizar para a bandeja ao fechar".
        _viewModel.AppUpdate.ShutdownRequested += (_, _) => System.Windows.Application.Current.Shutdown();

        _trayIconService = new TrayIconService(
            this,
            _viewModel.UpdatesPage,
            _viewModel.Notifications,
            () => _viewModel.TraySettings.MinimizeToTray);
    }

    /// <summary>O handle da janela só existe a partir daqui; é quando a barra de título pode ser ajustada.</summary>
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        WindowTitleBar.Apply(this, AppearanceService.IsDarkThemeApplied);
    }

    protected override void OnClosed(EventArgs e)
    {
        Dispose();
        base.OnClosed(e);
    }

    public void Dispose()
    {
        _viewModel.Dispose();
        _trayIconService.Dispose();
        GC.SuppressFinalize(this);
    }
}
