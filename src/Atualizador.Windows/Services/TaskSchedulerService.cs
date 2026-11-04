using System.Globalization;
using System.IO;
using Atualizador.Windows.Models;

namespace Atualizador.Windows.Services;

/// <summary>Resultado de uma operação no Agendador de Tarefas.</summary>
public sealed record ScheduledTaskResult(bool Succeeded, string Message, int ExitCode);

/// <summary>
/// Cria, desativa e remove a tarefa de atualização agendada usando o <c>schtasks.exe</c> do Windows.
/// <para>
/// A tarefa tem nome fixo (<see cref="TaskName"/>), roda com a conta do usuário, sem elevação
/// (<c>/RL LIMITED</c>), só com o usuário conectado (<c>/IT</c>), e executa sempre a mesma ação:
/// <c>"caminho\Atualizador.Windows.exe" --scheduled</c>. Nenhum texto do usuário entra na ação.
/// </para>
/// </summary>
public sealed class TaskSchedulerService
{
    /// <summary>Nome da tarefa. Não mudar: tarefas já criadas por versões anteriores usam este nome.</summary>
    public const string TaskName = "\\Atualizador.Windows.AutoUpdate.v1";

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
    private readonly IProcessExecutor _processExecutor;
    private readonly string _applicationPath;

    /// <param name="applicationPath">Executável que a tarefa vai abrir. Padrão: o executável atual.</param>
    public TaskSchedulerService(
        IProcessExecutor? processExecutor = null,
        string? applicationPath = null)
    {
        _processExecutor = processExecutor ?? new SystemProcessExecutor();
        _applicationPath = applicationPath ?? Environment.ProcessPath ?? string.Empty;
    }

    /// <summary>Cria a tarefa ou a substitui (<c>/F</c>). Com frequência "Desligado", só desativa a existente.</summary>
    public Task<ScheduledTaskResult> CreateOrUpdateAsync(
        SchedulePreferences preferences,
        CancellationToken cancellationToken = default)
    {
        if (preferences.Mode == ScheduleMode.Desligado)
        {
            return DisableAsync(cancellationToken);
        }

        var arguments = new List<string>
        {
            "/Create",
            "/TN", TaskName,
            "/TR", BuildAction(),
            "/SC", GetScheduleType(preferences.Mode),
            "/MO", GetModifier(preferences).ToString(CultureInfo.InvariantCulture),
            "/IT",
            "/RL", "LIMITED",
            "/F"
        };
        if (preferences.Mode == ScheduleMode.Semanal)
        {
            arguments.Add("/D");
            arguments.Add(ToSchtasksDay(preferences.DayOfWeek));
        }

        return RunSchtasksAsync(arguments, cancellationToken);
    }

    public Task<ScheduledTaskResult> DisableAsync(CancellationToken cancellationToken = default) =>
        RunSchtasksAsync(["/Change", "/TN", TaskName, "/DISABLE"], cancellationToken);

    public Task<ScheduledTaskResult> RemoveAsync(CancellationToken cancellationToken = default) =>
        RunSchtasksAsync(["/Delete", "/TN", TaskName, "/F"], cancellationToken);

    private async Task<ScheduledTaskResult> RunSchtasksAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        var result = await _processExecutor.ExecuteAsync(
            new ProcessExecutionRequest("schtasks.exe", arguments, Timeout),
            cancellationToken);
        if (!result.Started || result.Canceled || result.TimedOut)
        {
            return new(false, result.ErrorMessage ?? "O Agendador de Tarefas não respondeu.", result.ExitCode);
        }

        return result.ExitCode == 0
            ? new(true, "Agendamento atualizado.", result.ExitCode)
            : new(false, "O Agendador de Tarefas recusou a operação.", result.ExitCode);
    }

    /// <summary>
    /// Monta a ação da tarefa. O caminho fica entre aspas (pode ter espaços); por isso um caminho
    /// que já contenha aspas é recusado, para não alterar o comando.
    /// </summary>
    private string BuildAction()
    {
        if (string.IsNullOrWhiteSpace(_applicationPath) || _applicationPath.Contains('"'))
        {
            throw new InvalidOperationException("O caminho do aplicativo não é válido para agendamento.");
        }

        return $"\"{Path.GetFullPath(_applicationPath)}\" --scheduled";
    }

    private static string GetScheduleType(ScheduleMode mode) => mode switch
    {
        ScheduleMode.ACadaHora => "HOURLY",
        ScheduleMode.Diario => "DAILY",
        ScheduleMode.Semanal => "WEEKLY",
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };

    /// <summary>Valor de <c>/MO</c>: o intervalo em horas no modo por hora; 1 nos demais.</summary>
    private static int GetModifier(SchedulePreferences preferences) =>
        preferences.Mode == ScheduleMode.ACadaHora ? Math.Clamp(preferences.IntervalHours, 1, 168) : 1;

    private static string ToSchtasksDay(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "MON",
        DayOfWeek.Tuesday => "TUE",
        DayOfWeek.Wednesday => "WED",
        DayOfWeek.Thursday => "THU",
        DayOfWeek.Friday => "FRI",
        DayOfWeek.Saturday => "SAT",
        DayOfWeek.Sunday => "SUN",
        _ => "MON"
    };
}
