using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atualizador.Windows.Services;

/// <summary>Release mais nova. Os links do instalador e das somas só existem quando a release publica os dois arquivos.</summary>
public sealed record AvailableRelease(Version Version, Uri PageUrl, Uri? InstallerUrl = null, Uri? ChecksumsUrl = null);

/// <summary>Resultado de uma consulta de nova versão do próprio aplicativo.</summary>
public enum UpdateCheckOutcome
{
    /// <summary>Compilação local (0.0.0.0): a consulta não é feita.</summary>
    NotApplicable,

    /// <summary>A versão em uso é a mais recente publicada.</summary>
    UpToDate,

    /// <summary>Há uma release mais nova; veja <see cref="UpdateCheckResult.Release"/>.</summary>
    UpdateAvailable,

    /// <summary>Falha de rede, resposta inválida ou release fora do padrão esperado.</summary>
    Failed
}

public sealed record UpdateCheckResult(UpdateCheckOutcome Outcome, AvailableRelease? Release = null);

/// <summary>
/// Consulta a última release publicada no GitHub (<c>/releases/latest</c>).
/// As versões seguem o padrão <c>yyyy.MM.dd.NN</c> e a tag é <c>v</c> + versão.
/// Por segurança, só aceita páginas e downloads hospedados em <c>https://github.com</c>.
/// </summary>
public sealed class UpdateCheckService(HttpClient? httpClient = null, Uri? latestReleaseUrl = null)
{
    private static readonly Uri DefaultLatestReleaseUrl =
        new("https://api.github.com/repos/lucasmota-dev/updater/releases/latest");

    /// <summary>Nome do instalador publicado pelo workflow Release.</summary>
    public const string InstallerAssetName = "Atualizador-Setup.exe";

    /// <summary>Arquivo com as somas SHA-256 dos binários da mesma release.</summary>
    public const string ChecksumsAssetName = "SHA256SUMS.txt";

    private readonly HttpClient _httpClient = httpClient ?? CreateDefaultClient();
    private readonly Uri _latestReleaseUrl = latestReleaseUrl ?? DefaultLatestReleaseUrl;

    /// <summary>Versão do executável atual, injetada pelo workflow Release. Compilações locais usam 0.0.0.0.</summary>
    public static Version CurrentVersion { get; } =
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);

    /// <summary>Indica uma versão publicada (as compilações locais têm Major = 0).</summary>
    public static bool IsReleaseBuild(Version version) => version.Major > 0;

    /// <summary>Formata no padrão do projeto, com dois dígitos: <c>2026.10.28.01</c>.</summary>
    public static string Format(Version version) =>
        $"{version.Major}.{version.Minor:00}.{version.Build:00}.{version.Revision:00}";

    /// <summary>Converte a tag (<c>v2026.10.28.01</c>) em versão. Rejeita tags fora do padrão.</summary>
    public static bool TryParseTag(string? tag, out Version version)
    {
        version = new Version(0, 0, 0, 0);
        var text = tag?.TrimStart('v', 'V');
        if (text is null
            || text.Count(character => character == '.') != 3
            || !Version.TryParse(text, out var parsed)
            || parsed is not { Major: > 0, Minor: > 0, Build: > 0, Revision: >= 0 })
        {
            return false;
        }

        version = parsed;
        return true;
    }

    /// <summary>Atalho para quem só precisa saber se existe release mais nova (null = não existe ou falhou).</summary>
    public async Task<AvailableRelease?> CheckAsync(Version current, CancellationToken cancellationToken = default) =>
        (await CheckDetailedAsync(current, cancellationToken).ConfigureAwait(false)).Release;

    /// <summary>
    /// Consulta a última release e compara com <paramref name="current"/>.
    /// Nunca lança exceção por falha de rede ou de formato: devolve <see cref="UpdateCheckOutcome.Failed"/>.
    /// Só propaga <see cref="OperationCanceledException"/> quando o próprio chamador cancelou.
    /// </summary>
    public async Task<UpdateCheckResult> CheckDetailedAsync(Version current, CancellationToken cancellationToken = default)
    {
        if (!IsReleaseBuild(current))
        {
            return new(UpdateCheckOutcome.NotApplicable);
        }

        try
        {
            using var response = await _httpClient.GetAsync(_latestReleaseUrl, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return new(UpdateCheckOutcome.Failed);
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var release = JsonSerializer.Deserialize<GitHubRelease>(json);
            if (release is null
                || release.Draft
                || release.Prerelease
                || !TryParseTag(release.TagName, out var latest)
                || !TryGetGitHubUrl(release.HtmlUrl, out var pageUrl))
            {
                return new(UpdateCheckOutcome.Failed);
            }

            if (latest <= current)
            {
                return new(UpdateCheckOutcome.UpToDate);
            }

            return new(UpdateCheckOutcome.UpdateAvailable, new AvailableRelease(
                latest,
                pageUrl,
                FindAssetUrl(release.Assets, InstallerAssetName),
                FindAssetUrl(release.Assets, ChecksumsAssetName)));
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException
            // Timeout do HttpClient (não foi o chamador que cancelou).
            || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            return new(UpdateCheckOutcome.Failed);
        }
    }

    private static Uri? FindAssetUrl(IReadOnlyList<GitHubAsset>? assets, string name)
    {
        var asset = assets?.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.Ordinal));
        return TryGetGitHubUrl(asset?.DownloadUrl, out var url) ? url : null;
    }

    /// <summary>Aceita apenas URLs absolutas em <c>https://github.com</c>.</summary>
    private static bool TryGetGitHubUrl(string? value, out Uri url)
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
        client.DefaultRequestHeaders.UserAgent.ParseAdd("App-Updater");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    // Campos usados da resposta da API do GitHub. Os demais são ignorados.
    private sealed record GitHubRelease(
        [property: JsonPropertyName("tag_name")] string? TagName,
        [property: JsonPropertyName("html_url")] string? HtmlUrl,
        [property: JsonPropertyName("draft")] bool Draft,
        [property: JsonPropertyName("prerelease")] bool Prerelease,
        [property: JsonPropertyName("assets")] IReadOnlyList<GitHubAsset>? Assets);

    private sealed record GitHubAsset(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("browser_download_url")] string? DownloadUrl);
}
