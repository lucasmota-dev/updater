namespace Atualizador.Windows.Models;

public enum ScheduleMode
{
    Desligado,
    ACadaHora,
    Diario,
    Semanal
}

public enum ScheduleScope
{
    TodosElegiveis,
    PacotesSelecionados
}

public sealed record SchedulePreferences(
    int Version,
    ScheduleMode Mode,
    int IntervalHours,
    DayOfWeek DayOfWeek,
    ScheduleScope Scope,
    IReadOnlyList<UpdatablePackage> SelectedPackages)
{
    public const int CurrentVersion = 1;

    public static SchedulePreferences Defaults { get; } = new(
        CurrentVersion,
        ScheduleMode.Desligado,
        24,
        DayOfWeek.Monday,
        ScheduleScope.TodosElegiveis,
        []);
}
