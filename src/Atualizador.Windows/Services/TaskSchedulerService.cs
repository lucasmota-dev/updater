using System.Globalization;
using System.IO;
using Atualizador.Windows.Models;

namespace Atualizador.Windows.Services;

public sealed record ScheduledTaskResult(bool Succeeded, string Message, int ExitCode);

public sealed class TaskSchedulerService
{
    public const string TaskName = "\\Atualizador.Windows.AutoUpdate.v1";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
    private readonly IProcessExecutor _processExecutor;
    private readonly string _applicationPath;

    public TaskSchedulerService(
        IProcessExecutor? processExecutor = null,
        string? applicationPath = null)
    {
        _processExecutor = processExecutor ?? new SystemProcessExecutor();
        _applicationPath = applicationPath ?? Environment.ProcessPath ?? string.Empty;
    }

    public Task<ScheduledTaskResult> CreateOrUpdateAsync(
        SchedulePreferences preferences,
        CancellationToken cancellationToken = default)
    {
        if (preferences.Mode == ScheduleMode.Desligado)
        {
            return ChangeAsync(["/Change", "/TN", TaskName, "/DISABLE"], cancellationToken);
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

        return ChangeAsync(arguments, cancellationToken);
    }

    public Task<ScheduledTaskResult> EnableAsync(CancellationToken cancellationToken = default) =>
        ChangeAsync(["/Change", "/TN", TaskName, "/ENABLE"], cancellationToken);

    public Task<ScheduledTaskResult> DisableAsync(CancellationToken cancellationToken = default) =>
        ChangeAsync(["/Change", "/TN", TaskName, "/DISABLE"], cancellationToken);

    public Task<ScheduledTaskResult> RemoveAsync(CancellationToken cancellationToken = default) =>
        ChangeAsync(["/Delete", "/TN", TaskName, "/F"], cancellationToken);

    private async Task<ScheduledTaskResult> ChangeAsync(
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

    private static int GetModifier(SchedulePreferences preferences) => preferences.Mode switch
    {
        ScheduleMode.ACadaHora => Math.Clamp(preferences.IntervalHours, 1, 168),
        ScheduleMode.Diario or ScheduleMode.Semanal => 1,
        _ => 1
    };

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
