namespace Atualizador.Windows.Models;

public enum PackageUpdateStatus
{
    NotStarted,
    InProgress,
    Succeeded,
    Failed,
    Canceled,
    Ignored,
    RestartRequired
}

public sealed record PackageUpdateResult(
    UpdatablePackage Package,
    PackageUpdateStatus Status,
    string Message,
    int? ExitCode);
