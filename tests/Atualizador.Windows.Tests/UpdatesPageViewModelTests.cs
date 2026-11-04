using Atualizador.Windows.Models;
using Atualizador.Windows.Services;
using Atualizador.Windows.ViewModels;

namespace Atualizador.Windows.Tests;

[TestClass]
public sealed class UpdatesPageViewModelTests
{
    private string _directory = string.Empty;

    [TestInitialize]
    public void CreateDirectory()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"atualizador-updates-vm-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
    }

    [TestCleanup]
    public void DeleteDirectory() => Directory.Delete(_directory, recursive: true);

    [TestMethod]
    public async Task AreAllSelected_ReflectsAndControlsTheSelection()
    {
        var (viewModel, _) = CreateViewModel(Package("A.One"), Package("B.Two"));
        await viewModel.RefreshIfStaleAsync();

        Assert.IsFalse(viewModel.AreAllSelected);

        viewModel.Packages[0].IsSelected = true;
        Assert.IsNull(viewModel.AreAllSelected, "Só alguns marcados: estado indeterminado.");

        viewModel.AreAllSelected = true;
        Assert.IsTrue(viewModel.Packages.All(package => package.IsSelected));

        viewModel.AreAllSelected = false;
        Assert.AreEqual(0, viewModel.SelectedCount);
    }

    [TestMethod]
    public async Task UpdateAll_UpdatesEveryListedPackageOneByOne()
    {
        var (viewModel, service) = CreateViewModel(Package("A.One"), Package("B.Two"), Package("C.Three"));
        await viewModel.RefreshIfStaleAsync();

        viewModel.UpdateAllCommand.Execute(null);
        await WaitUntilAsync(() => !viewModel.IsUpdating && service.Upgraded.Count == 3);

        CollectionAssert.AreEquivalent(new[] { "A.One", "B.Two", "C.Three" }, service.Upgraded.Select(package => package.Id).ToArray());
    }

    [TestMethod]
    public async Task RefreshIfStale_DoesNotQueryAgainRightAfterARefresh()
    {
        var (viewModel, service) = CreateViewModel(Package("A.One"));

        await viewModel.RefreshIfStaleAsync();
        await viewModel.RefreshIfStaleAsync();

        Assert.AreEqual(1, service.DiscoveryCalls);
    }

    private (UpdatesPageViewModel ViewModel, FakeUpdateService Service) CreateViewModel(params UpdatablePackage[] packages)
    {
        var service = new FakeUpdateService(packages);
        var viewModel = new UpdatesPageViewModel(
            service,
            new HistoryStore(Path.Combine(_directory, "history.json")),
            new ExcludedPackagesStore(Path.Combine(_directory, "excluded.json")),
            new AlwaysConfirm());
        return (viewModel, service);
    }

    private static UpdatablePackage Package(string id) => new(id, id, "1.0", "2.0", "winget", false);

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 100 && !condition(); attempt++)
        {
            await Task.Delay(20);
        }

        Assert.IsTrue(condition(), "A condição esperada não foi atingida.");
    }

    private sealed class AlwaysConfirm : IUserConfirmation
    {
        public bool Confirm(string title, string message) => true;
    }

    private sealed class FakeUpdateService(IReadOnlyList<UpdatablePackage> packages) : IPackageUpdateService
    {
        public int DiscoveryCalls { get; private set; }

        public List<UpdatablePackage> Upgraded { get; } = [];

        public Task<WingetDiscoveryResult> DiscoverUpdatesAsync(bool includeUnknown = false, CancellationToken cancellationToken = default)
        {
            DiscoveryCalls++;
            return Task.FromResult(new WingetDiscoveryResult(WingetDiscoveryStatus.Success, packages, [], "1.0", "ok", 0, []));
        }

        public Task<PackageUpdateResult> UpgradeAsync(UpdatablePackage package, CancellationToken cancellationToken = default)
        {
            Upgraded.Add(package);
            return Task.FromResult(new PackageUpdateResult(package, PackageUpdateStatus.Succeeded, "ok", 0));
        }

        public Task<WingetSearchResult> SearchAsync(string query, string source = "winget", CancellationToken cancellationToken = default) =>
            Task.FromResult(new WingetSearchResult(WingetSearchStatus.NoPackagesFound, [], "ok"));

        public Task<PackageInstallResult> InstallAsync(InstallablePackage package, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PackageInstallResult(package, PackageUpdateStatus.Succeeded, "ok"));
    }
}
