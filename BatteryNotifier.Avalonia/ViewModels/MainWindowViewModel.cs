using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using BatteryNotifier.Avalonia.Models;
using BatteryNotifier.Core;
using BatteryNotifier.Core.Logger;
using BatteryNotifier.Core.Managers;
using BatteryNotifier.Core.Models;
using BatteryNotifier.Core.Services;
using BatteryNotifier.Core.Store;
using BatteryNotifier.Core.Utils;
using BatteryNotifier.Avalonia.Utils;
using ReactiveUI;
using Serilog;

namespace BatteryNotifier.Avalonia.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase, IDisposable
{
    private static readonly ILogger Logger = BatteryNotifierAppLogger.ForContext(nameof(MainWindowViewModel));

    private const string FullBatteryAsset = "FullBattery.png";
    private const string SufficientAsset = "Sufficient.png";
    private const string LowBatteryAsset = "LowBattery.png";
    
    private bool _disposed;
    private bool _isWindowVisible;
    private bool _pendingRefresh;
    private CancellationTokenSource? _phraseCts;
    private bool _accessibilityChecked;
    private CancellationTokenSource? _dndCts;
    private CancellationTokenSource? _navigateCts;
    private CancellationTokenSource? _updateCheckCts;

    public MainWindowViewModel()
    {
        NavigateToSettingsCommand = ReactiveCommand.Create(NavigateToSettings);
        HideWindowCommand = ReactiveCommand.Create(HideWindow);
        CheckForUpdatesCommand = ReactiveCommand.CreateFromTask(CheckForUpdates);
        ExitCommand = ReactiveCommand.Create(ExitApplication);
        DismissInlineNotificationCommand = ReactiveCommand.Create(DismissInlineNotification);
        OpenPauseSheetCommand = ReactiveCommand.Create(() => { IsPauseSheetOpen = true; });

        _inlineNotifications.StateChanged += OnInlineNotificationStateChanged;

        SystemStateDetector.InitializeFocusMonitor();
        
        try
        {
            BatteryMonitorService.Instance.BatteryStatusChanged += OnBatteryStatusChanged;
            BatteryMonitorService.Instance.PowerLineStatusChanged += OnPowerLineStatusChanged;
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Battery monitoring unavailable on this platform — status updates disabled");
        }

        BatteryHealthService.Instance.HealthUpdated += OnHealthUpdated;

        NotificationService.Instance.PausedChanged += OnPausedChanged;

        _ = BatteryHistoryService.Instance;

        RefreshBatteryStatus();
    }
    
    /// <summary>
    /// Controls whether UI updates are processed or deferred.
    /// </summary>
    public void OnWindowVisibilityChanged(bool isVisible)
    {
        _isWindowVisible = isVisible;

        if (isVisible)
        {
            try { BatteryMonitorService.Instance.ForceCheck(); }
            catch (Exception ex) { Logger.Debug(ex, "ForceCheck failed — battery monitoring unavailable"); }

            if (_pendingRefresh)
            {
                _pendingRefresh = false;
            }
            RefreshBatteryStatus();

            RefreshDndStatus();
            StartDndMonitor();

            CheckAccessibilityPermission();

            StatusMessage = BatteryPhrases.StatusMessage(BatteryManagerStore.Instance.BatteryState, BatteryManagerStore.Instance.IsCharging);
            StartPhraseCycling();
        }
        else
        {
            StopDndMonitor();
            StopPhraseCycling();
            StatusMessage = string.Empty;
        }

        UpdatePauseCountdown();
    }

    private void StartPhraseCycling()
    {
        StopPhraseCycling();
        _phraseCts = new CancellationTokenSource();
        _ = RunPhraseCycleAsync(_phraseCts.Token);
    }

    private void StopPhraseCycling()
    {
        _phraseCts?.Cancel();
        _phraseCts?.Dispose();
        _phraseCts = null;
    }

    private async Task RunPhraseCycleAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(5000, ct).ConfigureAwait(false);
            Dispatcher.UIThread.Post(() => StatusMessage = string.Empty);

            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(2));
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                Dispatcher.UIThread.Post(RefreshTimeRemainingPhrase);
            }
        }
        catch (OperationCanceledException ex)
        {
            Logger.Verbose(ex, "Phrase cycle stopped.");
        }
    }

    /// <summary>Real estimate when available, otherwise a playful placeholder phrase.</summary>
    private void RefreshTimeRemainingPhrase()
    {
        var store = BatteryManagerStore.Instance;
        TimeRemaining = store.BatteryLifeRemaining > 0
            ? BatteryStatusText.FormatTimeRemaining(store)
            : BatteryPhrases.BatteryPhrase(IsCharging);
    }

    private void OnBatteryStatusChanged(object? sender, BatteryStatusEventArgs e)
    {
        if (_isWindowVisible)
            Dispatcher.UIThread.Post(RefreshBatteryStatus);
        else
            _pendingRefresh = true;
    }

    private void OnPowerLineStatusChanged(object? sender, BatteryStatusEventArgs e)
    {
        if (_isWindowVisible)
            Dispatcher.UIThread.Post(RefreshBatteryStatus);
        else
            _pendingRefresh = true;
    }

    private void OnHealthUpdated(object? sender, BatteryHealthInfo info)
    {
        Dispatcher.UIThread.Post(() =>
        {
            this.RaisePropertyChanged(nameof(HealthSummary));
            this.RaisePropertyChanged(nameof(HealthAccentColor));
            this.RaisePropertyChanged(nameof(HealthIcon));
        });
    }

    private void RefreshBatteryStatus()
    {
        var store = BatteryManagerStore.Instance;

        BatteryPercentage = store.BatteryLifePercent;
        IsCharging = store.IsCharging || store.IsPluggedIn;

        BatteryStatus = BatteryStatusText.StatusLabel(store);

        TimeRemaining = store.BatteryLifeRemaining > 0
            ? BatteryStatusText.FormatTimeRemaining(store)
            : BatteryPhrases.BatteryPhrase(IsCharging);

        StatusLine = BatteryStatusText.BuildStatusLine(store);

        var assetName = store.BatteryState switch
        {
            BatteryState.Full or BatteryState.Adequate => FullBatteryAsset,
            BatteryState.Sufficient => SufficientAsset,
            BatteryState.Low or BatteryState.Critical => LowBatteryAsset,
            _ => SufficientAsset
        };

        BatteryImage = ResourceHelper.LoadBitmap(assetName);
    }

    // ── Properties ───────────────────────────────────────────────

    public double BatteryPercentage
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool IsCharging
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string BatteryStatus
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    public string StatusLine
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    public Bitmap? BatteryImage
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string TimeRemaining
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    /// <summary>Pause durations offered on the home screen (shared with the tray menu).</summary>
    public static IReadOnlyList<PauseOption> PauseOptions => NotificationPauseOptions.All;

    public ReactiveCommand<TimeSpan?, Unit> PauseNotificationsCommand { get; } =
        ReactiveCommand.Create<TimeSpan?>(duration => NotificationService.Instance.PauseNotifications(duration));

    public bool IsPauseSheetOpen
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public ReactiveCommand<Unit, Unit> OpenPauseSheetCommand { get; }

    // ── Health Dashboard ────────────────────────────────────────

    public HealthDashboardViewModel HealthDashboard { get; } = new();

    public bool IsHealthSheetOpen
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public static string HealthSummary
    {
        get
        {
            var health = BatteryHealthService.Instance.LatestHealth;
            if (health == null) return "Checking...";
            return health.HealthStatus switch
            {
                MetricStatus.Good => "Healthy",
                MetricStatus.Fair => "Fair",
                MetricStatus.Poor => "Service Recommended",
                _ => "Unavailable"
            };
        }
    }

    public static global::Avalonia.Media.Geometry? HealthIcon
    {
        get
        {
            var health = BatteryHealthService.Instance.LatestHealth;
            if (health == null) return ResourceHelper.ResolveGeometry("Icon.Spinner");
            return health.HealthStatus switch
            {
                MetricStatus.Good => ResourceHelper.ResolveGeometry("Icon.CheckFat"),
                MetricStatus.Fair => ResourceHelper.ResolveGeometry("Icon.HeartFill"),
                MetricStatus.Poor => ResourceHelper.ResolveGeometry("Icon.ExclamationMarkFill"),
                _ => ResourceHelper.ResolveGeometry("Icon.HeartFill")
            };
        }
    }

    public static string HealthAccentColor =>
        HealthColors.ForHealth(BatteryHealthService.Instance.LatestHealth?.HealthStatus ?? MetricStatus.Unavailable);

    public SettingsViewModel? CurrentView
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public ReactiveCommand<Unit, Unit> NavigateToSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> HideWindowCommand { get; }
    public ReactiveCommand<Unit, Unit> CheckForUpdatesCommand { get; }
    public ReactiveCommand<Unit, Unit> ExitCommand { get; }
    public ReactiveCommand<Unit, Unit> DismissInlineNotificationCommand { get; }

    public static string Version => Constants.ApplicationVersion;

    // ── DND status ───────────────────────────────────────────────

    public bool IsDndActive
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string DndMessage
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    public bool IsNotificationsPaused
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>Show pause banner only when paused AND DND is not active (DND already covers it).</summary>
    public bool ShowPausedBanner => IsNotificationsPaused && !IsDndActive;

    /// <summary>Banner text with a live countdown of the remaining pause time.</summary>
    public static string PausedBannerText
    {
        get
        {
            if (NotificationService.Instance.PauseResumesAt is not { } resumesAt)
                return "Notifications paused · until you turn it back on";

            var remaining = resumesAt - DateTime.UtcNow;
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;
            return $"Notifications paused · {TimeFormat.Countdown(remaining)} remaining";
        }
    }

    public ReactiveCommand<Unit, Unit> ResumeNotificationsCommand { get; } =
        ReactiveCommand.Create(() => NotificationService.Instance.ResumeNotifications());

    private void OnPausedChanged(bool paused)
    {
        Dispatcher.UIThread.Post(() =>
        {
            IsNotificationsPaused = paused;
            this.RaisePropertyChanged(nameof(ShowPausedBanner));
            this.RaisePropertyChanged(nameof(PausedBannerText));
            UpdatePauseCountdown();
        });
    }

    private IDisposable? _pauseCountdown;

    /// <summary>Ticks the banner countdown once a second, but only while the window is visible and
    /// a timed pause is active. Starts/stops itself as those conditions change.</summary>
    private void UpdatePauseCountdown()
    {
        var svc = NotificationService.Instance;
        var active = _isWindowVisible && svc.IsPaused && svc.PauseDuration is { };

        if (active && _pauseCountdown == null)
        {
            _pauseCountdown = DispatcherTimer.Run(TickPauseCountdown, TimeSpan.FromSeconds(1));
        }
        else if (!active)
        {
            _pauseCountdown?.Dispose();
            _pauseCountdown = null;
        }
    }

    private bool TickPauseCountdown()
    {
        var svc = NotificationService.Instance;

        if (svc.PauseResumesAt is { } resumesAt && DateTime.UtcNow >= resumesAt)
        {
            svc.ResumeNotifications(); 
            return false;
        }

        this.RaisePropertyChanged(nameof(PausedBannerText));
        return true;
    }

    private void CheckAccessibilityPermission()
    {
        if (_accessibilityChecked || !OperatingSystem.IsMacOS())
            return;
        _accessibilityChecked = true;

        if (!SystemStateDetector.HasAccessibilityPermission())
        {
            ShowInlineNotification(
                "Accessibility permission needed for Do Not Disturb detection. Opening Settings...",
                InlineNotificationLevel.Warning, durationMs: 6000);
            SystemStateDetector.OpenAccessibilitySettings();
        }
    }

    private void StartDndMonitor()
    {
        StopDndMonitor();
        _dndCts = new CancellationTokenSource();
        _ = RunDndMonitorAsync(_dndCts.Token);
    }

    private void StopDndMonitor()
    {
        _dndCts?.Cancel();
        _dndCts?.Dispose();
        _dndCts = null;
    }

    /// <summary>
    /// Monitors DND state changes while the window is visible.
    /// </summary>
    private async Task RunDndMonitorAsync(CancellationToken ct)
    {
        try
        {
            var tickCount = 0;
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                tickCount++;

                // Fast path: Darwin notify fires instantly on pre-Tahoe macOS
                if (SystemStateDetector.HasPendingFocusChange())
                {
                    RefreshDndStatus(ct);
                    tickCount = 0;
                    continue;
                }

                // Slow path: direct poll every 5s for Tahoe+ and non-macOS
                if (tickCount >= 5)
                {
                    RefreshDndStatus(ct);
                    tickCount = 0;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Window was hidden — expected
        }
    }

    /// <summary>
    /// Recomputes DND/fullscreen suppression off the UI thread.
    /// </summary>
    private void RefreshDndStatus(CancellationToken ct = default)
    {
        Task.Run(() =>
        {
            bool active;
            try
            {
                active = SystemStateDetector.GetSuppressionState().ShouldSuppressToast;
            }
            catch
            {
                active = false;
            }

            Dispatcher.UIThread.Post(() => ApplyDndState(active));
        }, ct);
    }

    private void ApplyDndState(bool active)
    {
        if (active == IsDndActive) return;

        IsDndActive = active;
        DndMessage = active ? BatteryPhrases.DndMessage() : string.Empty;
        this.RaisePropertyChanged(nameof(ShowPausedBanner));
    }

    public string StatusMessage
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;
    
    
    private readonly InlineNotificationManager _inlineNotifications = InlineNotificationManager.Instance;

    public string InlineNotificationMessage => _inlineNotifications.Message;
    public bool IsInlineNotificationVisible => _inlineNotifications.IsVisible;
    public InlineNotificationLevel InlineNotificationLevel => _inlineNotifications.Level;

    public bool IsInlineSuccess => _inlineNotifications.Level == InlineNotificationLevel.Success;
    public bool IsInlineWarning => _inlineNotifications.Level == InlineNotificationLevel.Warning;
    public bool IsInlineError => _inlineNotifications.Level == InlineNotificationLevel.Error;

    public void ShowInlineNotification(string message, InlineNotificationLevel level = InlineNotificationLevel.Info, int durationMs = 3000)
        => _inlineNotifications.Show(message, level, durationMs);

    public void DismissInlineNotification()
        => _inlineNotifications.Dismiss();

    private void OnInlineNotificationStateChanged()
    {
        Dispatcher.UIThread.Post(() =>
        {
            this.RaisePropertyChanged(nameof(InlineNotificationMessage));
            this.RaisePropertyChanged(nameof(IsInlineNotificationVisible));
            this.RaisePropertyChanged(nameof(InlineNotificationLevel));
            this.RaisePropertyChanged(nameof(IsInlineSuccess));
            this.RaisePropertyChanged(nameof(IsInlineWarning));
            this.RaisePropertyChanged(nameof(IsInlineError));
        });
    }

    // ── Commands ─────────────────────────────────────────────────

    private static void HideWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow?.Hide();
            Services.MacOSDockIconHelper.HideDockIcon();
        }
    }

    private static void ExitApplication()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    private async Task CheckForUpdates()
    {
        _updateCheckCts?.Cancel();
        _updateCheckCts?.Dispose();
        _updateCheckCts = new CancellationTokenSource();
        var ct = _updateCheckCts.Token;

        try
        {
            var result = await UpdateService.Instance.CheckForUpdateManualAsync(ct).ConfigureAwait(false);

            Dispatcher.UIThread.Post(() =>
            {
                switch (result.Status)
                {
                    case CheckStatus.UpdateAvailable when result.Release != null:
                        Services.PlatformHelper.OpenUrl(result.Release.HtmlUrl);
                        break;

                    case CheckStatus.UpToDate:
                        ShowInlineNotification(
                            $"You're running the latest version (v{Constants.ApplicationVersion}).",
                            InlineNotificationLevel.Success);
                        break;

                    case CheckStatus.Failed:
                        ShowInlineNotification(
                            "Could not reach GitHub. Check your internet connection.",
                            InlineNotificationLevel.Error);
                        break;
                }
            });
        }
        catch (OperationCanceledException ex)
        {
            Logger.Verbose(ex, "Update check cancelled.");
        }
        catch (Exception)
        {
            Dispatcher.UIThread.Post(() =>
                ShowInlineNotification(
                    "Could not check for updates.",
                    InlineNotificationLevel.Error));
        }
    }



    private void NavigateToSettings()
    {
        CurrentView = new SettingsViewModel(NavigateToMain);
    }

    /// <summary>Raised to request settings close animation before CurrentView is cleared.</summary>
    public event Action? SettingsCloseRequested;

    private async void NavigateToMain()
    {
        var old = CurrentView;
        if (old == null) return;

        SettingsCloseRequested?.Invoke();

        _navigateCts?.Cancel();
        _navigateCts?.Dispose();
        _navigateCts = new CancellationTokenSource();
        var ct = _navigateCts.Token;

        try
        {
            await Task.Delay(250, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            Logger.Verbose(ex, "Settings-close delay cancelled.");
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            CurrentView = null;
            old.Dispose();
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        StopDndMonitor();
        StopPhraseCycling();
        _navigateCts?.Cancel();
        _navigateCts?.Dispose();
        _updateCheckCts?.Cancel();
        _updateCheckCts?.Dispose();
        _pauseCountdown?.Dispose();
        CurrentView?.Dispose();
        HealthDashboard.Dispose();
        BatteryMonitorService.Instance.BatteryStatusChanged -= OnBatteryStatusChanged;
        BatteryMonitorService.Instance.PowerLineStatusChanged -= OnPowerLineStatusChanged;
        BatteryHealthService.Instance.HealthUpdated -= OnHealthUpdated;
        NotificationService.Instance.PausedChanged -= OnPausedChanged;
        _inlineNotifications.StateChanged -= OnInlineNotificationStateChanged;
        SystemStateDetector.CleanupFocusMonitor();
        _disposed = true;
    }
}
