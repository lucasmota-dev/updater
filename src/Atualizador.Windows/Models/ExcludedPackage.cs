namespace Atualizador.Windows.Models;

public sealed record ExcludedPackage(string Name, string Id, string Source)
{
    public string Identity => ExcludedPackageIdentity.Create(Id, Source);
}

public static class ExcludedPackageIdentity
{
    public static string Create(string id, string source) =>
        $"{source.Trim()}\u001F{id.Trim()}";
}
