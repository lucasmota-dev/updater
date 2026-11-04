using Atualizador.Windows.Models;

namespace Atualizador.Windows.Services;

/// <summary>
/// Histórico local de verificações, atualizações e instalações (<c>history.json</c>).
/// Mantém só os <see cref="MaxEntries"/> registros mais recentes, do mais novo para o mais antigo.
/// O <c>lock</c> evita que a tela e uma operação em andamento gravem o arquivo ao mesmo tempo.
/// </summary>
public sealed class HistoryStore(string? filePath = null)
{
    public const int CurrentVersion = 1;
    public const int MaxEntries = 100;
    private readonly object _sync = new();

    public string FilePath { get; } = filePath ?? AppDataPaths.File("history.json");

    public IReadOnlyList<HistoryEntry> Load()
    {
        lock (_sync)
        {
            return JsonFile.TryRead<HistoryFile>(FilePath) is { Version: CurrentVersion, Entries: { } entries }
                ? entries
                : [];
        }
    }

    /// <summary>Insere o registro no topo e descarta os mais antigos além do limite.</summary>
    public void Append(HistoryEntry entry)
    {
        lock (_sync)
        {
            var entries = Load().Prepend(entry).Take(MaxEntries).ToArray();
            JsonFile.TryWrite(FilePath, new HistoryFile(CurrentVersion, entries));
        }
    }

    public void Clear()
    {
        lock (_sync)
        {
            JsonFile.TryDelete(FilePath);
        }
    }

    private sealed record HistoryFile(int Version, IReadOnlyList<HistoryEntry>? Entries);
}
