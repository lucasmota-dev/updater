using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Atualizador.Windows.Models;

namespace Atualizador.Windows.Services;

public sealed class ScheduleStore
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public ScheduleStore(string? filePath = null)
    {
        FilePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Atualizador.Windows",
            "schedule.json");
    }

    public string FilePath { get; }

    public SchedulePreferences Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return SchedulePreferences.Defaults;
            }

            var preferences = JsonSerializer.Deserialize<SchedulePreferences>(
                File.ReadAllText(FilePath), _jsonOptions);
            return preferences is { Version: SchedulePreferences.CurrentVersion }
                ? preferences with { SelectedPackages = preferences.SelectedPackages ?? [] }
                : SchedulePreferences.Defaults;
        }
        catch (IOException)
        {
            return SchedulePreferences.Defaults;
        }
        catch (JsonException)
        {
            return SchedulePreferences.Defaults;
        }
        catch (UnauthorizedAccessException)
        {
            return SchedulePreferences.Defaults;
        }
    }

    public void Save(SchedulePreferences preferences)
    {
        var directory = Path.GetDirectoryName(FilePath);
        if (directory is null)
        {
            return;
        }

        Directory.CreateDirectory(directory);
        var temporaryPath = $"{FilePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(preferences, _jsonOptions));
            File.Move(temporaryPath, FilePath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
