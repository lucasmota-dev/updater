using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Atualizador.Windows.ViewModels;

/// <summary>
/// Base dos ViewModels: implementa <see cref="INotifyPropertyChanged"/>, que é como o WPF fica
/// sabendo que uma propriedade mudou e atualiza a tela (binding).
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Grava o novo valor e avisa a tela, mas só se ele for diferente do atual.
    /// Retorna <c>true</c> quando houve mudança, para o chamador reagir (ex.: reavaliar comandos).
    /// </summary>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>Avisa a tela que uma propriedade calculada (sem campo próprio) mudou.</summary>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
