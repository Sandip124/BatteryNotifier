using System;
using System.Collections.Generic;
using System.Windows.Forms;
using BatteryNotifier.Utils;

namespace BatteryNotifier.Lib.Services;

public sealed class NotificationService
{
    private static readonly Lazy<NotificationService> _instance = 
        new Lazy<NotificationService>(() => new NotificationService());
    
    public static NotificationService Instance => _instance.Value;
    
    private readonly Queue<NotificationMessage?> _notificationQueue;
    private readonly object _queueLock = new();
    
    public event EventHandler<NotificationMessage>? NotificationReceived;
    
    private NotificationService()
    {
        _notificationQueue = new Queue<NotificationMessage?>();
    }
    
    public void PublishNotification(string message, NotificationType type = NotificationType.Global, int duration = 3000)
    {
        var notification = new NotificationMessage
        {
            Message = message,
            Timestamp = DateTime.Now,
            Type = type,
            Duration = duration
        };
        
        lock (_queueLock)
        {
            _notificationQueue.Enqueue(notification);
        }

        if (NotificationReceived == null) return;
        if (Application.OpenForms.Count <= 0) return;
        
        var mainForm = Application.OpenForms[0];
        
        UtilityHelper.SafeInvoke(mainForm, () =>
        {
            NotificationReceived?.Invoke(this, notification);
        });
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
}

public class NotificationMessage
{
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }
    public NotificationType Type { get; set; }
    public int Duration { get; set; } = 3000;
}

public enum NotificationType
{
    Global,
    Inline,
}