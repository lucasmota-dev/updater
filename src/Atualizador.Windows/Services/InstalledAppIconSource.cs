using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace Atualizador.Windows.Services;

/// <summary>
/// Ícone de um aplicativo já instalado, lido do próprio Windows, sem internet.
/// <para>
/// O WinGet não informa ícones. Mas o nome que ele mostra em <c>winget list</c> é o nome registrado
/// em "Aplicativos instalados" (chaves <c>Uninstall</c> do registro). Essas chaves têm o valor
/// <c>DisplayIcon</c>, com o arquivo e o índice do ícone, ex.: <c>"C:\Program Files\App\app.exe",0</c>.
/// </para>
/// <para>O registro é só lido, uma vez por execução do aplicativo.</para>
/// </summary>
public sealed class InstalledAppIconSource
{
    private const string UninstallKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
    private const int IconSize = PackageIconService.IconPixelSize;

    private readonly Lazy<IReadOnlyDictionary<string, string>> _iconsByName;

    /// <param name="loadEntries">Lista de pares (nome exibido, DisplayIcon). Os testes informam dados fictícios.</param>
    public InstalledAppIconSource(Func<IEnumerable<(string Name, string DisplayIcon)>>? loadEntries = null)
    {
        _iconsByName = new(() => BuildIndex((loadEntries ?? ReadRegistryEntries)()));
    }

    /// <summary>Procura o ícone pelo nome exibido. Retorna <c>null</c> se o aplicativo não for encontrado ou não tiver ícone.</summary>
    public ImageSource? TryLoad(string displayName)
    {
        var location = FindDisplayIcon(displayName);
        return location is null ? null : TryExtract(location);
    }

    /// <summary>
    /// Valor <c>DisplayIcon</c> do aplicativo com esse nome. Nomes longos aparecem cortados na tabela
    /// do WinGet ("Microsoft Visual C++ 2015-2022 Redistributable (x…"); nesse caso, compara o início.
    /// </summary>
    public string? FindDisplayIcon(string displayName)
    {
        var name = Normalize(displayName);
        if (name.Length == 0)
        {
            return null;
        }

        var index = _iconsByName.Value;
        if (index.TryGetValue(name, out var exact))
        {
            return exact;
        }

        var truncated = name.TrimEnd('…', '.').TrimEnd();
        if (truncated.Length == name.Length || truncated.Length < 8)
        {
            return null;
        }

        return index
            .Where(entry => entry.Key.StartsWith(truncated, StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .Select(entry => entry.Value)
            .FirstOrDefault();
    }

    /// <summary>Separa <c>"arquivo",índice</c>. Variáveis de ambiente (<c>%ProgramFiles%</c>) são expandidas.</summary>
    public static (string Path, int Index) ParseDisplayIcon(string displayIcon)
    {
        var value = displayIcon.Trim();
        var index = 0;
        var comma = value.LastIndexOf(',');
        if (comma > 0 && int.TryParse(value[(comma + 1)..].Trim(), out var parsedIndex))
        {
            index = parsedIndex;
            value = value[..comma];
        }

        var path = Environment.ExpandEnvironmentVariables(value.Trim().Trim('"'));
        return (path, index);
    }

    private static Dictionary<string, string> BuildIndex(IEnumerable<(string Name, string DisplayIcon)> entries)
    {
        var index = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, displayIcon) in entries)
        {
            var key = Normalize(name);
            if (key.Length > 0 && !string.IsNullOrWhiteSpace(displayIcon))
            {
                index.TryAdd(key, displayIcon);
            }
        }

        return index;
    }

    private static string Normalize(string? name) => (name ?? string.Empty).Trim();

    private static BitmapSource? TryExtract(string displayIcon)
    {
        var (path, index) = ParseDisplayIcon(displayIcon);
        try
        {
            if (!Path.IsPathFullyQualified(path) || !File.Exists(path))
            {
                return null;
            }

            using var icon = System.Drawing.Icon.ExtractIcon(path, index, IconSize);
            if (icon is null)
            {
                return null;
            }

            var image = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            image.Freeze();
            return image;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
            or ArgumentException or System.ComponentModel.Win32Exception or System.Runtime.InteropServices.COMException)
        {
            return null;
        }
    }

    /// <summary>Lê as chaves de desinstalação da máquina (64 e 32 bits) e do usuário atual.</summary>
    private static List<(string Name, string DisplayIcon)> ReadRegistryEntries()
    {
        (RegistryHive Hive, RegistryView View)[] roots =
        [
            (RegistryHive.LocalMachine, RegistryView.Registry64),
            (RegistryHive.LocalMachine, RegistryView.Registry32),
            (RegistryHive.CurrentUser, RegistryView.Default)
        ];

        var entries = new List<(string Name, string DisplayIcon)>();
        foreach (var (hive, view) in roots)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                using var uninstall = baseKey.OpenSubKey(UninstallKeyPath);
                if (uninstall is null)
                {
                    continue;
                }

                foreach (var subKeyName in uninstall.GetSubKeyNames())
                {
                    using var app = uninstall.OpenSubKey(subKeyName);
                    if (app?.GetValue("DisplayName") is string name && app.GetValue("DisplayIcon") is string icon)
                    {
                        entries.Add((name, icon));
                    }
                }
            }
            catch (Exception exception) when (exception is System.Security.SecurityException or UnauthorizedAccessException or IOException)
            {
                // Uma chave sem permissão de leitura não impede as outras.
            }
        }

        return entries;
    }
}
