using BatteryNotifier.Core.Services;

namespace BatteryNotifier.Tests;

public class NotificationTemplatesTests
{
    [Theory]
    [InlineData(5, 0)]   // Critical, first
    [InlineData(10, 1)]  // Critical, second
    [InlineData(15, 0)]  // Very low, first
    [InlineData(20, 2)]  // Very low, third
    [InlineData(30, 0)]  // Mild, first
    [InlineData(25, 3)]  // Mild, final
    public void GetLowBatteryMessage_ContainsBatteryLevel(int level, int escalation)
    {
        var message = NotificationTemplates.GetLowBatteryMessage(level, escalation);

        Assert.Contains($"{level}%", message);
        Assert.False(string.IsNullOrWhiteSpace(message));
    }

    [Theory]
    [InlineData(100, 0)]  // Complete, first
    [InlineData(100, 2)]  // Complete, third
    [InlineData(98, 0)]   // Nearly full, first
    [InlineData(97, 1)]   // Nearly full, second
    [InlineData(90, 0)]   // Above threshold, first
    [InlineData(85, 3)]   // Above threshold, final
    public void GetFullBatteryMessage_ContainsBatteryLevel(int level, int escalation)
    {
        var message = NotificationTemplates.GetFullBatteryMessage(level, escalation);

        Assert.Contains($"{level}%", message);
        Assert.False(string.IsNullOrWhiteSpace(message));
    }

    [Fact]
    public void GetLowBatteryMessage_CriticalLevel_HasUrgentTone()
    {
        // At 5%, messages should be urgent regardless of escalation
        var messages = Enumerable.Range(0, 20)
            .Select(_ => NotificationTemplates.GetLowBatteryMessage(5, 0))
            .Distinct()
            .ToList();

        // All messages should contain urgent keywords
        Assert.All(messages, m =>
            Assert.True(
                m.Contains("critical", StringComparison.OrdinalIgnoreCase) ||
                m.Contains("immediately", StringComparison.OrdinalIgnoreCase) ||
                m.Contains("shut down", StringComparison.OrdinalIgnoreCase) ||
                m.Contains("save", StringComparison.OrdinalIgnoreCase),
                $"Expected urgent tone in: {m}"));
    }

    [Fact]
    public void GetLowBatteryMessage_MildLevel_HasCasualTone()
    {
        var messages = Enumerable.Range(0, 20)
            .Select(_ => NotificationTemplates.GetLowBatteryMessage(30, 0))
            .Distinct()
            .ToList();

        // Mild messages should NOT contain "critical" or "urgent"
        Assert.All(messages, m =>
            Assert.DoesNotContain("critical", m, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetLowBatteryMessage_HighEscalation_ClampedGracefully()
    {
        // Escalation way beyond max should not throw
        var message = NotificationTemplates.GetLowBatteryMessage(15, 100);
        Assert.False(string.IsNullOrWhiteSpace(message));
    }

    [Fact]
    public void GetFullBatteryMessage_HighEscalation_ClampedGracefully()
    {
        var message = NotificationTemplates.GetFullBatteryMessage(100, 100);
        Assert.False(string.IsNullOrWhiteSpace(message));
    }

    [Fact]
    public void GetLowBatteryMessage_DifferentLevels_ProduceDifferentTemplates()
    {
        // Collect all possible messages for critical vs mild to verify they're distinct pools
        var criticalMessages = Enumerable.Range(0, 50)
            .Select(_ => NotificationTemplates.GetLowBatteryMessage(5, 0))
            .Distinct()
            .ToHashSet();

        var mildMessages = Enumerable.Range(0, 50)
            .Select(_ => NotificationTemplates.GetLowBatteryMessage(35, 0))
            .Distinct()
            .ToHashSet();

        // The two sets should not overlap (different level = different templates)
        Assert.Empty(criticalMessages.Intersect(mildMessages));
    }
}
