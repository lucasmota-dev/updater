using System.Windows.Input;

namespace Atualizador.Windows.ViewModels;

public sealed class DelegateCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    private readonly Func<bool> _canExecute = canExecute ?? (() => true);

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute();

    public void Execute(object? parameter) => execute();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
