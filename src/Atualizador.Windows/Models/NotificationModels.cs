namespace Atualizador.Windows.Models;

public enum NotificationKind
{
    Informacao,
    Sucesso,
    Erro
}

/// <summary>
/// Aviso exibido na faixa do topo da janela. <see cref="ActionLabel"/> e <see cref="ActionUri"/>
/// são opcionais e criam um botão que abre o link (ex.: página da nova versão).
/// </summary>
public sealed record AppNotification(
    NotificationKind Kind,
    string Title,
    string Message,
    string? ActionLabel = null,
    Uri? ActionUri = null);
