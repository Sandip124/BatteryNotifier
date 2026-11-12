using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using BatteryNotifier.Avalonia.Models;
using BatteryNotifier.Avalonia.Views;
using BatteryNotifier.Core;
using BatteryNotifier.Core.Logger;
using BatteryNotifier.Core.Managers;
using BatteryNotifier.Core.Services;
using Serilog;
using Tmds.DBus.Protocol;
using Velopack;
using Velopack.Sources;

namespace BatteryNotifier.Avalonia.Services;

internal sealed class TrayIconService : IDisposable
{
    // Static: the show/hide/click helpers below are static and log to the same context.
    private static readonly ILogger Logger = BatteryNotifierAppLogger.ForContext<TrayIconService>();
    private TrayIcon? _trayIcon;
    private NativeMenu? _trayMenu;
    private StatusNotifierWatcherMonitor? _watcherMonitor;
    private bool _usingNativeMacStatusItem;

    // Tags for the native macOS status-item context menu.
    private const int MenuResume = 1;
    private const int MenuUpdates = 2;
    private const int MenuAbout = 3;
    private const int MenuExit = 4;
    private const int MenuPauseBase = 10; // pause options use MenuPauseBase + index

    private static readonly IReadOnlyList<PauseOption> PauseOptions = NotificationPauseOptions.All;

    private NotificationManager? _notificationManager;
    private NotificationDisplayService? _displayService;
    private bool _disposed;

    // Store menu items for clean unsubscription in Dispose
    private NativeMenuItem? _pauseNotificationsMenuItem;
    private NativeMenu? _pauseSubmenu;
    private NativeMenuItem? _showWindowMenuItem;
    private NativeMenuItem? _aboutMenuItem;
    private NativeMenuItem? _updateMenuItem;
    private NativeMenuItem? _exitMenuItem;

    public void Initialize()
    {
        try
        {
            // Build the tray context menu — used by the Avalonia tray icon on Windows/Linux
            // and as the macOS fallback if the native status item can't be installed.
            var trayMenu = new NativeMenu();

            // Linux needs an explicit "Open" entry; Windows/macOS do not (the icon click itself
            // reaches the app there). On GNOME the shell swallows a single left-click to open
            // this menu and only forwards a *double*-click to us as Activate, so the menu is the
            // one gesture a user can rely on to get the window back.
            if (OperatingSystem.IsLinux())
            {
                _showWindowMenuItem = new NativeMenuItem { Header = "Open BatteryNotifier" };
                _showWindowMenuItem.Click += OnShowWindowMenuItem;
                trayMenu.Add(_showWindowMenuItem);
                trayMenu.Add(new NativeMenuItemSeparator());
            }

            // No Show/Hide toggle item — the entry above always shows, and clicking the icon
            // itself handles the rest; the menu is otherwise Pause/Updates/About/Exit.
            // "Pause notification for" opens a submenu of durations; toggles to "Resume" when paused.
            _pauseSubmenu = new NativeMenu();
            foreach (var option in PauseOptions)
            {
                var d = option.Duration;
                var optItem = new NativeMenuItem { Header = option.Label };
                optItem.Click += (_, _) => NotificationService.Instance.PauseNotifications(d);
                _pauseSubmenu.Add(optItem);
            }

            _pauseNotificationsMenuItem = new NativeMenuItem();
            UpdatePauseMenuItem(NotificationService.Instance.IsPaused);

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
                _trayMenu = trayMenu;

                // Linux only: Avalonia asks the SNI host to adopt the icon once, when the TrayIcon
                // is constructed, and gives up for good if no host owns the name right then —
                // routine on GNOME, where the host is a shell extension that loads after us at
                // login. So on Linux we defer construction until a host actually exists; every
                // other platform creates it immediately.
                if (OperatingSystem.IsLinux())
                {
                    InstallTrayDBusCrashGuard();
                    _watcherMonitor = new StatusNotifierWatcherMonitor();
                    _watcherMonitor.AvailabilityChanged += OnSniHostAvailabilityChanged;
                    _watcherMonitor.Start();
                }
                else
                {
                    CreateTrayIcon();
                }
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
                Logger.Warning(serviceEx, "Some battery services could not be initialized on this platform");
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
                Logger.Warning(updateEx, "Update service could not be initialized");
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to initialize TrayIcon");
        }
    }



    // ── Tray icon lifecycle ──

    /// <summary>
    /// Keeps a tray-icon D-Bus failure from killing the app. Avalonia registers the icon on an
    /// async continuation and rethrows any failure onto the dispatcher, where nothing handles it
    /// and the process terminates — observed as
    /// <c>org.gnome.gjs.JSError.ValueError: Impossible to register an indicator...</c> when a host
    /// rejects the registration. A flaky desktop shell must not be able to take the app down, so
    /// swallow exactly that case (D-Bus errors raised from Avalonia's tray implementation) and
    /// leave every other exception to the normal crash path.
    /// </summary>
    private static void InstallTrayDBusCrashGuard()
    {
        global::Avalonia.Threading.Dispatcher.UIThread.UnhandledException += (_, e) =>
        {
            if (e.Exception is not DBusException) return;
            if (e.Exception.StackTrace?.Contains("DBusTrayIconImpl", StringComparison.Ordinal) != true) return;

            Logger.Warning(e.Exception,
                "Tray icon registration was rejected by the SNI host; continuing without a tray icon");
            e.Handled = true;
        };
    }

    private void CreateTrayIcon()
    {
        if (_trayMenu == null) return;

        _trayIcon = new TrayIcon { ToolTipText = "BatteryNotifier" };

        try
        {
            using var assetLoader = AssetLoader.Open(AssetUris.LogoIco);
            _trayIcon.Icon = new WindowIcon(assetLoader);
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to load tray icon from assets");
        }

        _trayIcon.Menu = _trayMenu;

        // Left-click toggles the window on Windows/Linux. On macOS the OS shows the menu
        // on click instead (NSStatusItem always shows its menu), so Clicked never fires.
        if (!OperatingSystem.IsMacOS())
            _trayIcon.Clicked += OnTrayIconClicked;
    }

    private void DestroyTrayIcon()
    {
        if (_trayIcon == null) return;

        _trayIcon.Clicked -= OnTrayIconClicked;
        _trayIcon.Menu = null;
        _trayIcon.Dispose();
        _trayIcon = null;
    }

    /// <summary>
    /// Creates the tray icon the first time an SNI host shows up; see
    /// <see cref="StatusNotifierWatcherMonitor"/>.
    ///
    /// Deliberately create-once and never replace. Disposing a live <see cref="TrayIcon"/> to build
    /// a fresh one races on the shared <c>org.kde.StatusNotifierItem-{pid}-0</c> bus name: the old
    /// icon's asynchronous teardown releases the name the new one just claimed, the host then
    /// rejects the registration ("Impossible to register an indicator..."), and Avalonia rethrows
    /// that D-Bus error on the dispatcher, which **terminates the process**. Once an icon exists,
    /// re-registration after a host restart is Avalonia's own business (it watches the name too).
    /// </summary>
    private void OnSniHostAvailabilityChanged(bool available)
    {
        if (!available) return;

        global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (_disposed || _trayIcon != null) return;

            Logger.Information("SNI host appeared — creating tray icon");
            CreateTrayIcon();
        });
    }

    private static void OnResumeNotifications(object? sender, EventArgs e)
        => NotificationService.Instance.ResumeNotifications();

    /// <summary>
    /// Linux tray-menu entry. Always shows and raises — never toggles: opening the panel menu has
    /// already deactivated the flyout, so the window is off screen by the time this fires.
    /// </summary>
    private static void OnShowWindowMenuItem(object? sender, EventArgs e) => ShowMainWindow();

    /// <summary>Paused → single "Resume Notifications"; otherwise → "Pause notification for" submenu.</summary>
    private void UpdatePauseMenuItem(bool paused)
    {
        if (_pauseNotificationsMenuItem == null) return;

        _pauseNotificationsMenuItem.Click -= OnResumeNotifications;
        if (paused)
        {
            _pauseNotificationsMenuItem.Header = "Resume Notifications";
            _pauseNotificationsMenuItem.Menu = null;
            _pauseNotificationsMenuItem.Click += OnResumeNotifications;
        }
        else
        {
            _pauseNotificationsMenuItem.Header = "Pause notification for";
            _pauseNotificationsMenuItem.Menu = _pauseSubmenu;
        }
    }

    private void OnPausedStateChanged(bool paused)
    {
        global::Avalonia.Threading.Dispatcher.UIThread.Post(() => UpdatePauseMenuItem(paused));
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
            Logger.Warning(ex, "Native macOS status item install threw; using Avalonia tray instead");
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
            Logger.Warning(ex, "Failed to load macOS menu-bar icon asset");
            return Array.Empty<byte>();
        }
    }

    private static IReadOnlyList<MacMenuItem> BuildMacMenu()
    {
        var pauseEntry = NotificationService.Instance.IsPaused
            ? MacMenuItem.Item("Resume Notifications", MenuResume)
            : MacMenuItem.Sub("Pause notification for",
                PauseOptions.Select((o, i) => MacMenuItem.Item(o.Label, MenuPauseBase + i)).ToList());

        return new List<MacMenuItem>
        {
            pauseEntry,
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
            case MenuResume: NotificationService.Instance.ResumeNotifications(); break;
            case MenuUpdates: OnCheckForUpdates(null, EventArgs.Empty); break;
            case MenuAbout: OpenAbout(); break;
            case MenuExit: OnExit(null, EventArgs.Empty); break;
            default:
                var index = tag - MenuPauseBase;
                if (index >= 0 && index < PauseOptions.Count)
                    NotificationService.Instance.PauseNotifications(PauseOptions[index].Duration);
                break;
        }
    }

    private static void OnTrayIconClicked(object? sender, EventArgs e)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        if (desktop.MainWindow is not MainWindow mainWindow)
            return;

        Logger.Debug("Tray icon clicked: onScreen={OnScreen} isVisible={IsVisible} state={State}",
            mainWindow.IsOnScreen, mainWindow.IsVisible, mainWindow.WindowState);

        // Linux: the icon is a control-center indicator, so a click *activates* the app — show,
        // raise and focus — never toggles it away. The window there can be on screen but buried
        // behind other windows, where hiding it is never what the click meant. Windows/macOS keep
        // the flyout toggle: the click arrives in-process and a visible window is the focused one.
        //
        // IsOnScreen rather than IsVisible, because a minimized window still reports IsVisible.
        if (mainWindow.IsOnScreen && !OperatingSystem.IsLinux())
            HideMainWindow();
        else
            ShowMainWindow();
    }

    private static void HideMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        if (desktop.MainWindow is MainWindow mainWindow)
            mainWindow.HideToTray();
    }

    internal static void ShowMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        if (desktop.MainWindow is not MainWindow mainWindow)
            return;

        EfficiencyModeService.Instance.DisableEfficiency();

        // Guard against the activation settle immediately re-hiding the window. Set *before*
        // Show(), because on X11 the map can raise Deactivated before this call would return.
        mainWindow.NotifyShown();

        // Restore before showing, via a real unmap/map cycle: X11 window managers (mutter) ignore
        // a plain Minimized → Normal flip on a still-mapped window and leave it stuck with
        // _NET_WM_STATE_HIDDEN.
        //
        // On Linux this must not be conditioned on WindowState. The WM iconifies windows behind
        // Avalonia's back — minimizing from the dock, or a WM shortcut — leaving WindowState
        // reporting Normal for a window that is not on screen, where Show()/Activate() alone do
        // nothing at all. Remapping whenever the window is not already focused covers every case.
        // Never done while a dialog is open: unmapping a parent out from under its modal child
        // would take the dialog with it.
        var needsRemap = OperatingSystem.IsLinux()
            ? !mainWindow.IsActive && mainWindow.OwnedWindows.Count == 0
            : mainWindow.WindowState == WindowState.Minimized;

        if (needsRemap)
        {
            mainWindow.Hide();
            mainWindow.WindowState = WindowState.Normal;
        }

        mainWindow.Show();
        mainWindow.BringToFront();

        Logger.Debug("Show main window: isVisible={IsVisible} state={State} isActive={IsActive}",
            mainWindow.IsVisible, mainWindow.WindowState, mainWindow.IsActive);
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
            Logger.Error(ex, "Update check/download failed");
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
                _pauseNotificationsMenuItem.Click -= OnResumeNotifications;
                _pauseNotificationsMenuItem = null;
            }

            if (_showWindowMenuItem != null)
            {
                _showWindowMenuItem.Click -= OnShowWindowMenuItem;
                _showWindowMenuItem = null;
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
            
            if (_watcherMonitor != null)
            {
                _watcherMonitor.AvailabilityChanged -= OnSniHostAvailabilityChanged;
                _watcherMonitor.Dispose();
                _watcherMonitor = null;
            }

            DestroyTrayIcon();
            _trayMenu = null;

            if (_usingNativeMacStatusItem)
            {
                MacStatusItem.Uninstall();
                _usingNativeMacStatusItem = false;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error disposing TrayIconService");
        }

        _disposed = true;
    }
}