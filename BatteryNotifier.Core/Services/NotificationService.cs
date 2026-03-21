using BatteryNotifier.Core.Logger;
using Serilog;

namespace BatteryNotifier.Core.Services;

public sealed class NotificationService : IDisposable
{
    private static readonly Lazy<NotificationService> _instance =
        new Lazy<NotificationService>(() => new NotificationService());

    public static NotificationService Instance => _instance.Value;

    private static readonly ILogger Logger = BatteryNotifierAppLogger.ForContext("NotificationService");

    private readonly PriorityQueue<NotificationMessageEventArgs, int> _notificationQueue;
    private readonly object _queueLock = new();

    private readonly Dictionary<string, NotificationTracker> _trackers =
        new Dictionary<string, NotificationTracker>(StringComparer.OrdinalIgnoreCase);
    private readonly object _trackersLock = new object();

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

    private static readonly TimeSpan[] BackoffIntervals =
    [
        TimeSpan.Zero,
        TimeSpan.FromMinutes(2),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(10),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromMinutes(30),
        TimeSpan.FromMinutes(45)
    ];

    private const int MaxNotificationsBeforeSilence = 7;

    // Duolingo "recovering arm" concept: after this duration of silence,
    // the tracker auto-resets so the user gets a fresh reminder cycle.
    private static readonly TimeSpan RecoveryInterval = TimeSpan.FromHours(2);

    public event EventHandler<NotificationMessageEventArgs>? NotificationReceived;

    private NotificationService()
    {
        _notificationQueue = new PriorityQueue<NotificationMessageEventArgs, int>();
    }

    // ── Pause / Resume ────────────────────────────────────────

    private static readonly TimeSpan PauseAutoResumeAfter = TimeSpan.FromHours(2);
    private DateTime _pausedAt;

    public event Action<bool>? PausedChanged;

    public void PauseNotifications()
    {
        _paused = true;
        _pausedAt = DateTime.UtcNow;
        PausedChanged?.Invoke(true);
    }

    public void ResumeNotifications()
    {
        _paused = false;
        PausedChanged?.Invoke(false);
    }

    public bool IsPaused => _paused;

    private void AutoResumeIfExpired()
    {
        if (_paused && (DateTime.UtcNow - _pausedAt) >= PauseAutoResumeAfter)
        {
            Logger.Information("Auto-resuming notifications after {Duration}", PauseAutoResumeAfter);
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

    public void PublishNotification(NotificationMessageEventArgs notification)
    {
        AutoResumeIfExpired();

        // Inline notifications bypass backoff — they're in-app only
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

        lock (_trackersLock)
        {
            if (!_trackers.TryGetValue(tag, out var tracker))
            {
                tracker = new NotificationTracker();
                _trackers[tag] = tracker;
            }

            // Critical notifications bypass backoff and silencing entirely
            var isCritical = notification.Priority >= NotificationPriority.Critical;

            // Auto-recover after RecoveryInterval (Duolingo "recovering arm" concept)
            if (tracker.IsSilenced && (DateTime.UtcNow - tracker.LastNotificationTime) >= RecoveryInterval)
            {
                tracker.Count = 0;
                tracker.IsSilenced = false;
            }

            if (tracker.IsSilenced && !isCritical)
            {
                Logger.Debug("Notification silenced for tag {Tag} (reached max {Max} notifications, will recover after {Recovery})",
                    tag, MaxNotificationsBeforeSilence, RecoveryInterval);
                return;
            }

            // Check backoff interval
            var backoffIndex = Math.Min(tracker.Count, BackoffIntervals.Length - 1);
            var requiredDelay = BackoffIntervals[backoffIndex];
            var elapsed = DateTime.UtcNow - tracker.LastNotificationTime;

            if (tracker.Count > 0 && elapsed < requiredDelay && !isCritical)
            {
                Logger.Debug("Notification for tag {Tag} held back by backoff (elapsed {Elapsed}, required {Required})",
                    tag, elapsed, requiredDelay);
                return;
            }

            // Increment and check cap
            tracker.Count++;
            tracker.LastNotificationTime = DateTime.UtcNow;

            if (tracker.Count >= MaxNotificationsBeforeSilence)
                tracker.IsSilenced = true;
        }

        // Apply throttle for rapid-fire prevention
        DateTime lastTime;
        lock (_lastNotificationTimeLock) { lastTime = _lastNotificationTime; }
        var now = DateTime.UtcNow;
        var timeSinceLastNotification = now - lastTime;

        if (timeSinceLastNotification < ThrottleInterval && notification.Priority < NotificationPriority.Critical)
        {
            lock (_pendingLock)
            {
                var key = tag;
                _pendingNotifications[key] = notification;
            }
            ScheduleFlush();
            return;
        }

        EnqueueAndEmit(notification);
    }

    /// <summary>
    /// Reset notification tracking for a specific tag. Called when battery state changes
    /// (charger plugged/unplugged) so notifications can fire eagerly again.
    /// </summary>
    public void ResetTracker(string tag)
    {
        lock (_trackersLock)
        {
            _trackers.Remove(tag);
        }
    }

    /// <summary>
    /// Returns how many notifications have been sent for a tag (0 if none).
    /// Used by callers to pick escalation-appropriate message templates.
    /// </summary>
    public int GetEscalationCount(string tag)
    {
        lock (_trackersLock)
        {
            return _trackers.TryGetValue(tag, out var tracker) ? tracker.Count : 0;
        }
    }

    /// <summary>
    /// Reset all notification trackers and discard any queued pending notifications.
    /// Called on significant state changes (e.g. charger plugged/unplugged) so that
    /// stale notifications (like "unplug charger") are never delivered after the
    /// state they refer to has already changed.
    /// </summary>
    public void ResetAllTrackers()
    {
        lock (_trackersLock)
        {
            _trackers.Clear();
        }

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

            NotificationMessageEventArgs? highest = null;
            foreach (var n in _pendingNotifications.Values)
            {
                if (highest == null || n.Priority > highest.Priority)
                    highest = n;
            }

            if (highest != null)
            {
                EnqueueAndEmit(highest);
            }

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

        lock (_trackersLock)
        {
            _trackers.Clear();
        }

        NotificationReceived = null;
    }
}

/// <summary>
/// Tracks per-tag notification state for escalating backoff.
/// </summary>
internal sealed class NotificationTracker
{
    public int Count { get; set; }
    public DateTime LastNotificationTime { get; set; } = DateTime.MinValue;
    public bool IsSilenced { get; set; }
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
