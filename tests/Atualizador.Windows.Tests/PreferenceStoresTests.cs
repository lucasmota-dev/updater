using Atualizador.Windows.Services;

namespace Atualizador.Windows.Tests;

[TestClass]
public sealed class PreferenceStoresTests
{
    private string _directory = string.Empty;

    [TestInitialize]
    public void CreateDirectory()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"atualizador-preferences-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
    }

    [TestCleanup]
    public void DeleteDirectory() => Directory.Delete(_directory, recursive: true);

    [TestMethod]
    public void NavigationStore_DefaultsToExpandedAndRemembersChoice()
    {
        var store = new NavigationPreferencesStore(Path.Combine(_directory, "navigation.json"));
        Assert.IsFalse(store.LoadIsCollapsed());

        store.SaveIsCollapsed(true);

        Assert.IsTrue(new NavigationPreferencesStore(store.FilePath).LoadIsCollapsed());
    }

    [TestMethod]
    public void TrayStore_ReadsFileWrittenByPreviousVersions()
    {
        // Formato gravado pelas versões anteriores (sem indentação): continua compatível.
        var path = Path.Combine(_directory, "tray.json");
        File.WriteAllText(path, """{"Version":1,"MinimizeToTray":true}""");

        Assert.IsTrue(new TrayPreferencesStore(path).Load());
    }

    [TestMethod]
    public void Stores_CreateMissingDirectoryAndIgnoreCorruptedFiles()
    {
        var path = Path.Combine(_directory, "subpasta", "startup.json");
        var store = new StartupPreferencesStore(path);

        store.Save(true);
        Assert.IsTrue(store.Load());

        File.WriteAllText(path, "{ não é json");
        Assert.IsFalse(store.Load());
    }
}
