using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Atualizador.Windows.Models;
using Microsoft.Win32;
using Color = System.Windows.Media.Color;

namespace Atualizador.Windows.Services;

/// <summary>
/// Aplica o tema e a cor de destaque em tempo de execução.
/// <para>
/// Tema: troca o dicionário <c>Theme.Light.xaml</c>/<c>Theme.Dark.xaml</c> dentro dos recursos do app.
/// Como todas as telas usam <c>DynamicResource</c>, a troca aparece imediatamente.
/// </para>
/// <para>
/// Cor de destaque: recria os pincéis <c>Accent*</c>, <c>SubtleAccentBrush</c>, <c>SelectionBrush</c> e
/// <c>FocusBrush</c> a partir de uma única cor (do Windows, predefinida ou hexadecimal do usuário).
/// </para>
/// </summary>
public sealed class AppearanceService
{
    /// <summary>Valor salvo em <see cref="AppearancePreferences.AccentHex"/> para "usar a cor do Windows".</summary>
    public const string SystemAccentKey = "SYSTEM";

    private const string LightThemeSource = "Resources/Theme.Light.xaml";
    private const string DarkThemeSource = "Resources/Theme.Dark.xaml";
    private static readonly Color FallbackAccent = Color.FromRgb(0x00, 0x71, 0xE3);
    private readonly AppearancePreferencesStore _preferencesStore;

    public AppearanceService(AppearancePreferencesStore? preferencesStore = null)
    {
        _preferencesStore = preferencesStore ?? new AppearancePreferencesStore();
        Preferences = Normalize(_preferencesStore.Load());
        ApplyTheme(Preferences.Mode);
        ApplyAccent(Preferences.AccentHex);
    }

    public AppearancePreferences Preferences { get; private set; }

    /// <summary>Tema escuro em uso (já resolvido quando o modo é "Sistema"). Usado pela barra de título.</summary>
    public static bool IsDarkThemeApplied { get; private set; }

    public void SetMode(AppearanceMode mode)
    {
        Preferences = Preferences with { Mode = mode };
        ApplyTheme(mode);
        if (IsSystemAccent(Preferences.AccentHex))
        {
            // A cor do Windows pode ter mudado junto com o tema; relê do registro.
            ApplyAccent(SystemAccentKey);
        }

        SavePreferences();
    }

    public void SetSystemAccent()
    {
        Preferences = Preferences with { AccentHex = SystemAccentKey };
        ApplyAccent(SystemAccentKey);
        SavePreferences();
    }

    /// <summary>Valida e aplica uma cor hexadecimal (<c>#RRGGBB</c> ou <c>#RRGGBBAA</c>).</summary>
    public bool TrySetAccent(string? hex, out string errorMessage)
    {
        if (!TryParseHexColor(hex, out var color))
        {
            errorMessage = "Informe uma cor hexadecimal válida, por exemplo, #0071E3.";
            return false;
        }

        var normalizedHex = ToHex(color);
        Preferences = Preferences with { AccentHex = normalizedHex };
        ApplyAccent(normalizedHex);
        SavePreferences();
        errorMessage = string.Empty;
        return true;
    }

    public void RestoreDefaults()
    {
        Preferences = AppearancePreferences.Defaults;
        ApplyTheme(Preferences.Mode);
        ApplyAccent(Preferences.AccentHex);
        SavePreferences();
    }

    /// <summary>Converte <c>#RRGGBB</c> ou <c>#RRGGBBAA</c> (o <c>#</c> é opcional) em cor.</summary>
    public static bool TryParseHexColor(string? value, out Color color)
    {
        color = default;
        var hex = value?.Trim().TrimStart('#');
        if (hex is null || hex.Length is not (6 or 8))
        {
            return false;
        }

        if (!TryParseByte(hex[..2], out var red)
            || !TryParseByte(hex[2..4], out var green)
            || !TryParseByte(hex[4..6], out var blue))
        {
            return false;
        }

        var alpha = (byte)255;
        if (hex.Length == 8 && !TryParseByte(hex[6..8], out alpha))
        {
            return false;
        }

        color = Color.FromArgb(alpha, red, green, blue);
        return true;
    }

    /// <summary>Lê a cor de destaque do Windows (registro do DWM). Usa azul padrão se não conseguir.</summary>
    public static Color GetSystemAccentColor()
    {
        try
        {
            // O DWM grava a cor como 0xAABBGGRR.
            const string dwmKey = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM";
            if (Registry.GetValue(dwmKey, "AccentColor", null) is int dwmColor)
            {
                return Color.FromRgb(
                    (byte)(dwmColor & 0xFF),
                    (byte)((dwmColor >> 8) & 0xFF),
                    (byte)((dwmColor >> 16) & 0xFF));
            }

            return SystemParameters.WindowGlassColor;
        }
        catch (Exception)
        {
            // Acesso negado ao registro ou valor inesperado: a cor padrão é suficiente.
            return FallbackAccent;
        }
    }

    private static bool IsSystemAccent(string hex) => hex.Equals(SystemAccentKey, StringComparison.OrdinalIgnoreCase);

    private static bool TryParseByte(string text, out byte value) =>
        byte.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);

    /// <summary>Substitui o dicionário de tema atual (ou o insere na primeira posição) e ajusta a barra de título.</summary>
    private static void ApplyTheme(AppearanceMode mode)
    {
        IsDarkThemeApplied = mode switch
        {
            AppearanceMode.Escuro => true,
            AppearanceMode.Claro => false,
            _ => IsSystemDarkTheme()
        };
        WindowTitleBar.ApplyToAll(IsDarkThemeApplied);

        var dictionaries = System.Windows.Application.Current.Resources.MergedDictionaries;
        var dictionary = new ResourceDictionary
        {
            Source = new Uri(IsDarkThemeApplied ? DarkThemeSource : LightThemeSource, UriKind.Relative)
        };
        var index = FindThemeDictionaryIndex(dictionaries);
        if (index >= 0)
        {
            dictionaries[index] = dictionary;
        }
        else
        {
            dictionaries.Insert(0, dictionary);
        }
    }

    /// <summary>Gera todos os pincéis derivados da cor de destaque e os publica nos recursos do app.</summary>
    private static void ApplyAccent(string hex)
    {
        Color accent;
        if (IsSystemAccent(hex))
        {
            accent = GetSystemAccentColor();
        }
        else if (!TryParseHexColor(hex, out accent))
        {
            accent = FallbackAccent;
        }

        var resources = System.Windows.Application.Current.Resources;
        resources["AccentColor"] = accent;
        resources["AccentBrush"] = CreateBrush(accent);
        resources["AccentBrushHover"] = CreateBrush(AdjustBrightness(accent, 0.86));
        resources["AccentBrushPressed"] = CreateBrush(AdjustBrightness(accent, 0.72));
        // Texto sobre a cor de destaque: preto em cores claras, branco nas demais (contraste).
        resources["AccentForegroundBrush"] = CreateBrush(GetLuminance(accent) > 0.62 ? Colors.Black : Colors.White);
        resources["SubtleAccentBrush"] = CreateBrush(Color.FromArgb(0x1F, accent.R, accent.G, accent.B));
        resources["SelectionBrush"] = CreateBrush(Color.FromArgb(0x2E, accent.R, accent.G, accent.B));
        resources["FocusBrush"] = CreateBrush(accent);
    }

    /// <summary>Lê a opção "Modo de aplicativo" do Windows (0 = escuro).</summary>
    private static bool IsSystemDarkTheme()
    {
        try
        {
            const string personalizeKey = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            return Registry.GetValue(personalizeKey, "AppsUseLightTheme", 1) is 0;
        }
        catch (System.Security.SecurityException)
        {
            return false;
        }
    }

    private static int FindThemeDictionaryIndex(Collection<ResourceDictionary> dictionaries)
    {
        for (var index = 0; index < dictionaries.Count; index++)
        {
            if (dictionaries[index].Source?.OriginalString is LightThemeSource or DarkThemeSource)
            {
                return index;
            }
        }

        return -1;
    }

    private void SavePreferences() => _preferencesStore.Save(Preferences);

    /// <summary>Corrige valores inválidos vindos de um arquivo editado à mão.</summary>
    private static AppearancePreferences Normalize(AppearancePreferences preferences)
    {
        var mode = Enum.IsDefined(preferences.Mode) ? preferences.Mode : AppearanceMode.Sistema;
        var accent = IsSystemAccent(preferences.AccentHex)
            ? SystemAccentKey
            : TryParseHexColor(preferences.AccentHex, out var color)
                ? ToHex(color)
                : AppearancePreferences.DefaultAccentHex;
        return new AppearancePreferences(AppearancePreferences.CurrentVersion, mode, accent);
    }

    private static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    /// <summary>Pincéis congelados são imutáveis e mais leves para o WPF.</summary>
    private static SolidColorBrush CreateBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static Color AdjustBrightness(Color color, double factor) => Color.FromRgb(
        ClampToByte(color.R * factor),
        ClampToByte(color.G * factor),
        ClampToByte(color.B * factor));

    private static byte ClampToByte(double value) => (byte)Math.Clamp((int)Math.Round(value), 0, 255);

    /// <summary>Luminância relativa aproximada (0 = preto, 1 = branco).</summary>
    private static double GetLuminance(Color color) =>
        (0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B) / 255;
}
