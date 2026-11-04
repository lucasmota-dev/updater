using System.Diagnostics;

namespace Atualizador.Windows.ViewModels;

/// <summary>
/// Página Sobre: o que o app faz, como funciona, quem criou, links do projeto e a atualização
/// do próprio App Updater (versão atual, busca automática e o botão "Buscar nova versão").
/// </summary>
public sealed class AboutPageViewModel : PageViewModel
{
    // Únicos endereços que a página abre; nenhum vem de dados externos.
    private static readonly Dictionary<string, Uri> Links = new(StringComparer.Ordinal)
    {
        ["repository"] = new("https://github.com/lucasmota-dev/updater"),
        ["releases"] = new("https://github.com/lucasmota-dev/updater/releases"),
        ["issues"] = new("https://github.com/lucasmota-dev/updater/issues"),
        ["privacy"] = new("https://github.com/lucasmota-dev/updater#privacidade"),
        ["license"] = new("https://github.com/lucasmota-dev/updater/blob/main/LICENSE"),
        ["author"] = new("https://github.com/lucasmota-dev")
    };

    public AboutPageViewModel(AppUpdateViewModel appUpdate)
        : base("Sobre", "O App Updater, como ele funciona e quem o criou.", "")
    {
        AppUpdate = appUpdate;
        OpenLinkCommand = new DelegateCommand<string>(OpenLink);
    }

    /// <summary>Versão atual, busca automática e botão "Buscar nova versão".</summary>
    public AppUpdateViewModel AppUpdate { get; }

    /// <summary>Abre um dos links fixos da página. O parâmetro é a chave do link (ex.: "repository").</summary>
    public DelegateCommand<string> OpenLinkCommand { get; }

    private static void OpenLink(string? key)
    {
        if (key is not null && Links.TryGetValue(key, out var uri))
        {
            using var _ = Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }
    }
}
