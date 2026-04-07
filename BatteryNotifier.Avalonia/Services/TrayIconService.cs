using System;
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
    private NotificationManager? _notificationManager;
    private NotificationDisplayService? _displayService;
    private IDisposable? _visibilitySubscription;
    private bool _disposed;

    // Store menu items for clean unsubscription in Dispose
    private NativeMenuItem? _pauseNotificationsMenuItem;
    private NativeMenuItem? _showHideMenuItem;
    private NativeMenuItem? _aboutMenuItem;
    private NativeMenuItem? _updateMenuItem;
    private NativeMenuItem? _exitMenuItem;

    public TrayIconService(IDisposable? visibilitySubscription = null)
    {
        _visibilitySubscription = visibilitySubscription;
        _logger = BatteryNotifierAppLogger.ForContext<TrayIconService>();
    }

    public void Initialize()
    {
        try
        {
            _trayIcon = new TrayIcon();

            // Set icon using AssetLoader
            try
            {
                var assetLoader = AssetLoader.Open(AssetUris.LogoIco);
                _trayIcon.Icon = new WindowIcon(assetLoader);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to load tray icon from assets");
            }

            // Set tooltip
            _trayIcon.ToolTipText = "BatteryNotifier";

            // Create menu
             var trayMenu = new NativeMenu();

            _showHideMenuItem = new NativeMenuItem { Header = "Show Window" };
            _showHideMenuItem.Click += OnShowHideWindow;

            _pauseNotificationsMenuItem = new NativeMenuItem { Header = "Pause Notifications (2h)" };
            _pauseNotificationsMenuItem.Click += OnTogglePauseNotifications;

            _aboutMenuItem = new NativeMenuItem { Header = "About" };
            _aboutMenuItem.Click += OnOpenAbout;

            _updateMenuItem = new NativeMenuItem { Header = "Check for Updates..." };
            _updateMenuItem.Click += OnCheckForUpdates;

            _exitMenuItem = new NativeMenuItem { Header = "Exit" };
            _exitMenuItem.Click += OnExit;

            trayMenu.Add(_showHideMenuItem);
            trayMenu.Add(_pauseNotificationsMenuItem);
            trayMenu.Add(new NativeMenuItemSeparator());
            trayMenu.Add(_updateMenuItem);
            trayMenu.Add(_aboutMenuItem);
            trayMenu.Add(new NativeMenuItemSeparator());
            trayMenu.Add(_exitMenuItem);

            _trayIcon.Menu = trayMenu;

            // Sync pause menu label from any source (tray toggle, main window Resume, auto-resume)
            NotificationService.Instance.PausedChanged += OnPausedStateChanged;

            // Handle left-click (Windows/Linux only — macOS routes all clicks to the menu)
            if (!OperatingSystem.IsMacOS())
                _trayIcon.Clicked += OnTrayIconClicked;

            // Subscribe to battery changes to update icon
            try
            {
                NotificationService.Instance.NotificationReceived += OnNotificationReceived;
                _notificationManager = new NotificationManager(new SoundManager());
                _displayService = new NotificationDisplayService();
                _displayService.SetNotificationManager(_notificationManager);
            }
            catch (Exception serviceEx)
            {
                _logger.Warning(serviceEx, "Some battery services could not be initialized on this platform");
            }

            // Subscribe to window visibility changes to keep tray menu label in sync.
            // This catches hides from the in-window menu, the X button, tray toggle, etc.
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } mainWin })
            {
                var visibilitySub = mainWin.GetObservable(Visual.IsVisibleProperty)
                    .Subscribe(_ => UpdateShowHideMenuLabel());
                var activeSub = mainWin.GetObservable(Window.IsActiveProperty)
                    .Subscribe(_ => UpdateShowHideMenuLabel());
                _visibilitySubscription = new CompositeDisposable(visibilitySub, activeSub);
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

    private static void OnTrayIconClicked(object? sender, EventArgs e)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        if (desktop.MainWindow is not { IsVisible: true } mainWindow)
        {
            ShowMainWindow();
            return;
        }

        // Window is visible — if focused, hide it. If behind other apps, just activate.
        if (mainWindow.IsActive)
            HideMainWindow();
        else
            mainWindow.Activate();
    }

    private void OnShowHideWindow(object? sender, EventArgs e)
    {
        // Use _wasVisibleBeforeMenu to determine intent — on macOS, opening the menu
        // deactivates the window, so we can't rely on IsActive at menu-click time.
        if (_wasVisibleBeforeMenu)
            HideMainWindow();
        else
            ShowMainWindow();
    }

    /// <summary>
    /// Tracks whether the window was visible before the tray menu opened.
    /// On macOS, opening the tray menu deactivates the window, making IsActive unreliable.
    /// </summary>
    private bool _wasVisibleBeforeMenu;

    private void UpdateShowHideMenuLabel()
    {
        if (_showHideMenuItem == null) return;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is { } win)
        {
            // Capture visibility state when the menu label updates.
            // On macOS, this fires when IsActive changes (window deactivated by menu open),
            // so we check IsVisible — if visible, the user had the window open before the menu.
            _wasVisibleBeforeMenu = win.IsVisible;
            _showHideMenuItem.Header = win.IsVisible ? "Hide Window" : "Show Window";
        }
        else
        {
            _wasVisibleBeforeMenu = false;
            _showHideMenuItem.Header = "Show Window";
        }
    }

    private static void HideMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

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

        if (mainWindow.WindowState == WindowState.Minimized)
            mainWindow.WindowState = WindowState.Normal;
    }

    private static AboutWindow? _openAboutWindow;

    private static void OnOpenAbout(object? sender, EventArgs e)
    {
        if (_openAboutWindow is { } existing)
        {
            existing.Activate();
            return;
        }

        var aboutWindow = new AboutWindow();
        aboutWindow.Closed += (_, _) => _openAboutWindow = null;
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
            UpdateService.Instance.UpdateAvailable -= OnUpdateAvailable;
            UpdateService.Instance.Dispose();


            _visibilitySubscription?.Dispose();
            _visibilitySubscription = null;


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

            if (_showHideMenuItem != null)
            {
                _showHideMenuItem.Click -= OnShowHideWindow;
                _showHideMenuItem = null;
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
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error disposing TrayIconService");
        }

        _disposed = true;
    }
}