using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using Microsoft.Win32;

namespace Atualizador.Windows.Services;

/// <summary>
/// Mantém o App Updater rodando <b>sem</b> permissão de administrador.
/// <para>
/// Se o app for aberto elevado ("Executar como administrador", ou herdando a elevação de outro
/// programa), os instaladores por usuário (Discord, Spotify e muitos outros) se recusam a instalar,
/// e os aplicativos acabam instalados no perfil errado. Por isso, ao abrir elevado, o app se reabre
/// pelo Explorador de Arquivos, que inicia programas sem elevação, e a cópia elevada fecha.
/// </para>
/// <para>
/// Quando a elevação é realmente necessária (Windows Update ou um instalador que exige administrador),
/// só o processo daquela operação é elevado, com o aviso do UAC.
/// </para>
/// </summary>
public static class ElevationGuard
{
    /// <summary>O processo atual está elevado (administrador com UAC aprovado).</summary>
    public static bool IsElevated { get; } = CheckIsElevated();

    /// <summary>
    /// Orientação ao usuário quando o app não conseguiu sair do modo administrador. Com o UAC
    /// desligado, todo programa de um administrador roda elevado, e só religar o UAC resolve.
    /// </summary>
    public static string ElevationAdvice => IsUacEnabled()
        ? "Feche o App Updater e abra-o normalmente, sem \"Executar como administrador\"."
        : "O Controle de Conta de Usuário (UAC) está desligado neste computador, então todo programa roda como administrador. "
          + "Para resolver, religue o UAC (Painel de Controle › Contas de Usuário › Alterar configurações de Controle de Conta de Usuário) e reinicie o computador.";

    /// <summary>
    /// Reabre o app sem elevação, se possível. Retorna <c>true</c> quando a cópia nova foi iniciada
    /// e esta deve encerrar. Não faz nada se o UAC estiver desligado (o Explorador também estaria
    /// elevado e o app entraria em repetição) ou se o Explorador não estiver em execução.
    /// </summary>
    public static bool TryRelaunchUnelevated()
    {
        var executable = Environment.ProcessPath;
        if (!IsElevated
            || IsElevationAllowedByEnvironment()
            || string.IsNullOrEmpty(executable)
            || !IsUacEnabled()
            || Process.GetProcessesByName("explorer").Length == 0)
        {
            return false;
        }

        try
        {
            var explorer = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
            var startInfo = new ProcessStartInfo(explorer) { UseShellExecute = false };
            // O Explorador repassa a abertura para a instância da área de trabalho, que roda sem elevação.
            startInfo.ArgumentList.Add(executable);
            using var _ = Process.Start(startInfo);
            return true;
        }
        catch (Win32Exception)
        {
            return false;
        }
    }

    /// <summary>Nome da variável que permite rodar elevado sem se reabrir.</summary>
    public const string AllowElevatedVariable = "APP_UPDATER_ALLOW_ELEVATED";

    /// <summary>
    /// <c>APP_UPDATER_ALLOW_ELEVATED=1</c> desliga a reabertura. Usado pelo teste de abertura do CI,
    /// cujo executor do GitHub Actions roda como administrador com UAC: sem isso, o app se reabriria
    /// pelo Explorador e o teste veria o processo original encerrar.
    /// </summary>
    private static bool IsElevationAllowedByEnvironment() =>
        Environment.GetEnvironmentVariable(AllowElevatedVariable) == "1";

    private static bool CheckIsElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }

    /// <summary>EnableLUA = 0 significa UAC desligado: todo programa de administrador já nasce elevado.</summary>
    public static bool IsUacEnabled()
    {
        try
        {
            const string key = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System";
            return Registry.GetValue(key, "EnableLUA", 1) is not 0;
        }
        catch (System.Security.SecurityException)
        {
            return true;
        }
    }
}
