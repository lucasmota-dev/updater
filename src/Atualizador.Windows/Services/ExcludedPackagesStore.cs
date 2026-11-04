using Atualizador.Windows.Models;

namespace Atualizador.Windows.Services;

/// <summary>
/// Lista de aplicativos que o usuário excluiu das atualizações (<c>excluded-packages.json</c>).
/// Um pacote é identificado pelo par ID + origem, nunca apenas pelo nome exibido.
/// A lista vale para a tela de atualizações e para a execução agendada.
/// </summary>
public sealed class ExcludedPackagesStore(string? filePath = null)
{
    private const int CurrentVersion = 1;
    private readonly object _sync = new();

    public string FilePath { get; } = filePath ?? AppDataPaths.File("excluded-packages.json");

    /// <summary>Disparado depois de qualquer inclusão ou remoção, para a tela de Configurações recarregar a lista.</summary>
    public event EventHandler? Changed;

    public IReadOnlyList<ExcludedPackage> Load()
    {
        lock (_sync)
        {
            return JsonFile.TryRead<ExcludedPackagesFile>(FilePath) is { Version: CurrentVersion, Packages: { } packages }
                ? packages
                : [];
        }
    }

    public bool IsExcluded(UpdatablePackage package) => IsExcluded(package.Id, package.Source);

    public bool IsExcluded(string id, string source)
    {
        var identity = ExcludedPackageIdentity.Create(id, source);
        return Load().Any(item => item.Identity.Equals(identity, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Remove da sequência os pacotes excluídos. Lê o arquivo uma única vez.</summary>
    public IReadOnlyList<UpdatablePackage> Filter(IEnumerable<UpdatablePackage> packages)
    {
        var excluded = Load().Select(item => item.Identity).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return packages
            .Where(package => !excluded.Contains(ExcludedPackageIdentity.Create(package.Id, package.Source)))
            .ToArray();
    }

    /// <summary>Adiciona os pacotes à lista. Pacotes já excluídos são atualizados, não duplicados.</summary>
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
            Save(Load()
                .Where(item => !item.Identity.Equals(package.Identity, StringComparison.OrdinalIgnoreCase))
                .ToArray());
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void Save(IReadOnlyList<ExcludedPackage> packages) =>
        JsonFile.TryWrite(FilePath, new ExcludedPackagesFile(CurrentVersion, packages));

    private sealed record ExcludedPackagesFile(int Version, IReadOnlyList<ExcludedPackage>? Packages);
}
