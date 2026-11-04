using System.Windows.Input;

namespace Atualizador.Windows.ViewModels;

/// <summary>
/// Comando de botão que chama um método do ViewModel. <paramref name="canExecute"/> decide se o
/// botão fica habilitado; chame <see cref="RaiseCanExecuteChanged"/> quando essa condição mudar.
/// </summary>
public sealed class DelegateCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    private readonly Func<bool> _canExecute = canExecute ?? (() => true);

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute();

    public void Execute(object? parameter) => execute();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>Variante que recebe o <c>CommandParameter</c> do XAML (ex.: o item da lista clicado).</summary>
public sealed class DelegateCommand<T>(Action<T?> execute, Func<T?, bool>? canExecute = null) : ICommand
{
    private readonly Func<T?, bool> _canExecute = canExecute ?? (_ => true);

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute(parameter is T value ? value : default);

    public void Execute(object? parameter)
    {
        if (parameter is T value)
        {
            execute(value);
        }
        else if (parameter is null && default(T) is null)
        {
            execute(default);
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
