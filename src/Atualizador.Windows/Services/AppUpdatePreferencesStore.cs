namespace Atualizador.Windows.Services;

/// <summary>
/// Guarda se o app deve procurar uma nova versão no GitHub ao abrir (<c>app-update.json</c>).
/// Ligado por padrão. É a única comunicação que o app faz por conta própria, por isso o usuário
/// pode desligá-la em Configurações (veja a seção Privacidade do README).
/// </summary>
public sealed class AppUpdatePreferencesStore(string? filePath = null)
{
    public string FilePath { get; } = filePath ?? AppDataPaths.File("app-update.json");

    /// <summary>Retorna <c>true</c> se a busca automática está ligada (ou nunca foi configurada).</summary>
    public bool LoadCheckOnStartup() => JsonFile.TryRead<AppUpdatePreferences>(FilePath)?.CheckOnStartup ?? true;

    public void SaveCheckOnStartup(bool checkOnStartup) =>
        JsonFile.TryWrite(FilePath, new AppUpdatePreferences(1, checkOnStartup));

    private sealed record AppUpdatePreferences(int Version, bool CheckOnStartup);
}
