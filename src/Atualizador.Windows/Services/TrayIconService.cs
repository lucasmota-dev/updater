using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using Atualizador.Windows.ViewModels;

namespace Atualizador.Windows.Services;

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
            Text = "Atualizador Windows",
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

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (!_isExiting && _minimizeToTray())
        {
            e.Cancel = true;
            _window.Hide();
        }
    }

    private void ExitApplication()
    {
        _isExiting = true;
        _window.Close();
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

    private void OnNotificationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(NotificationService.Current) || _notifications.Current is null)
        {
            return;
        }

        _statusItem.Text = $"Status: {_notifications.Current.Message}";
    }

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
