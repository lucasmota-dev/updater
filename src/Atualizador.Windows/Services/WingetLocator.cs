using System.IO;

namespace Atualizador.Windows.Services;

public interface IExecutableLocator
{
    string? Locate();
}

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
