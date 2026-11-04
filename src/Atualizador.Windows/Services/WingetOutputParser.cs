using Atualizador.Windows.Models;

namespace Atualizador.Windows.Services;

/// <summary>Pacotes lidos da tabela e avisos sobre linhas que não puderam ser interpretadas.</summary>
public sealed record WingetParseResult(
    IReadOnlyList<UpdatablePackage> Packages,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Interpreta as tabelas de texto do WinGet (<c>list --upgrade-available</c> e <c>search</c>).
/// <para>
/// O WinGet não oferece saída JSON para esses comandos, então a leitura é por colunas:
/// encontra a linha de cabeçalho (em inglês, português ou espanhol), guarda a posição de cada
/// coluna, pula a linha de traços e recorta cada linha de dados nessas posições.
/// </para>
/// </summary>
/// <example>
/// <code>
/// Nome              ID                Versão   Disponível Origem
/// --------------------------------------------------------------
/// Microsoft Edge    Microsoft.Edge    155.0    156.0      winget
/// 1 atualizações disponíveis.
/// </code>
/// </example>
public static class WingetOutputParser
{
    private static readonly string[] NameHeaders = ["Name", "Nome", "Nombre"];
    private static readonly string[] IdHeaders = ["Id", "ID", "Identificador"];
    private static readonly string[] VersionHeaders = ["Version", "Versão", "Versione"];
    private static readonly string[] AvailableHeaders = ["Available", "Disponível", "Disponible"];
    private static readonly string[] SourceHeaders = ["Source", "Origem", "Origen"];
    private static readonly string[] MatchHeaders = ["Match", "Correspondência", "Coincidencia"];

    // Chaves internas das colunas (independem do idioma do WinGet).
    private const string NameColumn = "nome";
    private const string IdColumn = "id";
    private const string VersionColumn = "versão";
    private const string AvailableColumn = "disponível";
    private const string SourceColumn = "origem";
    private const string MatchColumn = "correspondência";

    /// <summary>
    /// Lê a tabela de <c>winget list --upgrade-available</c>. Ignora pacotes duplicados.
    /// Quando a consulta usa <c>--source winget</c>, o WinGet omite a coluna Origem; nesse caso
    /// todos os pacotes recebem <paramref name="defaultSource"/>.
    /// </summary>
    public static WingetParseResult ParseUpgradeList(string output, string defaultSource = "winget")
    {
        var lines = SplitLines(output);
        var header = FindHeader(lines, [NameColumn, IdColumn, VersionColumn, AvailableColumn], [SourceColumn]);
        if (header is null)
        {
            return new([], ["O cabeçalho da lista de atualizações não foi reconhecido."]);
        }

        var packages = new List<UpdatablePackage>();
        var packageKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var warnings = new List<string>();

        foreach (var line in EnumerateDataLines(lines, header.LineIndex))
        {
            var fields = SliceColumns(line, header.Columns);
            var id = EmptyToNull(fields.GetValueOrDefault(IdColumn));
            var source = header.Has(SourceColumn) ? EmptyToNull(fields.GetValueOrDefault(SourceColumn)) : defaultSource;
            if (id is null || source is null)
            {
                warnings.Add("Uma linha de atualização foi ignorada por não conter ID e origem.");
                continue;
            }

            var installedVersion = fields.GetValueOrDefault(VersionColumn);
            var package = new UpdatablePackage(
                EmptyToNull(fields.GetValueOrDefault(NameColumn)) ?? id,
                id,
                NormalizeVersion(installedVersion),
                NormalizeVersion(fields.GetValueOrDefault(AvailableColumn)),
                source,
                IsUnknownVersion(installedVersion));

            if (!packageKeys.Add(PackageKey(id, source)))
            {
                warnings.Add($"O pacote {id} foi duplicado para a origem {source} e foi ignorado.");
                continue;
            }

            packages.Add(package);
        }

        return new(packages, warnings);
    }

    /// <summary>
    /// Lê a tabela de <c>winget search</c>. Só Nome e ID são obrigatórios; Versão, Correspondência
    /// e Origem são usadas quando aparecem. O publicador é inferido do ID (<c>Mozilla.Firefox</c> → Mozilla).
    /// </summary>
    public static IReadOnlyList<InstallablePackage> ParseSearchResults(string output, string defaultSource = "winget")
    {
        var lines = SplitLines(output);
        var header = FindHeader(lines, [NameColumn, IdColumn], [VersionColumn, MatchColumn, SourceColumn]);
        if (header is null)
        {
            return [];
        }

        var packages = new List<InstallablePackage>();
        var packageKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in EnumerateDataLines(lines, header.LineIndex))
        {
            var fields = SliceColumns(line, header.Columns);
            var id = EmptyToNull(fields.GetValueOrDefault(IdColumn));
            if (id is null)
            {
                continue;
            }

            var source = EmptyToNull(fields.GetValueOrDefault(SourceColumn)) ?? defaultSource;
            if (!packageKeys.Add(PackageKey(id, source)))
            {
                continue;
            }

            packages.Add(new InstallablePackage(
                EmptyToNull(fields.GetValueOrDefault(NameColumn)) ?? id,
                id,
                EmptyToNull(fields.GetValueOrDefault(VersionColumn)) ?? "Desconhecida",
                source,
                InstallablePackage.InferPublisher(id),
                fields.GetValueOrDefault(MatchColumn) ?? string.Empty));
        }

        return packages;
    }

    private static string[] SplitLines(string output) =>
        output.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

    private static string PackageKey(string id, string source) => $"{id}\u001f{source}";

    /// <summary>
    /// Procura a primeira linha que contém todos os cabeçalhos obrigatórios. Os opcionais entram
    /// na lista de colunas só quando aparecem na linha.
    /// </summary>
    private static HeaderInfo? FindHeader(string[] lines, string[] required, string[] optional)
    {
        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];
            var requiredColumns = required.Select(name => FindColumn(line, name)).ToArray();
            if (requiredColumns.Any(column => column.Position < 0))
            {
                continue;
            }

            var columns = requiredColumns
                .Concat(optional.Select(name => FindColumn(line, name)).Where(column => column.Position >= 0))
                .ToArray();
            // Dois cabeçalhos na mesma posição indicam uma linha que não é o cabeçalho real.
            if (columns.Select(column => column.Position).Distinct().Count() == columns.Length)
            {
                return new HeaderInfo(lineIndex, columns.OrderBy(column => column.Position).ToArray());
            }
        }

        return null;
    }

    private static ColumnPosition FindColumn(string line, string columnName)
    {
        var headers = columnName switch
        {
            NameColumn => NameHeaders,
            IdColumn => IdHeaders,
            VersionColumn => VersionHeaders,
            AvailableColumn => AvailableHeaders,
            SourceColumn => SourceHeaders,
            _ => MatchHeaders
        };

        foreach (var header in headers)
        {
            var position = line.IndexOf(header, StringComparison.OrdinalIgnoreCase);
            if (position >= 0)
            {
                return new ColumnPosition(columnName, position);
            }
        }

        return new ColumnPosition(columnName, -1);
    }

    /// <summary>
    /// Linhas de dados: depois do cabeçalho e da linha de traços, sem linhas vazias e sem o
    /// rodapé ("N atualizações disponíveis").
    /// </summary>
    private static IEnumerable<string> EnumerateDataLines(string[] lines, int headerIndex)
    {
        var separatorFound = false;
        for (var index = headerIndex + 1; index < lines.Length; index++)
        {
            var line = lines[index];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (IsSeparator(line))
            {
                separatorFound = true;
                continue;
            }

            if (separatorFound && !IsFooter(line))
            {
                yield return line;
            }
        }
    }

    /// <summary>Recorta a linha nas posições das colunas: cada valor vai até o início da coluna seguinte.</summary>
    private static Dictionary<string, string> SliceColumns(string line, IReadOnlyList<ColumnPosition> columns)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < columns.Count; index++)
        {
            var start = Math.Min(columns[index].Position, line.Length);
            var end = index + 1 < columns.Count ? Math.Min(columns[index + 1].Position, line.Length) : line.Length;
            values[columns[index].Name] = line[start..Math.Max(start, end)].Trim();
        }

        return values;
    }

    private static bool IsSeparator(string line)
    {
        var trimmed = line.Trim();
        return trimmed.Length > 2 && trimmed.All(character => character is '-' or '─' or '—');
    }

    private static bool IsFooter(string line) =>
        line.Contains("upgrade", StringComparison.OrdinalIgnoreCase)
        || line.Contains("atualiza", StringComparison.OrdinalIgnoreCase)
        || line.Contains("actualiza", StringComparison.OrdinalIgnoreCase);

    /// <summary>Versões "Unknown", "Desconhecida", "-" ou vazias viram <c>null</c>.</summary>
    private static string? NormalizeVersion(string? version)
    {
        var normalized = EmptyToNull(version);
        return IsUnknownVersion(normalized) ? null : normalized;
    }

    private static bool IsUnknownVersion(string? version)
    {
        var normalized = version?.Trim();
        return string.IsNullOrEmpty(normalized)
            || normalized.Equals("unknown", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("desconhecida", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("desconocida", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("-", StringComparison.Ordinal);
    }

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record HeaderInfo(int LineIndex, IReadOnlyList<ColumnPosition> Columns)
    {
        public bool Has(string columnName) => Columns.Any(column => column.Name == columnName);
    }

    private sealed record ColumnPosition(string Name, int Position);
}
