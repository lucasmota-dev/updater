using System.Windows;

namespace Atualizador.Windows.Services;

/// <summary>Pergunta "Sim/Não" ao usuário. Os testes usam uma versão falsa que responde sozinha.</summary>
public interface IUserConfirmation
{
    bool Confirm(string title, string message);
}

/// <summary>Confirmação por caixa de mensagem do Windows. "Não" é o botão padrão, por segurança.</summary>
public sealed class MessageBoxUserConfirmation : IUserConfirmation
{
    public bool Confirm(string title, string message) =>
        System.Windows.MessageBox.Show(
            message,
            title,
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No) == MessageBoxResult.Yes;
}
