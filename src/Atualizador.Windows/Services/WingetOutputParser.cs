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
        var match = headers
            .Select(header => (Header: header, Position: line.IndexOf(header, StringComparison.OrdinalIgnoreCase)))
            .Where(item => item.Position >= 0)
            .OrderBy(item => item.Position)
            .FirstOrDefault();

        return new ColumnPosition(name, match.Position, match.Header?.Length ?? 0);
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

    private sealed record ParsedFields(
        string? Name,
        string? Id,
        string? InstalledVersion,
        string? AvailableVersion,
        string? Source);
}
