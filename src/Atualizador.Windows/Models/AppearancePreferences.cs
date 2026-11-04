namespace Atualizador.Windows.Models;

/// <summary>
/// Conteúdo de <c>preferences.json</c>. <see cref="AccentHex"/> é <c>#RRGGBB</c> ou <c>SYSTEM</c>.
/// <see cref="Version"/> permite descartar arquivos de formatos antigos com segurança.
/// </summary>
public sealed record AppearancePreferences(
    int Version,
    AppearanceMode Mode,
    string AccentHex)
{
    public const int CurrentVersion = 1;
    public const string DefaultAccentHex = "#0071E3";

    public static AppearancePreferences Defaults { get; } =
        new(CurrentVersion, AppearanceMode.Sistema, DefaultAccentHex);
}
