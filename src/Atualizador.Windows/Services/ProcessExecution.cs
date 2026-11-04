using System.ComponentModel;
using System.Diagnostics;

namespace Atualizador.Windows.Services;

/// <summary>Pedido de execução: programa, argumentos (um por item, sem shell) e tempo limite.</summary>
public sealed record ProcessExecutionRequest(
    string FileName,
    IReadOnlyList<string> Arguments,
    TimeSpan Timeout);

/// <summary>
/// Resultado de uma execução. <see cref="Started"/> = false quando o programa nem abriu;
/// <see cref="TimedOut"/> e <see cref="Canceled"/> indicam que o processo foi encerrado à força.
/// </summary>
public sealed record ProcessExecutionResult(
    bool Started,
    int ExitCode,
    string StandardOutput,
    string StandardError,
    bool TimedOut,
    bool Canceled,
    string? ErrorMessage)
{
    public static ProcessExecutionResult NotStarted(string errorMessage) =>
        new(false, -1, string.Empty, string.Empty, false, false, errorMessage);
}

/// <summary>
/// Abstração da execução de processos. Permite que os testes usem um executor falso e
/// nunca rodem o WinGet ou o Agendador de Tarefas de verdade.
/// </summary>
public interface IProcessExecutor
{
    Task<ProcessExecutionResult> ExecuteAsync(
        ProcessExecutionRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Executor real. Sempre usa <c>UseShellExecute = false</c> e <c>ArgumentList</c>, então nenhum
/// argumento passa pelo <c>cmd.exe</c> e não há como injetar comandos. A janela do console fica oculta.
/// </summary>
public sealed class SystemProcessExecutor : IProcessExecutor
{
    public async Task<ProcessExecutionResult> ExecuteAsync(
        ProcessExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        using var process = new Process
        {
            StartInfo = CreateStartInfo(request),
            EnableRaisingEvents = true
        };

        try
        {
            if (!process.Start())
            {
                return ProcessExecutionResult.NotStarted("O processo não pôde ser iniciado.");
            }
        }
        catch (Win32Exception exception)
        {
            return ProcessExecutionResult.NotStarted(exception.Message);
        }

        // A saída é lida em paralelo com a espera: se o buffer encher sem ninguém ler, o processo trava.
        var outputTask = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        var errorTask = process.StandardError.ReadToEndAsync(CancellationToken.None);
        var waitTask = process.WaitForExitAsync(CancellationToken.None);

        // O Delay termina por tempo limite ou por cancelamento do usuário; o que vier primeiro.
        var timeoutTask = Task.Delay(request.Timeout, cancellationToken);
        var completedTask = await Task.WhenAny(waitTask, timeoutTask);

        if (completedTask != waitTask)
        {
            var canceled = cancellationToken.IsCancellationRequested;
            TryKill(process);
            await Task.WhenAll(outputTask, errorTask);
            return new(
                true,
                -1,
                outputTask.Result,
                errorTask.Result,
                TimedOut: !canceled,
                Canceled: canceled,
                canceled ? "A execução foi cancelada." : "A execução excedeu o tempo limite.");
        }

        await waitTask;
        return new(true, process.ExitCode, await outputTask, await errorTask, false, false, null);
    }

    private static ProcessStartInfo CreateStartInfo(ProcessExecutionRequest request)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = request.FileName,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (var argument in request.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    /// <summary>Encerra o processo e os filhos (o WinGet abre o instalador como processo filho).</summary>
    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // O processo terminou entre a verificação e o Kill.
        }
    }
}
