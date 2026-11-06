using System.Collections.Concurrent;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Atualizador.Windows.Services;

/// <summary>
/// Encontra o ícone de um pacote. O WinGet não fornece ícones, então a busca segue esta ordem:
/// <list type="number">
/// <item><b>Windows</b>: se o aplicativo está instalado, usa o ícone registrado por ele
/// (<see cref="InstalledAppIconSource"/>). Rápido e sem internet.</item>
/// <item><b>Cache</b>: ícone já baixado antes, em <c>%LOCALAPPDATA%\Atualizador.Windows\icons</c>
/// (válido por 30 dias; uma busca sem resultado só é repetida após 7 dias).</item>
/// <item><b>Site do aplicativo</b>: lê o site com <c>winget show</c> e baixa o ícone de lá
/// (<see cref="WebIconSource"/>).</item>
/// </list>
/// Sem ícone, a tela mostra a inicial do nome. Uma falha aqui nunca interrompe a página.
/// </summary>
public sealed partial class PackageIconService
{
    /// <summary>Tamanho, em pixels, dos ícones guardados (exibidos a 28 px; sobra para telas com escala de 200%).</summary>
    public const int IconPixelSize = 64;

    /// <summary>Downloads simultâneos. Cada um roda um <c>winget show</c>, então o número é pequeno.</summary>
    private const int MaxConcurrentDownloads = 3;

    private static readonly TimeSpan CacheLifetime = TimeSpan.FromDays(30);
    private static readonly TimeSpan MissRetryInterval = TimeSpan.FromDays(7);

    private readonly IPackageUpdateService _wingetService;
    private readonly InstalledAppIconSource _installedSource;
    private readonly WebIconSource _webSource;
    private readonly string _cacheDirectory;
    private readonly LifoGate _downloadGate = new(MaxConcurrentDownloads);
    private readonly ConcurrentDictionary<string, Task<ImageSource?>> _icons = new(StringComparer.OrdinalIgnoreCase);

    public PackageIconService(
        IPackageUpdateService wingetService,
        InstalledAppIconSource? installedSource = null,
        WebIconSource? webSource = null,
        string? cacheDirectory = null)
    {
        _wingetService = wingetService;
        _installedSource = installedSource ?? new InstalledAppIconSource();
        _webSource = webSource ?? new WebIconSource();
        _cacheDirectory = cacheDirectory ?? AppDataPaths.File("icons");
    }

    /// <summary>
    /// Ícone do pacote, ou <c>null</c> se não houver. Cada ID é buscado uma vez por execução;
    /// chamadas repetidas recebem o mesmo resultado.
    /// </summary>
    public Task<ImageSource?> GetIconAsync(string name, string id) =>
        _icons.GetOrAdd(string.IsNullOrWhiteSpace(id) ? name : id, _ => LoadSafelyAsync(name, id));

    private async Task<ImageSource?> LoadSafelyAsync(string name, string id)
    {
        try
        {
            return await Task.Run(() => LoadAsync(name, id));
        }
#pragma warning disable CA1031 // Ícone é decorativo: qualquer falha inesperada vira "sem ícone", nunca um erro na tela.
        catch (Exception)
#pragma warning restore CA1031
        {
            return null;
        }
    }

    private async Task<ImageSource?> LoadAsync(string name, string id)
    {
        if (_installedSource.TryLoad(name) is { } installedIcon)
        {
            return installedIcon;
        }

        if (!SafeIdRegex().IsMatch(id))
        {
            return null;
        }

        var iconPath = Path.Combine(_cacheDirectory, $"pkg-{id}.png");
        var missPath = Path.Combine(_cacheDirectory, $"pkg-{id}.none");
        if (IsFresh(iconPath, CacheLifetime) && TryDecode(File.ReadAllBytes(iconPath)) is { } cachedIcon)
        {
            return cachedIcon;
        }

        if (IsFresh(missPath, MissRetryInterval))
        {
            return null;
        }

        using (await _downloadGate.EnterAsync())
        {
            var website = await _wingetService.GetWebsiteAsync(id);
            var png = website is null ? null : await _webSource.TryDownloadAsync(website, CancellationToken.None);
            var icon = png is null ? null : TryDecode(png);
            SaveToCache(icon is null ? null : png, iconPath, missPath);
            return icon;
        }
    }

    private void SaveToCache(byte[]? png, string iconPath, string missPath)
    {
        try
        {
            Directory.CreateDirectory(_cacheDirectory);
            if (png is null)
            {
                File.WriteAllBytes(missPath, []);
                return;
            }

            File.WriteAllBytes(iconPath, png);
            File.Delete(missPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Sem cache, o ícone só é buscado de novo na próxima execução.
        }
    }

    private static bool IsFresh(string path, TimeSpan lifetime)
    {
        try
        {
            return File.Exists(path) && DateTime.UtcNow - File.GetLastWriteTimeUtc(path) < lifetime;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static BitmapImage? TryDecode(byte[] png)
    {
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = new MemoryStream(png, writable: false);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception exception) when (exception is NotSupportedException or FileFormatException
            or ArgumentException or InvalidOperationException or IOException
            or System.Runtime.InteropServices.COMException)
        {
            return null;
        }
    }

    /// <summary>Mesmo formato de ID aceito pelo <see cref="WingetService"/>; também serve de nome de arquivo seguro.</summary>
    [GeneratedRegex(@"\A[A-Za-z0-9][A-Za-z0-9._-]*\z", RegexOptions.CultureInvariant)]
    private static partial Regex SafeIdRegex();
}

/// <summary>
/// Limita quantas tarefas rodam ao mesmo tempo e atende primeiro quem pediu por <b>último</b>.
/// Assim, depois de uma nova pesquisa, os ícones dos resultados novos (os que estão na tela)
/// passam na frente dos que ainda esperavam da pesquisa anterior.
/// </summary>
public sealed class LifoGate(int slots)
{
    private readonly Lock _lock = new();
    private readonly Stack<TaskCompletionSource<IDisposable>> _waiting = new();
    private int _available = slots;

    public Task<IDisposable> EnterAsync()
    {
        lock (_lock)
        {
            if (_available > 0)
            {
                _available--;
                return Task.FromResult<IDisposable>(new Releaser(this));
            }

            var waiter = new TaskCompletionSource<IDisposable>(TaskCreationOptions.RunContinuationsAsynchronously);
            _waiting.Push(waiter);
            return waiter.Task;
        }
    }

    private void Release()
    {
        TaskCompletionSource<IDisposable>? next;
        lock (_lock)
        {
            if (!_waiting.TryPop(out next))
            {
                _available++;
                return;
            }
        }

        next.SetResult(new Releaser(this));
    }

    private sealed class Releaser(LifoGate gate) : IDisposable
    {
        private int _released;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                gate.Release();
            }
        }
    }
}
