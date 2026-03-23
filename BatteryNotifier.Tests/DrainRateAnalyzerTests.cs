using BatteryNotifier.Core.Models;
using BatteryNotifier.Core.Services;

namespace BatteryNotifier.Tests;

public class DrainRateAnalyzerTests
{
    private static long T(int minutesAgo) => DateTimeOffset.UtcNow.ToUnixTimeSeconds() - minutesAgo * 60;

    [Fact]
    public void EmptyHistory_ReturnsNull()
    {
        var result = DrainRateAnalyzer.CalculateDrainRate([], T(0));
        Assert.Null(result);
    }

    [Fact]
    public void TooFewReadings_ReturnsNull()
    {
        var history = new List<ChargeHistoryEntry>
        {
            new(T(2), 80, false),
            new(T(1), 78, false),
        };

        Assert.Null(DrainRateAnalyzer.CalculateDrainRate(history, T(0)));
    }

    [Fact]
    public void AllCharging_ReturnsNull()
    {
        var history = new List<ChargeHistoryEntry>
        {
            new(T(4), 70, true),
            new(T(3), 75, true),
            new(T(2), 80, true),
            new(T(1), 85, true),
        };

        Assert.Null(DrainRateAnalyzer.CalculateDrainRate(history, T(0)));
    }

    [Fact]
    public void NormalDrain_CalculatesCorrectRate()
    {
        // 1% per minute drain over 4 minutes
        var history = new List<ChargeHistoryEntry>
        {
            new(T(4), 84, false),
            new(T(3), 83, false),
            new(T(2), 82, false),
            new(T(1), 81, false),
            new(T(0), 80, false),
        };

        var rate = DrainRateAnalyzer.CalculateDrainRate(history, T(0));

        Assert.NotNull(rate);
        Assert.Equal(1.0, rate.Value);
        Assert.False(DrainRateAnalyzer.IsRapidDrain(rate));
    }

    [Fact]
    public void RapidDrain_Detected()
    {
        // 5% per minute drain
        var history = new List<ChargeHistoryEntry>
        {
            new(T(3), 85, false),
            new(T(2), 80, false),
            new(T(1), 75, false),
            new(T(0), 70, false),
        };

        var rate = DrainRateAnalyzer.CalculateDrainRate(history, T(0));

        Assert.NotNull(rate);
        Assert.Equal(5.0, rate.Value);
        Assert.True(DrainRateAnalyzer.IsRapidDrain(rate));
    }

    [Fact]
    public void MixedChargingDischarging_OnlyUsesDischarging()
    {
        var history = new List<ChargeHistoryEntry>
        {
            new(T(5), 90, true),   // charging — excluded
            new(T(4), 85, false),  // discharging
            new(T(3), 80, true),   // charging — excluded
            new(T(2), 80, false),  // discharging
            new(T(1), 75, false),  // discharging
        };

        var rate = DrainRateAnalyzer.CalculateDrainRate(history, T(0));

        // First discharging: T(4) at 85%, last discharging: T(1) at 75%
        // 10% over 3 minutes = 3.3%/min
        Assert.NotNull(rate);
        Assert.True(rate.Value > 3.0);
    }

    [Fact]
    public void OldReadings_Excluded()
    {
        // Readings from 10+ minutes ago should be outside the 5-minute window
        var history = new List<ChargeHistoryEntry>
        {
            new(T(10), 95, false),
            new(T(9), 90, false),
            new(T(8), 85, false),
        };

        Assert.Null(DrainRateAnalyzer.CalculateDrainRate(history, T(0)));
    }

    [Fact]
    public void StableOrGainingCharge_ReturnsNull()
    {
        // Battery stable at 50%
        var history = new List<ChargeHistoryEntry>
        {
            new(T(3), 50, false),
            new(T(2), 50, false),
            new(T(1), 50, false),
        };

        Assert.Null(DrainRateAnalyzer.CalculateDrainRate(history, T(0)));
    }

    [Fact]
    public void ExactThreshold_IsRapidDrain()
    {
        Assert.True(DrainRateAnalyzer.IsRapidDrain(3.0));
        Assert.False(DrainRateAnalyzer.IsRapidDrain(2.9));
        Assert.False(DrainRateAnalyzer.IsRapidDrain(null));
    }

    [Fact]
    public void ElapsedUnderOneMinute_ReturnsNull()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var history = new List<ChargeHistoryEntry>
        {
            new(now - 30, 80, false),
            new(now - 20, 79, false),
            new(now - 10, 78, false),
        };

        Assert.Null(DrainRateAnalyzer.CalculateDrainRate(history, now));
    }
}
