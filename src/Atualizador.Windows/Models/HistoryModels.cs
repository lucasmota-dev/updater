namespace Atualizador.Windows.Models;

public enum HistoryOperation
{
    Descoberta,
    Atualizacao,
    Instalacao
}

public enum HistoryEntryStatus
{
    Sucesso,
    SemAtualizacoes,
    Parcial,
    Falha,
    Cancelado
}

public sealed record HistoryPackageResult(
    string Name,
    string Id,
    string Source,
    PackageUpdateStatus Status,
    string Message);

public sealed record HistoryEntry(
    Guid Id,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt,
    HistoryOperation Operation,
    string Source,
    int PackageCount,
    HistoryEntryStatus Status,
    string Message,
    IReadOnlyList<HistoryPackageResult> Packages);

public enum HistoryDateFilter
{
    Todos,
    UltimosSeteDias,
    UltimosTrintaDias
}

public enum HistoryStatusFilter
{
    Todos,
    Sucesso,
    SemAtualizacoes,
    Parcial,
    Falha,
    Cancelado
}
