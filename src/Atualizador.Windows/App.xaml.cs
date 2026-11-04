using System.Windows;
using Atualizador.Windows.Services;

namespace Atualizador.Windows;

public partial class App : System.Windows.Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Length > 0
            && (e.Args.Length != 1
                || !e.Args[0].Equals("--scheduled", StringComparison.OrdinalIgnoreCase)))
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

        new Views.MainWindow().Show();
    }
}
