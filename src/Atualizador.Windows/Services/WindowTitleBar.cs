using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Atualizador.Windows.Services;

/// <summary>
/// Deixa a barra de título da janela (desenhada pelo Windows, não pelo WPF) clara ou escura,
/// acompanhando o tema do app. Sem isso, a barra continua clara mesmo com o tema escuro.
/// Em versões do Windows sem suporte, a chamada é ignorada.
/// </summary>
internal static class WindowTitleBar
{
    // DWMWA_USE_IMMERSIVE_DARK_MODE: Windows 10 20H1 ou mais recente e Windows 11.
    private const int UseImmersiveDarkMode = 20;

    /// <summary>Aplica em uma janela. A janela já precisa ter handle (use em <c>OnSourceInitialized</c>).</summary>
    public static void Apply(Window window, bool isDark)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var value = isDark ? 1 : 0;
        _ = DwmSetWindowAttribute(handle, UseImmersiveDarkMode, ref value, sizeof(int));
    }

    /// <summary>Aplica em todas as janelas abertas (chamado quando o usuário troca o tema).</summary>
    public static void ApplyToAll(bool isDark)
    {
        foreach (Window window in System.Windows.Application.Current.Windows)
        {
            Apply(window, isDark);
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
