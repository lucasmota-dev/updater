using System.Windows.Input;

namespace Atualizador.Windows.ViewModels;

public sealed class AsyncDelegateCommand(Func<Task> execute, Func<bool>? canExecute = null) : ICommand
{
    private readonly Func<bool> _canExecute = canExecute ?? (() => true);
    private bool _isExecuting;

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !_isExecuting && _canExecute();

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        _isExecuting = true;
        RaiseCanExecuteChanged();
        try
        {
            await execute();
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
