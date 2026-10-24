namespace Atualizador.Windows.Models;

public sealed record UpdatablePackage(
    string Name,
    string Id,
    string? InstalledVersion,
    string? AvailableVersion,
    string Source,
    bool IsVersionUnknown);
