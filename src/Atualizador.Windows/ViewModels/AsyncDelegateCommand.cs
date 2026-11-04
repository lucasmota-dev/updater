using System.Windows.Input;

namespace Atualizador.Windows.ViewModels;

/// <summary>
/// Comando para métodos assíncronos. Enquanto a tarefa roda, o botão fica desabilitado,
/// o que impede cliques repetidos de iniciar a mesma operação duas vezes.
/// </summary>
public sealed class AsyncDelegateCommand(Func<Task> execute, Func<bool>? canExecute = null) : ICommand
{
    private readonly Func<bool> _canExecute = canExecute ?? (() => true);
    private bool _isExecuting;

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !_isExecuting && _canExecute();

    // ICommand.Execute é void; "async void" é aceitável aqui porque é um ponto de entrada da interface.
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
