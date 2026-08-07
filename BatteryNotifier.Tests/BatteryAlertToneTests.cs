using BatteryNotifier.Core.Models;

namespace BatteryNotifier.Tests;

public class BatteryAlertToneTests
{
    private static BatteryAlert Alert(int lower, int upper) =>
        new() { LowerBound = lower, UpperBound = upper };

    [Theory]
    [InlineData(0, 25, AlertTone.Low)]     // default low
    [InlineData(0, 50, AlertTone.Low)]     // low half
    [InlineData(0, 85, AlertTone.Low)]     // wide low (reaches empty) — the reported case
    [InlineData(96, 100, AlertTone.Full)]  // default full
    [InlineData(80, 100, AlertTone.Full)]  // wide full (reaches full) — the reported case
    [InlineData(50, 100, AlertTone.Full)]  // high half
    [InlineData(20, 80, AlertTone.Neutral)] // mid — anchored to neither extreme
    [InlineData(0, 100, AlertTone.Neutral)] // spans both extremes
    public void Tone_ClassifiesRange(int lower, int upper, AlertTone expected)
    {
        Assert.Equal(expected, Alert(lower, upper).Tone);
    }
}
