using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atualizador.Windows.Services;

public sealed record AvailableRelease(Version Version, Uri PageUrl);

/// <summary>
/// Consulta a última release publicada no GitHub. Versões seguem o padrão yyyy.MM.dd.NN.
/// Qualquer falha de rede ou de formato resulta em "sem atualização": a verificação é opcional.
/// </summary>
public sealed class UpdateCheckService(HttpClient? httpClient = null, Uri? latestReleaseUrl = null)
{
    private static readonly Uri DefaultLatestReleaseUrl =
        new("https://api.github.com/repos/lucasmota-dev/updater/releases/latest");

    private readonly HttpClient _httpClient = httpClient ?? CreateDefaultClient();
    private readonly Uri _latestReleaseUrl = latestReleaseUrl ?? DefaultLatestReleaseUrl;

    /// <summary>Versão do executável atual. Compilações locais usam 0.0.0.0.</summary>
    public static Version CurrentVersion { get; } =
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);

    public static bool IsReleaseBuild(Version version) => version.Major > 0;

    public static string Format(Version version) =>
        $"{version.Major}.{version.Minor:00}.{version.Build:00}.{version.Revision:00}";

    public static bool TryParseTag(string? tag, out Version version)
    {
        version = new Version(0, 0, 0, 0);
        var text = tag?.TrimStart('v', 'V');
        return text is not null
            && text.Count(character => character == '.') == 3
            && Version.TryParse(text, out var parsed)
            && parsed is { Major: > 0, Minor: > 0, Build: > 0, Revision: >= 0 }
            && (version = parsed) is not null;
    }

    public async Task<AvailableRelease?> CheckAsync(Version current, CancellationToken cancellationToken = default)
    {
        if (!IsReleaseBuild(current))
        {
            return null;
        }

        try
        {
            using var response = await _httpClient.GetAsync(_latestReleaseUrl, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var release = JsonSerializer.Deserialize<GitHubRelease>(json);
            if (release is null
                || release.Draft
                || release.Prerelease
                || !TryParseTag(release.TagName, out var latest)
                || latest <= current
                || !IsGitHubPage(release.HtmlUrl, out var pageUrl))
            {
                return null;
            }

            return new AvailableRelease(latest, pageUrl);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    private static bool IsGitHubPage(string? value, out Uri url)
    {
        url = DefaultLatestReleaseUrl;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var parsed)
            || parsed.Scheme != Uri.UriSchemeHttps
            || !parsed.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        url = parsed;
        return true;
    }

    private static HttpClient CreateDefaultClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Atualizador-Windows");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    private sealed record GitHubRelease(
        [property: JsonPropertyName("tag_name")] string? TagName,
        [property: JsonPropertyName("html_url")] string? HtmlUrl,
        [property: JsonPropertyName("draft")] bool Draft,
        [property: JsonPropertyName("prerelease")] bool Prerelease);
}
