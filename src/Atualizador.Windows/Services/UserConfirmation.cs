using System.Windows;

namespace Atualizador.Windows.Services;

public interface IUserConfirmation
{
    bool Confirm(string title, string message);
}

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
