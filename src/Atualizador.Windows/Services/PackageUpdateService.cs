using Atualizador.Windows.Models;

namespace Atualizador.Windows.Services;

public interface IPackageUpdateService
{
    Task<WingetDiscoveryResult> DiscoverUpdatesAsync(
        bool includeUnknown = false,
        CancellationToken cancellationToken = default);

    Task<PackageUpdateResult> UpgradeAsync(
        UpdatablePackage package,
        CancellationToken cancellationToken = default);

    Task<WingetSearchResult> SearchAsync(
        string query,
        string source = "winget",
        CancellationToken cancellationToken = default);

    Task<PackageInstallResult> InstallAsync(
        InstallablePackage package,
        CancellationToken cancellationToken = default);
}
