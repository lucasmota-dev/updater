using System.Globalization;
using System.Collections.ObjectModel;
using System.Security;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using Atualizador.Windows.Models;
using Color = System.Windows.Media.Color;

namespace Atualizador.Windows.Services;

public sealed class AppearanceService
{
    private const string LightThemeSource = "Resources/Theme.Light.xaml";
    private const string DarkThemeSource = "Resources/Theme.Dark.xaml";
    private readonly AppearancePreferencesStore _preferencesStore;

    public AppearanceService(AppearancePreferencesStore? preferencesStore = null)
    {
        _preferencesStore = preferencesStore ?? new AppearancePreferencesStore();
        Preferences = Normalize(_preferencesStore.Load());
        ApplyTheme(Preferences.Mode);
        ApplyAccent(Preferences.AccentHex);
    }

    public AppearancePreferences Preferences { get; private set; }

    public void SetMode(AppearanceMode mode)
    {
        Preferences = Preferences with { Mode = mode };
        ApplyTheme(mode);
        SavePreferences();
    }

    public bool TrySetAccent(string? hex, out string errorMessage)
    {
        if (!TryParseHexColor(hex, out var color))
        {
            errorMessage = "Informe uma cor hexadecimal válida, por exemplo, #0067C0.";
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

    public static bool TryParseHexColor(string? value, out Color color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var hex = value.Trim();
        if (hex.StartsWith('#'))
        {
            hex = hex[1..];
        }

        if (hex.Length is not (6 or 8))
        {
            return false;
        }

        if (!byte.TryParse(hex[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var red)
            || !byte.TryParse(hex[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var green)
            || !byte.TryParse(hex[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var blue))
        {
            return false;
        }

        var alpha = hex.Length == 8
            && byte.TryParse(hex[6..8], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var parsedAlpha)
            ? parsedAlpha
            : (byte)255;

        color = Color.FromArgb(alpha, red, green, blue);
        return true;
    }

    private static void ApplyTheme(AppearanceMode mode)
    {
        var source = ResolveThemeSource(mode);
        var dictionaries = System.Windows.Application.Current.Resources.MergedDictionaries;
        var index = FindThemeDictionaryIndex(dictionaries);
        var dictionary = new ResourceDictionary { Source = new Uri(source, UriKind.Relative) };

        if (index >= 0)
        {
            dictionaries[index] = dictionary;
        }
        else
        {
            dictionaries.Insert(0, dictionary);
        }
    }

    private static void ApplyAccent(string hex)
    {
        if (!TryParseHexColor(hex, out var accent))
        {
            accent = Color.FromRgb(0, 103, 192);
        }

        var resources = System.Windows.Application.Current.Resources;
        resources["AccentColor"] = accent;
        resources["AccentBrush"] = CreateBrush(accent);
        resources["AccentBrushHover"] = CreateBrush(AdjustBrightness(accent, 0.86));
        resources["AccentBrushPressed"] = CreateBrush(AdjustBrightness(accent, 0.72));
        resources["AccentForegroundBrush"] = CreateBrush(GetLuminance(accent) > 0.62
            ? Colors.Black
            : Colors.White);
        resources["SubtleAccentBrush"] = CreateBrush(Color.FromArgb(0x18, accent.R, accent.G, accent.B));
        resources["SelectionBrush"] = CreateBrush(Color.FromArgb(0x45, accent.R, accent.G, accent.B));
        resources["FocusBrush"] = CreateBrush(accent);
    }

    private static string ResolveThemeSource(AppearanceMode mode)
    {
        var useDarkTheme = mode switch
        {
            AppearanceMode.Escuro => true,
            AppearanceMode.Claro => false,
            _ => IsSystemDarkTheme()
        };

        return useDarkTheme ? DarkThemeSource : LightThemeSource;
    }

    private static bool IsSystemDarkTheme()
    {
        try
        {
            const string personalizeKey = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            return Registry.GetValue(personalizeKey, "AppsUseLightTheme", 1) is int value && value == 0;
        }
        catch (SecurityException)
        {
            return false;
        }
    }

    private static int FindThemeDictionaryIndex(Collection<ResourceDictionary> dictionaries)
    {
        for (var index = 0; index < dictionaries.Count; index++)
        {
            var source = dictionaries[index].Source?.OriginalString;
            if (source is LightThemeSource or DarkThemeSource)
            {
                return index;
            }
        }

        return -1;
    }

    private void SavePreferences() => _preferencesStore.Save(Preferences);

    private static AppearancePreferences Normalize(AppearancePreferences preferences)
    {
        var mode = Enum.IsDefined(preferences.Mode) ? preferences.Mode : AppearanceMode.Sistema;
        var accent = TryParseHexColor(preferences.AccentHex, out var color)
            ? ToHex(color)
            : AppearancePreferences.DefaultAccentHex;
        return new AppearancePreferences(AppearancePreferences.CurrentVersion, mode, accent);
    }

    private static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    private static SolidColorBrush CreateBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static Color AdjustBrightness(Color color, double factor) => Color.FromRgb(
        Clamp(color.R * factor),
        Clamp(color.G * factor),
        Clamp(color.B * factor));

    private static byte Clamp(double value) => (byte)Math.Clamp((int)Math.Round(value), 0, 255);

    private static double GetLuminance(Color color) =>
        (0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B) / 255;
}
