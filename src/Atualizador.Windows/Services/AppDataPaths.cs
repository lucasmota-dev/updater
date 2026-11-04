using System.IO;

namespace Atualizador.Windows.Services;

/// <summary>
/// Local dos dados do usuário: <c>%LOCALAPPDATA%\Atualizador.Windows</c>.
/// O nome da pasta é mantido mesmo com o app chamado "App Updater", para que quem já usa
/// o aplicativo não perca preferências, exclusões, agendamento e histórico.
/// </summary>
internal static class AppDataPaths
{
    public static string Directory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Atualizador.Windows");

    /// <summary>Caminho completo de um arquivo dentro da pasta de dados.</summary>
    public static string File(string fileName) => Path.Combine(Directory, fileName);
}
