using System;
using System.Collections.Concurrent;
using System.Diagnostics;
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
using BatteryNotifier.Core.Logger;
using BatteryNotifier.Core.Managers;
using BatteryNotifier.Core.Services;
using BatteryNotifier.Core.Store;
using ReactiveUI;

namespace BatteryNotifier.Avalonia.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly AppSettings _settings = AppSettings.Instance;

    private bool _fullBatteryNotification;
    private bool _lowBatteryNotification;
    private bool _disposed;
    private readonly IDisposable? _fullBatteryNotificationSub;
    private readonly IDisposable? _lowBatteryNotificationSub;

    /// <summary>True when the main window is visible to the user.</summary>
    private bool _isWindowVisible;

    /// <summary>Set when a battery update arrives while the window is hidden.</summary>
    private bool _pendingRefresh;

    /// <summary>Timer for cycling funny phrases — only runs while window is visible.</summary>
    private CancellationTokenSource? _phraseCts;

    /// <summary>Timer for polling DND state changes — only runs while window is visible.</summary>
    private CancellationTokenSource? _dndCts;

    /// <summary>Cancels any in-progress typewriter animation.</summary>
    private CancellationTokenSource? _typewriterCts;

    public MainWindowViewModel()
    {
        _fullBatteryNotification = _settings.FullBatteryNotification;
        _lowBatteryNotification = _settings.LowBatteryNotification;

        NavigateToSettingsCommand = ReactiveCommand.Create(NavigateToSettings);
        HideWindowCommand = ReactiveCommand.Create(HideWindow);
        OpenAboutCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            await OpenAboutInteraction.Handle(Unit.Default);
        });
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

        // Initial populate — window may or may not be visible yet
        RefreshBatteryStatus();

        _fullBatteryNotificationSub = this.WhenAnyValue(x => x.FullBatteryNotification)
            .Skip(1)
            .Subscribe(enabled =>
            {
                _settings.FullBatteryNotification = enabled;
                _settings.Save();
            });

        _lowBatteryNotificationSub = this.WhenAnyValue(x => x.LowBatteryNotification)
            .Skip(1)
            .Subscribe(enabled =>
            {
                _settings.LowBatteryNotification = enabled;
                _settings.Save();
            });
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
                var uri = new Uri($"avares://BatteryNotifier/Assets/{key}");
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

    public bool FullBatteryNotification
    {
        get => _fullBatteryNotification;
        set => this.RaiseAndSetIfChanged(ref _fullBatteryNotification, value);
    }

    public bool LowBatteryNotification
    {
        get => _lowBatteryNotification;
        set => this.RaiseAndSetIfChanged(ref _lowBatteryNotification, value);
    }

    public SettingsViewModel? CurrentView
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public ReactiveCommand<Unit, Unit> NavigateToSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> HideWindowCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenAboutCommand { get; }
    public Interaction<Unit, Unit> OpenAboutInteraction { get; } = new();
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
    /// Polls for Focus/DND state changes every 5 seconds via Darwin notifications
    /// (a trivial in-process memory check — zero cost). When a change is detected,
    /// triggers a full DND state refresh. Also does a periodic full check every 2 minutes
    /// as a fallback in case Darwin notifications don't fire on this macOS version.
    /// </summary>
    private async Task RunDndMonitorAsync(CancellationToken ct)
    {
        try
        {
            var tickCount = 0;
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                tickCount++;
                var shouldCheck = SystemStateDetector.HasPendingFocusChange();

                // Fallback: full check every 2 minutes (24 ticks × 5s) regardless
                if (!shouldCheck && tickCount >= 24)
                {
                    shouldCheck = true;
                    tickCount = 0;
                }

                if (shouldCheck)
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
        var result = await UpdateService.Instance.CheckForUpdateManualAsync().ConfigureAwait(false);

        Dispatcher.UIThread.Post(() =>
        {
            switch (result.Status)
            {
                case CheckStatus.UpdateAvailable when result.Release != null:
                    OpenUrlInBrowser(result.Release.HtmlUrl);
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

    private static void OpenUrlInBrowser(string url)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var psi = new ProcessStartInfo("open") { UseShellExecute = false };
                psi.ArgumentList.Add(url);
                using var p = Process.Start(psi);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using var p = Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else
            {
                var psi = new ProcessStartInfo("xdg-open") { UseShellExecute = false };
                psi.ArgumentList.Add(url);
                using var p = Process.Start(psi);
            }
        }
        catch { }
    }

    private void NavigateToSettings()
    {
        CurrentView = new SettingsViewModel(NavigateToMain);
    }

    private void NavigateToMain()
    {
        var old = CurrentView;
        CurrentView = null;
        old?.Dispose();
    }

    public void Dispose()
    {
        if (_disposed) return;
        StopDndMonitor();
        StopPhraseCycling();
        CancelTypewriter();
        _fullBatteryNotificationSub?.Dispose();
        _lowBatteryNotificationSub?.Dispose();
        CurrentView?.Dispose();
        try
        {
            BatteryMonitorService.Instance.BatteryStatusChanged -= OnBatteryStatusChanged;
            BatteryMonitorService.Instance.PowerLineStatusChanged -= OnPowerLineStatusChanged;
        }
        catch { }
        _inlineNotifications.StateChanged -= OnInlineNotificationStateChanged;
        SystemStateDetector.CleanupFocusMonitor();
        _disposed = true;
    }
}
