using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using BatteryNotifier.Avalonia.Views;
using BatteryNotifier.Core;
using BatteryNotifier.Core.Logger;
using BatteryNotifier.Core.Managers;
using BatteryNotifier.Core.Services;
using Serilog;
using Velopack;
using Velopack.Sources;

namespace BatteryNotifier.Avalonia.Services;

internal sealed class TrayIconService : IDisposable
{
    private readonly ILogger _logger;
    private TrayIcon? _trayIcon;
    private bool _usingNativeMacStatusItem;

    // Tags for the native macOS status-item context menu.
    private const int MenuPause = 1;
    private const int MenuUpdates = 2;
    private const int MenuAbout = 3;
    private const int MenuExit = 4;
    private NotificationManager? _notificationManager;
    private NotificationDisplayService? _displayService;
    private bool _disposed;

    // Store menu items for clean unsubscription in Dispose
    private NativeMenuItem? _pauseNotificationsMenuItem;
    private NativeMenuItem? _aboutMenuItem;
    private NativeMenuItem? _updateMenuItem;
    private NativeMenuItem? _exitMenuItem;

    public TrayIconService()
    {
        _logger = BatteryNotifierAppLogger.ForContext<TrayIconService>();
    }

    public void Initialize()
    {
        try
        {
            // Build the tray context menu — used by the Avalonia tray icon on Windows/Linux
            // and as the macOS fallback if the native status item can't be installed.
            var trayMenu = new NativeMenu();

            // No Show/Hide item — a single click on the tray icon already toggles the window.
            _pauseNotificationsMenuItem = new NativeMenuItem { Header = "Pause Notifications (2h)" };
            _pauseNotificationsMenuItem.Click += OnTogglePauseNotifications;

            _aboutMenuItem = new NativeMenuItem { Header = "About" };
            _aboutMenuItem.Click += OnOpenAbout;

            _updateMenuItem = new NativeMenuItem { Header = "Check for Updates..." };
            _updateMenuItem.Click += OnCheckForUpdates;

            _exitMenuItem = new NativeMenuItem { Header = "Exit" };
            _exitMenuItem.Click += OnExit;

            trayMenu.Add(_pauseNotificationsMenuItem);
            trayMenu.Add(new NativeMenuItemSeparator());
            trayMenu.Add(_updateMenuItem);
            trayMenu.Add(_aboutMenuItem);
            trayMenu.Add(new NativeMenuItemSeparator());
            trayMenu.Add(_exitMenuItem);

            // macOS: install a native NSStatusItem so a single left-click toggles the window and
            // a right-click (or control-click) shows the context menu — Avalonia's cross-platform
            // TrayIcon can't do this on macOS (it forces menu-on-click and never fires Clicked).
            // Falls back to the Avalonia tray icon (menu-driven) if the native item fails.
            _usingNativeMacStatusItem = OperatingSystem.IsMacOS() && TryInstallMacStatusItem();

            if (!_usingNativeMacStatusItem)
            {
                _trayIcon = new TrayIcon { ToolTipText = "BatteryNotifier" };

                try
                {
                    var assetLoader = AssetLoader.Open(AssetUris.LogoIco);
                    _trayIcon.Icon = new WindowIcon(assetLoader);
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Failed to load tray icon from assets");
                }

                _trayIcon.Menu = trayMenu;

                // Left-click toggles the window on Windows/Linux. On macOS the OS shows the menu
                // on click instead (NSStatusItem always shows its menu), so Clicked never fires.
                if (!OperatingSystem.IsMacOS())
                    _trayIcon.Clicked += OnTrayIconClicked;
            }

            // Keep the pause menu label in sync from any source (tray toggle, Resume, auto-resume)
            NotificationService.Instance.PausedChanged += OnPausedStateChanged;

            // Subscribe to battery changes to update icon
            try
            {
                NotificationService.Instance.NotificationReceived += OnNotificationReceived;
                _notificationManager = new NotificationManager(new SoundManager());
                _displayService = new NotificationDisplayService();
                _displayService.SetNotificationManager(_notificationManager);

                // Auto-dismiss the on-screen alert when the charger is plugged/unplugged.
                BatteryMonitorService.Instance.PowerLineStatusChanged += OnPowerLineStatusChanged;
            }
            catch (Exception serviceEx)
            {
                _logger.Warning(serviceEx, "Some battery services could not be initialized on this platform");
            }

            // Start background update checks (if enabled)
            try
            {
                UpdateService.Instance.UpdateAvailable += OnUpdateAvailable;
                if (AppSettings.Instance.AutoCheckForUpdates)
                    UpdateService.Instance.StartBackgroundChecks();
            }
            catch (Exception updateEx)
            {
                _logger.Warning(updateEx, "Update service could not be initialized");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to initialize TrayIcon");
        }
    }



    private static void OnTogglePauseNotifications(object? sender, EventArgs e)
    {
        if (NotificationService.Instance.IsPaused)
            NotificationService.Instance.ResumeNotifications();
        else
            NotificationService.Instance.PauseNotifications();
    }

    private void OnPausedStateChanged(bool paused)
    {
        global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (_pauseNotificationsMenuItem != null)
                _pauseNotificationsMenuItem.Header = paused
                    ? "Resume Notifications"
                    : "Pause Notifications (2h)";
        });
    }

    private void OnNotificationReceived(object? sender, NotificationMessageEventArgs notification)
    {
        _displayService?.DeliverNotification(notification);
    }

    private void OnPowerLineStatusChanged(object? sender, BatteryStatusEventArgs e)
    {
        _displayService?.DismissAll();
    }

    // ── Native macOS status item ──

    private bool TryInstallMacStatusItem()
    {
        try
        {
            return MacStatusItem.Install(
                LoadIconBytes(),
                onLeftClick: () => OnTrayIconClicked(null, EventArgs.Empty),
                menuProvider: BuildMacMenu,
                onMenuItem: HandleMacMenuSelection);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Native macOS status item install threw; using Avalonia tray instead");
            return false;
        }
    }

    private byte[] LoadIconBytes()
    {
        // Monochrome glyph for the macOS menu bar (set as a template image by MacStatusItem).
        try
        {
            using var stream = AssetLoader.Open(AssetUris.MenuBarIconMono);
            using var ms = new System.IO.MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to load macOS menu-bar icon asset");
            return Array.Empty<byte>();
        }
    }

    private static IReadOnlyList<MacMenuItem> BuildMacMenu()
    {
        var paused = NotificationService.Instance.IsPaused;

        return new List<MacMenuItem>
        {
            MacMenuItem.Item(paused ? "Resume Notifications" : "Pause Notifications (2h)", MenuPause),
            MacMenuItem.Separator,
            MacMenuItem.Item("Check for Updates...", MenuUpdates),
            MacMenuItem.Item("About", MenuAbout),
            MacMenuItem.Separator,
            MacMenuItem.Item("Exit", MenuExit),
        };
    }

    private void HandleMacMenuSelection(int tag)
    {
        switch (tag)
        {
            case MenuPause: OnTogglePauseNotifications(null, EventArgs.Empty); break;
            case MenuUpdates: OnCheckForUpdates(null, EventArgs.Empty); break;
            case MenuAbout: OpenAbout(); break;
            case MenuExit: OnExit(null, EventArgs.Empty); break;
        }
    }

    private static void OnTrayIconClicked(object? sender, EventArgs e)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        // Simple toggle: visible → hide, hidden → show. Clicking outside the window is
        // now handled by the window's own flyout-style auto-hide, so the tray click no
        // longer needs an "activate if behind" branch (a visible window is a focused one).
        if (desktop.MainWindow is { IsVisible: true })
            HideMainWindow();
        else
            ShowMainWindow();
    }

    private static void HideMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        if (desktop.MainWindow is MainWindow mainWindow)
        {
            mainWindow.HideToTray();
            return;
        }

        desktop.MainWindow?.Hide();
        MacOSDockIconHelper.HideDockIcon();
        EfficiencyModeService.Instance.EnableEfficiency();
    }

    private static void ShowMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        if (desktop.MainWindow is not MainWindow mainWindow)
            return;

        EfficiencyModeService.Instance.DisableEfficiency();

        mainWindow.Show();
        mainWindow.Activate();
        // Guard against the activation settle immediately re-hiding the window.
        mainWindow.NotifyShown();

        if (mainWindow.WindowState == WindowState.Minimized)
            mainWindow.WindowState = WindowState.Normal;
    }

    private static AboutWindow? _openAboutWindow;

    private static void OnOpenAbout(object? sender, EventArgs e) => OpenAbout();

    /// <summary>Opens the About window (single-instance). Shared by the tray menu and the in-window menu.</summary>
    internal static void OpenAbout()
    {
        if (_openAboutWindow is { } existing)
        {
            existing.Activate();
            return;
        }

        var aboutWindow = new AboutWindow();
        aboutWindow.Closed += (_, _) =>
        {
            _openAboutWindow = null;
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                && desktop.MainWindow is MainWindow mainWindow && mainWindow.IsVisible)
                mainWindow.ScheduleAutoHideCheck();
        };
        _openAboutWindow = aboutWindow;
        aboutWindow.ShowStandalone();
    }

    private static void OnUpdateAvailable(object? sender, UpdateAvailableEventArgs e)
    {
        var safeTag = PlatformHelper.SanitizeExternalText(e.Release.TagName);
        InlineNotificationManager.Instance.Show(
            $"Update available: BatteryNotifier {safeTag}. Click 'Check for Updates' to install.", durationMs: 8000);
    }

    private async void OnCheckForUpdates(object? sender, EventArgs e)
    {
        try
        {
            var mgr = new UpdateManager(new GithubSource(Constants.SourceRepositoryUrl, null, false));

            if (!mgr.IsInstalled)
            {
                // Portable/dev mode — fall back to opening GitHub
                var result = await UpdateService.Instance.CheckForUpdateManualAsync();
                if (result.Status == CheckStatus.UpdateAvailable && result.Release != null)
                    PlatformHelper.OpenUrl(result.Release.HtmlUrl);
                else if (result.Status == CheckStatus.UpToDate)
                    InlineNotificationManager.Instance.Show(
                        $"You're running the latest version ({Constants.ApplicationVersion}).",
                        InlineNotificationLevel.Success);
                else if (result.Status == CheckStatus.Failed)
                    InlineNotificationManager.Instance.Show(
                        "Could not reach GitHub. Check your internet connection.",
                        InlineNotificationLevel.Error);
                return;
            }

            var updateInfo = await mgr.CheckForUpdatesAsync();
            if (updateInfo == null)
            {
                InlineNotificationManager.Instance.Show(
                    $"You're running the latest version ({Constants.ApplicationVersion}).",
                    InlineNotificationLevel.Success);
                return;
            }

            InlineNotificationManager.Instance.Show(
                $"Downloading BatteryNotifier {updateInfo.TargetFullRelease.Version}...",
                InlineNotificationLevel.Info, durationMs: 30000);

            await mgr.DownloadUpdatesAsync(updateInfo);

            InlineNotificationManager.Instance.Show(
                $"BatteryNotifier {updateInfo.TargetFullRelease.Version} downloaded. Restarting...",
                InlineNotificationLevel.Success, durationMs: 5000);

            // Brief delay so user can see the notification
            await Task.Delay(2000);

            mgr.ApplyUpdatesAndRestart(updateInfo.TargetFullRelease);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Update check/download failed");
            InlineNotificationManager.Instance.Show(
                "Update check failed. Check your internet connection.",
                InlineNotificationLevel.Error);
        }
    }

    private static void OnExit(object? sender, EventArgs e)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            NotificationService.Instance.NotificationReceived -= OnNotificationReceived;
            BatteryMonitorService.Instance.PowerLineStatusChanged -= OnPowerLineStatusChanged;
            UpdateService.Instance.UpdateAvailable -= OnUpdateAvailable;
            UpdateService.Instance.Dispose();

            _notificationManager?.Dispose();
            _notificationManager = null;
            _displayService?.DismissAll();
            _displayService = null;

            NotificationService.Instance.PausedChanged -= OnPausedStateChanged;

            // Unsubscribe menu item Click handlers to prevent event leaks
            if (_pauseNotificationsMenuItem != null)
            {
                _pauseNotificationsMenuItem.Click -= OnTogglePauseNotifications;
                _pauseNotificationsMenuItem = null;
            }

            if (_aboutMenuItem != null)
            {
                _aboutMenuItem.Click -= OnOpenAbout;
                _aboutMenuItem = null;
            }

            if (_updateMenuItem != null)
            {
                _updateMenuItem.Click -= OnCheckForUpdates;
                _updateMenuItem = null;
            }

            if (_exitMenuItem != null)
            {
                _exitMenuItem.Click -= OnExit;
                _exitMenuItem = null;
            }
            
            if (_trayIcon != null)
            {
                _trayIcon.Clicked -= OnTrayIconClicked;
                _trayIcon.Dispose();
                _trayIcon = null;
            }

            if (_usingNativeMacStatusItem)
            {
                MacStatusItem.Uninstall();
                _usingNativeMacStatusItem = false;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error disposing TrayIconService");
        }

        _disposed = true;
    }
}