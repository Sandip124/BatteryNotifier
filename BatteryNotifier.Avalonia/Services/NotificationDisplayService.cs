using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using BatteryNotifier.Avalonia.ViewModels;
using BatteryNotifier.Avalonia.Views;
using BatteryNotifier.Core;
using BatteryNotifier.Core.Logger;
using BatteryNotifier.Core.Managers;
using BatteryNotifier.Core.Models;
using BatteryNotifier.Core.Services;
using BatteryNotifier.Core.Store;
using Serilog;

namespace BatteryNotifier.Avalonia.Services;

/// <summary>
/// Avalonia-native notification delivery: screen flash + persistent notification cards.
/// Dismissing a notification stops sound, clears flash overlays, and closes cards.
/// </summary>
public sealed class NotificationDisplayService
{
    private static readonly ILogger Logger = BatteryNotifierAppLogger.ForContext("NotificationDisplayService");
    private readonly List<NotificationCard> _activeCards = new();

    private readonly List<ScreenFlashOverlay> _flashOverlays = new();
    private string? _flashScreenSignature;
    private IDisposable? _flashPoolTeardown;
    private static readonly TimeSpan FlashPoolIdle = TimeSpan.FromSeconds(60);
    private readonly object _cardsLock = new();
    private readonly object _overlaysLock = new();
    private const int CardSpacing = 8;
    private const int CardMargin = 20;

    private NotificationManager? _notificationManager;
    private Screens? _screens;

    /// <summary>Current instance, set by TrayIconService on init.</summary>
    public static NotificationDisplayService? Current { get; private set; }

    public void SetNotificationManager(NotificationManager manager)
    {
        _notificationManager = manager;
        Current = this;

        // Pre-generate flash envelopes for configured sounds so the first flash reacts
        if (AppSettings.Instance.ScreenFlashEnabled)
            foreach (var alert in AppSettings.Instance.Alerts)
                FlashSequenceLibrary.Instance.EnsureGenerated(alert.Sound);

        // handle resolution/scaling changes
        if (_screens == null &&
            Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow != null)
        {
            _screens = desktop.MainWindow.Screens;
            _screens.Changed += OnScreensChanged;
        }
    }

    private void OnScreensChanged(object? sender, EventArgs e)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => OnScreensChanged(sender, e));
            return;
        }

        Logger.Information("Display configuration changed — refreshing notification/flash geometry");

        lock (_cardsLock)
        {
            if (_activeCards.Count > 0)
                PositionCards();
        }
        
        if (_screens != null)
        {
            lock (_overlaysLock)
            {
                RefreshFlashPoolGeometry(_screens);
            }
        }
    }

    /// <summary>
    /// Full notification delivery pipeline: checks DND/fullscreen suppression,
    /// manages efficiency mode, shows visual notification, and plays sound.
    /// Call this instead of ShowNotification for battery alert notifications.
    /// </summary>
    public void DeliverNotification(NotificationMessageEventArgs notification)
    {
        if (notification.Type == NotificationType.Inline) return;

        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => DeliverNotification(notification));
            return;
        }

        var suppression = SystemStateDetector.GetSuppressionState();
        var isCritical = notification.Priority >= NotificationPriority.Critical;

        Logger.Information("Notification received: tag={Tag} DND={DND} fullscreen={Fullscreen} critical={Critical}",
            notification.Tag, suppression.IsDoNotDisturb, suppression.IsFullscreen, isCritical);

        if (suppression.ShouldSuppressToast && !isCritical)
        {
            Logger.Information("Notification suppressed (DND={DND}, fullscreen={Fullscreen})",
                suppression.IsDoNotDisturb, suppression.IsFullscreen);
            return;
        }

        EfficiencyModeService.Instance.AcquireNormalMode();

        var alert = !string.IsNullOrEmpty(notification.Tag)
            ? AppSettings.Instance.Alerts.Find(a => a.Id == notification.Tag)
            : null;

        var willPlaySound = !suppression.ShouldSuppressSound || isCritical;
        if (!willPlaySound)
            Logger.Information("Sound suppressed by DND");

        ShowNotification(notification, alert, playSound: willPlaySound, dismissalTag: notification.Tag);
    }

    public void ShowNotification(NotificationMessageEventArgs notification, BatteryAlert? alert,
        bool playSound = false, string? dismissalTag = null, Action? onClosed = null)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => ShowNotification(notification, alert, playSound, dismissalTag, onClosed));
            return;
        }

        var level = (int)BatteryManagerStore.Instance.BatteryLifePercent;
        var color = DetermineColor(alert, level);
        var title = alert?.Label ?? DetermineTitle(notification.Tag);

        if (playSound && _notificationManager != null)
        {
            _ = PlaySoundWithSyncedFlashAsync(notification, alert, color);
        }
        else
        {
            TriggerFlash(color, FlashSequenceLibrary.Instance.Get(alert?.Sound));
            FlashSequenceLibrary.Instance.EnsureGenerated(alert?.Sound);
        }

        // Notification card
        ShowCard(title, notification.Message, level, ColorToHex(color), dismissalTag, onClosed);
    }

    private static readonly TimeSpan FlashSequenceReadyTimeout = TimeSpan.FromSeconds(3);

    private async Task PlaySoundWithSyncedFlashAsync(NotificationMessageEventArgs notification, BatteryAlert? alert, Color color)
    {
        FlashSequence? sequence = null;
        if (AppSettings.Instance.ScreenFlashEnabled)
        {
            try
            {
                sequence = await FlashSequenceLibrary.Instance.GetOrGenerateAsync(alert?.Sound)
                    .WaitAsync(FlashSequenceReadyTimeout).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                Logger.Debug("Flash envelope generation for {Sound} took longer than {Timeout} — " +
                    "using the default pulse for this play", alert?.Sound, FlashSequenceReadyTimeout);
            }
        }

        await _notificationManager!.EmitGlobalNotification(notification,
            onSoundStarted: () => Dispatcher.UIThread.Post(() => TriggerFlash(color, sequence))).ConfigureAwait(false);
    }

    private void TriggerFlash(Color color, FlashSequence? sequence)
    {
        if (!AppSettings.Instance.ScreenFlashEnabled) return;
        ShowScreenFlash(color, sequence);
    }

    private static string DetermineTitle(string? tag) => tag switch
    {
        Constants.LowBatteryTag => "Low Battery",
        Constants.FullBatteryTag => "Full Battery",
        _ => Constants.AppName
    };

    private static Color DetermineColor(BatteryAlert? alert, int level)
    {
        // Explicit user-configured flash color wins.
        if (alert?.FlashColor is { } hex && !string.IsNullOrEmpty(hex))
        {
            return Color.Parse(hex);
        }

        // Auto: derive from the same tone that drives the message, so they always agree.
        return alert?.Tone switch
        {
            AlertTone.Full => AlertAccent.Green,
            AlertTone.Low => level <= 10 ? AlertAccent.Red : AlertAccent.Amber,
            _ => LevelColor(level),
        };
    }

    private static Color LevelColor(int level)
    {
        if (level <= 10) return AlertAccent.Red;
        if (level <= 30) return AlertAccent.Amber;
        return AlertAccent.Green;
    }

    private static string ColorToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    private void ShowScreenFlash(Color color, FlashSequence? sequence,
        int durationMs = Constants.NotificationDurationMs)
    {
        try
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                return;

            var screens = desktop.MainWindow?.Screens;
            if (screens == null) return;

            _flashPoolTeardown?.Dispose();   // in active use again — cancel any pending idle teardown
            _flashPoolTeardown = null;

            List<ScreenFlashOverlay> overlays;
            lock (_overlaysLock)
            {
                EnsureFlashPool(screens);
                overlays = new List<ScreenFlashOverlay>(_flashOverlays);
            }

            foreach (var overlay in overlays)
            {
                overlay.Show();                                  // no-op if already shown; reuses the pooled window
                _ = overlay.FlashAsync(color, durationMs, sequence); // null sequence → default pulse
            }
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to show screen flash overlay");
        }
    }

    /// <summary>
    /// Lazily (re)builds the flash overlay pool to match the current screen layout. Overlays are
    /// created once and kept for reuse; they're only recreated when screens change. Must be called
    /// under <see cref="_overlaysLock"/>.
    /// </summary>
    private void EnsureFlashPool(Screens screens)
    {
        var all = screens.All;
        var signature = BuildScreenSignature(all);

        if (signature == _flashScreenSignature && _flashOverlays.Count == all.Count)
            return;

        foreach (var stale in _flashOverlays)
            stale.Close();
        _flashOverlays.Clear();

        foreach (var screen in all)
        {
            var overlay = new ScreenFlashOverlay();
            ApplyScreenGeometry(overlay, screen);
            _flashOverlays.Add(overlay);
            Logger.Information("Flash overlay created: {Width}x{Height} pos {Pos}",
                overlay.Width, overlay.Height, overlay.Position);
        }

        _flashScreenSignature = signature;
    }

    private void RefreshFlashPoolGeometry(Screens screens)
    {
        if (_flashOverlays.Count == 0)
            return;

        var all = screens.All;

        if (_flashOverlays.Count != all.Count)
        {
            foreach (var stale in _flashOverlays)
                stale.Close();
            _flashOverlays.Clear();
            _flashScreenSignature = null;
            _flashPoolTeardown?.Dispose();
            _flashPoolTeardown = null;
            return;
        }

        for (int i = 0; i < all.Count; i++)
            ApplyScreenGeometry(_flashOverlays[i], all[i]);

        _flashScreenSignature = BuildScreenSignature(all);
    }

    private static void ApplyScreenGeometry(ScreenFlashOverlay overlay, Screen screen)
    {
        var scaling = SafeScaling(screen);
        overlay.Width = screen.Bounds.Width / scaling;
        overlay.Height = screen.Bounds.Height / scaling;
        overlay.Position = screen.Bounds.Position;
    }

    private static string BuildScreenSignature(IReadOnlyList<Screen> screens) =>
        string.Join("|", screens.Select(s => $"{s.Bounds.X},{s.Bounds.Y},{s.Bounds.Width},{s.Bounds.Height},{s.Scaling}"));

    private static double SafeScaling(Screen screen)
    {
        var scaling = screen.Scaling;
        return double.IsFinite(scaling) && scaling > 0 ? scaling : 1.0;
    }

    private void ShowCard(string title, string message, int level, string accentColor,
        string? dismissalTag = null, Action? onClosed = null)
    {
        try
        {
            DismissAllCards();

            var card = new NotificationCard();
            var vm = new NotificationCardViewModel(
                title, message, level, accentColor,
                onDismiss: userInitiated => DismissNotification(card, dismissalTag, userInitiated));
            card.DataContext = vm;
            
            if (onClosed != null)
                card.Closed += (_, _) => onClosed();

            lock (_cardsLock) { _activeCards.Add(card); }

            card.SetAnchor(AppSettings.Instance.NotificationPosition);
            PositionCards();
            card.Show();
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to show notification card");
        }
    }

    /// <summary>
    /// Dismisses a single notification card and stops all associated effects (sound + flash).
    /// </summary>
    private void DismissNotification(NotificationCard card, string? dismissalTag = null, bool userInitiated = false)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => DismissNotification(card, dismissalTag, userInitiated));
            return;
        }

        if (!string.IsNullOrEmpty(dismissalTag))
            AlertEvaluationService.Instance.RecordDismissal(dismissalTag, userInitiated);

        lock (_cardsLock) { _activeCards.Remove(card); }
        card.Close();
        PositionCards();

        _notificationManager?.StopSound();

        ClearOverlays();

        EfficiencyModeService.Instance.ReleaseNormalMode();
    }

    private void DismissAllCards()
    {
        List<NotificationCard> cards;
        lock (_cardsLock)
        {
            cards = new List<NotificationCard>(_activeCards);
            _activeCards.Clear();
        }

        foreach (var card in cards)
        {
            card.Close();
            EfficiencyModeService.Instance.ReleaseNormalMode();
        }
    }

    public void DismissCard(NotificationCard card)
    {
        DismissNotification(card);
    }

    public void DismissAll()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(DismissAll);
            return;
        }

        List<NotificationCard> cards;
        lock (_cardsLock)
        {
            cards = new List<NotificationCard>(_activeCards);
            _activeCards.Clear();
        }

        foreach (var card in cards)
        {
            card.Close();
            EfficiencyModeService.Instance.ReleaseNormalMode();
        }

        _notificationManager?.StopSound();
        ClearOverlays();
    }

    private void ClearOverlays()
    {
        List<ScreenFlashOverlay> overlays;
        lock (_overlaysLock)
        {
            overlays = new List<ScreenFlashOverlay>(_flashOverlays);
        }

        foreach (var overlay in overlays)
        {
            overlay.StopFlash();
        }
        
        _flashPoolTeardown?.Dispose();
        _flashPoolTeardown = DispatcherTimer.RunOnce(TeardownFlashPool, FlashPoolIdle);
    }

    private void TeardownFlashPool()
    {
        _flashPoolTeardown = null;
        lock (_overlaysLock)
        {
            foreach (var overlay in _flashOverlays)
                overlay.Close();
            _flashOverlays.Clear();
            _flashScreenSignature = null;
        }
    }

    private void PositionCards()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var screen = desktop.MainWindow?.Screens.Primary;
        if (screen == null) return;

        var area = screen.WorkingArea;
        var scaling = SafeScaling(screen);
        var position = AppSettings.Instance.NotificationPosition;
        var margin = (int)Math.Round(CardMargin * scaling);
        var spacing = (int)Math.Round(CardSpacing * scaling);

        lock (_cardsLock)
        {
            var stackOffset = margin;

            foreach (var card in _activeCards)
            {
                var cardSize = GetScaledCardSize(card, scaling);
                card.Position = ComputeCardPosition(area, position, cardSize, margin, stackOffset);
                stackOffset += cardSize.Height + spacing;
            }
        }
    }

    private static PixelSize GetScaledCardSize(NotificationCard card, double scaling) => new(
        (int)Math.Round(card.Width * scaling),
        (int)Math.Round(card.Height * scaling));

    private static PixelPoint ComputeCardPosition(PixelRect area, NotificationPosition position,
        PixelSize cardSize, int margin, int stackOffset)
    {
        var (x, y) = ComputeUnclampedPosition(area, position, cardSize, margin, stackOffset);

        x = ClampToRange(x, area.X, area.Width, cardSize.Width);
        y = ClampToRange(y, area.Y, area.Height, cardSize.Height);

        return new PixelPoint(x, y);
    }

    private static (int X, int Y) ComputeUnclampedPosition(PixelRect area, NotificationPosition position,
        PixelSize cardSize, int margin, int stackOffset)
    {
        var x = position switch
        {
            NotificationPosition.TopLeft or NotificationPosition.BottomLeft
                => area.X + margin,
            NotificationPosition.TopRight or NotificationPosition.BottomRight
                => area.X + area.Width - cardSize.Width - margin,
            _
                => area.X + (area.Width - cardSize.Width) / 2,
        };

        var y = IsBottomPosition(position)
            ? area.Y + area.Height - cardSize.Height - stackOffset
            : area.Y + stackOffset;

        return (x, y);
    }

    private static bool IsBottomPosition(NotificationPosition position) =>
        position is NotificationPosition.BottomLeft
            or NotificationPosition.BottomCenter
            or NotificationPosition.BottomRight;

    private static int ClampToRange(int value, int areaStart, int areaLength, int itemLength) =>
        Math.Clamp(value, areaStart, Math.Max(areaStart, areaStart + areaLength - itemLength));
}