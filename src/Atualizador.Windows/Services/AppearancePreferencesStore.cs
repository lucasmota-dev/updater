using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Atualizador.Windows.Models;

namespace Atualizador.Windows.Services;

public sealed class AppearancePreferencesStore
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Atualizador.Windows",
        "preferences.json");

    public AppearancePreferences Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return AppearancePreferences.Defaults;
            }

            var preferences = JsonSerializer.Deserialize<AppearancePreferences>(
                File.ReadAllText(FilePath), _jsonOptions);

            return preferences is { Version: AppearancePreferences.CurrentVersion }
                ? preferences
                : AppearancePreferences.Defaults;
        }
        catch (IOException)
        {
            return AppearancePreferences.Defaults;
        }
        catch (JsonException)
        {
            return AppearancePreferences.Defaults;
        }
    }

    public void Save(AppearancePreferences preferences)
    {
        var directory = Path.GetDirectoryName(FilePath);
        if (directory is null)
        {
            return;
        }

        Directory.CreateDirectory(directory);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(preferences, _jsonOptions));
    }
}
