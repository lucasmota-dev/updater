namespace Atualizador.Windows.Models;

/// <summary>
/// Item da lista "Cor de destaque" em Configurações.
/// <paramref name="IsSystem"/> = segue a cor do Windows; <paramref name="IsCustom"/> = usa o campo hexadecimal.
/// </summary>
public sealed record AccentOption(string Name, string Hex, bool IsCustom = false, bool IsSystem = false);
