using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Atualizador.Windows.Models;

namespace Atualizador.Windows.Services;

public sealed class HistoryStore
{
    public const int CurrentVersion = 1;
    public const int MaxEntries = 100;
    private readonly object _sync = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public HistoryStore(string? filePath = null)
    {
        FilePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Atualizador.Windows",
            "history.json");
    }

    public string FilePath { get; }

    public IReadOnlyList<HistoryEntry> Load()
    {
        lock (_sync)
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    return [];
                }

                var file = JsonSerializer.Deserialize<HistoryFile>(File.ReadAllText(FilePath), _jsonOptions);
                return file is { Version: CurrentVersion, Entries: not null }
                    ? file.Entries
                    : [];
            }
            catch (IOException)
            {
                return [];
            }
            catch (JsonException)
            {
                return [];
            }
            catch (UnauthorizedAccessException)
            {
                return [];
            }
        }
    }

    public void Append(HistoryEntry entry)
    {
        lock (_sync)
        {
            var entries = Load().ToList();
            entries.Insert(0, entry);
            if (entries.Count > MaxEntries)
            {
                entries.RemoveRange(MaxEntries, entries.Count - MaxEntries);
            }

            try
            {
                Save(entries);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    public void Clear()
    {
        lock (_sync)
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    File.Delete(FilePath);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private void Save(IReadOnlyList<HistoryEntry> entries)
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
            var file = new HistoryFile(CurrentVersion, entries);
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(file, _jsonOptions));
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

    private sealed record HistoryFile(int Version, IReadOnlyList<HistoryEntry> Entries);
}
