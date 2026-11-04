using System.IO;

namespace Atualizador.Windows.Services;

/// <summary>Localiza um executável. Existe como interface para os testes simularem "WinGet ausente".</summary>
public interface IExecutableLocator
{
    /// <summary>Caminho completo do executável, ou <c>null</c> se não for encontrado.</summary>
    string? Locate();
}

/// <summary>
/// Procura o <c>winget.exe</c> nas pastas do PATH e, se não achar, no alias de execução do
/// App Installer (<c>%LOCALAPPDATA%\Microsoft\WindowsApps</c>). O caminho completo é usado na
/// execução para não depender da pasta atual.
/// </summary>
public sealed class WingetLocator : IExecutableLocator
{
    public string? Locate()
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrWhiteSpace(path))
        {
            foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                var candidate = Path.Combine(directory, "winget.exe");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        var windowsApps = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft",
            "WindowsApps",
            "winget.exe");

        return File.Exists(windowsApps) ? windowsApps : null;
    }
}
