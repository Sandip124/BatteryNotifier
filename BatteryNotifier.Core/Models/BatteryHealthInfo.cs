namespace BatteryNotifier.Core.Models;

public sealed class BatteryHealthInfo
{
    public double? HealthPercent { get; set; }
    public int? CycleCount { get; set; }
    public int? DesignCycleCount { get; set; }
    public double? TemperatureCelsius { get; set; }
    public double? VoltageVolts { get; set; }
    public int? TimeRemainingSeconds { get; set; }
    public double? PowerRateWatts { get; set; }

    public MetricStatus HealthStatus => HealthPercent switch
    {
        null => MetricStatus.Unavailable,
        >= 80 => MetricStatus.Good,
        >= 60 => MetricStatus.Fair,
        _ => MetricStatus.Poor
    };

    public MetricStatus TemperatureStatus => TemperatureCelsius switch
    {
        null => MetricStatus.Unavailable,
        < 35 => MetricStatus.Good,
        <= 45 => MetricStatus.Fair,
        _ => MetricStatus.Poor
    };

    public MetricStatus CycleStatus => CycleCount switch
    {
        null => MetricStatus.Unavailable,
        < 300 => MetricStatus.Good,
        <= 700 => MetricStatus.Fair,
        _ => MetricStatus.Poor
    };

    /// <summary>
    /// Merges non-null fields from <paramref name="other"/> into this instance.
    /// </summary>
    public void MergeFrom(BatteryHealthInfo other)
    {
        if (other.HealthPercent.HasValue) HealthPercent = other.HealthPercent;
        if (other.CycleCount.HasValue) CycleCount = other.CycleCount;
        if (other.DesignCycleCount.HasValue) DesignCycleCount = other.DesignCycleCount;
        if (other.TemperatureCelsius.HasValue) TemperatureCelsius = other.TemperatureCelsius;
        if (other.VoltageVolts.HasValue) VoltageVolts = other.VoltageVolts;
        if (other.PowerRateWatts.HasValue) PowerRateWatts = other.PowerRateWatts;
    }

    public string RecommendationMessage
    {
        get
        {
            // Return recommendation based on worst metric
            if (HealthStatus == MetricStatus.Poor)
                return "Battery health is degraded. Consider replacing the battery.";
            if (TemperatureStatus == MetricStatus.Poor)
                return "Battery temperature is high. Avoid charging in hot environments.";
            if (CycleStatus == MetricStatus.Poor)
                return "High cycle count. Battery capacity may be reduced.";
            if (HealthStatus == MetricStatus.Fair || CycleStatus == MetricStatus.Fair)
                return "Battery is aging normally. Monitor for changes.";
            if (TemperatureStatus == MetricStatus.Fair)
                return "Temperature slightly elevated. Ensure good ventilation.";
            return "Battery is in good condition.";
        }
    }
}

public enum MetricStatus
{
    Good,
    Fair,
    Poor,
    Unavailable
}
