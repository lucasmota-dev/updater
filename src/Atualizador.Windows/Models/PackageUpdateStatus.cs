namespace Atualizador.Windows.Models;

/// <summary>Estado de um pacote durante e depois de uma atualização ou instalação.</summary>
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

/// <summary>Resultado da atualização de um pacote. <see cref="ExitCode"/> é o código do WinGet, quando ele chegou a rodar.</summary>
public sealed record PackageUpdateResult(
    UpdatablePackage Package,
    PackageUpdateStatus Status,
    string Message,
    int? ExitCode);
