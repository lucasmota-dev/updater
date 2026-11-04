using System.Net;
using System.Net.Http;
using Atualizador.Windows.Services;

namespace Atualizador.Windows.Tests;

[TestClass]
public sealed class UpdateCheckServiceTests
{
    private static readonly Version Current = new(2026, 10, 28, 1);

    [TestMethod]
    [DataRow("v2026.10.28.01", true)]
    [DataRow("2026.10.28.02", true)]
    [DataRow("v2026.10.28", false)]
    [DataRow("v1.2.3", false)]
    [DataRow("continuous", false)]
    [DataRow("", false)]
    [DataRow(null, false)]
    public void TryParseTag_AcceptsOnlyDateVersions(string? tag, bool expected)
    {
        Assert.AreEqual(expected, UpdateCheckService.TryParseTag(tag, out _));
    }

    [TestMethod]
    public void TryParseTag_TreatsLeadingZeroAsSameNumber()
    {
        UpdateCheckService.TryParseTag("v2026.10.28.01", out var version);

        Assert.AreEqual(new Version(2026, 10, 28, 1), version);
        Assert.AreEqual("2026.10.28.01", UpdateCheckService.Format(version));
    }

    [TestMethod]
    public void TryParseTag_OrdersSameDayReleasesBySequence()
    {
        UpdateCheckService.TryParseTag("v2026.10.28.02", out var second);
        UpdateCheckService.TryParseTag("v2026.10.28.10", out var tenth);
        UpdateCheckService.TryParseTag("v2026.11.01.01", out var nextMonth);

        Assert.IsTrue(second > Current);
        Assert.IsTrue(tenth > second);
        Assert.IsTrue(nextMonth > tenth);
    }

    [TestMethod]
    public async Task CheckAsync_ReturnsNewerRelease()
    {
        var service = CreateService(Json("v2026.10.28.02", "https://github.com/lucasmota-dev/updater/releases/tag/v2026.10.28.02"));

        var release = await service.CheckAsync(Current);

        Assert.IsNotNull(release);
        Assert.AreEqual(new Version(2026, 10, 28, 2), release.Version);
    }

    [TestMethod]
    public async Task CheckAsync_ReadsInstallerAndChecksumAssetsOnlyFromGitHub()
    {
        const string download = "https://github.com/lucasmota-dev/updater/releases/download/v2026.10.28.02/";
        var body = $$"""
            {"tag_name":"v2026.10.28.02","html_url":"https://github.com/lucasmota-dev/updater/releases/tag/v2026.10.28.02",
             "prerelease":false,"draft":false,"assets":[
               {"name":"Atualizador-Setup.exe","browser_download_url":"{{download}}Atualizador-Setup.exe"},
               {"name":"SHA256SUMS.txt","browser_download_url":"https://evil.example/SHA256SUMS.txt"}]}
            """;

        var release = await CreateService(body).CheckAsync(Current);

        Assert.IsNotNull(release);
        Assert.AreEqual(new Uri(download + "Atualizador-Setup.exe"), release.InstallerUrl);
        Assert.IsNull(release.ChecksumsUrl);
    }

    [TestMethod]
    public async Task CheckAsync_IgnoresSameOrOlderRelease()
    {
        var same = CreateService(Json("v2026.10.28.01", "https://github.com/lucasmota-dev/updater/releases/tag/v2026.10.28.01"));
        var older = CreateService(Json("v2026.10.27.05", "https://github.com/lucasmota-dev/updater/releases/tag/v2026.10.27.05"));

        Assert.IsNull(await same.CheckAsync(Current));
        Assert.IsNull(await older.CheckAsync(Current));
    }

    [TestMethod]
    public async Task CheckAsync_SkipsLocalBuilds()
    {
        var handler = new StubHandler(HttpStatusCode.OK, Json("v2026.10.28.02", "https://github.com/x/y"));
        var service = new UpdateCheckService(new HttpClient(handler), new Uri("https://example.test/latest"));

        Assert.IsNull(await service.CheckAsync(new Version(0, 0, 0, 0)));
        Assert.AreEqual(0, handler.Calls);
    }

    [TestMethod]
    public async Task CheckAsync_RejectsPageOutsideGitHub()
    {
        var service = CreateService(Json("v2026.10.28.02", "https://evil.example/download"));

        Assert.IsNull(await service.CheckAsync(Current));
    }

    [TestMethod]
    public async Task CheckAsync_RejectsPrereleaseAndDraft()
    {
        var prerelease = CreateService(Json("v2026.10.28.02", "https://github.com/a/b", prerelease: true));
        var draft = CreateService(Json("v2026.10.28.02", "https://github.com/a/b", draft: true));

        Assert.IsNull(await prerelease.CheckAsync(Current));
        Assert.IsNull(await draft.CheckAsync(Current));
    }

    [TestMethod]
    public async Task CheckAsync_ReturnsNullOnHttpErrorOrInvalidJson()
    {
        var notFound = CreateService("", HttpStatusCode.NotFound);
        var invalid = CreateService("<html>");

        Assert.IsNull(await notFound.CheckAsync(Current));
        Assert.IsNull(await invalid.CheckAsync(Current));
    }

    [TestMethod]
    public async Task CheckDetailedAsync_DistinguishesUpToDateFromFailure()
    {
        // A busca manual precisa dizer "você já usa a mais recente" ou "não foi possível consultar".
        var upToDate = CreateService(Json("v2026.10.28.01", "https://github.com/lucasmota-dev/updater/releases/tag/v2026.10.28.01"));
        var failed = CreateService("", HttpStatusCode.InternalServerError);
        var newer = CreateService(Json("v2026.10.28.02", "https://github.com/lucasmota-dev/updater/releases/tag/v2026.10.28.02"));

        Assert.AreEqual(UpdateCheckOutcome.UpToDate, (await upToDate.CheckDetailedAsync(Current)).Outcome);
        Assert.AreEqual(UpdateCheckOutcome.Failed, (await failed.CheckDetailedAsync(Current)).Outcome);
        var available = await newer.CheckDetailedAsync(Current);
        Assert.AreEqual(UpdateCheckOutcome.UpdateAvailable, available.Outcome);
        Assert.AreEqual(new Version(2026, 10, 28, 2), available.Release?.Version);
    }

    [TestMethod]
    public async Task CheckDetailedAsync_LocalBuildIsNotApplicable()
    {
        var service = CreateService(Json("v2026.10.28.02", "https://github.com/a/b"));

        Assert.AreEqual(UpdateCheckOutcome.NotApplicable, (await service.CheckDetailedAsync(new Version(0, 0, 0, 0))).Outcome);
    }

    private static UpdateCheckService CreateService(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(new HttpClient(new StubHandler(status, body)), new Uri("https://example.test/latest"));

    private static string Json(string tag, string url, bool prerelease = false, bool draft = false) =>
        $$"""{"tag_name":"{{tag}}","html_url":"{{url}}","prerelease":{{prerelease.ToString().ToLowerInvariant()}},"draft":{{draft.ToString().ToLowerInvariant()}}}""";

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
        }
    }
}
