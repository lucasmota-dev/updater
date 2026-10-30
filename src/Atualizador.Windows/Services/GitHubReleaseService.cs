using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Atualizador.Windows.Services
{
    public record ReleaseInfo(string Tag, string AssetUrl, string Sha256);

    public interface IGitHubReleaseService
    {
        Task<ReleaseInfo?> GetLatestReleaseAsync(string owner, string repo, string assetName);
    }

    public sealed class GitHubReleaseService : IGitHubReleaseService
    {
        private static readonly HttpClient _client = new HttpClient();

        static GitHubReleaseService()
        {
            // GitHub requer um User‑Agent
            _client.DefaultRequestHeaders.Add("User-Agent", "Atualizador.Windows");
        }

        public async Task<ReleaseInfo?> GetLatestReleaseAsync(string owner, string repo, string assetName)
        {
            var url = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
            using var response = await _client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;

            var tag = root.GetProperty("tag_name").GetString() ?? string.Empty;
            ReleaseInfo? result = null;

            foreach (var asset in root.GetProperty("assets").EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString() ?? string.Empty;
                if (!name.Equals(assetName, StringComparison.OrdinalIgnoreCase)) continue;

                var downloadUrl = asset.GetProperty("browser_download_url").GetString()!;
                // opcional: o campo label pode conter o hash SHA‑256
                var sha256 = string.Empty;
                if (asset.TryGetProperty("label", out var label) && !string.IsNullOrWhiteSpace(label.GetString()))
                    sha256 = label.GetString()!;

                result = new ReleaseInfo(tag, downloadUrl, sha256);
                break;
            }
            return result;
        }
    }
}

