using System.Windows;
using System.Windows.Input;
using TextBox = System.Windows.Controls.TextBox;

namespace Atualizador.Windows.Behaviors;

/// <summary>
/// Comandos extras para campos de texto. "Clear" apaga o texto do campo (usado pelo botão ✕
/// do SettingsTextBoxStyle) e devolve o foco a ele, para o usuário digitar de novo.
/// </summary>
public static class TextBoxCommands
{
    public static readonly RoutedUICommand Clear = new("Limpar", nameof(Clear), typeof(TextBoxCommands));

    static TextBoxCommands()
    {
        CommandManager.RegisterClassCommandBinding(
            typeof(TextBox),
            new CommandBinding(Clear, OnClearExecuted, OnClearCanExecute));
    }

    private static void OnClearCanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            e.CanExecute = textBox.IsEnabled && !textBox.IsReadOnly && textBox.Text.Length > 0;
            e.Handled = true;
        }
    }

    private static void OnClearExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            return;
        }

        // SelectAll + SelectedText mantém o Desfazer (Ctrl+Z) funcionando, ao contrário de Clear().
        textBox.SelectAll();
        textBox.SelectedText = string.Empty;
        textBox.Focus();
        e.Handled = true;
    }
}
