using Atualizador.Windows.Models;
using Atualizador.Windows.Services;

namespace Atualizador.Windows.Tests;

[TestClass]
public sealed class ExcludedPackagesStoreTests
{
    [TestMethod]
    public void Add_PersistsAndLoadsExcludedPackages()
    {
        using var fixture = new ExcludedStoreFixture();
        var store = new ExcludedPackagesStore(fixture.Path);
        var package = new UpdatablePackage("Test App", "Test.App", "1.0", "2.0", "winget", false);

        store.Add([package]);

        var loaded = store.Load();
        Assert.HasCount(1, loaded);
        Assert.AreEqual("Test App", loaded[0].Name);
        Assert.AreEqual("Test.App", loaded[0].Id);
        Assert.AreEqual("winget", loaded[0].Source);
        Assert.IsTrue(store.IsExcluded(package));
        Assert.IsTrue(store.IsExcluded("TEST.APP", "WINGET"));
    }

    [TestMethod]
    public void Remove_RemovesPackageFromStore()
    {
        using var fixture = new ExcludedStoreFixture();
        var store = new ExcludedPackagesStore(fixture.Path);
        var package = new UpdatablePackage("App 1", "App.One", "1.0", "2.0", "winget", false);
        store.Add([package]);

        Assert.IsTrue(store.IsExcluded(package));

        var excluded = store.Load()[0];
        store.Remove(excluded);

        Assert.IsEmpty(store.Load());
        Assert.IsFalse(store.IsExcluded(package));
    }

    [TestMethod]
    public void Filter_RemovesExcludedPackagesFromList()
    {
        using var fixture = new ExcludedStoreFixture();
        var store = new ExcludedPackagesStore(fixture.Path);
        var pkg1 = new UpdatablePackage("App 1", "App.One", "1.0", "2.0", "winget", false);
        var pkg2 = new UpdatablePackage("App 2", "App.Two", "1.0", "2.0", "winget", false);

        store.Add([pkg1]);

        var filtered = store.Filter([pkg1, pkg2]);

        Assert.HasCount(1, filtered);
        Assert.AreEqual("App.Two", filtered[0].Id);
    }

    [TestMethod]
    public async Task ScheduledRunService_SkipsExcludedPackages()
    {
        using var scheduleFixture = new ExcludedStoreFixture();
        using var historyFixture = new ExcludedStoreFixture();
        using var excludedFixture = new ExcludedStoreFixture();

        var scheduleStore = new ScheduleStore(scheduleFixture.Path);
        var historyStore = new HistoryStore(historyFixture.Path);
        var excludedStore = new ExcludedPackagesStore(excludedFixture.Path);

        var excludedPackage = new UpdatablePackage("Blocked", "Blocked.App", "1.0", "2.0", "winget", false);
        var allowedPackage = new UpdatablePackage("Allowed", "Allowed.App", "1.0", "2.0", "winget", false);

        excludedStore.Add([excludedPackage]);

        scheduleStore.Save(new(
            SchedulePreferences.CurrentVersion,
            ScheduleMode.Diario,
            24,
            DayOfWeek.Monday,
            ScheduleScope.TodosElegiveis,
            [excludedPackage, allowedPackage]));

        var fakeService = new FakePackageUpdateService([excludedPackage, allowedPackage]);
        var runner = new ScheduledRunService(scheduleStore, fakeService, historyStore, excludedStore);

        var exitCode = await runner.RunAsync();

        Assert.AreEqual(0, exitCode);
        Assert.HasCount(1, fakeService.UpgradedPackages);
        Assert.AreEqual("Allowed.App", fakeService.UpgradedPackages[0].Id);
    }

    private sealed class FakePackageUpdateService(IReadOnlyList<UpdatablePackage> discoveryPackages) : IPackageUpdateService
    {
        public List<UpdatablePackage> UpgradedPackages { get; } = [];

        public Task<WingetDiscoveryResult> DiscoverUpdatesAsync(bool includeUnknown = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WingetDiscoveryResult(WingetDiscoveryStatus.Success, discoveryPackages, [], "1.0", "ok", 0, []));

        public Task<PackageUpdateResult> UpgradeAsync(UpdatablePackage package, CancellationToken cancellationToken = default)
        {
            UpgradedPackages.Add(package);
            return Task.FromResult(new PackageUpdateResult(package, PackageUpdateStatus.Succeeded, "ok", 0));
        }

        public Task<WingetSearchResult> SearchAsync(string query, string source = "winget", CancellationToken cancellationToken = default) =>
            Task.FromResult(new WingetSearchResult(WingetSearchStatus.Success, [], "ok", 0));

        public Task<PackageInstallResult> InstallAsync(InstallablePackage package, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PackageInstallResult(package, PackageUpdateStatus.Succeeded, "ok", 0));
    }

    private sealed class ExcludedStoreFixture : IDisposable
    {
        private readonly string _directory;

        public ExcludedStoreFixture()
        {
            _directory = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"atualizador-excluded-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_directory);
            Path = System.IO.Path.Combine(_directory, "data.json");
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }
    }
}

