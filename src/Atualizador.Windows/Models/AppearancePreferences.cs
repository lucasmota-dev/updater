namespace Atualizador.Windows.Models;

public sealed record AppearancePreferences(
    int Version,
    AppearanceMode Mode,
    string AccentHex)
{
    public const int CurrentVersion = 1;
    public const string DefaultAccentHex = "#0067C0";

    public static AppearancePreferences Defaults { get; } =
        new(CurrentVersion, AppearanceMode.Sistema, DefaultAccentHex);
}
