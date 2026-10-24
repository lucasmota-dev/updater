using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Atualizador.Windows.Models;

namespace Atualizador.Windows.Services;

public sealed class ExcludedPackagesStore
{
    private const int CurrentVersion = 1;
    private readonly object _sync = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public ExcludedPackagesStore(string? filePath = null)
    {
        FilePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Atualizador.Windows",
            "excluded-packages.json");
    }

    public string FilePath { get; }

    public event EventHandler? Changed;

    public IReadOnlyList<ExcludedPackage> Load()
    {
        lock (_sync)
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    return [];
                }

                var file = JsonSerializer.Deserialize<ExcludedPackagesFile>(
                    File.ReadAllText(FilePath), _jsonOptions);
                return file is { Version: CurrentVersion, Packages: not null }
                    ? file.Packages
                    : [];
            }
            catch (IOException)
            {
                return [];
            }
            catch (JsonException)
            {
                return [];
            }
            catch (UnauthorizedAccessException)
            {
                return [];
            }
        }
    }

    public bool IsExcluded(UpdatablePackage package) => IsExcluded(package.Id, package.Source);

    public bool IsExcluded(string id, string source)
    {
        var identity = ExcludedPackageIdentity.Create(id, source);
        return Load().Any(item => item.Identity.Equals(identity, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<UpdatablePackage> Filter(IEnumerable<UpdatablePackage> packages) =>
        packages.Where(package => !IsExcluded(package)).ToArray();

    public void Add(IEnumerable<UpdatablePackage> packages)
    {
        lock (_sync)
        {
            var excluded = Load().ToDictionary(item => item.Identity, StringComparer.OrdinalIgnoreCase);
            foreach (var package in packages)
            {
                var item = new ExcludedPackage(package.Name, package.Id, package.Source);
                excluded[item.Identity] = item;
            }

            Save(excluded.Values.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase).ToArray());
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Remove(ExcludedPackage package)
    {
        lock (_sync)
        {
            var remaining = Load()
                .Where(item => !item.Identity.Equals(package.Identity, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            Save(remaining);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void Save(IReadOnlyList<ExcludedPackage> packages)
    {
        var directory = Path.GetDirectoryName(FilePath);
        if (directory is null)
        {
            return;
        }

        Directory.CreateDirectory(directory);
        var temporaryPath = $"{FilePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            var file = new ExcludedPackagesFile(CurrentVersion, packages);
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(file, _jsonOptions));
            File.Move(temporaryPath, FilePath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private sealed record ExcludedPackagesFile(int Version, IReadOnlyList<ExcludedPackage> Packages);
}
