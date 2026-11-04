using System.Windows;
using Atualizador.Windows.Services;

namespace Atualizador.Windows;

/// <summary>
/// Ponto de entrada do aplicativo. Há três modos de execução:
/// <list type="bullet">
/// <item>sem argumentos: abre a janela principal;</item>
/// <item><c>--minimized</c>: usado pela inicialização com o Windows; fica só na área de notificação,
/// sem mostrar a janela nem tirar o foco do que o usuário está fazendo;</item>
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

        if (e.Args.Length == 1 && IsArgument(e.Args[0], StartupTaskService.MinimizedArgument))
        {
            // A janela é criada (com o ícone da bandeja), mas não exibida. O usuário a abre pela bandeja.
            _ = new Views.MainWindow();
            return;
        }

        if (e.Args.Length == 1 && IsArgument(e.Args[0], ScheduledArgument))
        {
            var exitCode = await new ScheduledRunService().RunAsync();
            Shutdown(exitCode);
            return;
        }

        Shutdown(2);
    }

    private static bool IsArgument(string value, string expected) =>
        value.Equals(expected, StringComparison.OrdinalIgnoreCase);
}
