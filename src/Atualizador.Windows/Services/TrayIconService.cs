using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using Atualizador.Windows.ViewModels;

namespace Atualizador.Windows.Services;

/// <summary>
/// Ícone na área de notificação (bandeja), com menu Abrir / Verificar atualizações / Status / Sair.
/// Usa o <c>NotifyIcon</c> do Windows Forms, porque o WPF não tem um controle equivalente.
/// Quando "Minimizar para a bandeja ao fechar" está ligado, fechar a janela só a esconde;
/// o app termina pelo item Sair.
/// </summary>
public sealed class TrayIconService : IDisposable
{
    private readonly Window _window;
    private readonly UpdatesPageViewModel _updatesPage;
    private readonly NotificationService _notifications;
    private readonly Func<bool> _minimizeToTray;
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _statusItem;
    private bool _isExiting;
    private bool _disposed;

    public TrayIconService(
        Window window,
        UpdatesPageViewModel updatesPage,
        NotificationService notifications,
        Func<bool> minimizeToTray)
    {
        _window = window;
        _updatesPage = updatesPage;
        _notifications = notifications;
        _minimizeToTray = minimizeToTray;

        var icon = TryLoadApplicationIcon() ?? SystemIcons.Application;
        _statusItem = new ToolStripMenuItem("Status: aguardando verificação") { Enabled = false };
        var menu = new ContextMenuStrip();
        menu.Items.Add("Abrir", null, (_, _) => ShowWindow());
        menu.Items.Add("Verificar atualizações", null, (_, _) => _updatesPage.RefreshCommand.Execute(null));
        menu.Items.Add(_statusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Sair", null, (_, _) => ExitApplication());

        _notifyIcon = new NotifyIcon
        {
            Icon = icon,
            Text = "App Updater",
            Visible = true,
            ContextMenuStrip = menu
        };
        _notifyIcon.DoubleClick += (_, _) => ShowWindow();
        _window.Closing += OnWindowClosing;
        _notifications.PropertyChanged += OnNotificationChanged;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _window.Closing -= OnWindowClosing;
        _notifications.PropertyChanged -= OnNotificationChanged;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }

    /// <summary>Transforma "fechar" em "esconder" quando a opção da bandeja está ligada.</summary>
    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (!_isExiting && _minimizeToTray())
        {
            e.Cancel = true;
            _window.Hide();
        }
    }

    /// <summary>
    /// Encerra o app de verdade, ignorando "minimizar para a bandeja ao fechar". Usado pelo item
    /// Sair e pela atualização automática. A janela é escondida primeiro, para sumir na hora.
    /// </summary>
    public void ExitApplication()
    {
        _isExiting = true;
        _window.Hide();
        _window.Close();

        // Com --minimized a janela pode nunca ter sido exibida; nesse caso o Close não encerra o app sozinho.
        Dispose();
        System.Windows.Application.Current.Shutdown();
    }

    private void ShowWindow()
    {
        _window.Show();
        if (_window.WindowState == WindowState.Minimized)
        {
            _window.WindowState = WindowState.Normal;
        }

        _window.Activate();
    }

    /// <summary>Mostra o último aviso do app no item "Status" do menu.</summary>
    private void OnNotificationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(NotificationService.Current) || _notifications.Current is null)
        {
            return;
        }

        _statusItem.Text = $"Status: {_notifications.Current.Message}";
    }

    /// <summary>Usa o ícone embutido no próprio executável.</summary>
    private static Icon? TryLoadApplicationIcon()
    {
        try
        {
            var processPath = Environment.ProcessPath;
            return string.IsNullOrWhiteSpace(processPath)
                ? null
                : Icon.ExtractAssociatedIcon(processPath);
        }
        catch (System.Runtime.InteropServices.ExternalException)
        {
            return null;
        }
    }
}
