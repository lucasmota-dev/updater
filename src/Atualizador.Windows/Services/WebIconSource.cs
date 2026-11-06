using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Atualizador.Windows.Services;

/// <summary>
/// Ícone de um aplicativo obtido no site dele (página informada no manifesto do WinGet).
/// <para>Regras de segurança:</para>
/// <list type="bullet">
/// <item>só <c>https</c>; redirecionamentos para <c>http</c> não são seguidos (padrão do .NET);</item>
/// <item>endereços locais ou de rede privada (<c>localhost</c>, <c>192.168.x.x</c>…) são recusados;</item>
/// <item>tempo limite curto e tamanho máximo para a página e para a imagem;</item>
/// <item>só PNG, ICO, JPEG, GIF, BMP e WebP; SVG nunca é aberto. A imagem é decodificada,
/// reduzida e regravada como PNG antes de ir para o cache.</item>
/// </list>
/// </summary>
public sealed partial class WebIconSource(HttpClient? httpClient = null)
{
    private const int MaxPageBytes = 512 * 1024;
    private const int MaxImageBytes = 1024 * 1024;
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(8);

    private readonly HttpClient _httpClient = httpClient ?? CreateDefaultClient();

    /// <summary>
    /// Baixa o melhor ícone do site e devolve um PNG de <see cref="PackageIconService.IconPixelSize"/> px.
    /// Retorna <c>null</c> se nenhum ícone válido for encontrado.
    /// </summary>
    public async Task<byte[]?> TryDownloadAsync(Uri website, CancellationToken cancellationToken)
    {
        if (!IsAllowedAddress(website))
        {
            return null;
        }

        var html = IsGitHubRepository(website) ? null : await TryGetTextAsync(website, cancellationToken);
        IReadOnlyList<Uri> candidates;
        try
        {
            candidates = FindIconCandidates(website, html);
        }
        catch (RegexMatchTimeoutException)
        {
            candidates = FindIconCandidates(website, null); // página malformada: tenta só o /favicon.ico
        }

        foreach (var candidate in candidates)
        {
            if (!IsAllowedAddress(candidate))
            {
                continue;
            }

            var bytes = await TryGetBytesAsync(candidate, cancellationToken);
            if (bytes is not null && TryNormalize(bytes) is { } png)
            {
                return png;
            }
        }

        return null;
    }

    /// <summary>
    /// Endereços de ícone em ordem de preferência:
    /// <list type="number">
    /// <item>projeto no GitHub: a imagem do dono do repositório (<c>github.com/dono.png</c>);</item>
    /// <item>ícones declarados na página (<c>apple-touch-icon</c> primeiro, depois o maior <c>icon</c>);</item>
    /// <item><c>/favicon.ico</c> na raiz do site.</item>
    /// </list>
    /// </summary>
    public static IReadOnlyList<Uri> FindIconCandidates(Uri website, string? html)
    {
        var candidates = new List<Uri>();
        if (IsGitHubRepository(website))
        {
            var owner = website.AbsolutePath.Trim('/').Split('/')[0];
            candidates.Add(new Uri($"https://github.com/{Uri.EscapeDataString(owner)}.png?size=128"));
            return candidates;
        }

        if (!string.IsNullOrEmpty(html))
        {
            candidates.AddRange(ParseIconLinks(html, website));
        }

        candidates.Add(new Uri(website, "/favicon.ico"));
        return candidates.Distinct().ToArray();
    }

    /// <summary>Lê as tags <c>&lt;link rel="...icon..." href="..."&gt;</c> e ordena da maior para a menor.</summary>
    public static IEnumerable<Uri> ParseIconLinks(string html, Uri baseUri)
    {
        var links = new List<(Uri Uri, int Score)>();
        foreach (Match tag in LinkTagRegex().Matches(html))
        {
            var attributes = AttributeRegex().Matches(tag.Value)
                .GroupBy(match => match.Groups["name"].Value.ToLowerInvariant())
                .ToDictionary(group => group.Key, group => WebUtility.HtmlDecode(group.First().Groups["value"].Value));

            if (!attributes.TryGetValue("rel", out var rel) || !attributes.TryGetValue("href", out var href)
                || !rel.Contains("icon", StringComparison.OrdinalIgnoreCase)
                || rel.Contains("mask-icon", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var isSvg = href.Split('?')[0].EndsWith(".svg", StringComparison.OrdinalIgnoreCase)
                || (attributes.TryGetValue("type", out var type) && type.Contains("svg", StringComparison.OrdinalIgnoreCase));
            if (isSvg || !Uri.TryCreate(baseUri, href.Trim(), out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            {
                continue;
            }

            var size = attributes.TryGetValue("sizes", out var sizes) ? LargestSize(sizes) : 0;
            var bonus = rel.Contains("apple-touch-icon", StringComparison.OrdinalIgnoreCase) ? 1000 : 0;
            links.Add((uri, bonus + size));
        }

        return links.OrderByDescending(link => link.Score).Select(link => link.Uri);
    }

    /// <summary>Recusa endereços que não sejam <c>https</c> na porta 443 e os que apontam para a própria máquina ou rede local.</summary>
    public static bool IsAllowedAddress(Uri uri)
    {
        if (uri.Scheme != Uri.UriSchemeHttps || uri.Port != 443 || uri.IsLoopback)
        {
            return false;
        }

        var host = uri.DnsSafeHost;
        return IPAddress.TryParse(host, out var address)
            ? IsPublicAddress(address)
            : host.Contains('.') && !host.EndsWith(".local", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Endereço IP da internet (não é loopback, rede privada nem link-local).</summary>
    public static bool IsPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address) || IPAddress.Any.Equals(address) || IPAddress.IPv6Any.Equals(address))
        {
            return false;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return !(address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6UniqueLocal);
        }

        var bytes = address.GetAddressBytes();
        return !(bytes[0] is 0 or 10 or 127
            || bytes[0] == 100 && bytes[1] is >= 64 and <= 127
            || bytes[0] == 169 && bytes[1] == 254
            || bytes[0] == 172 && bytes[1] is >= 16 and <= 31
            || bytes[0] == 192 && bytes[1] == 168);
    }

    /// <summary>
    /// Decodifica a imagem (só formatos de bitmap conhecidos), escolhe o maior quadro (arquivos ICO têm
    /// vários tamanhos), reduz para o tamanho do ícone e regrava como PNG.
    /// </summary>
    public static byte[]? TryNormalize(byte[] bytes)
    {
        if (!HasBitmapSignature(bytes))
        {
            return null;
        }

        try
        {
            using var input = new MemoryStream(bytes, writable: false);
            var decoder = BitmapDecoder.Create(input, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            var frame = decoder.Frames.OrderByDescending(item => item.PixelWidth * item.PixelHeight).FirstOrDefault();
            if (frame is null || frame.PixelWidth < 16 || frame.PixelHeight < 16)
            {
                return null;
            }

            BitmapSource image = frame;
            var target = (double)PackageIconService.IconPixelSize;
            var scale = Math.Min(target / frame.PixelWidth, target / frame.PixelHeight);
            if (scale < 1)
            {
                image = new TransformedBitmap(frame, new ScaleTransform(scale, scale));
            }

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            using var output = new MemoryStream();
            encoder.Save(output);
            return output.ToArray();
        }
        catch (Exception exception) when (exception is NotSupportedException or FileFormatException
            or ArgumentException or InvalidOperationException or IOException or OverflowException
            or System.Runtime.InteropServices.COMException)
        {
            return null;
        }
    }

    private static bool HasBitmapSignature(byte[] bytes)
    {
        ReadOnlySpan<byte> data = bytes;
        return data.StartsWith((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47])           // PNG
            || data.StartsWith((ReadOnlySpan<byte>)[0x00, 0x00, 0x01, 0x00])           // ICO
            || data.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF])                 // JPEG
            || data.StartsWith("GIF8"u8)
            || data.StartsWith("BM"u8)
            || data.Length > 12 && data.StartsWith("RIFF"u8) && data[8..12].SequenceEqual("WEBP"u8);
    }

    private static bool IsGitHubRepository(Uri website) =>
        website.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase)
        && website.AbsolutePath.Trim('/').Length > 0;

    private static int LargestSize(string sizes) =>
        sizes.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(size => size.Split('x', 'X'))
            .Select(parts => parts.Length == 2 && int.TryParse(parts[0], out var width) ? width : 0)
            .DefaultIfEmpty(0)
            .Max();

    private async Task<string?> TryGetTextAsync(Uri uri, CancellationToken cancellationToken)
    {
        var bytes = await TryGetBytesAsync(uri, cancellationToken, MaxPageBytes, allowTruncated: true);
        return bytes is null ? null : Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// Baixa até <paramref name="maxBytes"/>. Imagens maiores são descartadas; da página HTML basta o início,
    /// onde ficam as tags <c>&lt;link&gt;</c> (<paramref name="allowTruncated"/>).
    /// </summary>
    private async Task<byte[]?> TryGetBytesAsync(
        Uri uri,
        CancellationToken cancellationToken,
        int maxBytes = MaxImageBytes,
        bool allowTruncated = false)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(RequestTimeout);
        try
        {
            using var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            var finalUri = response.RequestMessage?.RequestUri ?? uri;
            if (!response.IsSuccessStatusCode || !IsAllowedAddress(finalUri)
                || response.Content.Headers.ContentLength > maxBytes && !allowTruncated)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var buffer = new MemoryStream();
            var chunk = new byte[16 * 1024];
            int read;
            while ((read = await stream.ReadAsync(chunk, timeout.Token)) > 0)
            {
                var remaining = maxBytes - (int)buffer.Length;
                if (read > remaining)
                {
                    if (!allowTruncated)
                    {
                        return null;
                    }

                    buffer.Write(chunk, 0, remaining);
                    break;
                }

                buffer.Write(chunk, 0, read);
            }

            return buffer.ToArray();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null; // tempo limite
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or SocketException)
        {
            return null;
        }
    }

    private static HttpClient CreateDefaultClient()
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5,
            AutomaticDecompression = DecompressionMethods.All,
            UseCookies = false,
            // O nome do site pode apontar para um IP interno; a conexão só é aberta com IPs públicos.
            ConnectCallback = ConnectToPublicAddressAsync
        };
        var client = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("AppUpdater/1.0 (Windows; icon fetcher)");
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html,image/png,image/x-icon,image/*;q=0.8,*/*;q=0.5");
        return client;
    }

    private static async ValueTask<Stream> ConnectToPublicAddressAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken);
        var allowed = addresses.Where(IsPublicAddress).ToArray();
        if (allowed.Length == 0)
        {
            throw new HttpRequestException("O endereço do ícone não é público.");
        }

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(allowed, context.DnsEndPoint.Port, cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    [GeneratedRegex(@"<link\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 500)]
    private static partial Regex LinkTagRegex();

    [GeneratedRegex(@"(?<name>[a-zA-Z-]+)\s*=\s*(?:""(?<value>[^""]*)""|'(?<value>[^']*)'|(?<value>[^\s>""']+))", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 500)]
    private static partial Regex AttributeRegex();
}
