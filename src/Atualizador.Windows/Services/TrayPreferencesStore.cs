using System.IO;
using System.Text.Json;

namespace Atualizador.Windows.Services;

public sealed class TrayPreferencesStore
{
    public TrayPreferencesStore(string? filePath = null)
    {
        FilePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Atualizador.Windows",
            "tray.json");
    }

    public string FilePath { get; }

    public bool Load()
    {
        try
        {
            return File.Exists(FilePath)
                && JsonSerializer.Deserialize<TrayPreferences>(File.ReadAllText(FilePath))?.MinimizeToTray == true;
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

    public void Save(bool minimizeToTray)
    {
        var directory = Path.GetDirectoryName(FilePath);
        if (directory is null)
        {
            return;
        }

        Directory.CreateDirectory(directory);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(new TrayPreferences(1, minimizeToTray)));
    }

    private sealed record TrayPreferences(int Version, bool MinimizeToTray);
}
