using System.IO;
using System.Text.Json;

namespace Atualizador.Windows.Services;

public sealed class StartupPreferencesStore
{
    public StartupPreferencesStore(string? filePath = null)
    {
        FilePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Atualizador.Windows",
            "startup.json");
    }

    public string FilePath { get; }

    public bool Load()
    {
        try
        {
            return File.Exists(FilePath)
                && JsonSerializer.Deserialize<StartupPreferences>(File.ReadAllText(FilePath))?.Enabled == true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public void Save(bool enabled)
    {
        var directory = Path.GetDirectoryName(FilePath);
        if (directory is null)
        {
            return;
        }

        Directory.CreateDirectory(directory);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(new StartupPreferences(1, enabled)));
    }

    private sealed record StartupPreferences(int Version, bool Enabled);
}
