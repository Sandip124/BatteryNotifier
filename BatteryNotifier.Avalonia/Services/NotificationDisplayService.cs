using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
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
    // Persistent, reused pool of flash overlays (one per screen). Created once and hidden between
    // flashes so firing a flash is instant (no per-notification window creation) and stays in sync
    // with the sound. Rebuilt only when the screen layout changes.
    private readonly List<ScreenFlashOverlay> _flashOverlays = new();
    private string? _flashScreenSignature;
    private IDisposable? _flashPoolTeardown;
    private static readonly TimeSpan FlashPoolIdle = TimeSpan.FromSeconds(60);
    private readonly object _cardsLock = new();
    private readonly object _overlaysLock = new();
    private const int CardSpacing = 8;
    private const int CardMargin = 20;

    private NotificationManager? _notificationManager;

    /// <summary>Current instance, set by TrayIconService on init.</summary>
    public static NotificationDisplayService? Current { get; private set; }

    public void SetNotificationManager(NotificationManager manager)
    {
        _notificationManager = manager;
        Current = this;

        // Pre-generate flash envelopes for configured sounds so the first flash reacts — but only
        // when the flash is actually enabled (otherwise it's wasted decode/disk work).
        if (AppSettings.Instance.ScreenFlashEnabled)
            foreach (var alert in AppSettings.Instance.Alerts)
                FlashSequenceLibrary.Instance.EnsureGenerated(alert.Sound);
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

        // Start the sound first so its spawn latency overlaps the flash/card UI and they land together.
        if (!suppression.ShouldSuppressSound || isCritical)
            _ = _notificationManager?.EmitGlobalNotification(notification);
        else
            Logger.Information("Sound suppressed by DND");

        ShowNotification(notification, alert, dismissalTag: notification.Tag);
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
            _ = _notificationManager.EmitGlobalNotification(notification);
        }

        // Screen flash (if enabled) — drive the glow from the sound's loudness envelope when we
        // have one; EnsureGenerated readies it for next time if this is the first use.
        if (AppSettings.Instance.ScreenFlashEnabled)
        {
            var sequence = FlashSequenceLibrary.Instance.Get(alert?.Sound);
            FlashSequenceLibrary.Instance.EnsureGenerated(alert?.Sound);
            ShowScreenFlash(color, sequence);
        }

        // Notification card
        ShowCard(title, notification.Message, level, ColorToHex(color), dismissalTag, onClosed);
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
            try { return Color.Parse(hex); }
            catch { /* fall through to auto */ }
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
        var signature = string.Join("|", all.Select(s =>
            $"{s.Bounds.X},{s.Bounds.Y},{s.Bounds.Width},{s.Bounds.Height},{s.Scaling}"));

        if (signature == _flashScreenSignature && _flashOverlays.Count == all.Count)
            return;

        foreach (var stale in _flashOverlays)
            stale.Close();
        _flashOverlays.Clear();

        foreach (var screen in all)
        {
            _flashOverlays.Add(new ScreenFlashOverlay
            {
                Width = screen.Bounds.Width / screen.Scaling,
                Height = screen.Bounds.Height / screen.Scaling,
                Position = screen.Bounds.Position
            });
        }

        _flashScreenSignature = signature;
    }

    private void ShowCard(string title, string message, int level, string accentColor,
        string? dismissalTag = null, Action? onClosed = null)
    {
        try
        {
            // Dismiss existing card before showing a new one (single instance)
            DismissAllCards();

            var card = new NotificationCard();
            var vm = new NotificationCardViewModel(
                title, message, level, accentColor,
                onDismiss: userInitiated => DismissNotification(card, dismissalTag, userInitiated));
            card.DataContext = vm;

            // Notify the caller whenever the card closes (timeout, user, or replaced) — used by the
            // alert preview to reset its play/stop toggle.
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

        // Remove and close the card
        lock (_cardsLock) { _activeCards.Remove(card); }
        card.Close();
        PositionCards();

        // Stop sound
        _notificationManager?.StopSound();

        // Clear all flash overlays
        ClearOverlays();

        // Release efficiency mode hold
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
            // Release the normal-mode so efficiency mode can re-engage once no notifications are active.
            EfficiencyModeService.Instance.ReleaseNormalMode();
        }

        // Stop sound + clear overlays
        _notificationManager?.StopSound();
        ClearOverlays();
    }

    private void ClearOverlays()
    {
        List<ScreenFlashOverlay> overlays;
        lock (_overlaysLock)
        {
            // Snapshot only — the pool persists; StopFlash gracefully fades then hides each overlay.
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

        var workArea = screen.WorkingArea;
        var scaling = screen.Scaling;
        var position = AppSettings.Instance.NotificationPosition;

        var areaX = (int)(workArea.X / scaling);
        var areaY = (int)(workArea.Y / scaling);
        var areaW = (int)(workArea.Width / scaling);
        var areaH = (int)(workArea.Height / scaling);

        lock (_cardsLock)
        {
            var isBottom = position is NotificationPosition.BottomLeft
                or NotificationPosition.BottomCenter
                or NotificationPosition.BottomRight;

            // Stack direction: top positions stack downward, bottom positions stack upward
            var offset = CardMargin;

            for (int i = 0; i < _activeCards.Count; i++)
            {
                var card = _activeCards[i];
                var cardW = (int)card.Width;
                var cardH = (int)card.Height;

                var x = position switch
                {
                    NotificationPosition.TopLeft or NotificationPosition.BottomLeft
                        => areaX + CardMargin,
                    NotificationPosition.TopRight or NotificationPosition.BottomRight
                        => areaX + areaW - cardW - CardMargin,
                    _ // Center
                        => areaX + (areaW - cardW) / 2,
                };

                var y = isBottom
                    ? areaY + areaH - cardH - offset
                    : areaY + offset;

                card.Position = new PixelPoint(x, y);
                offset += cardH + CardSpacing;
            }
        }
    }
}