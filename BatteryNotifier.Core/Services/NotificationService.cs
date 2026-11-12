using BatteryNotifier.Core.Logger;
using Serilog;

namespace BatteryNotifier.Core.Services;

/// <summary>
/// Generic notification <b>delivery pipe</b>. It does not decide <i>when</i> an alert should fire —
/// that lives in <see cref="AlertEvaluationService"/>. This layer only: drops non-critical
/// notifications while the user has paused; coalesces rapid-fire bursts within a 2 s window (keeping
/// the latest per tag); and emits surviving notifications by priority via <see cref="NotificationReceived"/>.
/// </summary>
public sealed class NotificationService : IDisposable
{
    private static readonly Lazy<NotificationService> _instance =
        new Lazy<NotificationService>(() => new NotificationService());

    public static NotificationService Instance => _instance.Value;

    private static readonly ILogger Logger = BatteryNotifierAppLogger.ForContext("NotificationService");

    private readonly PriorityQueue<NotificationMessageEventArgs, int> _notificationQueue;
    private readonly object _queueLock = new();

    private readonly Dictionary<string, NotificationMessageEventArgs> _pendingNotifications =
        new Dictionary<string, NotificationMessageEventArgs>(StringComparer.OrdinalIgnoreCase);
    private readonly object _pendingLock = new object();

    private Timer? _flushTimer;
    private readonly object _flushTimerLock = new object();

    private TimeSpan ThrottleInterval { get; set; } = TimeSpan.FromSeconds(2);

    private readonly object _lastNotificationTimeLock = new object();
    private DateTime _lastNotificationTime = DateTime.MinValue;

    private bool _disposed;
    private volatile bool _paused;

    public event EventHandler<NotificationMessageEventArgs>? NotificationReceived;

    private NotificationService()
    {
        _notificationQueue = new PriorityQueue<NotificationMessageEventArgs, int>();
    }

    // ── Pause / Resume ────────────────────────────────────────

    private DateTime _pausedAt;
    private TimeSpan? _pauseDuration; // null = paused until manually resumed

    public event Action<bool>? PausedChanged;

    /// <summary>Pauses non-critical notifications. <paramref name="duration"/> null = until manually resumed.</summary>
    public void PauseNotifications(TimeSpan? duration)
    {
        _paused = true;
        _pausedAt = DateTime.UtcNow;
        _pauseDuration = duration;
        PausedChanged?.Invoke(true);
    }

    public void ResumeNotifications()
    {
        _paused = false;
        _pauseDuration = null;
        PausedChanged?.Invoke(false);
    }

    public bool IsPaused => _paused;

    /// <summary>Duration the current pause will last, or null if paused until manually resumed.</summary>
    public TimeSpan? PauseDuration => _pauseDuration;

    /// <summary>When the current timed pause will auto-resume (UTC), or null if indefinite/not paused.</summary>
    public DateTime? PauseResumesAt => _paused && _pauseDuration is { } d ? _pausedAt + d : null;

    private void AutoResumeIfExpired()
    {
        if (_paused && _pauseDuration is { } duration && (DateTime.UtcNow - _pausedAt) >= duration)
        {
            Logger.Information("Auto-resuming notifications after {Duration}", duration);
            ResumeNotifications();
        }
    }

    // ── Publish ─────────────────────────────────────────────

    public void PublishNotification(string message, NotificationType type = NotificationType.Global, int duration = 3000, string? tag = null)
    {
        var notification = new NotificationMessageEventArgs
        {
            Message = message,
            Type = type,
            Duration = duration,
            Tag = tag
        };

        PublishNotification(notification);
    }

    /// <summary>Queues a notification for delivery, applying pause, throttle-coalescing, and priority.</summary>
    public void PublishNotification(NotificationMessageEventArgs notification)
    {
        AutoResumeIfExpired();

        // Inline notifications are in-app only — deliver immediately.
        if (notification.Type == NotificationType.Inline)
        {
            EnqueueAndEmit(notification);
            return;
        }

        // User-paused notifications are dropped (critical still goes through)
        if (_paused && notification.Priority < NotificationPriority.Critical)
        {
            Logger.Debug("Notification dropped — notifications paused by user (tag={Tag})", notification.Tag);
            return;
        }

        var tag = notification.Tag ?? "default";

        // Coalesce rapid-fire bursts within the throttle window (keeps the latest per tag),
        // except critical which always goes straight through.
        DateTime lastTime;
        lock (_lastNotificationTimeLock) { lastTime = _lastNotificationTime; }

        if (DateTime.UtcNow - lastTime < ThrottleInterval && notification.Priority < NotificationPriority.Critical)
        {
            lock (_pendingLock) { _pendingNotifications[tag] = notification; }
            ScheduleFlush();
            return;
        }

        EnqueueAndEmit(notification);
    }

    /// <summary>
    /// Discards any queued/pending notifications. Called on significant state changes
    /// (e.g. charger plugged/unplugged) so stale notifications (like "unplug charger") are
    /// never delivered after the state they refer to has already changed. The escalation
    /// cycle itself is reset separately via <see cref="AlertEvaluationService.ResetAll"/>.
    /// </summary>
    public void ResetAllTrackers()
    {
        ClearNotifications();
        ClearPendingNotifications();
    }

    private void ScheduleFlush()
    {
        lock (_flushTimerLock)
        {
            if (_flushTimer != null) return;

            _flushTimer = new Timer(_ =>
            {
                FlushPendingNotifications();
                lock (_flushTimerLock)
                {
                    _flushTimer?.Dispose();
                    _flushTimer = null;
                }
            }, null, (int)ThrottleInterval.TotalMilliseconds, Timeout.Infinite);
        }
    }

    private void EnqueueAndEmit(NotificationMessageEventArgs notification)
    {
        lock (_queueLock)
        {
            int priority = -(int)notification.Priority;
            _notificationQueue.Enqueue(notification, priority);
        }

        lock (_lastNotificationTimeLock) { _lastNotificationTime = DateTime.UtcNow; }

        Logger.Information("Emitting notification: tag={Tag} message={Message}", notification.Tag, notification.Message);
        NotificationReceived?.Invoke(this, notification);
    }

    public void FlushPendingNotifications()
    {
        lock (_pendingLock)
        {
            if (_pendingNotifications.Count == 0) return;

            var highest = _pendingNotifications.Values.MaxBy(n => n.Priority);
            if (highest != null)
                EnqueueAndEmit(highest);

            _pendingNotifications.Clear();
        }
    }

    public NotificationMessageEventArgs? GetNextNotification()
    {
        lock (_queueLock)
        {
            return _notificationQueue.Count > 0 ? _notificationQueue.Dequeue() : null;
        }
    }

    public void SetThrottleInterval(TimeSpan interval)
    {
        ThrottleInterval = interval;
    }

    public int PendingCount
    {
        get
        {
            lock (_queueLock)
            {
                return _notificationQueue.Count;
            }
        }
    }

    public void ClearNotifications()
    {
        lock (_queueLock)
        {
            _notificationQueue.Clear();
        }
    }

    public void ClearPendingNotifications()
    {
        lock (_pendingLock)
        {
            _pendingNotifications.Clear();
        }
    }

    public void Dispose()
    {
        lock (_flushTimerLock)
        {
            if (_disposed) return;
            _disposed = true;

            _flushTimer?.Dispose();
            _flushTimer = null;
        }

        ClearNotifications();
        ClearPendingNotifications();

        NotificationReceived = null;
    }
}

#pragma warning disable CA1710 // Kept as NotificationMessage for domain clarity
public sealed class NotificationMessageEventArgs : EventArgs
#pragma warning restore CA1710
{
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; private set; } = DateTime.UtcNow;
    public NotificationType Type { get; set; }
    public int Duration { get; set; } = 3000;
    public string? Tag { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public InlineNotificationLevel Level { get; set; } = InlineNotificationLevel.Info;

    public override bool Equals(object? obj)
    {
        if (obj is NotificationMessageEventArgs other)
        {
            return Message == other.Message &&
                   Tag == other.Tag &&
                   Type == other.Type;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Message, Tag, Type);
    }
}

public enum NotificationType
{
    Global,
    Inline,
}

public enum NotificationPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}

public enum InlineNotificationLevel
{
    Info,
    Success,
    Warning,
    Error
}
