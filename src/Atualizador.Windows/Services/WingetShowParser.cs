namespace Atualizador.Windows.Services;

/// <summary>
/// Lê o site do aplicativo na saída de <c>winget show</c>, que é texto no formato "Rótulo: valor".
/// <para>
/// O rótulo muda com o idioma do WinGet. Os textos abaixo vêm dos recursos oficiais do WinGet
/// (<c>ShowLabelPackageUrl</c> e <c>ShowLabelPublisherUrl</c>) em inglês e português. Em outros
/// idiomas o site não é encontrado e o aplicativo mostra a inicial do nome no lugar do ícone.
/// </para>
/// </summary>
public static class WingetShowParser
{
    private static readonly string[] HomepageLabels = ["Homepage:", "Página inicial:"];
    private static readonly string[] PublisherUrlLabels = ["Publisher Url:", "URL do Fornecedor:"];

    /// <summary>Retorna a página do aplicativo ou, se não houver, a do publicador, sempre com <c>https</c>.</summary>
    public static Uri? ParseWebsite(string output) =>
        FindUrl(output, HomepageLabels) ?? FindUrl(output, PublisherUrlLabels);

    private static Uri? FindUrl(string output, string[] labels)
    {
        foreach (var rawLine in output.Split('\n'))
        {
            // Linhas recuadas são sub-itens (documentação, tags, instalador), não campos do pacote.
            if (rawLine.Length == 0 || char.IsWhiteSpace(rawLine[0]))
            {
                continue;
            }

            var line = rawLine.TrimEnd('\r', ' ');
            foreach (var label in labels)
            {
                if (line.StartsWith(label, StringComparison.OrdinalIgnoreCase)
                    && Uri.TryCreate(line[label.Length..].Trim(), UriKind.Absolute, out var uri))
                {
                    // Endereços "http" são trocados por "https": o ícone nunca é baixado sem criptografia.
                    if (uri.Scheme == Uri.UriSchemeHttps)
                    {
                        return uri;
                    }

                    if (uri.Scheme == Uri.UriSchemeHttp)
                    {
                        return new UriBuilder(uri) { Scheme = Uri.UriSchemeHttps, Port = -1 }.Uri;
                    }
                }
            }
        }

        return null;
    }
}
