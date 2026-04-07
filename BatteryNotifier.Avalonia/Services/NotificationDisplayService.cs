using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
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
    private readonly List<ScreenFlashOverlay> _activeOverlays = new();
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

        ShowNotification(notification, alert);

        if (!suppression.ShouldSuppressSound || isCritical)
            _ = _notificationManager?.EmitGlobalNotification(notification);
        else
            Logger.Information("Sound suppressed by DND");
    }

    public void ShowNotification(NotificationMessageEventArgs notification, BatteryAlert? alert,
        bool playSound = false)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => ShowNotification(notification, alert, playSound));
            return;
        }

        var level = (int)BatteryManagerStore.Instance.BatteryLifePercent;
        var color = DetermineColor(alert, level);
        var title = alert?.Label ?? DetermineTitle(notification.Tag);

        // Screen flash (if enabled)
        if (AppSettings.Instance.ScreenFlashEnabled)
        {
            _ = ShowScreenFlashAsync(color);
        }

        // Play sound if requested (preview mode)
        if (playSound && _notificationManager != null)
        {
            _ = _notificationManager.EmitGlobalNotification(notification);
        }

        // Notification card
        ShowCard(title, notification.Message, level, ColorToHex(color));
    }

    private static string DetermineTitle(string? tag) => tag switch
    {
        Constants.LowBatteryTag => "Low Battery",
        Constants.FullBatteryTag => "Full Battery",
        _ => Constants.AppName
    };

    private static Color DetermineColor(BatteryAlert? alert, int level)
    {
        // Use user-configured flash color if set
        if (alert?.FlashColor is { } hex && !string.IsNullOrEmpty(hex))
        {
            try { return Color.Parse(hex); }
            catch { /* fall through to defaults */ }
        }

        if (alert != null)
        {
            // Low range alerts → amber/red, high range alerts → green
            if (alert.UpperBound <= 50)
                return level <= 10 ? Color.Parse("#EF5350") : Color.Parse("#FFA726");
            if (alert.LowerBound >= 50)
                return Color.Parse("#66BB6A");
        }

        if (level <= 10) return Color.Parse("#EF5350");
        if (level <= 30) return Color.Parse("#FFA726");
        return Color.Parse("#66BB6A");
    }

    private static string ColorToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    private async Task ShowScreenFlashAsync(Color color)
    {
        try
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                return;

            var screens = desktop.MainWindow?.Screens;
            if (screens == null) return;

            foreach (var screen in screens.All)
            {
                var overlay = new ScreenFlashOverlay
                {
                    Width = screen.Bounds.Width / screen.Scaling,
                    Height = screen.Bounds.Height / screen.Scaling,
                    Position = screen.Bounds.Position
                };

                lock (_overlaysLock) { _activeOverlays.Add(overlay); }

                overlay.Closed += (_, _) =>
                {
                    lock (_overlaysLock) { _activeOverlays.Remove(overlay); }
                };

                overlay.Show();
                _ = overlay.FlashAsync(color);
            }
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to show screen flash overlay");
        }
    }

    private void ShowCard(string title, string message, int level, string accentColor)
    {
        try
        {
            // Dismiss existing card before showing a new one (single instance)
            DismissAllCards();

            var card = new NotificationCard();
            var vm = new NotificationCardViewModel(
                title, message, level, accentColor,
                onDismiss: () => DismissNotification(card));
            card.DataContext = vm;

            lock (_cardsLock) { _activeCards.Add(card); }

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
    private void DismissNotification(NotificationCard card)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => DismissNotification(card));
            return;
        }

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
            overlays = new List<ScreenFlashOverlay>(_activeOverlays);
            _activeOverlays.Clear();
        }

        foreach (var overlay in overlays)
        {
            overlay.StopFlash();
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