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

    /// <summary>
    /// When true, the battery cannot sustain the device unplugged.
    /// Overrides HealthStatus to Poor regardless of capacity ratio.
    /// Set by runtime observation (e.g., 0s remaining, instant shutdown on unplug).
    /// </summary>
    public bool CannotSustainLoad { get; set; }

    public MetricStatus HealthStatus
    {
        get
        {
            if (CannotSustainLoad) return MetricStatus.Poor;

            return HealthPercent switch
            {
                null => MetricStatus.Unavailable,
                >= 80 => MetricStatus.Good,
                >= 60 => MetricStatus.Fair,
                _ => MetricStatus.Poor
            };
        }
    }

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
        if (other.CannotSustainLoad) CannotSustainLoad = true;
    }
}

public enum MetricStatus
{
    Good,
    Fair,
    Poor,
    Unavailable
}
