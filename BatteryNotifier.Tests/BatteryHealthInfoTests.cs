using BatteryNotifier.Core.Models;

namespace BatteryNotifier.Tests;

public class BatteryHealthInfoTests
{
    [Theory]
    [InlineData(100, MetricStatus.Good)]
    [InlineData(85, MetricStatus.Good)]
    [InlineData(80, MetricStatus.Good)]
    [InlineData(79, MetricStatus.Fair)]
    [InlineData(60, MetricStatus.Fair)]
    [InlineData(59, MetricStatus.Poor)]
    [InlineData(20, MetricStatus.Poor)]
    public void HealthStatus_DerivedFromPercent(double percent, MetricStatus expected)
    {
        var info = new BatteryHealthInfo { HealthPercent = percent };
        Assert.Equal(expected, info.HealthStatus);
    }

    [Fact]
    public void HealthStatus_NullPercent_IsUnavailable()
    {
        var info = new BatteryHealthInfo { HealthPercent = null };
        Assert.Equal(MetricStatus.Unavailable, info.HealthStatus);
    }

    [Theory]
    [InlineData(30, MetricStatus.Good)]
    [InlineData(34.9, MetricStatus.Good)]
    [InlineData(35, MetricStatus.Fair)]
    [InlineData(45, MetricStatus.Fair)]
    [InlineData(46, MetricStatus.Poor)]
    public void TemperatureStatus_DerivedFromCelsius(double temp, MetricStatus expected)
    {
        var info = new BatteryHealthInfo { TemperatureCelsius = temp };
        Assert.Equal(expected, info.TemperatureStatus);
    }

    [Theory]
    [InlineData(100, MetricStatus.Good)]
    [InlineData(299, MetricStatus.Good)]
    [InlineData(300, MetricStatus.Fair)]
    [InlineData(700, MetricStatus.Fair)]
    [InlineData(701, MetricStatus.Poor)]
    public void CycleStatus_DerivedFromCount(int cycles, MetricStatus expected)
    {
        var info = new BatteryHealthInfo { CycleCount = cycles };
        Assert.Equal(expected, info.CycleStatus);
    }

    [Fact]
    public void Recommendation_PoorHealth_SuggestsReplacement()
    {
        var info = new BatteryHealthInfo { HealthPercent = 50 };
        Assert.Contains("replacing", info.RecommendationMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Recommendation_GoodHealth_PositiveMessage()
    {
        var info = new BatteryHealthInfo { HealthPercent = 95, CycleCount = 100, TemperatureCelsius = 25 };
        Assert.Contains("healthy", info.RecommendationMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Recommendation_AllNull_ShowsUnavailable()
    {
        // All null means unavailable → no data to assess
        var info = new BatteryHealthInfo();
        Assert.Contains("not available", info.RecommendationMessage, StringComparison.OrdinalIgnoreCase);
    }
}
