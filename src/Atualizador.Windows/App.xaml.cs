using System.Windows;
using Atualizador.Windows.Services;

namespace Atualizador.Windows;

/// <summary>
/// Ponto de entrada do aplicativo. Há dois modos de execução:
/// <list type="bullet">
/// <item>sem argumentos: abre a janela principal;</item>
/// <item><c>--scheduled</c>: chamado pela tarefa agendada; atualiza sem janela e encerra com um código de saída.</item>
/// </list>
/// Qualquer outro argumento é recusado (código 2), para que ninguém use o app para executar comandos arbitrários.
/// </summary>
public partial class App : System.Windows.Application
{
    private const string ScheduledArgument = "--scheduled";

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (e.Args.Length == 0)
        {
            new Views.MainWindow().Show();
            return;
        }

        if (e.Args.Length == 1 && e.Args[0].Equals(ScheduledArgument, StringComparison.OrdinalIgnoreCase))
        {
            var exitCode = await new ScheduledRunService().RunAsync();
            Shutdown(exitCode);
            return;
        }

        Shutdown(2);
    }
}
