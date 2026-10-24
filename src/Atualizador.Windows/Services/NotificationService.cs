using System.ComponentModel;
using System.Runtime.CompilerServices;
using Atualizador.Windows.Models;

namespace Atualizador.Windows.Services;

public sealed class NotificationService : INotifyPropertyChanged
{
    private AppNotification? _current;

    public event PropertyChangedEventHandler? PropertyChanged;

    public AppNotification? Current
    {
        get => _current;
        private set => SetProperty(ref _current, value);
    }

    public bool HasNotification => Current is not null;

    public void Publish(AppNotification notification)
    {
        Current = notification;
        OnPropertyChanged(nameof(HasNotification));
    }

    public void Clear()
    {
        Current = null;
        OnPropertyChanged(nameof(HasNotification));
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
