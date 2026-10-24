using System.Diagnostics;
using System.ComponentModel;

namespace Atualizador.Windows.Services;

public sealed record ProcessExecutionRequest(
    string FileName,
    IReadOnlyList<string> Arguments,
    TimeSpan Timeout);

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

public interface IProcessExecutor
{
    Task<ProcessExecutionResult> ExecuteAsync(
        ProcessExecutionRequest request,
        CancellationToken cancellationToken = default);
}

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

        var outputTask = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        var errorTask = process.StandardError.ReadToEndAsync(CancellationToken.None);
        var waitTask = process.WaitForExitAsync(CancellationToken.None);
        var timeoutTask = Task.Delay(request.Timeout, cancellationToken);
        var completedTask = await Task.WhenAny(waitTask, timeoutTask);

        if (completedTask != waitTask)
        {
            var canceled = cancellationToken.IsCancellationRequested;
            TryKill(process);
            await CompleteReadTasksAsync(outputTask, errorTask);
            return new(
                true,
                -1,
                outputTask.Result,
                errorTask.Result,
                !canceled,
                canceled,
                canceled ? "A execução foi cancelada." : "A execução excedeu o tempo limite.");
        }

        await waitTask;
        return new(
            true,
            process.ExitCode,
            await outputTask,
            await errorTask,
            false,
            false,
            null);
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

    private static async Task CompleteReadTasksAsync(Task<string> outputTask, Task<string> errorTask)
    {
        await Task.WhenAll(outputTask, errorTask);
    }

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
        }
    }
}
