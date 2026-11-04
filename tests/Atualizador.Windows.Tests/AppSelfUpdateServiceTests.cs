using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Atualizador.Windows.Services;

namespace Atualizador.Windows.Tests;

[TestClass]
public sealed class AppSelfUpdateServiceTests
{
    private static readonly Uri InstallerUrl = new("https://github.com/lucasmota-dev/updater/releases/download/v2026.10.28.02/Atualizador-Setup.exe");
    private static readonly Uri ChecksumsUrl = new("https://github.com/lucasmota-dev/updater/releases/download/v2026.10.28.02/SHA256SUMS.txt");
    private static readonly byte[] InstallerBytes = Encoding.ASCII.GetBytes("instalador fictício para teste");

    private string _directory = string.Empty;

    [TestInitialize]
    public void CreateDirectory()
    {
        _directory = Path.Combine(Path.GetTempPath(), "AtualizadorTests", Guid.NewGuid().ToString("N"));
    }

    [TestCleanup]
    public void DeleteDirectory()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [TestMethod]
    public void TryGetChecksum_FindsInstallerLine()
    {
        var hash = new string('a', 64);
        var sums = $"{new string('b', 64)}  Atualizador.exe\r\n{hash}  Atualizador-Setup.exe\r\n";

        Assert.IsTrue(AppSelfUpdateService.TryGetChecksum(sums, "Atualizador-Setup.exe", out var parsed));
        CollectionAssert.AreEqual(Convert.FromHexString(hash), parsed);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("abc  Atualizador-Setup.exe")]
    [DataRow("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz  Atualizador-Setup.exe")]
    [DataRow("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa  Outro.exe")]
    public void TryGetChecksum_RejectsMissingOrInvalidLine(string sums)
    {
        Assert.IsFalse(AppSelfUpdateService.TryGetChecksum(sums, "Atualizador-Setup.exe", out _));
    }

    [TestMethod]
    public void IsSameDirectory_IgnoresTrailingSeparatorAndCase()
    {
        Assert.IsTrue(AppSelfUpdateService.IsSameDirectory(@"C:\Users\x\AppData\Local\Programs\Atualizador\", @"c:\users\x\appdata\local\programs\atualizador"));
        Assert.IsFalse(AppSelfUpdateService.IsSameDirectory(@"C:\Atualizador\", @"C:\Users\x\Downloads"));
    }

    [TestMethod]
    public async Task DownloadInstallerAsync_ReturnsFileWhenHashMatches()
    {
        var service = CreateService(Convert.ToHexString(SHA256.HashData(InstallerBytes)).ToLowerInvariant());

        var path = await service.DownloadInstallerAsync(Release());

        Assert.IsNotNull(path);
        CollectionAssert.AreEqual(InstallerBytes, await File.ReadAllBytesAsync(path));
    }

    [TestMethod]
    public async Task DownloadInstallerAsync_RejectsWrongHashAndLeavesNoFile()
    {
        var service = CreateService(new string('0', 64));

        var path = await service.DownloadInstallerAsync(Release());

        Assert.IsNull(path);
        Assert.AreEqual(0, Directory.Exists(_directory) ? Directory.GetFiles(_directory).Length : 0);
    }

    [TestMethod]
    public async Task DownloadInstallerAsync_SkipsReleaseWithoutAssets()
    {
        var handler = new RoutingHandler(new Dictionary<Uri, byte[]>());
        var service = new AppSelfUpdateService(new HttpClient(handler), _directory);

        Assert.IsNull(await service.DownloadInstallerAsync(new AvailableRelease(new Version(2026, 10, 28, 2), InstallerUrl)));
        Assert.AreEqual(0, handler.Calls);
    }

    private AppSelfUpdateService CreateService(string hash)
    {
        var routes = new Dictionary<Uri, byte[]>
        {
            [ChecksumsUrl] = Encoding.ASCII.GetBytes($"{hash}  Atualizador-Setup.exe\n"),
            [InstallerUrl] = InstallerBytes
        };
        return new AppSelfUpdateService(new HttpClient(new RoutingHandler(routes)), _directory);
    }

    private static AvailableRelease Release() =>
        new(new Version(2026, 10, 28, 2), new Uri("https://github.com/lucasmota-dev/updater/releases/tag/v2026.10.28.02"), InstallerUrl, ChecksumsUrl);

    private sealed class RoutingHandler(IReadOnlyDictionary<Uri, byte[]> routes) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            var response = request.RequestUri is not null && routes.TryGetValue(request.RequestUri, out var body)
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(body) }
                : new HttpResponseMessage(HttpStatusCode.NotFound);
            return Task.FromResult(response);
        }
    }
}
