namespace Atualizador.Windows.Models;

public enum HistoryOperation
{
    Descoberta,
    Atualizacao,
    Instalacao,
    WindowsUpdate
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

/// <summary>Um registro do histórico: uma verificação, uma atualização ou uma instalação, com o resultado de cada pacote.</summary>
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
