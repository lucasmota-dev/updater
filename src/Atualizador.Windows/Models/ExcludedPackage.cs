namespace Atualizador.Windows.Models;

/// <summary>Aplicativo que o usuário excluiu das atualizações. O nome é só para exibição.</summary>
public sealed record ExcludedPackage(string Name, string Id, string Source)
{
    /// <summary>Chave de comparação: origem + ID (o mesmo ID pode existir em origens diferentes).</summary>
    public string Identity => ExcludedPackageIdentity.Create(Id, Source);
}

public static class ExcludedPackageIdentity
{
    // \u001F (separador de unidade) não aparece em IDs nem em nomes de origem.
    public static string Create(string id, string source) =>
        $"{source.Trim()}\u001F{id.Trim()}";
}
