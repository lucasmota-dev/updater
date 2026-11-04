using Atualizador.Windows.Models;

namespace Atualizador.Windows.Services;

/// <summary>Guarda o tema e a cor de destaque escolhidos (<c>preferences.json</c>).</summary>
public sealed class AppearancePreferencesStore(string? filePath = null)
{
    public string FilePath { get; } = filePath ?? AppDataPaths.File("preferences.json");

    /// <summary>Retorna as preferências salvas, ou os padrões se o arquivo não existir ou for de outra versão.</summary>
    public AppearancePreferences Load() =>
        JsonFile.TryRead<AppearancePreferences>(FilePath) is { Version: AppearancePreferences.CurrentVersion } preferences
            ? preferences
            : AppearancePreferences.Defaults;

    public void Save(AppearancePreferences preferences) => JsonFile.TryWrite(FilePath, preferences);
}
