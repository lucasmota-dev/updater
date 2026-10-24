using Atualizador.Windows.Models;
using Atualizador.Windows.Services;

namespace Atualizador.Windows.Tests;

[TestClass]
public sealed class WingetOutputParserTests
{
    [TestMethod]
    public void ParseUpgradeList_ParsesEnglishTableWithSpaces()
    {
        const string output = """
            Name           Id             Version       Available     Source
            ----------------------------------------------------------------
            Microsoft Edge Microsoft.Edge 155.0.4283.45 156.0.4314.42 winget
            1 upgrades available.
            """;

        var result = WingetOutputParser.ParseUpgradeList(output);

        Assert.HasCount(1, result.Packages);
        Assert.AreEqual("Microsoft Edge", result.Packages[0].Name);
        Assert.AreEqual("Microsoft.Edge", result.Packages[0].Id);
        Assert.AreEqual("155.0.4283.45", result.Packages[0].InstalledVersion);
        Assert.AreEqual("156.0.4314.42", result.Packages[0].AvailableVersion);
        Assert.AreEqual("winget", result.Packages[0].Source);
        Assert.IsFalse(result.Packages[0].IsVersionUnknown);
        Assert.IsEmpty(result.Warnings);
    }

    [TestMethod]
    public void ParseUpgradeList_ParsesLocalizedHeadersAndUnknownVersion()
    {
        const string output = """
            Nome                 ID                 Versão        Disponível     Origem
            --------------------------------------------------------------------------------
            Editor de Texto      Contoso.Editor     desconhecida  2.0            msstore
            """;

        var result = WingetOutputParser.ParseUpgradeList(output);

        Assert.HasCount(1, result.Packages);
        Assert.AreEqual("Contoso.Editor", result.Packages[0].Id);
        Assert.IsNull(result.Packages[0].InstalledVersion);
        Assert.AreEqual("2.0", result.Packages[0].AvailableVersion);
        Assert.AreEqual("msstore", result.Packages[0].Source);
        Assert.IsTrue(result.Packages[0].IsVersionUnknown);
    }

    [TestMethod]
    public void ParseUpgradeList_SkipsRowsWithoutVerifiedIdOrSource()
    {
        const string output = """
            Name       Id             Version Available Source
            --------------------------------------------------
            Good App   Good.App       1.0     2.0       winget
            Broken App                  1.0     2.0
            """;

        var result = WingetOutputParser.ParseUpgradeList(output);

        Assert.HasCount(1, result.Packages);
        Assert.AreEqual("Good.App", result.Packages[0].Id);
        Assert.HasCount(1, result.Warnings);
    }

    [TestMethod]
    public void ParseUpgradeList_ReturnsWarningForUnknownHeader()
    {
        var result = WingetOutputParser.ParseUpgradeList("Nenhuma atualização encontrada.");

        Assert.IsEmpty(result.Packages);
        Assert.HasCount(1, result.Warnings);
    }

    [TestMethod]
    public void ParseUpgradeList_DeduplicatesByIdAndSource()
    {
        const string output = """
            Name       Id             Version Available Source
            --------------------------------------------------
            First App  Contoso.App    1.0     2.0       winget
            Same App   Contoso.App    1.0     2.0       winget
            Store App  Contoso.App    1.0     2.0       msstore
            """;

        var result = WingetOutputParser.ParseUpgradeList(output);

        Assert.HasCount(2, result.Packages);
        Assert.HasCount(1, result.Warnings);
        Assert.AreEqual("msstore", result.Packages[1].Source);
    }

    [TestMethod]
    public void ParseSourceList_ParsesConfiguredSources()
    {
        const string output = """
            Name        Argument                                      Explicit
            ------------------------------------------------------------------
            msstore     https://storeedgefd.dsx.mp.microsoft.com/v9.0 false
            winget      https://cdn.winget.microsoft.com/cache        false
            winget-font https://cdn.winget.microsoft.com/fonts         true
            """;

        var sources = WingetSourceParser.Parse(output);

        Assert.HasCount(3, sources);
        Assert.AreEqual("msstore", sources[0].Name);
        Assert.IsFalse(sources[0].IsExplicit);
        Assert.IsTrue(sources[2].IsExplicit);
    }
}

[TestClass]
public sealed class WingetServiceTests
{
    [TestMethod]
    public async Task DiscoverUpdates_UsesFakeProcessAndDoesNotIncludeUnknownByDefault()
    {
        var executor = new FakeProcessExecutor(
        [
            new(true, 0, "v1.29.380", string.Empty, false, false, null),
            new(true, 0, "Name Argument Explicit\n-----------------------\nwinget https://example.test false", string.Empty, false, false, null),
            new(true, 0, "Name Id Version Available Source\n--------------------------------\nApp Test.App 1.0 2.0 winget", string.Empty, false, false, null)
        ]);
        var service = new WingetService(executor, new FakeLocator("winget.exe"));

        var result = await service.DiscoverUpdatesAsync();

        Assert.AreEqual(WingetDiscoveryStatus.Success, result.Status);
        Assert.HasCount(1, result.Packages);
        Assert.IsFalse(executor.Requests[2].Arguments.Contains("--include-unknown"));
    }

    [TestMethod]
    public async Task DiscoverUpdates_AddsIncludeUnknownOnlyWhenExplicit()
    {
        var executor = new FakeProcessExecutor(
        [
            new(true, 0, "v1.29.380", string.Empty, false, false, null),
            new(true, 0, "Name Argument Explicit\n-----------------------\nwinget https://example.test false", string.Empty, false, false, null),
            new(true, 0, "Name Id Version Available Source\n--------------------------------\nApp Test.App Unknown 2.0 winget", string.Empty, false, false, null)
        ]);
        var service = new WingetService(executor, new FakeLocator("winget.exe"));

        await service.DiscoverUpdatesAsync(includeUnknown: true);

        Assert.IsTrue(executor.Requests[2].Arguments.Contains("--include-unknown"));
    }

    [TestMethod]
    public async Task DiscoverUpdates_ReturnsSourceFailureWithoutRunningDiscovery()
    {
        var executor = new FakeProcessExecutor(
        [
            new(true, 0, "v1.29.380", string.Empty, false, false, null),
            new(true, 1, string.Empty, "source unavailable", false, false, null)
        ]);
        var service = new WingetService(executor, new FakeLocator("winget.exe"));

        var result = await service.DiscoverUpdatesAsync();

        Assert.AreEqual(WingetDiscoveryStatus.SourceFailed, result.Status);
        Assert.HasCount(2, executor.Requests);
    }

    [TestMethod]
    public async Task DiscoverUpdates_ReturnsUnavailableWithoutStartingProcess()
    {
        var executor = new FakeProcessExecutor([]);
        var service = new WingetService(executor, new FakeLocator(null));

        var result = await service.DiscoverUpdatesAsync();

        Assert.AreEqual(WingetDiscoveryStatus.WinGetUnavailable, result.Status);
        Assert.IsEmpty(executor.Requests);
    }

    [TestMethod]
    public async Task Upgrade_UsesExactIdAndSourceWithoutAll()
    {
        var executor = new FakeProcessExecutor(
        [new(true, 0, "upgrade complete", string.Empty, false, false, null)]);
        var service = new WingetService(executor, new FakeLocator("winget.exe"));
        var package = new UpdatablePackage("Edge", "Microsoft.Edge", "1.0", "2.0", "winget", false);

        var result = await service.UpgradeAsync(package);

        Assert.AreEqual(PackageUpdateStatus.Succeeded, result.Status);
        CollectionAssert.AreEqual(
            new[]
            {
                "upgrade", "--id", "Microsoft.Edge", "--exact", "--source", "winget",
                "--silent", "--accept-source-agreements", "--accept-package-agreements",
                "--disable-interactivity"
            },
            executor.Requests[0].Arguments.ToArray());
        Assert.IsFalse(executor.Requests[0].Arguments.Contains("--all"));
    }

    [TestMethod]
    public async Task Upgrade_RejectsUntrustedIdentifierWithoutStartingProcess()
    {
        var executor = new FakeProcessExecutor([]);
        var service = new WingetService(executor, new FakeLocator("winget.exe"));
        var package = new UpdatablePackage("Edge", "Microsoft Edge; del", "1.0", "2.0", "winget", false);

        var result = await service.UpgradeAsync(package);

        Assert.AreEqual(PackageUpdateStatus.Ignored, result.Status);
        Assert.IsEmpty(executor.Requests);
    }

    [TestMethod]
    public async Task Upgrade_ReportsCancellationAndFailure()
    {
        var canceledExecutor = new FakeProcessExecutor(
        [new(true, -1, string.Empty, string.Empty, false, true, "cancelled")]);
        var canceledService = new WingetService(canceledExecutor, new FakeLocator("winget.exe"));
        var package = new UpdatablePackage("Edge", "Microsoft.Edge", "1.0", "2.0", "winget", false);

        var canceled = await canceledService.UpgradeAsync(package);

        Assert.AreEqual(PackageUpdateStatus.Canceled, canceled.Status);

        var failedExecutor = new FakeProcessExecutor(
        [new(true, 42, string.Empty, "installer failed", false, false, null)]);
        var failedService = new WingetService(failedExecutor, new FakeLocator("winget.exe"));
        var failed = await failedService.UpgradeAsync(package);

        Assert.AreEqual(PackageUpdateStatus.Failed, failed.Status);
        StringAssert.Contains(failed.Message, "installer failed");
    }

    private sealed class FakeLocator(string? path) : IExecutableLocator
    {
        public string? Locate() => path;
    }

    private sealed class FakeProcessExecutor(IReadOnlyList<ProcessExecutionResult> results) : IProcessExecutor
    {
        private readonly Queue<ProcessExecutionResult> _results = new(results);

        public List<ProcessExecutionRequest> Requests { get; } = [];

        public Task<ProcessExecutionResult> ExecuteAsync(
            ProcessExecutionRequest request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(_results.Dequeue());
        }
    }
}
