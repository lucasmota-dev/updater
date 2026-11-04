using Atualizador.Windows.Models;
using Atualizador.Windows.Services;

namespace Atualizador.Windows.Tests;

/// <summary>Códigos conferidos com a tabela oficial returnCodes.md do microsoft/winget-cli.</summary>
[TestClass]
public sealed class WingetErrorCodesTests
{
    [TestMethod]
    [DataRow(unchecked((int)0x8A15002B), PackageUpdateStatus.Ignored, "Já instalado")]          // UPDATE_NOT_APPLICABLE
    [DataRow(unchecked((int)0x8A150061), PackageUpdateStatus.Ignored, "Já instalado")]          // PACKAGE_ALREADY_INSTALLED
    [DataRow(unchecked((int)0x8A150019), PackageUpdateStatus.RequiresElevation, "administrador")] // COMMAND_REQUIRES_ADMIN
    [DataRow(unchecked((int)0x8A150056), PackageUpdateStatus.Failed, "não aceita rodar como administrador")] // PROHIBITS_ELEVATION
    [DataRow(unchecked((int)0x8A150011), PackageUpdateStatus.Failed, "hash")]                   // INSTALLER_HASH_MISMATCH
    [DataRow(unchecked((int)0x8A150101), PackageUpdateStatus.Failed, "aberto")]                 // INSTALL_PACKAGE_IN_USE
    [DataRow(unchecked((int)0x8A150109), PackageUpdateStatus.RestartRequired, "Reinicie")]      // REBOOT_REQUIRED_TO_FINISH
    public void Describe_MapsOfficialCodes(int exitCode, PackageUpdateStatus expectedStatus, string expectedText)
    {
        var described = WingetErrorCodes.Describe(exitCode, "instalação");

        Assert.IsNotNull(described);
        Assert.AreEqual(expectedStatus, described.Value.Status);
        StringAssert.Contains(described.Value.Message, expectedText);
    }

    [TestMethod]
    public async Task InstallAsync_AlreadyInstalledIsNotReportedAsAdminProblem()
    {
        // Caso real do Discord: o app antigo dizia "exige administrador" para este código.
        var executor = new FakeProcessExecutor([new(true, unchecked((int)0x8A15002B), "No applicable update found.", string.Empty, false, false, null)]);
        var service = new WingetService(executor, new FakeLocator("winget.exe"), isElevated: false);

        var result = await service.InstallAsync(new InstallablePackage("Discord", "Discord.Discord", "1.0", "winget", "Discord"));

        Assert.AreEqual(PackageUpdateStatus.Ignored, result.Status);
        StringAssert.Contains(result.Message, "Já instalado");
    }

    [TestMethod]
    public void Describe_UnknownCodeFallsBackToNull() =>
        Assert.IsNull(WingetErrorCodes.Describe(12345, "instalação"));
}
