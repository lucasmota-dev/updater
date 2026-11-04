namespace Atualizador.Windows.Models;

/// <summary>
/// Pacote instalado com atualização disponível, como lido da tabela do WinGet.
/// <see cref="IsVersionUnknown"/> = o WinGet não sabe a versão instalada; esses pacotes não são atualizados.
/// </summary>
public sealed record UpdatablePackage(
    string Name,
    string Id,
    string? InstalledVersion,
    string? AvailableVersion,
    string Source,
    bool IsVersionUnknown);
