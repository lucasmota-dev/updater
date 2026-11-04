using Atualizador.Windows.Models;
using Atualizador.Windows.ViewModels;

namespace Atualizador.Windows.Services;

/// <summary>
/// Aviso atual do aplicativo, exibido na faixa do topo da janela e no item "Status" da bandeja.
/// Só existe um aviso por vez: publicar um novo substitui o anterior.
/// </summary>
public sealed class NotificationService : ViewModelBase
{
    private AppNotification? _current;

    public AppNotification? Current
    {
        get => _current;
        private set
        {
            if (SetProperty(ref _current, value))
            {
                OnPropertyChanged(nameof(HasNotification));
            }
        }
    }

    public bool HasNotification => Current is not null;

    public void Publish(AppNotification notification) => Current = notification;

    public void Clear() => Current = null;
}
