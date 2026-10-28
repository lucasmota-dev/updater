namespace Atualizador.Windows.Models;

public enum NotificationKind
{
    Informacao,
    Sucesso,
    Erro
}

public sealed record AppNotification(
    NotificationKind Kind,
    string Title,
    string Message,
    string? ActionLabel = null,
    Uri? ActionUri = null);
