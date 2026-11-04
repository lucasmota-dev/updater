namespace Atualizador.Windows.Services;

/// <summary>Guarda se fechar a janela deve apenas escondê-la na área de notificação (<c>tray.json</c>).</summary>
public sealed class TrayPreferencesStore(string? filePath = null)
{
    public string FilePath { get; } = filePath ?? AppDataPaths.File("tray.json");

    /// <summary>Retorna a preferência salva. O padrão é <c>false</c>: fechar encerra o app.</summary>
    public bool Load() => JsonFile.TryRead<TrayPreferences>(FilePath)?.MinimizeToTray == true;

    public void Save(bool minimizeToTray) =>
        JsonFile.TryWrite(FilePath, new TrayPreferences(1, minimizeToTray));

    private sealed record TrayPreferences(int Version, bool MinimizeToTray);
}
