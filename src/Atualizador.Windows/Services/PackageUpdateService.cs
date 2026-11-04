using Atualizador.Windows.Models;

namespace Atualizador.Windows.Services;

/// <summary>
/// Operações de pacotes usadas pelas telas e pela execução agendada. A implementação real é o
/// <see cref="WingetService"/>; os testes usam uma versão falsa que não altera o computador.
/// </summary>
public interface IPackageUpdateService
{
    /// <summary>Lista os pacotes instalados que têm versão mais nova disponível.</summary>
    Task<WingetDiscoveryResult> DiscoverUpdatesAsync(
        bool includeUnknown = false,
        CancellationToken cancellationToken = default);

    /// <summary>Atualiza um pacote específico (por ID e origem).</summary>
    Task<PackageUpdateResult> UpgradeAsync(
        UpdatablePackage package,
        CancellationToken cancellationToken = default);

    /// <summary>Pesquisa pacotes no catálogo pelo nome ou ID.</summary>
    Task<WingetSearchResult> SearchAsync(
        string query,
        string source = "winget",
        CancellationToken cancellationToken = default);

    /// <summary>Instala um pacote específico (por ID e origem).</summary>
    Task<PackageInstallResult> InstallAsync(
        InstallablePackage package,
        CancellationToken cancellationToken = default);

    /// <summary>Instala como administrador (aviso do UAC). Só para instaladores que exigem elevação.</summary>
    Task<PackageInstallResult> InstallElevatedAsync(InstallablePackage package, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PackageInstallResult(package, PackageUpdateStatus.Failed, "Instalação como administrador não disponível."));

    /// <summary>Atualiza como administrador (aviso do UAC). Só para instaladores que exigem elevação.</summary>
    Task<PackageUpdateResult> UpgradeElevatedAsync(UpdatablePackage package, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PackageUpdateResult(package, PackageUpdateStatus.Failed, "Atualização como administrador não disponível.", null));
}
