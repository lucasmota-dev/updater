namespace Atualizador.Windows.Services;

/// <summary>Guarda se o menu lateral está recolhido (só ícones) ou expandido (<c>navigation.json</c>).</summary>
public sealed class NavigationPreferencesStore(string? filePath = null)
{
    public string FilePath { get; } = filePath ?? AppDataPaths.File("navigation.json");

    /// <summary>Retorna <c>true</c> se o menu foi recolhido na última sessão. O padrão é expandido.</summary>
    public bool LoadIsCollapsed() => JsonFile.TryRead<NavigationPreferences>(FilePath)?.IsCollapsed == true;

    public void SaveIsCollapsed(bool isCollapsed) =>
        JsonFile.TryWrite(FilePath, new NavigationPreferences(1, isCollapsed));

    private sealed record NavigationPreferences(int Version, bool IsCollapsed);
}
