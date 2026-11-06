using System.IO;
using System.Net;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Atualizador.Windows.Models;
using Atualizador.Windows.Services;
using Atualizador.Windows.ViewModels;

namespace Atualizador.Windows.Tests;

/// <summary>Ícones dos pacotes. Nenhum teste acessa a internet, o registro real ou o WinGet.</summary>
[TestClass]
public sealed class PackageIconTests
{
    [TestMethod]
    public void ParseWebsite_ReadsEnglishHomepage()
    {
        const string output = """
            Found PowerToys [Microsoft.PowerToys]
            Version: 0.102.3001
            Publisher Url: https://github.com/microsoft
            Homepage: https://github.com/microsoft/PowerToys
            Documentation:
              Wiki: https://github.com/microsoft/PowerToys/wiki
            """;

        Assert.AreEqual(new Uri("https://github.com/microsoft/PowerToys"), WingetShowParser.ParseWebsite(output));
    }

    [TestMethod]
    public void ParseWebsite_ReadsPortugueseLabelsAndFallsBackToPublisher()
    {
        Assert.AreEqual(
            new Uri("https://www.mozilla.org/firefox/"),
            WingetShowParser.ParseWebsite("Encontrado Firefox [Mozilla.Firefox]\r\nPágina inicial: https://www.mozilla.org/firefox/\r\n"));
        Assert.AreEqual(
            new Uri("https://www.videolan.org/"),
            WingetShowParser.ParseWebsite("URL do Fornecedor: https://www.videolan.org/\n"));
    }

    [TestMethod]
    public void ParseWebsite_UpgradesHttpAndIgnoresIndentedOrInvalidLines()
    {
        Assert.AreEqual(new Uri("https://example.com/app"), WingetShowParser.ParseWebsite("Homepage: http://example.com/app"));
        Assert.IsNull(WingetShowParser.ParseWebsite("  Homepage: https://example.com"));
        Assert.IsNull(WingetShowParser.ParseWebsite("Homepage: ftp://example.com"));
        Assert.IsNull(WingetShowParser.ParseWebsite("Nenhum pacote encontrado."));
    }

    [TestMethod]
    public async Task GetWebsiteAsync_RunsShowByExactIdOnWingetSource()
    {
        var executor = new FakeProcessExecutor([new(true, 0, "Homepage: https://example.com", "", false, false, null)]);
        var service = new WingetService(executor, new FakeLocator("winget.exe"), isElevated: false);

        var website = await service.GetWebsiteAsync("Example.App");

        Assert.AreEqual(new Uri("https://example.com"), website);
        CollectionAssert.AreEqual(
            new[] { "show", "--id", "Example.App", "--exact", "--source", "winget", "--disable-interactivity" },
            executor.Requests.Single().Arguments.ToArray());
    }

    [TestMethod]
    public async Task GetWebsiteAsync_RejectsUnsafeIdWithoutRunningWinget()
    {
        var executor = new FakeProcessExecutor([]);
        var service = new WingetService(executor, new FakeLocator("winget.exe"), isElevated: false);

        Assert.IsNull(await service.GetWebsiteAsync("App & calc"));
        Assert.IsEmpty(executor.Requests);
    }

    [TestMethod]
    public void FindIconCandidates_UsesOwnerAvatarForGitHubProjects()
    {
        var candidates = WebIconSource.FindIconCandidates(new Uri("https://github.com/microsoft/PowerToys"), html: null);

        Assert.AreEqual(new Uri("https://github.com/microsoft.png?size=128"), candidates.Single());
    }

    [TestMethod]
    public void FindIconCandidates_PrefersLargestDeclaredIconsAndSkipsSvg()
    {
        const string html = """
            <head>
              <link rel="icon" href="/small.png" sizes="16x16">
              <link rel="icon" type="image/svg+xml" href="/logo.svg">
              <link href="img/big.png" rel="icon" sizes="192x192">
              <link rel='apple-touch-icon' href='https://cdn.example.com/touch.png'>
              <link rel="icon" href="http://example.com/insecure.png">
              <link rel="stylesheet" href="/site.css">
            </head>
            """;

        var candidates = WebIconSource.FindIconCandidates(new Uri("https://example.com/app/"), html);

        CollectionAssert.AreEqual(
            new[]
            {
                new Uri("https://cdn.example.com/touch.png"),
                new Uri("https://example.com/app/img/big.png"),
                new Uri("https://example.com/small.png"),
                new Uri("https://example.com/favicon.ico")
            },
            candidates.ToArray());
    }

    [TestMethod]
    [DataRow("https://example.com/icon.png", true)]
    [DataRow("http://example.com/icon.png", false)]
    [DataRow("https://example.com:8443/icon.png", false)]
    [DataRow("https://localhost/icon.png", false)]
    [DataRow("https://192.168.0.10/icon.png", false)]
    [DataRow("https://10.1.2.3/icon.png", false)]
    [DataRow("https://[::1]/icon.png", false)]
    [DataRow("https://printer.local/icon.png", false)]
    [DataRow("https://intranet/icon.png", false)]
    public void IsAllowedAddress_AcceptsOnlyPublicHttps(string address, bool expected)
    {
        Assert.AreEqual(expected, WebIconSource.IsAllowedAddress(new Uri(address)));
    }

    [TestMethod]
    public void IsPublicAddress_RejectsPrivateRanges()
    {
        Assert.IsTrue(WebIconSource.IsPublicAddress(IPAddress.Parse("140.82.112.3")));
        Assert.IsFalse(WebIconSource.IsPublicAddress(IPAddress.Parse("172.20.0.1")));
        Assert.IsFalse(WebIconSource.IsPublicAddress(IPAddress.Parse("169.254.169.254")));
        Assert.IsFalse(WebIconSource.IsPublicAddress(IPAddress.Parse("::ffff:127.0.0.1")));
        Assert.IsFalse(WebIconSource.IsPublicAddress(IPAddress.Parse("fd00::1")));
    }

    [TestMethod]
    public void TryNormalize_ShrinksLargeImagesToIconSize()
    {
        var png = WebIconSource.TryNormalize(CreatePng(256));

        Assert.IsNotNull(png);
        var decoded = BitmapDecoder.Create(new MemoryStream(png), BitmapCreateOptions.None, BitmapCacheOption.OnLoad).Frames[0];
        Assert.AreEqual(PackageIconService.IconPixelSize, decoded.PixelWidth);
    }

    [TestMethod]
    public void TryNormalize_RejectsSvgHtmlAndTinyImages()
    {
        Assert.IsNull(WebIconSource.TryNormalize("<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>"u8.ToArray()));
        Assert.IsNull(WebIconSource.TryNormalize("<!doctype html><html></html>"u8.ToArray()));
        Assert.IsNull(WebIconSource.TryNormalize(CreatePng(8)));
    }

    [TestMethod]
    public void FindDisplayIcon_MatchesExactAndTruncatedNames()
    {
        var source = new InstalledAppIconSource(() =>
        [
            ("Mozilla Firefox (x64 pt-BR)", @"C:\Program Files\Mozilla Firefox\firefox.exe,0"),
            ("Microsoft Visual C++ 2015-2022 Redistributable (x64) - 14.44", @"C:\ProgramData\vc.exe,0"),
            ("Sem ícone", "")
        ]);

        Assert.AreEqual(@"C:\Program Files\Mozilla Firefox\firefox.exe,0", source.FindDisplayIcon("mozilla firefox (x64 pt-br)"));
        Assert.AreEqual(@"C:\ProgramData\vc.exe,0", source.FindDisplayIcon("Microsoft Visual C++ 2015-2022 Redistributable (x…"));
        Assert.IsNull(source.FindDisplayIcon("Mozilla"));
        Assert.IsNull(source.FindDisplayIcon("Sem ícone"));
    }

    [TestMethod]
    public void ParseDisplayIcon_SplitsPathAndIndex()
    {
        Assert.AreEqual((@"C:\App\app.exe", -101), InstalledAppIconSource.ParseDisplayIcon("\"C:\\App\\app.exe\",-101"));
        Assert.AreEqual((@"C:\App\app.ico", 0), InstalledAppIconSource.ParseDisplayIcon(@"C:\App\app.ico"));
    }

    [TestMethod]
    public async Task GetIconAsync_SkipsWingetWhenARecentMissIsCached()
    {
        var cacheDirectory = Directory.CreateTempSubdirectory("icon-cache-").FullName;
        try
        {
            File.WriteAllBytes(Path.Combine(cacheDirectory, "pkg-Example.App.none"), []);
            var winget = new WebsiteCountingService();
            var service = new PackageIconService(winget, new InstalledAppIconSource(() => []), cacheDirectory: cacheDirectory);

            Assert.IsNull(await service.GetIconAsync("Example", "Example.App"));
            Assert.AreEqual(0, winget.Calls);
        }
        finally
        {
            Directory.Delete(cacheDirectory, recursive: true);
        }
    }

    [TestMethod]
    public async Task GetIconAsync_UsesCachedIconAndAsksOncePerId()
    {
        var cacheDirectory = Directory.CreateTempSubdirectory("icon-cache-").FullName;
        try
        {
            File.WriteAllBytes(Path.Combine(cacheDirectory, "pkg-Example.App.png"), CreatePng(64));
            var winget = new WebsiteCountingService();
            var service = new PackageIconService(winget, new InstalledAppIconSource(() => []), cacheDirectory: cacheDirectory);

            var first = service.GetIconAsync("Example", "Example.App");
            var second = service.GetIconAsync("Example", "example.app");

            Assert.AreSame(first, second);
            Assert.IsNotNull(await first);
            Assert.AreEqual(0, winget.Calls);
        }
        finally
        {
            Directory.Delete(cacheDirectory, recursive: true);
        }
    }

    [TestMethod]
    public async Task LifoGate_ServesTheMostRecentWaiterFirst()
    {
        var gate = new LifoGate(1);
        var holder = await gate.EnterAsync();
        var older = gate.EnterAsync();
        var newer = gate.EnterAsync();

        holder.Dispose();
        var winner = await Task.WhenAny(older, newer);

        Assert.AreSame(newer, winner);
        (await newer).Dispose();
        (await older).Dispose();
    }

    [TestMethod]
    [DataRow("Mozilla Firefox", "M")]
    [DataRow("7-Zip", "7")]
    [DataRow("  .NET SDK", "N")]
    [DataRow("", "?")]
    public void PackageIcon_ShowsInitialWithoutService(string name, string expected)
    {
        var icon = new PackageIconViewModel(name, "Example.App", iconService: null);

        Assert.AreEqual(expected, icon.Initial);
        Assert.IsNull(icon.Image);
        Assert.IsFalse(icon.HasImage);
    }

    private static byte[] CreatePng(int size)
    {
        var pixels = new byte[size * size * 4];
        Array.Fill<byte>(pixels, 0xFF);
        var bitmap = BitmapSource.Create(size, size, 96, 96, PixelFormats.Bgra32, null, pixels, size * 4);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private sealed class WebsiteCountingService : IPackageUpdateService
    {
        public int Calls { get; private set; }

        public Task<Uri?> GetWebsiteAsync(string id, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult<Uri?>(null);
        }

        public Task<WingetDiscoveryResult> DiscoverUpdatesAsync(bool includeUnknown = false, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PackageUpdateResult> UpgradeAsync(UpdatablePackage package, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<WingetSearchResult> SearchAsync(string query, string source = "winget", CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PackageInstallResult> InstallAsync(InstallablePackage package, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
