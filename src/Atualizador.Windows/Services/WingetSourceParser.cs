using Atualizador.Windows.Models;

namespace Atualizador.Windows.Services;

public static class WingetSourceParser
{
    public static IReadOnlyList<WingetSource> Parse(string output)
    {
        var sources = new List<WingetSource>();
        var lines = output.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var headerIndex = Array.FindIndex(lines, line =>
            line.Contains("Name", StringComparison.OrdinalIgnoreCase)
            || line.Contains("Nome", StringComparison.OrdinalIgnoreCase));

        if (headerIndex < 0)
        {
            return sources;
        }

        for (var index = headerIndex + 1; index < lines.Length; index++)
        {
            var line = lines[index].Trim();
            if (string.IsNullOrWhiteSpace(line) || line.All(character => character is '-' or '─' or '—'))
            {
                continue;
            }

            var fields = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length < 3 || fields[0].Equals("Name", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            sources.Add(new WingetSource(
                fields[0],
                fields[1],
                fields[2].Equals("true", StringComparison.OrdinalIgnoreCase)
                    || fields[2].Equals("verdadeiro", StringComparison.OrdinalIgnoreCase)));
        }

        return sources;
    }
}
