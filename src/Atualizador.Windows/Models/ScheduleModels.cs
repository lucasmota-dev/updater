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

/// <summary>
/// Conteúdo de <c>schedule.json</c>. <see cref="SelectedPackages"/> só é usado no escopo
/// <see cref="ScheduleScope.PacotesSelecionados"/>; <see cref="IntervalHours"/> só no modo <see cref="ScheduleMode.ACadaHora"/>.
/// </summary>
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
