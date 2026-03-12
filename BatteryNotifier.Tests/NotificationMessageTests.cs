using BatteryNotifier.Core.Services;

namespace BatteryNotifier.Tests;

public class NotificationMessageTests
{
    [Fact]
    public void Equals_SameProperties_ReturnsTrue()
    {
        var a = new NotificationMessage { Message = "Hello", Tag = "Tag1", Type = NotificationType.Global };
        var b = new NotificationMessage { Message = "Hello", Tag = "Tag1", Type = NotificationType.Global };

        Assert.True(a.Equals(b));
    }

    [Fact]
    public void Equals_DifferentMessage_ReturnsFalse()
    {
        var a = new NotificationMessage { Message = "Hello", Tag = "Tag1", Type = NotificationType.Global };
        var b = new NotificationMessage { Message = "World", Tag = "Tag1", Type = NotificationType.Global };

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_DifferentTag_ReturnsFalse()
    {
        var a = new NotificationMessage { Message = "Hello", Tag = "Tag1", Type = NotificationType.Global };
        var b = new NotificationMessage { Message = "Hello", Tag = "Tag2", Type = NotificationType.Global };

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_DifferentType_ReturnsFalse()
    {
        var a = new NotificationMessage { Message = "Hello", Tag = "Tag1", Type = NotificationType.Global };
        var b = new NotificationMessage { Message = "Hello", Tag = "Tag1", Type = NotificationType.Inline };

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        var a = new NotificationMessage { Message = "Hello", Tag = "Tag1", Type = NotificationType.Global };
        Assert.False(a.Equals(null));
    }

    [Fact]
    public void GetHashCode_EqualObjects_SameHashCode()
    {
        var a = new NotificationMessage { Message = "Hello", Tag = "Tag1", Type = NotificationType.Global };
        var b = new NotificationMessage { Message = "Hello", Tag = "Tag1", Type = NotificationType.Global };

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentObjects_DifferentHashCode()
    {
        var a = new NotificationMessage { Message = "Hello", Tag = "Tag1", Type = NotificationType.Global };
        var b = new NotificationMessage { Message = "World", Tag = "Tag2", Type = NotificationType.Inline };

        // Not strictly required but extremely likely for different data
        Assert.NotEqual(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void CanBeUsedAsDictionaryKey()
    {
        var msg = new NotificationMessage { Message = "Hello", Tag = "Tag1", Type = NotificationType.Global };
        var dict = new Dictionary<NotificationMessage, int> { { msg, 42 } };

        var lookup = new NotificationMessage { Message = "Hello", Tag = "Tag1", Type = NotificationType.Global };
        Assert.True(dict.ContainsKey(lookup));
        Assert.Equal(42, dict[lookup]);
    }
}
