using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using BatteryNotifier.Core;
using BatteryNotifier.Core.Managers;
using BatteryNotifier.Core.Models;
using BatteryNotifier.Core.Services;
using BatteryNotifier.Core.Store;
using ReactiveUI;

namespace BatteryNotifier.Avalonia.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly AppSettings _settings = AppSettings.Instance;

    private bool _disposed;

    /// <summary>True when the main window is visible to the user.</summary>
    private bool _isWindowVisible;

    /// <summary>Set when a battery update arrives while the window is hidden.</summary>
    private bool _pendingRefresh;

    /// <summary>Timer for cycling funny phrases — only runs while window is visible.</summary>
    private CancellationTokenSource? _phraseCts;

    /// <summary>Whether we've already checked for Accessibility permission on macOS.</summary>
    private bool _accessibilityChecked;

    /// <summary>Timer for polling DND state changes — only runs while window is visible.</summary>
    private CancellationTokenSource? _dndCts;

    /// <summary>Cancels any in-progress typewriter animation.</summary>
    private CancellationTokenSource? _typewriterCts;

    public MainWindowViewModel()
    {
        NavigateToSettingsCommand = ReactiveCommand.Create(NavigateToSettings);
        HideWindowCommand = ReactiveCommand.Create(HideWindow);
        CheckForUpdatesCommand = ReactiveCommand.CreateFromTask(CheckForUpdates);
        ExitCommand = ReactiveCommand.Create(ExitApplication);
        DismissInlineNotificationCommand = ReactiveCommand.Create(DismissInlineNotification);

        _inlineNotifications.StateChanged += OnInlineNotificationStateChanged;

        // Initialize the Darwin notification monitor for Focus/DND changes (macOS only, no-op elsewhere)
        SystemStateDetector.InitializeFocusMonitor();

        // Access BatteryMonitorService FIRST — its constructor does a synchronous
        // initial check that populates BatteryManagerStore, so the store has real
        // values before RefreshBatteryStatus() reads them.
        try
        {
            BatteryMonitorService.Instance.BatteryStatusChanged += OnBatteryStatusChanged;
            BatteryMonitorService.Instance.PowerLineStatusChanged += OnPowerLineStatusChanged;
        }
        catch { /* Battery monitoring not available on this platform */ }

        // Subscribe to health updates for the compact bar
        BatteryHealthService.Instance.HealthUpdated += OnHealthUpdated;

        // Instant UI update when notifications are paused/resumed from tray
        NotificationService.Instance.PausedChanged += OnPausedChanged;

        // Start recording battery history (charge sparkline + wear trend)
        _ = BatteryHistoryService.Instance;

        // Start power usage monitoring (top CPU consumers)
        _ = PowerUsageService.Instance;

        // Initial populate — window may or may not be visible yet
        RefreshBatteryStatus();
    }

    // ── Visibility-aware UI updates ──────────────────────────────

    /// <summary>
    /// Called by MainWindow when it becomes visible or hidden.
    /// Controls whether UI updates are processed or deferred.
    /// </summary>
    public void OnWindowVisibilityChanged(bool isVisible)
    {
        _isWindowVisible = isVisible;

        if (isVisible)
        {
            // Immediately fetch fresh battery state (catches charger plug/unplug while hidden)
            try { BatteryMonitorService.Instance.ForceCheck(); }
            catch { /* Battery monitoring not available */ }

            // Catch up on any missed battery updates
            if (_pendingRefresh)
            {
                _pendingRefresh = false;
            }
            RefreshBatteryStatus();

            // Check DND state and start monitoring for changes
            RefreshDndStatus();
            StartDndMonitor();

            // One-time check: prompt for Accessibility on macOS if needed for DND detection
            CheckAccessibilityPermission();

            // Show greeting and start phrase cycling
            StatusMessage = PickStatusMessage(
                BatteryManagerStore.Instance.BatteryState,
                BatteryManagerStore.Instance.IsCharging);
            StartPhraseCycling();
        }
        else
        {
            // Window hidden — stop all UI timers
            StopDndMonitor();
            StopPhraseCycling();
            CancelTypewriter();
            StatusMessage = string.Empty;
        }
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
            // Show greeting for 5 seconds, then clear it
            await Task.Delay(5000, ct).ConfigureAwait(false);
            Dispatcher.UIThread.Post(() => StatusMessage = string.Empty);

            // Cycle the time remaining phrase every 2 minutes (only funny phrases, not real estimates)
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(2));
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                Dispatcher.UIThread.Post(RefreshTimeRemainingPhrase);
            }
        }
        catch (OperationCanceledException)
        {
            // Window was hidden — expected
        }
    }

    /// <summary>
    /// Refreshes the time remaining text. Real estimates are shown instantly.
    /// Funny phrases use a typewriter animation with thinking dots.
    /// </summary>
    private void RefreshTimeRemainingPhrase()
    {
        var store = BatteryManagerStore.Instance;
        if (store.BatteryLifeRemaining > 0)
        {
            CancelTypewriter();
            TimeRemaining = FormatTimeRemaining(store);
        }
        else
        {
            _ = TypewritePhrase(PickBatteryPhrase());
        }
    }

    private void CancelTypewriter()
    {
        _typewriterCts?.Cancel();
        _typewriterCts?.Dispose();
        _typewriterCts = null;
    }

    private async Task TypewritePhrase(string phrase)
    {
        CancelTypewriter();
        var cts = new CancellationTokenSource();
        _typewriterCts = cts;
        var ct = cts.Token;

        try
        {
            // Phase 1: Thinking dots animation
            for (int cycle = 0; cycle < 2; cycle++)
            {
                for (int dots = 1; dots <= 3; dots++)
                {
                    ct.ThrowIfCancellationRequested();
                    Dispatcher.UIThread.Post(() => TimeRemaining = new string('.', dots));
                    await Task.Delay(300, ct).ConfigureAwait(false);
                }
            }

            // Phase 2: Type out the phrase character by character
            for (int i = 1; i <= phrase.Length; i++)
            {
                ct.ThrowIfCancellationRequested();
                var partial = phrase[..i];
                Dispatcher.UIThread.Post(() => TimeRemaining = partial);
                await Task.Delay(35, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // New phrase or real estimate replaced us — expected
        }
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

        if (store.HasNoBattery)
            BatteryStatus = "No Battery";
        else if (store.IsUnknown)
            BatteryStatus = "Unknown";
        else if (store.IsCharging)
            BatteryStatus = "Charging";
        else if (store.IsPluggedIn)
            BatteryStatus = "Plugged In";
        else
            BatteryStatus = "Discharging";

        if (store.BatteryLifeRemaining > 0)
        {
            CancelTypewriter();
            TimeRemaining = FormatTimeRemaining(store);
        }
        else
        {
            _ = TypewritePhrase(PickBatteryPhrase());
        }

        StatusLine = BuildStatusLine(store);

        var assetName = store.BatteryState switch
        {
            BatteryState.Full => "FullBattery.png",
            BatteryState.Adequate => "FullBattery.png",
            BatteryState.Sufficient => "Sufficient.png",
            BatteryState.Low => "LowBattery.png",
            BatteryState.Critical => "LowBattery.png",
            _ => "Sufficient.png"
        };

        BatteryImage = LoadAsset(assetName);
    }

    /// <summary>
    /// Builds a single contextual line combining status, time, and charge tip.
    /// Examples: "Charging · 1h 23m to full" / "2h 15m remaining" / "Unplug — 80% reached"
    /// </summary>
    private static string BuildStatusLine(BatteryManagerStore store)
    {
        if (store.HasNoBattery || store.IsUnknown) return string.Empty;

        var pct = (int)store.BatteryLifePercent;
        var time = FormatTimeShort(store);

        return (store.IsCharging || store.IsPluggedIn)
            ? ChargingStatusLine(pct, time)
            : DischargingStatusLine(pct, time);
    }

    private static string ChargingStatusLine(int pct, string? time)
    {
        if (pct >= 80) return "Unplug now — extend battery lifespan";
        if (pct >= 70) return WithTime(time, "to full · Unplug at 80%", "Unplug at 80% for longevity");
        if (pct >= 50) return WithTime(time, "to full", "Optimal range is 20–80%");
        return WithTime(time, "to full", "Charging — avoid draining below 20%");
    }

    private static string DischargingStatusLine(int pct, string? time)
    {
        if (pct <= 5) return WithTime(time, "left · Plug in now", "Critical — plug in now");
        if (pct <= 20) return WithTime(time, "left · Plug in soon", "Low — plug in soon");
        if (pct <= 50) return WithTime(time, "remaining", "Keep above 20% for battery health");
        return WithTime(time, "remaining", "Battery in good shape");
    }

    private static string WithTime(string? time, string suffix, string fallback) =>
        time != null ? $"{time} {suffix}" : fallback;

    private static string? FormatTimeShort(BatteryManagerStore store)
    {
        if (store.BatteryLifeRemaining <= 0) return null;
        var ts = store.BatteryLifeRemainingInSeconds;
        var h = (int)ts.TotalHours;
        return h > 0 ? $"{h}h {ts.Minutes}m" : $"{ts.Minutes}m";
    }

    private static string FormatTimeRemaining(BatteryManagerStore store)
    {
        var ts = store.BatteryLifeRemainingInSeconds;
        var hours = (int)ts.TotalHours;
        var mins = ts.Minutes;

        var timeStr = hours > 0 ? $"{hours}h {mins}m" : $"{mins}m";

        return store.IsCharging
            ? $"{timeStr} to full charge"
            : $"{timeStr} of battery remaining";
    }

    private static readonly ConcurrentDictionary<string, Bitmap?> _bitmapCache = new();

    private static Bitmap? LoadAsset(string fileName)
    {
        return _bitmapCache.GetOrAdd(fileName, static key =>
        {
            try
            {
                var uri = AssetUris.ForAsset(key);
                using var stream = AssetLoader.Open(uri);
                return new Bitmap(stream);
            }
            catch
            {
                return null;
            }
        });
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

    public string AlertsSummary
    {
        get
        {
            var alerts = _settings.Alerts;
            var active = alerts.Count(a => a.IsEnabled);
            return active switch
            {
                0 => "No alerts active",
                1 => "1 alert active",
                _ => $"{active} alerts active"
            };
        }
    }

    public bool FullBatteryAlertEnabled
    {
        get => _settings.Alerts.Find(a => a.Id == "fullbatt")?.IsEnabled ?? true;
        set
        {
            var alert = _settings.Alerts.Find(a => a.Id == "fullbatt");
            if (alert != null)
            {
                alert.IsEnabled = value;
                _settings.Save();
                this.RaisePropertyChanged();
            }
        }
    }

    public bool LowBatteryAlertEnabled
    {
        get => _settings.Alerts.Find(a => a.Id == "lowbatt_")?.IsEnabled ?? true;
        set
        {
            var alert = _settings.Alerts.Find(a => a.Id == "lowbatt_");
            if (alert != null)
            {
                alert.IsEnabled = value;
                _settings.Save();
                this.RaisePropertyChanged();
            }
        }
    }

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
                _ => "Checking..."
            };
        }
    }

    private static global::Avalonia.Media.Geometry? _iconCheck;
    private static global::Avalonia.Media.Geometry? _iconHeart;
    private static global::Avalonia.Media.Geometry? _iconWarn;
    private static global::Avalonia.Media.Geometry? _iconSpinner;

    private static global::Avalonia.Media.Geometry? ResolveIcon(string key)
    {
        var app = Application.Current;
        if (app?.Resources.TryGetResource(key, null, out var res) == true
            && res is global::Avalonia.Media.Geometry geo)
            return geo;
        // Try merged dictionaries
        foreach (var dict in app?.Resources.MergedDictionaries ?? [])
            if (dict.TryGetResource(key, null, out var r) && r is global::Avalonia.Media.Geometry g)
                return g;
        return null;
    }

    public static global::Avalonia.Media.Geometry? HealthIcon
    {
        get
        {
            var health = BatteryHealthService.Instance.LatestHealth;
            return health?.HealthStatus switch
            {
                MetricStatus.Good => _iconCheck ??= ResolveIcon("Icon.CheckFat"),
                MetricStatus.Fair => _iconHeart ??= ResolveIcon("Icon.HeartFill"),
                MetricStatus.Poor => _iconWarn ??= ResolveIcon("Icon.ExclamationMarkFill"),
                _ => _iconSpinner ??= ResolveIcon("Icon.Spinner")
            };
        }
    }

    public static string HealthAccentColor
    {
        get
        {
            var health = BatteryHealthService.Instance.LatestHealth;
            return health?.HealthStatus switch
            {
                MetricStatus.Good => "#388E3C",
                MetricStatus.Fair => "#F9A825",
                MetricStatus.Poor => "#D32F2F",
                _ => "#808080"
            };
        }
    }

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

    private static readonly string[] DndMessages =
    [
        "Do Not Disturb is on — notifications are paused.",
        "Focus mode active — notifications won't show.",
        "DND enabled — you won't see battery alerts.",
        "Notifications silenced by Do Not Disturb.",
    ];

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

    public ReactiveCommand<Unit, Unit> ResumeNotificationsCommand { get; } =
        ReactiveCommand.Create(() => NotificationService.Instance.ResumeNotifications());

    private void OnPausedChanged(bool paused)
    {
        Dispatcher.UIThread.Post(() =>
        {
            IsNotificationsPaused = paused;
            this.RaisePropertyChanged(nameof(ShowPausedBanner));
        });
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
    /// Checks Darwin notify every 1s (free memory read) — triggers instant refresh on
    /// older macOS where the notification fires. Every 5s, does a full refresh regardless
    /// as a fallback for Tahoe+ where Darwin notify for DND was removed.
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
                    Dispatcher.UIThread.Post(RefreshDndStatus);
                    tickCount = 0;
                    continue;
                }

                // Slow path: direct poll every 5s for Tahoe+ and non-macOS
                if (tickCount >= 5)
                {
                    Dispatcher.UIThread.Post(RefreshDndStatus);
                    tickCount = 0;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Window was hidden — expected
        }
    }

    private void RefreshDndStatus()
    {
        try
        {
            var suppression = SystemStateDetector.GetSuppressionState();
            var active = suppression.ShouldSuppressToast;

            if (active != IsDndActive)
            {
                IsDndActive = active;
                DndMessage = active
                    ? DndMessages[Random.Shared.Next(DndMessages.Length)]
                    : string.Empty;
                this.RaisePropertyChanged(nameof(ShowPausedBanner));
            }
        }
        catch
        {
            IsDndActive = false;
            DndMessage = string.Empty;
        }

    }

    public string StatusMessage
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    // ── Inline notification (state lives in Core's InlineNotificationManager) ──

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

    // ── Funny phrases & greetings ────────────────────────────────

    private static readonly string[] ChargingPhrases =
    [
        "Charging — estimating time to full...",
        "Plugged in — calculating charge time...",
        "Charging up — estimate available soon",
        "Power connected — charging in progress",
    ];

    private static readonly string[] DischargingPhrases =
    [
        "On battery — estimating time remaining...",
        "Running on battery power",
        "Unplugged — calculating battery life...",
        "On battery — estimate available soon",
    ];

    private string PickBatteryPhrase()
    {
        var pool = IsCharging ? ChargingPhrases : DischargingPhrases;
        return pool[Random.Shared.Next(pool.Length)];
    }

    private static readonly string[] GreetingsFull =
    [
        "Your battery is vibing at 100%.",
        "Fully juiced! Time to unplug.",
        "Battery's living its best life.",
        "All topped up. You're golden!",
        "Full tank energy right here."
    ];

    private static readonly string[] GreetingsAdequate =
    [
        "Battery's looking great today!",
        "Smooth sailing ahead.",
        "You've got plenty of juice.",
        "All systems go. Carry on!",
        "Battery says: 'I'm chilling.'"
    ];

    private static readonly string[] GreetingsSufficient =
    [
        "Still going strong!",
        "Halfway there, keep cruising.",
        "Battery's holding steady.",
        "Not bad, not bad at all.",
        "Doing just fine over here."
    ];

    private static readonly string[] GreetingsLow =
    [
        "Getting a bit thirsty...",
        "Maybe find a charger soon?",
        "Battery's sending SOS vibes.",
        "Running on fumes here!",
        "A charger would be nice right about now."
    ];

    private static readonly string[] GreetingsCritical =
    [
        "MAYDAY! Plug in, plug in!",
        "Battery's on life support.",
        "We're in the danger zone!",
        "This is not a drill. Charge me!",
        "Counting down... find power NOW."
    ];

    private static readonly string[] GreetingsCharging =
    [
        "Charging up! Sit tight.",
        "Nom nom nom... delicious electricity.",
        "Sipping on some sweet power.",
        "Refueling in progress...",
        "Getting stronger by the minute!"
    ];

    private static string PickStatusMessage(BatteryState state, bool isCharging)
    {
        if (isCharging)
            return GreetingsCharging[Random.Shared.Next(GreetingsCharging.Length)];

        var pool = state switch
        {
            BatteryState.Full => GreetingsFull,
            BatteryState.Adequate => GreetingsAdequate,
            BatteryState.Sufficient => GreetingsSufficient,
            BatteryState.Low => GreetingsLow,
            BatteryState.Critical => GreetingsCritical,
            _ => GreetingsAdequate
        };

        return pool[Random.Shared.Next(pool.Length)];
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
        try
        {
            var result = await UpdateService.Instance.CheckForUpdateManualAsync().ConfigureAwait(false);

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

        // Trigger close animation while content is still visible
        SettingsCloseRequested?.Invoke();

        // Wait for animation to finish, then clear content and dispose
        await Task.Delay(250).ConfigureAwait(false);
        Dispatcher.UIThread.Post(() =>
        {
            CurrentView = null;
            old.Dispose();
            this.RaisePropertyChanged(nameof(AlertsSummary));
            this.RaisePropertyChanged(nameof(FullBatteryAlertEnabled));
            this.RaisePropertyChanged(nameof(LowBatteryAlertEnabled));
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        StopDndMonitor();
        StopPhraseCycling();
        CancelTypewriter();
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
