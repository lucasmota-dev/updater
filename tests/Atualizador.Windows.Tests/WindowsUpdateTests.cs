using Atualizador.Windows.Models;
using Atualizador.Windows.Services;

namespace Atualizador.Windows.Tests;

/// <summary>
/// Só testa a parte sem efeito no sistema: a validação da linha de comando do processo elevado e a
/// exibição dos dados. A procura e a instalação reais usam o Windows Update e não rodam em testes.
/// </summary>
[TestClass]
public sealed class WindowsUpdateTests
{
    [TestMethod]
    public void ElevatedArguments_AcceptOnlyTheFixedArgumentAndAGuid()
    {
        var id = Guid.NewGuid();

        Assert.IsTrue(WindowsUpdateInstallLauncher.TryParseElevatedArguments([WindowsUpdateInstallLauncher.ElevatedArgument, id.ToString("N")], out var parsed));
        Assert.AreEqual(id, parsed);

        Assert.IsFalse(WindowsUpdateInstallLauncher.TryParseElevatedArguments([WindowsUpdateInstallLauncher.ElevatedArgument], out _));
        Assert.IsFalse(WindowsUpdateInstallLauncher.TryParseElevatedArguments([WindowsUpdateInstallLauncher.ElevatedArgument, "C:\\Windows\\evil.exe"], out _));
        Assert.IsFalse(WindowsUpdateInstallLauncher.TryParseElevatedArguments([WindowsUpdateInstallLauncher.ElevatedArgument, id.ToString("N"), "--extra"], out _));
        Assert.IsFalse(WindowsUpdateInstallLauncher.TryParseElevatedArguments(["--outro", id.ToString("N")], out _));
    }

    [TestMethod]
    [DataRow(0L, "—")]
    [DataRow(512L * 1024, "512 KB")]
    [DataRow(35L * 1024 * 1024, "35 MB")]
    [DataRow(3L * 1024 * 1024 * 1024 / 2, "1.5 GB")]
    public void SizeText_UsesReadableUnits(long bytes, string expected)
    {
        var info = new WindowsUpdateInfo("id", "Atualização", "KB1", "Segurança", bytes, false);

        Assert.AreEqual(expected, info.SizeText.Replace(',', '.'));
    }
}
