namespace Atualizador.Windows.Services;

/// <summary>
/// Guarda se o usuário ativou "Iniciar com o Windows" (<c>startup.json</c>).
/// A tarefa real fica no Agendador de Tarefas (veja <see cref="StartupTaskService"/>);
/// este arquivo só lembra a escolha para exibir a caixa marcada na tela.
/// </summary>
public sealed class StartupPreferencesStore(string? filePath = null)
{
    public string FilePath { get; } = filePath ?? AppDataPaths.File("startup.json");

    public bool Load() => JsonFile.TryRead<StartupPreferences>(FilePath)?.Enabled == true;

    public void Save(bool enabled) =>
        JsonFile.TryWrite(FilePath, new StartupPreferences(1, enabled));

    private sealed record StartupPreferences(int Version, bool Enabled);
}
