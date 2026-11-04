namespace Atualizador.Windows.Models;

public enum WingetDiscoveryStatus
{
    Success,
    NoUpdates,
    WinGetUnavailable,
    ExecutionFailed,
    SourceFailed,
    Partial
}

/// <summary>Resultado da busca de atualizações, com a versão do WinGet e as fontes, para diagnóstico.</summary>
public sealed record WingetDiscoveryResult(
    WingetDiscoveryStatus Status,
    IReadOnlyList<UpdatablePackage> Packages,
    IReadOnlyList<WingetSource> Sources,
    string? WinGetVersion,
    string Message,
    int? ExitCode,
    IReadOnlyList<string> Warnings);
