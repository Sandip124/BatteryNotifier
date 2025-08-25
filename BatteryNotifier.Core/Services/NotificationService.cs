namespace BatteryNotifier.Core.Services;

public sealed class NotificationService : IDisposable
{
    private static readonly Lazy<NotificationService> _instance = 
        new Lazy<NotificationService>(() => new NotificationService());
    
    public static NotificationService Instance => _instance.Value;
    
    private readonly Queue<NotificationMessage?> _notificationQueue;
    private readonly object _queueLock = new();
    
    private readonly Dictionary<string, DateTime> _recentNotifications = new Dictionary<string, DateTime>();
    private readonly object _recentNotificationsLock = new object();

    private TimeSpan DeduplicationInterval { get; set; } = TimeSpan.FromSeconds(30);
    private TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(5);

    private DateTime _lastCleanup = DateTime.Now;
    
    private bool _disposed = false;
    
    public event EventHandler<NotificationMessage>? NotificationReceived;
    
    private NotificationService()
    {
        _notificationQueue = new Queue<NotificationMessage?>();
    }
    
    public void PublishNotification(string message, NotificationType type = NotificationType.Global, int duration = 3000, string tag = null)
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

        lock (_queueLock)
        {
            _notificationQueue.Enqueue(notification);
        }

        RecordNotification(notification);

        if (NotificationReceived == null) return;
        
        NotificationReceived?.Invoke(this, notification);
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

    private string CreateNotificationKey(NotificationMessage notification)
    {
        return $"{notification.Tag}_{notification.Message}_{notification.Type}".ToLowerInvariant();
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
        
        ClearNotifications();
        ClearDeduplicationCache();
        
        NotificationReceived = null;
        
        _disposed = true;
    }
}

public class NotificationMessage
{
    public string Message { get; set; }
    public DateTime Timestamp { get; protected set; } = DateTime.Now;
    public NotificationType Type { get; set; }
    public int Duration { get; set; } = 3000;
    public string Tag { get; set; }

    public override bool Equals(object obj)
    {
        if (obj is NotificationMessage other)
        {
            return Message == other.Message && 
                   Tag == other.Tag && 
                   Type == other.Type;
        }
        return false;
    }
}

public enum NotificationType
{
    Global,
    Inline,
}