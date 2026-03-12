using System.Linq;
using BatteryNotifier.Core.Logger;
using Serilog;

namespace BatteryNotifier.Core.Services;

public sealed class NotificationService : IDisposable
{
    private static readonly Lazy<NotificationService> _instance =
        new Lazy<NotificationService>(() => new NotificationService());

    public static NotificationService Instance => _instance.Value;

    private static readonly ILogger Logger = BatteryNotifierAppLogger.ForContext("NotificationService");

    private readonly PriorityQueue<NotificationMessage, int> _notificationQueue;
    private readonly object _queueLock = new();

    private readonly Dictionary<string, NotificationTracker> _trackers =
        new Dictionary<string, NotificationTracker>(StringComparer.OrdinalIgnoreCase);
    private readonly object _trackersLock = new object();

    private readonly Dictionary<string, NotificationMessage> _pendingNotifications =
        new Dictionary<string, NotificationMessage>(StringComparer.OrdinalIgnoreCase);
    private readonly object _pendingLock = new object();

    private Timer? _flushTimer;
    private readonly object _flushTimerLock = new object();

    private TimeSpan ThrottleInterval { get; set; } = TimeSpan.FromSeconds(2);

    private DateTime _lastNotificationTime = DateTime.MinValue;

    private bool _disposed;

    // Escalating backoff intervals: immediate → 5min → 15min → 45min → silenced
    private static readonly TimeSpan[] BackoffIntervals =
    [
        TimeSpan.Zero,
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromMinutes(45)
    ];

    private const int MaxNotificationsBeforeSilence = 4;

    // Duolingo "recovering arm" concept: after this duration of silence,
    // the tracker auto-resets so the user gets a fresh reminder cycle.
    private static readonly TimeSpan RecoveryInterval = TimeSpan.FromHours(2);

    public event EventHandler<NotificationMessage>? NotificationReceived;

    private NotificationService()
    {
        _notificationQueue = new PriorityQueue<NotificationMessage, int>();
    }

    public void PublishNotification(string message, NotificationType type = NotificationType.Global, int duration = 3000, string? tag = null)
    {
        var notification = new NotificationMessage
        {
            Message = message,
            Type = type,
            Duration = duration,
            Tag = tag
        };

        PublishNotification(notification);
    }

    public void PublishNotification(NotificationMessage notification)
    {
        // Inline notifications bypass backoff — they're in-app only
        if (notification.Type == NotificationType.Inline)
        {
            EnqueueAndEmit(notification);
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

            // Auto-recover after RecoveryInterval (Duolingo "recovering arm" concept)
            if (tracker.IsSilenced && (DateTime.Now - tracker.LastNotificationTime) >= RecoveryInterval)
            {
                Logger.Information("Tag {Tag} auto-recovered after {Hours:F1}h silence",
                    tag, (DateTime.Now - tracker.LastNotificationTime).TotalHours);
                tracker.Count = 0;
                tracker.IsSilenced = false;
            }

            // Still silenced — drop the notification
            if (tracker.IsSilenced)
            {
                Logger.Debug("Notification silenced for tag {Tag} (sent {Count} already)", tag, tracker.Count);
                return;
            }

            // Check backoff interval
            var backoffIndex = Math.Min(tracker.Count, BackoffIntervals.Length - 1);
            var requiredDelay = BackoffIntervals[backoffIndex];
            var elapsed = DateTime.Now - tracker.LastNotificationTime;

            if (tracker.Count > 0 && elapsed < requiredDelay)
            {
                Logger.Debug("Notification for tag {Tag} deferred — {Elapsed:F0}s elapsed, need {Required:F0}s",
                    tag, elapsed.TotalSeconds, requiredDelay.TotalSeconds);
                return;
            }

            // Increment and check cap
            tracker.Count++;
            tracker.LastNotificationTime = DateTime.Now;

            if (tracker.Count >= MaxNotificationsBeforeSilence)
            {
                tracker.IsSilenced = true;
                Logger.Information("Tag {Tag} silenced after {Count} notifications", tag, tracker.Count);
            }
        }

        // Apply throttle for rapid-fire prevention
        var now = DateTime.Now;
        var timeSinceLastNotification = now - _lastNotificationTime;

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
            if (_trackers.Remove(tag))
            {
                Logger.Information("Notification tracker reset for tag {Tag}", tag);
            }
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
    /// Reset all notification trackers. Called on significant state changes.
    /// </summary>
    public void ResetAllTrackers()
    {
        lock (_trackersLock)
        {
            _trackers.Clear();
            Logger.Information("All notification trackers reset");
        }
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

    private void EnqueueAndEmit(NotificationMessage notification)
    {
        lock (_queueLock)
        {
            int priority = -(int)notification.Priority;
            _notificationQueue.Enqueue(notification, priority);
        }

        _lastNotificationTime = DateTime.Now;

        NotificationReceived?.Invoke(this, notification);
    }

    public void FlushPendingNotifications()
    {
        lock (_pendingLock)
        {
            if (_pendingNotifications.Count == 0) return;

            var highestPriorityNotification = _pendingNotifications.Values
                .OrderByDescending(n => n.Priority)
                .FirstOrDefault();

            if (highestPriorityNotification != null)
            {
                EnqueueAndEmit(highestPriorityNotification);
            }

            _pendingNotifications.Clear();
        }
    }

    public NotificationMessage? GetNextNotification()
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
        if (_disposed) return;

        lock (_flushTimerLock)
        {
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

        _disposed = true;
    }
}

/// <summary>
/// Tracks per-tag notification state for escalating backoff.
/// </summary>
internal class NotificationTracker
{
    public int Count { get; set; }
    public DateTime LastNotificationTime { get; set; } = DateTime.MinValue;
    public bool IsSilenced { get; set; }
}

public class NotificationMessage
{
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; protected set; } = DateTime.Now;
    public NotificationType Type { get; set; }
    public int Duration { get; set; } = 3000;
    public string? Tag { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    public override bool Equals(object? obj)
    {
        if (obj is NotificationMessage other)
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
