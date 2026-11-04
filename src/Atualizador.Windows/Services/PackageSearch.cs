using System.Globalization;
using System.Text;
using Atualizador.Windows.Models;

namespace Atualizador.Windows.Services;

/// <summary>
/// Pesquisa aproximada de pacotes. O <c>winget search</c> só encontra o texto exato digitado
/// ("firefox br" não encontra "Mozilla Firefox (pt-BR)"), então a pesquisa é feita em duas partes:
/// <list type="number">
/// <item><see cref="BuildFallbackTerms"/> escolhe termos mais curtos para o WinGet (a palavra mais longa
/// e o começo dela, para tolerar erros de digitação);</item>
/// <item><see cref="Rank"/> filtra e ordena os resultados no app, comparando cada palavra digitada com
/// as palavras do nome, do ID e da correspondência (tag/moniker) do pacote.</item>
/// </list>
/// Tudo aqui é cálculo puro, sem chamar o WinGet, para ser fácil de testar.
/// </summary>
public static class PackageSearch
{
    /// <summary>Máximo de termos alternativos tentados depois do texto inteiro (cada um é uma chamada ao WinGet).</summary>
    public const int MaxFallbackTerms = 3;

    private const int MinTermLength = 3;

    /// <summary>
    /// Termos alternativos para o WinGet, do mais específico ao mais amplo, sem repetir o texto inteiro.
    /// Ex.: "firefox br" → ["firefox", "fire", "fir"]; "firfox" → ["firf", "fir"].
    /// </summary>
    public static IReadOnlyList<string> BuildFallbackTerms(string query)
    {
        var fullQuery = Normalize(query.Trim());
        var words = SplitWords(query).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        // Palavras com 3+ letras primeiro, as mais longas antes; palavras de 2 letras ("br")
        // devolveriam centenas de pacotes e só são usadas se não houver outra.
        var candidates = words.Where(word => word.Length >= MinTermLength).OrderByDescending(word => word.Length).ToList();
        if (candidates.Count == 0)
        {
            candidates.AddRange(words);
        }

        // Começo da palavra mais longa: encontra o pacote mesmo com erro de digitação no final.
        if (candidates.Count > 0 && candidates[0].Length > MinTermLength + 1)
        {
            var longest = candidates[0];
            candidates.Add(longest[..Math.Max(MinTermLength + 1, (longest.Length / 2) + 1)]);
            candidates.Add(longest[..MinTermLength]);
        }

        return candidates
            .Where(term => !Normalize(term).Equals(fullQuery, StringComparison.Ordinal))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxFallbackTerms)
            .ToArray();
    }

    /// <summary>
    /// Ordena os pacotes pela semelhança com <paramref name="query"/>.
    /// Com <paramref name="requireAllWords"/> = true, descarta os pacotes em que alguma palavra digitada
    /// não combina com nada (usado nos resultados dos termos alternativos, que são mais amplos).
    /// </summary>
    public static IReadOnlyList<InstallablePackage> Rank(
        string query,
        IEnumerable<InstallablePackage> packages,
        bool requireAllWords)
    {
        var queryWords = SplitWords(query).Select(Normalize).Where(word => word.Length > 0).Distinct().ToArray();
        if (queryWords.Length == 0)
        {
            return packages.ToArray();
        }

        return packages
            .Select(package => (Package: package, Scores: queryWords.Select(word => ScoreWord(word, package)).ToArray()))
            .Where(item => !requireAllWords || item.Scores.All(score => score > 0))
            .OrderByDescending(item => item.Scores.Sum())
            // Empate: o ID com menos partes costuma ser o pacote principal ("Mozilla.Firefox"),
            // e não uma variante ("Mozilla.Firefox.Beta.MSIX", "Mozilla.Firefox.de").
            .ThenBy(item => item.Package.Id.Count(character => character == '.'))
            .ThenBy(item => item.Package.Name.Length)
            .ThenBy(item => item.Package.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(item => item.Package)
            .ToArray();
    }

    /// <summary>
    /// Pontua uma palavra digitada contra o pacote: palavra igual (4), começo de palavra (3), trecho
    /// do nome/ID (2) ou palavra parecida, com 1 ou 2 letras diferentes (1). Combinar no nome vale um pouco mais.
    /// </summary>
    private static double ScoreWord(string word, InstallablePackage package)
    {
        var nameScore = ScoreAgainst(word, package.Name);
        var otherScore = Math.Max(ScoreAgainst(word, package.Id), ScoreAgainst(word, MatchText(package.Match)));
        return nameScore > 0 ? nameScore + 0.5 : otherScore;
    }

    private static double ScoreAgainst(string word, string text)
    {
        var words = SplitWords(text).Select(Normalize).ToArray();
        if (words.Length == 0)
        {
            return 0;
        }

        if (words.Contains(word))
        {
            return 4;
        }

        if (words.Any(candidate => candidate.StartsWith(word, StringComparison.Ordinal)))
        {
            return 3;
        }

        // Texto sem separadores: "ptbr" combina com "pt-BR", "vscode" com "VS Code".
        if (string.Concat(words).Contains(word, StringComparison.Ordinal))
        {
            return 2;
        }

        var tolerance = word.Length switch
        {
            < 4 => 0,
            < 7 => 1,
            _ => 2
        };
        return tolerance > 0 && words.Any(candidate => EditDistance(word, candidate) <= tolerance) ? 1 : 0;
    }

    /// <summary>A coluna de correspondência vem como "Tag: firefox-fork"; interessa só o valor.</summary>
    private static string MatchText(string match)
    {
        var separator = match.IndexOf(':', StringComparison.Ordinal);
        return separator >= 0 ? match[(separator + 1)..] : match;
    }

    /// <summary>Divide em palavras por qualquer caractere que não seja letra ou dígito ("pt-BR" → "pt", "BR").</summary>
    private static IEnumerable<string> SplitWords(string text)
    {
        var current = new StringBuilder();
        foreach (var character in text)
        {
            if (char.IsLetterOrDigit(character))
            {
                current.Append(character);
            }
            else if (current.Length > 0)
            {
                yield return current.ToString();
                current.Clear();
            }
        }

        if (current.Length > 0)
        {
            yield return current.ToString();
        }
    }

    /// <summary>Minúsculas e sem acentos, para "Música" combinar com "musica".</summary>
    private static string Normalize(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var character in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Distância de edição (Damerau-Levenshtein restrita): quantas letras precisam ser incluídas,
    /// removidas, trocadas ou invertidas para transformar uma palavra na outra. "firfox" → "firefox" = 1.
    /// </summary>
    private static int EditDistance(string first, string second)
    {
        if (Math.Abs(first.Length - second.Length) > 2)
        {
            return int.MaxValue;
        }

        var distances = new int[first.Length + 1, second.Length + 1];
        for (var i = 0; i <= first.Length; i++)
        {
            distances[i, 0] = i;
        }

        for (var j = 0; j <= second.Length; j++)
        {
            distances[0, j] = j;
        }

        for (var i = 1; i <= first.Length; i++)
        {
            for (var j = 1; j <= second.Length; j++)
            {
                var cost = first[i - 1] == second[j - 1] ? 0 : 1;
                distances[i, j] = Math.Min(
                    Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                    distances[i - 1, j - 1] + cost);
                if (i > 1 && j > 1 && first[i - 1] == second[j - 2] && first[i - 2] == second[j - 1])
                {
                    distances[i, j] = Math.Min(distances[i, j], distances[i - 2, j - 2] + 1);
                }
            }
        }

        return distances[first.Length, second.Length];
    }
}
