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

public sealed record WingetDiscoveryResult(
    WingetDiscoveryStatus Status,
    IReadOnlyList<UpdatablePackage> Packages,
    IReadOnlyList<WingetSource> Sources,
    string? WinGetVersion,
    string Message,
    int? ExitCode,
    IReadOnlyList<string> Warnings);
