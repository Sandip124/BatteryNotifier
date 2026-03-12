using System.Linq;

namespace BatteryNotifier.Core.Services;

public sealed class NotificationService : IDisposable
{
    private static readonly Lazy<NotificationService> _instance =
        new Lazy<NotificationService>(() => new NotificationService());

    public static NotificationService Instance => _instance.Value;

    private readonly PriorityQueue<NotificationMessage, int> _notificationQueue;
    private readonly object _queueLock = new();

    private readonly Dictionary<string, DateTime> _recentNotifications =
        new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
    private readonly object _recentNotificationsLock = new object();

    private readonly Dictionary<string, NotificationMessage> _pendingNotifications =
        new Dictionary<string, NotificationMessage>(StringComparer.OrdinalIgnoreCase);
    private readonly object _pendingLock = new object();

    private Timer? _flushTimer;
    private readonly object _flushTimerLock = new object();

    private TimeSpan DeduplicationInterval { get; set; } = TimeSpan.FromSeconds(30);
    private TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(5);
    private TimeSpan ThrottleInterval { get; set; } = TimeSpan.FromSeconds(2);

    private DateTime _lastCleanup = DateTime.Now;
    private DateTime _lastNotificationTime = DateTime.MinValue;

    private bool _disposed = false;

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
        if (ShouldDiscardDuplicate(notification))
        {
            return;
        }

        PerformPeriodicCleanup();

        var now = DateTime.Now;
        var timeSinceLastNotification = now - _lastNotificationTime;

        if (timeSinceLastNotification < ThrottleInterval && notification.Priority < NotificationPriority.Critical)
        {
            lock (_pendingLock)
            {
                var key = CreateNotificationKey(notification);
                _pendingNotifications[key] = notification;
            }
            ScheduleFlush();
            return;
        }

        EnqueueAndEmit(notification);
    }

    private void ScheduleFlush()
    {
        lock (_flushTimerLock)
        {
            // Already scheduled
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

        RecordNotification(notification);
        _lastNotificationTime = DateTime.Now;

        if (NotificationReceived == null) return;

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

    private bool ShouldDiscardDuplicate(NotificationMessage notification)
    {
        lock (_recentNotificationsLock)
        {
            string notificationKey = CreateNotificationKey(notification);

            DateTime notificationTime = notification.Timestamp;

            CleanupOldEntries(notificationTime);

            if (_recentNotifications.TryGetValue(notificationKey, out DateTime lastSeen))
            {
                TimeSpan timeSinceLastSeen = notificationTime - lastSeen;

                if (timeSinceLastSeen < DeduplicationInterval)
                {
                    return true;
                }
            }

            return false;
        }
    }

    private void RecordNotification(NotificationMessage notification)
    {
        lock (_recentNotificationsLock)
        {
            string notificationKey = CreateNotificationKey(notification);
            _recentNotifications[notificationKey] = notification.Timestamp;
        }
    }

    private static string CreateNotificationKey(NotificationMessage notification)
    {
        return $"{notification.Tag}_{notification.Message}_{notification.Type}";
    }

    private void CleanupOldEntries(DateTime currentTime)
    {
        var keysToRemove = _recentNotifications
            .Where(kvp => currentTime - kvp.Value > DeduplicationInterval)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _recentNotifications.Remove(key);
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

    public void ClearDeduplicationCache()
    {
        lock (_recentNotificationsLock)
        {
            _recentNotifications.Clear();
        }
    }

    private void PerformPeriodicCleanup()
    {
        var now = DateTime.Now;
        if (now - _lastCleanup > CleanupInterval)
        {
            _lastCleanup = now;

            ClearNotifications();
            ClearPendingNotifications();
            ClearDeduplicationCache();
        }
    }

    public void SetDeduplicationInterval(TimeSpan interval)
    {
        DeduplicationInterval = interval;
    }

    public void SetCleanUpInterval(TimeSpan interval)
    {
        CleanupInterval = interval;
    }

    public int RecentNotificationsCount
    {
        get
        {
            lock (_recentNotificationsLock)
            {
                return _recentNotifications.Count;
            }
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
        ClearDeduplicationCache();

        NotificationReceived = null;

        _disposed = true;
    }
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
