using System.IO;

namespace Atualizador.Windows.Services;

/// <summary>
/// Liga ou desliga "Iniciar com o Windows" por meio de uma tarefa do Agendador de Tarefas que abre
/// o app no logon. Foi escolhida uma tarefa (e não o registro <c>Run</c>) porque ela fica visível e o
/// usuário pode removê-la pelo próprio Agendador. Roda sem elevação (<c>/RL LIMITED</c>).
/// </summary>
public sealed class StartupTaskService
{
    /// <summary>Nome da tarefa. Não mudar: tarefas já criadas por versões anteriores usam este nome.</summary>
    public const string TaskName = "\\Atualizador.Windows.Startup.v1";

    /// <summary>
    /// Argumento da tarefa: abre o app só na área de notificação, sem mostrar a janela,
    /// para não atrapalhar o usuário logo após o logon.
    /// </summary>
    public const string MinimizedArgument = "--minimized";

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
    private readonly IProcessExecutor _processExecutor;
    private readonly string _applicationPath;

    /// <param name="applicationPath">Executável que a tarefa vai abrir. Padrão: o executável atual.</param>
    public StartupTaskService(IProcessExecutor? processExecutor = null, string? applicationPath = null)
    {
        _processExecutor = processExecutor ?? new SystemProcessExecutor();
        _applicationPath = applicationPath ?? Environment.ProcessPath ?? string.Empty;
    }

    public Task<ScheduledTaskResult> SetEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        if (!enabled)
        {
            return RunSchtasksAsync(["/Delete", "/TN", TaskName, "/F"], cancellationToken);
        }

        // O caminho vai entre aspas; um caminho que já contenha aspas alteraria o comando.
        if (string.IsNullOrWhiteSpace(_applicationPath) || _applicationPath.Contains('"'))
        {
            throw new InvalidOperationException("O caminho do aplicativo não é válido para inicialização.");
        }

        return RunSchtasksAsync(
        [
            "/Create", "/TN", TaskName,
            "/TR", $"\"{Path.GetFullPath(_applicationPath)}\" {MinimizedArgument}",
            "/SC", "ONLOGON", "/IT", "/RL", "LIMITED", "/F"
        ], cancellationToken);
    }

    private async Task<ScheduledTaskResult> RunSchtasksAsync(
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
