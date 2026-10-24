using Atualizador.Windows.Models;
using Atualizador.Windows.Services;

namespace Atualizador.Windows.Tests;

[TestClass]
public sealed class HistoryStoreTests
{
    [TestMethod]
    public void AppendAndLoad_PreservesNewestEntry()
    {
        using var fixture = new StoreFixture();
        var store = new HistoryStore(fixture.Path);
        var entry = CreateEntry("primeiro");

        store.Append(entry);

        var loaded = store.Load();
        Assert.HasCount(1, loaded);
        Assert.AreEqual(entry.Id, loaded[0].Id);
        Assert.AreEqual("primeiro", loaded[0].Message);
    }

    [TestMethod]
    public void Load_CorruptedFileReturnsEmptyCollection()
    {
        using var fixture = new StoreFixture();
        File.WriteAllText(fixture.Path, "{ invalid json");
        var store = new HistoryStore(fixture.Path);

        Assert.IsEmpty(store.Load());
    }

    [TestMethod]
    public void Append_RetainsAtMostOneHundredEntries()
    {
        using var fixture = new StoreFixture();
        var store = new HistoryStore(fixture.Path);
        for (var index = 0; index < HistoryStore.MaxEntries + 5; index++)
        {
            store.Append(CreateEntry(index.ToString()));
        }

        var loaded = store.Load();
        Assert.HasCount(HistoryStore.MaxEntries, loaded);
        Assert.AreEqual("104", loaded[0].Message);
    }

    [TestMethod]
    public void Clear_RemovesHistoryFile()
    {
        using var fixture = new StoreFixture();
        var store = new HistoryStore(fixture.Path);
        store.Append(CreateEntry("entry"));

        store.Clear();

        Assert.IsFalse(File.Exists(fixture.Path));
        Assert.IsEmpty(store.Load());
    }

    private static HistoryEntry CreateEntry(string message) => new(
        Guid.NewGuid(),
        DateTimeOffset.UtcNow,
        DateTimeOffset.UtcNow,
        HistoryOperation.Descoberta,
        "WinGet",
        0,
        HistoryEntryStatus.Sucesso,
        message,
        []);

    private sealed class StoreFixture : IDisposable
    {
        private readonly string _directory = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"atualizador-tests-{Guid.NewGuid():N}");

        public StoreFixture()
        {
            Directory.CreateDirectory(_directory);
            Path = System.IO.Path.Combine(_directory, "history.json");
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }
    }
}
