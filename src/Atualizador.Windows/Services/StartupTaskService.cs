using System.IO;

namespace Atualizador.Windows.Services;

public sealed class StartupTaskService
{
    public const string TaskName = "\\Atualizador.Windows.Startup.v1";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
    private readonly IProcessExecutor _processExecutor;
    private readonly string _applicationPath;

    public StartupTaskService(IProcessExecutor? processExecutor = null, string? applicationPath = null)
    {
        _processExecutor = processExecutor ?? new SystemProcessExecutor();
        _applicationPath = applicationPath ?? Environment.ProcessPath ?? string.Empty;
    }

    public Task<ScheduledTaskResult> SetEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        if (!enabled)
        {
            return ExecuteAsync(["/Delete", "/TN", TaskName, "/F"], cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(_applicationPath) || _applicationPath.Contains('"'))
        {
            throw new InvalidOperationException("O caminho do aplicativo não é válido para inicialização.");
        }

        var arguments = new[]
        {
            "/Create", "/TN", TaskName,
            "/TR", $"\"{Path.GetFullPath(_applicationPath)}\"",
            "/SC", "ONLOGON", "/IT", "/RL", "LIMITED", "/F"
        };
        return ExecuteAsync(arguments, cancellationToken);
    }

    private async Task<ScheduledTaskResult> ExecuteAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        var result = await _processExecutor.ExecuteAsync(
            new ProcessExecutionRequest("schtasks.exe", arguments, Timeout),
            cancellationToken);
        return result.Started && !result.Canceled && !result.TimedOut && result.ExitCode == 0
            ? new(true, "Inicialização atualizada.", result.ExitCode)
            : new(false, result.ErrorMessage ?? "Não foi possível atualizar a inicialização.", result.ExitCode);
    }
}
