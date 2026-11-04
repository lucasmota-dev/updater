using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using Microsoft.Win32;

namespace Atualizador.Windows.Services;

/// <summary>
/// Atualiza o próprio aplicativo: baixa o instalador da release, confere o SHA-256 com o
/// SHA256SUMS.txt da mesma release e executa o instalador por usuário em modo silencioso.
/// Só atua na cópia instalada pelo Inno Setup; cópias portáteis continuam apenas com o aviso.
/// </summary>
public sealed class AppSelfUpdateService(HttpClient? httpClient = null, string? downloadDirectory = null)
{
    // Mesmo AppId de installer/Atualizador.iss. O Inno Setup acrescenta "_is1" ao nome da chave.
    private const string UninstallKey =
        @"Software\Microsoft\Windows\CurrentVersion\Uninstall\{B7B9E7A4-3A09-4DB1-9F5D-0F994D700A01}_is1";

    // Limites contra respostas inesperadamente grandes (o instalador real tem dezenas de MB).
    private const long MaxInstallerBytes = 200L * 1024 * 1024;
    private const int MaxChecksumsBytes = 64 * 1024;

    private readonly HttpClient _httpClient = httpClient ?? CreateDefaultClient();
    private readonly string _downloadDirectory =
        downloadDirectory ?? Path.Combine(Path.GetTempPath(), "Atualizador", "AutoUpdate");

    /// <summary>
    /// Indica se o executável atual é a cópia instalada por usuário (registrada em HKCU).
    /// Instalações antigas por máquina (HKLM, C:\Atualizador) e cópias portáteis retornam false.
    /// </summary>
    public static bool IsRunningInstalledCopy()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(UninstallKey);
            return key?.GetValue("InstallLocation") is string location
                && IsSameDirectory(location, AppContext.BaseDirectory);
        }
        catch (Exception exception) when (exception is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            return false;
        }
    }

    /// <summary>Compara dois caminhos de pasta ignorando maiúsculas e a barra final.</summary>
    public static bool IsSameDirectory(string first, string second)
    {
        try
        {
            return string.Equals(
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(first)),
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(second)),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    /// <summary>Lê a soma SHA-256 de um arquivo no formato "hash  nome" gerado pelo workflow Release.</summary>
    public static bool TryGetChecksum(string checksums, string fileName, out byte[] hash)
    {
        hash = [];
        foreach (var line in checksums.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length != 2
                || !string.Equals(parts[1].TrimStart('*'), fileName, StringComparison.Ordinal)
                || parts[0].Length != 64)
            {
                continue;
            }

            try
            {
                hash = Convert.FromHexString(parts[0]);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// Baixa e valida o instalador. Retorna o caminho do arquivo conferido ou null em qualquer falha.
    /// </summary>
    public async Task<string?> DownloadInstallerAsync(AvailableRelease release, CancellationToken cancellationToken = default)
    {
        if (release.InstallerUrl is null || release.ChecksumsUrl is null)
        {
            return null;
        }

        string? partialPath = null;
        try
        {
            var checksums = await DownloadTextAsync(release.ChecksumsUrl, cancellationToken).ConfigureAwait(false);
            if (checksums is null || !TryGetChecksum(checksums, UpdateCheckService.InstallerAssetName, out var expectedHash))
            {
                return null;
            }

            Directory.CreateDirectory(_downloadDirectory);
            var version = UpdateCheckService.Format(release.Version);
            var finalPath = Path.Combine(_downloadDirectory, $"Atualizador-Setup-{version}.exe");
            partialPath = finalPath + ".partial";

            using var response = await _httpClient
                .GetAsync(release.InstallerUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength > MaxInstallerBytes)
            {
                return null;
            }

            // Grava em ".partial" e calcula o SHA-256 durante o download. O arquivo só recebe o nome
            // final depois de conferido; um download interrompido ou adulterado nunca é executado.
            byte[] actualHash;
            await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
            await using (var target = new FileStream(partialPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            {
                var buffer = new byte[81920];
                long total = 0;
                int read;
                while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    total += read;
                    if (total > MaxInstallerBytes)
                    {
                        return null;
                    }

                    sha256.AppendData(buffer, 0, read);
                    await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                }

                actualHash = sha256.GetHashAndReset();
            }

            if (!CryptographicOperations.FixedTimeEquals(actualHash, expectedHash))
            {
                return null;
            }

            File.Move(partialPath, finalPath, overwrite: true);
            partialPath = null;
            return finalPath;
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or UnauthorizedAccessException
            || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            return null;
        }
        finally
        {
            if (partialPath is not null)
            {
                TryDelete(partialPath);
            }
        }
    }

    /// <summary>
    /// Inicia o instalador por usuário em modo silencioso. /AUTOUPDATE=1 faz o instalador
    /// abrir o aplicativo de novo ao terminar. O chamador deve encerrar o aplicativo em seguida.
    /// </summary>
    public static bool TryStartInstaller(string installerPath)
    {
        var startInfo = new ProcessStartInfo(installerPath)
        {
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(installerPath) ?? string.Empty
        };
        foreach (var argument in new[] { "/VERYSILENT", "/SUPPRESSMSGBOXES", "/NORESTART", "/CLOSEAPPLICATIONS", "/AUTOUPDATE=1" })
        {
            startInfo.ArgumentList.Add(argument);
        }

        try
        {
            using var process = Process.Start(startInfo);
            return process is not null;
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
        {
            return false;
        }
    }

    /// <summary>Baixa um arquivo de texto pequeno (SHA256SUMS.txt). Retorna null se falhar ou passar do limite.</summary>
    private async Task<string?> DownloadTextAsync(Uri url, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        return bytes.Length > MaxChecksumsBytes ? null : System.Text.Encoding.ASCII.GetString(bytes);
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Arquivo temporário; a próxima execução sobrescreve.
        }
    }

    private static HttpClient CreateDefaultClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            string.Create(CultureInfo.InvariantCulture, $"App-Updater/{UpdateCheckService.Format(UpdateCheckService.CurrentVersion)}"));
        return client;
    }
}
