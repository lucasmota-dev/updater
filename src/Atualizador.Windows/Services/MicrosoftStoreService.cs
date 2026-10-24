using System.Diagnostics;

namespace Atualizador.Windows.Services;

public sealed class MicrosoftStoreService
{
    public const string UpdatesUri = "ms-windows-store://downloadsandupdates";

    public static bool TryOpenUpdatesPage(out string errorMessage)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = UpdatesUri,
                UseShellExecute = true
            });
            errorMessage = string.Empty;
            return true;
        }
        catch (InvalidOperationException exception)
        {
            errorMessage = exception.Message;
            return false;
        }
    }
}
