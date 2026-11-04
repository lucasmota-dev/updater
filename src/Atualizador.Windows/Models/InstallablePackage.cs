namespace Atualizador.Windows.Models;

/// <summary>Pacote encontrado pela pesquisa do WinGet, que pode ser instalado.</summary>
public sealed record InstallablePackage(
    string Name,
    string Id,
    string Version,
    string Source = "winget",
    string Publisher = "",
    string Match = "")
{
    /// <summary>A pesquisa do WinGet não mostra o publicador; usa a primeira parte do ID (Mozilla.Firefox → Mozilla).</summary>
    public static string InferPublisher(string id, string? explicitPublisher = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitPublisher))
        {
            return explicitPublisher.Trim();
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            return "Desconhecido";
        }

        var parts = id.Split('.');
        return parts.Length > 1 ? parts[0] : id;
    }
}

public sealed record PackageInstallResult(
    InstallablePackage Package,
    PackageUpdateStatus Status,
    string Message,
    int? ExitCode = null);

public enum WingetSearchStatus
{
    Success,
    NoPackagesFound,
    ExecutionFailed,
    WinGetUnavailable
}

public sealed record WingetSearchResult(
    WingetSearchStatus Status,
    IReadOnlyList<InstallablePackage> Packages,
    string Message,
    int? ExitCode = null,
    IReadOnlyList<string>? Warnings = null);
