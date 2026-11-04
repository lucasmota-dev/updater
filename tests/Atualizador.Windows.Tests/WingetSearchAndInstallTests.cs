using Atualizador.Windows.Models;
using Atualizador.Windows.Services;

namespace Atualizador.Windows.Tests;

[TestClass]
public sealed class WingetSearchAndInstallTests
{
    [TestMethod]
    public void ParseSearchResults_ParsesStandardSearchTable()
    {
        const string output = """
            Name                       Id                         Version                 Match
            ------------------------------------------------------------------------------------------
            VLC media player           VideoLAN.VLC               3.0.24                  Moniker: vlc
            pympress                   Cimbali.pympress           1.8.6                   Tag: vlc
            """;

        var packages = WingetOutputParser.ParseSearchResults(output, "winget");

        Assert.HasCount(2, packages);
        Assert.AreEqual("VLC media player", packages[0].Name);
        Assert.AreEqual("VideoLAN.VLC", packages[0].Id);
        Assert.AreEqual("3.0.24", packages[0].Version);
        Assert.AreEqual("winget", packages[0].Source);
        Assert.AreEqual("VideoLAN", packages[0].Publisher);
        Assert.AreEqual("Moniker: vlc", packages[0].Match);

        Assert.AreEqual("pympress", packages[1].Name);
        Assert.AreEqual("Cimbali.pympress", packages[1].Id);
        Assert.AreEqual("Cimbali", packages[1].Publisher);
    }

    [TestMethod]
    public void ParseSearchResults_ParsesMultiSourceTable()
    {
        const string output = """
            Name       Id             Version   Source
            ------------------------------------------
            VLC        XPDM1ZW6815MQM Unknown   msstore
            VLC Player VideoLAN.VLC   3.0.24    winget
            """;

        var packages = WingetOutputParser.ParseSearchResults(output, "winget");

        Assert.HasCount(2, packages);
        Assert.AreEqual("XPDM1ZW6815MQM", packages[0].Id);
        Assert.AreEqual("msstore", packages[0].Source);
        Assert.AreEqual("winget", packages[1].Source);
    }

    [TestMethod]
    public void ParseSearchResults_ReturnsEmptyForUnrecognizedOutput()
    {
        var packages = WingetOutputParser.ParseSearchResults("Nenhum pacote encontrado.", "winget");
        Assert.IsEmpty(packages);
    }

    [TestMethod]
    public async Task SearchAsync_RequiresMinimumTwoCharacters()
    {
        var executor = new FakeProcessExecutor([]);
        var service = new WingetService(executor, new FakeLocator("winget.exe"));

        var result = await service.SearchAsync("a");

        Assert.AreEqual(WingetSearchStatus.NoPackagesFound, result.Status);
        Assert.IsEmpty(executor.Requests);
    }

    [TestMethod]
    public async Task SearchAsync_PassesSafeArgumentsWithSource()
    {
        var executor = new FakeProcessExecutor(
        [
            new(true, 0, "Name Id Version Match\n-----------------------\nApp Vendor.App 1.0 Tag: app", string.Empty, false, false, null)
        ]);
        var service = new WingetService(executor, new FakeLocator("winget.exe"));

        var result = await service.SearchAsync("testApp", "winget");

        Assert.AreEqual(WingetSearchStatus.Success, result.Status);
        Assert.HasCount(1, result.Packages);
        var request = executor.Requests[0];
        Assert.AreEqual("search", request.Arguments[0]);
        Assert.AreEqual("testApp", request.Arguments[1]);
        Assert.IsTrue(request.Arguments.Contains("--source"));
        Assert.IsTrue(request.Arguments.Contains("winget"));
        Assert.IsTrue(request.Arguments.Contains("--disable-interactivity"));
    }

    [TestMethod]
    public async Task InstallAsync_UsesExactParametersAndSafeFlags()
    {
        var executor = new FakeProcessExecutor(
        [
            new(true, 0, "Successfully installed", string.Empty, false, false, null)
        ]);
        var service = new WingetService(executor, new FakeLocator("winget.exe"));
        var package = new InstallablePackage("Test App", "Vendor.TestApp", "1.0", "winget", "Vendor");

        var result = await service.InstallAsync(package);

        Assert.AreEqual(PackageUpdateStatus.Succeeded, result.Status);
        var args = executor.Requests[0].Arguments;
        CollectionAssert.AreEqual(
            new[]
            {
                "install",
                "--id", "Vendor.TestApp",
                "--exact",
                "--source", "winget",
                "--silent",
                "--accept-source-agreements",
                "--accept-package-agreements",
                "--disable-interactivity"
            },
            args.ToArray());
    }

    [TestMethod]
    public async Task InstallAsync_IdentifiesRequiresElevation()
    {
        var executor = new FakeProcessExecutor(
        [
            new(true, 5, string.Empty, "Access is denied. Administrator privileges required.", false, false, null)
        ]);
        var service = new WingetService(executor, new FakeLocator("winget.exe"));
        var package = new InstallablePackage("Admin App", "Vendor.AdminApp", "1.0", "winget", "Vendor");

        var result = await service.InstallAsync(package);

        Assert.AreEqual(PackageUpdateStatus.RequiresElevation, result.Status);
        StringAssert.Contains(result.Message, "administrador");
    }

    [TestMethod]
    public async Task InstallAsync_WhenAppIsElevated_ExplainsThatInstallerRefusesAdmin()
    {
        // Ex.: Spotify/Discord recusam instalar a partir de um processo de administrador.
        var output = new ProcessExecutionResult(true, 1, string.Empty, "The installer cannot be run from an administrator context.", false, false, null);
        var elevated = new WingetService(new FakeProcessExecutor([output]), new FakeLocator("winget.exe"), isElevated: true);
        var normal = new WingetService(new FakeProcessExecutor([output]), new FakeLocator("winget.exe"), isElevated: false);
        var package = new InstallablePackage("Spotify", "Spotify.Spotify", "1.0", "winget", "Spotify");

        var elevatedResult = await elevated.InstallAsync(package);
        var normalResult = await normal.InstallAsync(package);

        Assert.AreEqual(PackageUpdateStatus.Failed, elevatedResult.Status);
        StringAssert.Contains(elevatedResult.Message, "não aceita rodar como administrador");
        Assert.AreEqual(PackageUpdateStatus.RequiresElevation, normalResult.Status);
    }

    [TestMethod]
    public async Task UpgradeAsync_IdentifiesRequiresElevationByExitCode()
    {
        var executor = new FakeProcessExecutor([new(true, 5, string.Empty, "Access is denied.", false, false, null)]);
        var service = new WingetService(executor, new FakeLocator("winget.exe"), isElevated: false);

        var result = await service.UpgradeAsync(new UpdatablePackage("Admin App", "Vendor.AdminApp", "1.0", "2.0", "winget", false));

        Assert.AreEqual(PackageUpdateStatus.RequiresElevation, result.Status);
    }

    [TestMethod]
    public async Task InstallAsync_IdentifiesRequiresInteraction()
    {
        var executor = new FakeProcessExecutor(
        [
            new(true, 1, string.Empty, "Installer requires interactive prompt.", false, false, null)
        ]);
        var service = new WingetService(executor, new FakeLocator("winget.exe"));
        var package = new InstallablePackage("Interactive App", "Vendor.InteractiveApp", "1.0", "winget", "Vendor");

        var result = await service.InstallAsync(package);

        Assert.AreEqual(PackageUpdateStatus.RequiresInteraction, result.Status);
        StringAssert.Contains(result.Message, "interação");
    }

    [TestMethod]
    public async Task InstallAsync_RejectsUnsafeIdentifier()
    {
        var executor = new FakeProcessExecutor([]);
        var service = new WingetService(executor, new FakeLocator("winget.exe"));
        var unsafePackage = new InstallablePackage("Unsafe", "Bad;rmdir /s /q", "1.0", "winget", "Vendor");

        var result = await service.InstallAsync(unsafePackage);

        Assert.AreEqual(PackageUpdateStatus.Failed, result.Status);
        Assert.IsEmpty(executor.Requests);
    }
}
