using System.Windows;
using System.Windows.Input;

namespace Atualizador.Windows.Views;

/// <summary>
/// Janela de confirmação no tema do app. Use por <see cref="Services.ThemedUserConfirmation"/>.
/// "Não" recebe o foco e responde a Enter e Esc; só um clique (ou Alt+S) em "Sim" confirma.
/// </summary>
public partial class ConfirmationDialog : Window
{
    public ConfirmationDialog(string title, string message)
    {
        InitializeComponent();
        Title = title;
        TitleText.Text = title;
        MessageText.Text = message;
        Loaded += (_, _) => NoButton.Focus();
    }

    private void OnYesClick(object sender, RoutedEventArgs e) => DialogResult = true;

    /// <summary>Sem barra de título: arrastar o cartão move a janela.</summary>
    private void OnDragAreaMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
}
