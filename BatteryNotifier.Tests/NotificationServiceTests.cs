using BatteryNotifier.Core.Services;

namespace BatteryNotifier.Tests;

public class NotificationServiceTests
{
    private NotificationService CreateService()
    {
        var svc = NotificationService.Instance;
        svc.ClearNotifications();
        svc.ClearPendingNotifications();
        svc.ClearDeduplicationCache();
        svc.SetDeduplicationInterval(TimeSpan.FromSeconds(30));
        // Disable throttling by default so tests aren't affected by _lastNotificationTime
        svc.SetThrottleInterval(TimeSpan.Zero);
        return svc;
    }

    [Fact]
    public void PublishNotification_FirstNotification_IsEmitted()
    {
        var svc = CreateService();
        NotificationMessage? received = null;
        EventHandler<NotificationMessage> handler = (_, msg) => received = msg;
        svc.NotificationReceived += handler;

        try
        {
            svc.PublishNotification("Test message", NotificationType.Global, tag: "TestTag");

            Assert.NotNull(received);
            Assert.Equal("Test message", received.Message);
            Assert.Equal("TestTag", received.Tag);
        }
        finally
        {
            svc.NotificationReceived -= handler;
        }
    }

    [Fact]
    public void PublishNotification_DuplicateWithin30s_IsBlocked()
    {
        var svc = CreateService();
        int receivedCount = 0;
        EventHandler<NotificationMessage> handler = (_, _) => receivedCount++;
        svc.NotificationReceived += handler;

        try
        {
            svc.PublishNotification("Dup message", NotificationType.Global, tag: "DupTag");
            // Same notification again immediately — should be deduplicated
            svc.PublishNotification("Dup message", NotificationType.Global, tag: "DupTag");

            Assert.Equal(1, receivedCount);
        }
        finally
        {
            svc.NotificationReceived -= handler;
        }
    }

    [Fact]
    public void PublishNotification_DifferentMessages_BothEmitted()
    {
        var svc = CreateService();
        int receivedCount = 0;
        EventHandler<NotificationMessage> handler = (_, _) => receivedCount++;
        svc.NotificationReceived += handler;

        try
        {
            svc.PublishNotification("Message A", NotificationType.Global, tag: "TagA");
            svc.PublishNotification("Message B", NotificationType.Global, tag: "TagB");

            Assert.Equal(2, receivedCount);
        }
        finally
        {
            svc.NotificationReceived -= handler;
        }
    }

    [Fact]
    public void FlushPendingNotifications_EmitsHighestPriority()
    {
        var svc = CreateService();

        // First notification to set _lastNotificationTime
        svc.PublishNotification(new NotificationMessage
        {
            Message = "Initial",
            Type = NotificationType.Global,
            Tag = "Init"
        });

        // Now set throttle high so next notifications are pending
        svc.SetThrottleInterval(TimeSpan.FromMinutes(10));

        svc.PublishNotification(new NotificationMessage
        {
            Message = "Low priority",
            Type = NotificationType.Global,
            Tag = "Low",
            Priority = NotificationPriority.Low
        });

        svc.PublishNotification(new NotificationMessage
        {
            Message = "High priority",
            Type = NotificationType.Global,
            Tag = "High",
            Priority = NotificationPriority.High
        });

        NotificationMessage? flushed = null;
        EventHandler<NotificationMessage> handler = (_, msg) => flushed = msg;
        svc.NotificationReceived += handler;

        try
        {
            svc.FlushPendingNotifications();

            Assert.NotNull(flushed);
            Assert.Equal("High priority", flushed.Message);
        }
        finally
        {
            svc.NotificationReceived -= handler;
        }
    }

    [Fact]
    public void ClearNotifications_EmptiesQueue()
    {
        var svc = CreateService();
        svc.PublishNotification("Msg", NotificationType.Global, tag: "Clear");
        Assert.True(svc.PendingCount > 0);

        svc.ClearNotifications();
        Assert.Equal(0, svc.PendingCount);
    }

    [Fact]
    public void ClearDeduplicationCache_AllowsDuplicateAfterClear()
    {
        var svc = CreateService();
        int receivedCount = 0;
        EventHandler<NotificationMessage> handler = (_, _) => receivedCount++;
        svc.NotificationReceived += handler;

        try
        {
            svc.PublishNotification("Dup msg", NotificationType.Global, tag: "DupClear");
            svc.ClearDeduplicationCache();
            svc.PublishNotification("Dup msg", NotificationType.Global, tag: "DupClear");

            Assert.Equal(2, receivedCount);
        }
        finally
        {
            svc.NotificationReceived -= handler;
        }
    }
}
