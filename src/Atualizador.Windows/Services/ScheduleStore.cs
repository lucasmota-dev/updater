using Atualizador.Windows.Models;

namespace Atualizador.Windows.Services;

/// <summary>
/// Guarda a configuração do agendamento (<c>schedule.json</c>). A execução agendada
/// (<c>--scheduled</c>) lê este arquivo para saber o escopo e os pacotes escolhidos.
/// </summary>
public sealed class ScheduleStore(string? filePath = null)
{
    public string FilePath { get; } = filePath ?? AppDataPaths.File("schedule.json");

    /// <summary>Retorna a configuração salva, ou "desligado" se o arquivo não existir ou for de outra versão.</summary>
    public SchedulePreferences Load() =>
        JsonFile.TryRead<SchedulePreferences>(FilePath) is { Version: SchedulePreferences.CurrentVersion } preferences
            // Arquivos antigos podem não ter a lista; o restante do código espera uma lista vazia, nunca null.
            ? preferences with { SelectedPackages = preferences.SelectedPackages ?? [] }
            : SchedulePreferences.Defaults;

    public void Save(SchedulePreferences preferences) => JsonFile.TryWrite(FilePath, preferences);
}
