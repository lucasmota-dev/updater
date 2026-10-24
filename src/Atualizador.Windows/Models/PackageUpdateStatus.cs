namespace Atualizador.Windows.Models;

public enum PackageUpdateStatus
{
    NotStarted,
    InProgress,
    Succeeded,
    Failed,
    Canceled,
    Ignored,
    RestartRequired,
    RequiresElevation,
    RequiresInteraction
}

public sealed record PackageUpdateResult(
    UpdatablePackage Package,
    PackageUpdateStatus Status,
    string Message,
    int? ExitCode);
