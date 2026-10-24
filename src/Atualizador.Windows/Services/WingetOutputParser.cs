using System.Globalization;
using Atualizador.Windows.Models;

namespace Atualizador.Windows.Services;

public sealed record WingetParseResult(
    IReadOnlyList<UpdatablePackage> Packages,
    IReadOnlyList<string> Warnings);

public static class WingetOutputParser
{
    private static readonly string[] NameHeaders = ["Name", "Nome", "Nombre"];
    private static readonly string[] IdHeaders = ["Id", "ID", "Identificador"];
    private static readonly string[] VersionHeaders = ["Version", "Versão", "Versione"];
    private static readonly string[] AvailableHeaders = ["Available", "Disponível", "Disponible"];
    private static readonly string[] SourceHeaders = ["Source", "Origem", "Origen"];
    private static readonly string[] MatchHeaders = ["Match", "Correspondência", "Coincidencia"];

    public static WingetParseResult ParseUpgradeList(string output)
    {
        var lines = output.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var header = FindHeader(lines);
        if (header is null)
        {
            return new([], ["O cabeçalho da lista de atualizações não foi reconhecido."]);
        }

        var packages = new List<UpdatablePackage>();
        var packageKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var warnings = new List<string>();
        var separatorFound = false;

        for (var index = header.LineIndex + 1; index < lines.Length; index++)
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

            if (!separatorFound || IsFooter(line))
            {
                continue;
            }

            var fields = ReadFields(line, header.Columns);
            if (fields.Id is null || fields.Source is null)
            {
                warnings.Add("Uma linha de atualização foi ignorada por não conter ID e origem.");
                continue;
            }

            var package = new UpdatablePackage(
                fields.Name ?? fields.Id,
                fields.Id,
                NormalizeVersion(fields.InstalledVersion),
                NormalizeVersion(fields.AvailableVersion),
                fields.Source,
                IsUnknownVersion(fields.InstalledVersion));
            var key = $"{package.Id}\u001f{package.Source}";
            if (!packageKeys.Add(key))
            {
                warnings.Add($"O pacote {package.Id} foi duplicado para a origem {package.Source} e foi ignorado.");
                continue;
            }

            packages.Add(package);
        }

        return new(packages, warnings);
    }

    public static IReadOnlyList<InstallablePackage> ParseSearchResults(string output, string defaultSource = "winget")
    {
        var lines = output.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var header = FindSearchHeader(lines);
        if (header is null)
        {
            return [];
        }

        var packages = new List<InstallablePackage>();
        var packageKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var separatorFound = false;

        for (var index = header.LineIndex + 1; index < lines.Length; index++)
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

            if (!separatorFound || IsFooter(line))
            {
                continue;
            }

            var fields = ReadSearchFields(line, header.Columns);
            if (string.IsNullOrWhiteSpace(fields.Id))
            {
                continue;
            }

            var name = !string.IsNullOrWhiteSpace(fields.Name) ? fields.Name : fields.Id;
            var version = !string.IsNullOrWhiteSpace(fields.Version) ? fields.Version : "Desconhecida";
            var source = !string.IsNullOrWhiteSpace(fields.Source) ? fields.Source : defaultSource;
            var publisher = InstallablePackage.InferPublisher(fields.Id);
            var match = fields.Match ?? string.Empty;

            var key = $"{fields.Id}\u001f{source}";
            if (!packageKeys.Add(key))
            {
                continue;
            }

            packages.Add(new InstallablePackage(name, fields.Id, version, source, publisher, match));
        }

        return packages;
    }

    private static HeaderInfo? FindHeader(string[] lines)
    {
        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];
            var columns = new List<ColumnPosition>
            {
                FindColumn(line, NameHeaders, "nome"),
                FindColumn(line, IdHeaders, "id"),
                FindColumn(line, VersionHeaders, "versão instalada"),
                FindColumn(line, AvailableHeaders, "versão disponível"),
                FindColumn(line, SourceHeaders, "origem")
            };

            if (columns.All(column => column.Position >= 0)
                && columns.Select(column => column.Position).Distinct().Count() == columns.Count)
            {
                return new HeaderInfo(lineIndex, columns.OrderBy(column => column.Position).ToArray());
            }
        }

        return null;
    }

    private static ColumnPosition FindColumn(string line, IEnumerable<string> headers, string name)
    {
        foreach (var header in headers)
        {
            var position = line.IndexOf(header, StringComparison.OrdinalIgnoreCase);
            if (position >= 0)
            {
                return new ColumnPosition(name, position, header.Length);
            }
        }

        return new ColumnPosition(name, -1, 0);
    }

    private static ParsedFields ReadFields(string line, IReadOnlyList<ColumnPosition> columns)
    {
        var values = columns.Select((column, index) =>
        {
            var end = index + 1 < columns.Count ? columns[index + 1].Position : line.Length;
            var start = Math.Min(column.Position, line.Length);
            var length = Math.Max(0, Math.Min(end, line.Length) - start);
            return (column.Name, Value: line.Substring(start, length).Trim());
        }).ToDictionary(item => item.Name, item => item.Value, StringComparer.Ordinal);

        values.TryGetValue("nome", out var name);
        values.TryGetValue("id", out var id);
        values.TryGetValue("versão instalada", out var installed);
        values.TryGetValue("versão disponível", out var available);
        values.TryGetValue("origem", out var source);
        return new(name, EmptyToNull(id), installed, available, EmptyToNull(source));
    }

    private static bool IsSeparator(string line) =>
        line.Trim().Length > 2 && line.Trim().All(character => character is '-' or '─' or '—');

    private static bool IsFooter(string line) =>
        line.Contains("upgrade", StringComparison.OrdinalIgnoreCase)
        || line.Contains("atualiza", StringComparison.OrdinalIgnoreCase)
        || line.Contains("actualiza", StringComparison.OrdinalIgnoreCase);

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

    private sealed record HeaderInfo(int LineIndex, IReadOnlyList<ColumnPosition> Columns);

    private sealed record ColumnPosition(string Name, int Position, int HeaderLength);

    private static HeaderInfo? FindSearchHeader(string[] lines)
    {
        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];
            var nameCol = FindColumn(line, NameHeaders, "nome");
            var idCol = FindColumn(line, IdHeaders, "id");
            if (nameCol.Position < 0 || idCol.Position < 0)
            {
                continue;
            }

            var columns = new List<ColumnPosition> { nameCol, idCol };
            var verCol = FindColumn(line, VersionHeaders, "versão");
            if (verCol.Position >= 0) columns.Add(verCol);
            var matchCol = FindColumn(line, MatchHeaders, "match");
            if (matchCol.Position >= 0) columns.Add(matchCol);
            var sourceCol = FindColumn(line, SourceHeaders, "origem");
            if (sourceCol.Position >= 0) columns.Add(sourceCol);

            if (columns.Select(c => c.Position).Distinct().Count() == columns.Count)
            {
                return new HeaderInfo(lineIndex, columns.OrderBy(c => c.Position).ToArray());
            }
        }

        return null;
    }

    private static SearchParsedFields ReadSearchFields(string line, IReadOnlyList<ColumnPosition> columns)
    {
        var values = columns.Select((column, index) =>
        {
            var end = index + 1 < columns.Count ? columns[index + 1].Position : line.Length;
            var start = Math.Min(column.Position, line.Length);
            var length = Math.Max(0, Math.Min(end, line.Length) - start);
            return (column.Name, Value: line.Substring(start, length).Trim());
        }).ToDictionary(item => item.Name, item => item.Value, StringComparer.Ordinal);

        values.TryGetValue("nome", out var name);
        values.TryGetValue("id", out var id);
        values.TryGetValue("versão", out var version);
        values.TryGetValue("match", out var match);
        values.TryGetValue("origem", out var source);
        return new(name, EmptyToNull(id), version, match, EmptyToNull(source));
    }

    private sealed record SearchParsedFields(
        string? Name,
        string? Id,
        string? Version,
        string? Match,
        string? Source);

    private sealed record ParsedFields(
        string? Name,
        string? Id,
        string? InstalledVersion,
        string? AvailableVersion,
        string? Source);
}
