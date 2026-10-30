using System.Windows;
using Atualizador.Windows.Services;
using Atualizador.Windows.ViewModels;
using Atualizador.Windows.Views;

namespace Atualizador.Windows;

public partial class App : System.Windows.Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        // Argumentos de execução programada permanecem como antes
        if (e.Args.Length > 0 && (e.Args.Length != 1 || !e.Args[0].Equals("--scheduled", StringComparison.OrdinalIgnoreCase)))
        {
            Shutdown(2);
            return;
        }

        if (e.Args.Length == 1)
        {
            var exitCode = await new ScheduledRunService().RunAsync();
            Shutdown(exitCode);
            return;
        }

        // ---- Auto‑update ---------------------------------------------------
        var updateVm = new UpdateCheckViewModel();
        var splash = new UpdateCheckWindow { DataContext = updateVm };
        splash.Show();
        // Quando a checagem terminar (sem atualização ou após update), a janela será fechada
        splash.Closed += (_, __) =>
        {
            // Se a aplicação ainda está viva, abre a janela principal
            if (Current?.MainWindow == null)
            {
                var main = new MainWindow();
                MainWindow = main;
                main.Show();
            }
        };
        // O resto da lógica de inicialização será controlado pelo ViewModel;
        // não precisamos fazer mais nada aqui.
    }
}
