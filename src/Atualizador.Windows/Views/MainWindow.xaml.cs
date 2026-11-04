using System.Windows;
using Atualizador.Windows.Services;
using Atualizador.Windows.ViewModels;

namespace Atualizador.Windows.Views;

public partial class MainWindow : Window, IDisposable
{
    private readonly TrayIconService _trayIconService;

    public MainWindow()
    {
        InitializeComponent();
        var viewModel = new MainWindowViewModel();
        DataContext = viewModel;
        // O instalador da nova versão substitui o executável; Shutdown ignora o "minimizar para a bandeja".
        viewModel.ShutdownRequested += (_, _) => System.Windows.Application.Current.Shutdown();
        _trayIconService = new TrayIconService(
            this,
            viewModel.UpdatesPage,
            viewModel.Notifications,
            () => viewModel.TraySettings.MinimizeToTray);
    }

    protected override void OnClosed(EventArgs e)
    {
        Dispose();
        base.OnClosed(e);
    }

    public void Dispose()
    {
        (DataContext as IDisposable)?.Dispose();
        _trayIconService.Dispose();
        GC.SuppressFinalize(this);
    }
}
