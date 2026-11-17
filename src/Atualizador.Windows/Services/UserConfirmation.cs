using System.Windows;
using Atualizador.Windows.Views;

namespace Atualizador.Windows.Services;

/// <summary>Pergunta "Sim/Não" ao usuário. Os testes usam uma versão falsa que responde sozinha.</summary>
public interface IUserConfirmation
{
    bool Confirm(string title, string message);
}

/// <summary>
/// Confirmação em uma janela com o tema do app (cores, cor de destaque e botões).
/// "Não" é o botão padrão, por segurança. Fica centralizada sobre a janela ativa do app, quando houver.
/// </summary>
public sealed class ThemedUserConfirmation : IUserConfirmation
{
    public bool Confirm(string title, string message)
    {
        var dialog = new ConfirmationDialog(title, message);
        var owner = FindOwner();
        if (owner is null)
        {
            // Janela principal oculta (ex.: só na bandeja): a confirmação precisa aparecer sozinha.
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dialog.ShowInTaskbar = true;
        }
        else
        {
            dialog.Owner = owner;
        }

        return dialog.ShowDialog() == true;
    }

    private static Window? FindOwner()
    {
        var windows = System.Windows.Application.Current?.Windows.OfType<Window>().Where(w => w.IsVisible).ToArray();
        return windows?.FirstOrDefault(w => w.IsActive) ?? windows?.FirstOrDefault();
    }
}
