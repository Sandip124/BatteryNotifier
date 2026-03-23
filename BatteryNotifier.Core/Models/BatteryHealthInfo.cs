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

    /// <summary>Current drain rate in percent-per-minute. Positive = draining. Null if charging or insufficient data.</summary>
    public double? DrainRatePerMinute { get; set; }

    /// <summary>True when battery is draining abnormally fast (>3%/min).</summary>
    public bool IsRapidDrain { get; set; }

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

    public string RecommendationMessage
    {
        get
        {
            if (HealthStatus == MetricStatus.Unavailable
                && CycleStatus == MetricStatus.Unavailable
                && TemperatureStatus == MetricStatus.Unavailable)
                return "Battery health data is not available on this device.";

            if (CannotSustainLoad)
                return "Battery cannot sustain the device without charger. Replacement recommended.";

            if (TemperatureStatus == MetricStatus.Poor)
                return "Battery temperature is high. Avoid charging in hot environments.";
            if (TemperatureStatus == MetricStatus.Fair)
                return "Temperature slightly elevated. Ensure good ventilation.";

            return BuildPersonalizedAdvice();
        }
    }

    private string BuildPersonalizedAdvice()
    {
        var pct = HealthPercent;
        var cycles = CycleCount;
        var wearPct = pct.HasValue ? Math.Round(100 - pct.Value, 1) : (double?)null;

        // Both capacity and cycles available — give specific wear analysis
        if (pct.HasValue && cycles is > 0)
            return BuildWearAnalysis(pct.Value, wearPct!.Value, cycles.Value);

        // Only capacity
        if (pct.HasValue)
            return BuildCapacityOnlyAdvice(pct.Value, wearPct!.Value);

        // Only cycles
        if (cycles is > 0)
            return BuildCyclesOnlyAdvice(cycles.Value);

        return "Limited battery data available. Some metrics could not be read.";
    }

    private static string BuildWearAnalysis(double healthPct, double wearPct, int cycles)
    {
        // Wear rate per cycle: how much capacity lost per charge cycle
        var wearPerCycle = cycles > 0 ? wearPct / cycles : 0;

        // Estimate remaining cycles before 80% threshold (service territory)
        var pctAbove80 = healthPct - 80;
        var estimatedCyclesLeft = wearPerCycle > 0.001 ? (int)(pctAbove80 / wearPerCycle) : 0;

        if (healthPct >= 95)
            return $"{healthPct:F0}% capacity after {cycles} cycles — excellent condition.";

        if (healthPct >= 80)
        {
            if (estimatedCyclesLeft > 100)
                return $"{wearPct:F0}% wear after {cycles} cycles — ~{estimatedCyclesLeft} cycles before service.";
            if (estimatedCyclesLeft > 0)
                return $"{wearPct:F0}% wear after {cycles} cycles — nearing service range. Keep charges between 20–80%.";
            return $"{healthPct:F0}% capacity after {cycles} cycles — healthy wear rate.";
        }

        if (healthPct >= 60)
            return $"{wearPct:F0}% capacity lost over {cycles} cycles — above average wear. Consider replacement soon.";

        return $"Only {healthPct:F0}% capacity remaining after {cycles} cycles. Replacement recommended.";
    }

    private static string BuildCapacityOnlyAdvice(double healthPct, double wearPct)
    {
        if (healthPct >= 90)
            return $"{healthPct:F0}% capacity remaining — battery is in great shape.";
        if (healthPct >= 80)
            return $"{wearPct:F0}% capacity lost — normal wear. Charge between 20–80% to slow it.";
        if (healthPct >= 60)
            return $"{wearPct:F0}% capacity lost — noticeable degradation. Consider replacement soon.";
        return $"Only {healthPct:F0}% capacity remaining. Replacement recommended.";
    }

    private static string BuildCyclesOnlyAdvice(int cycles)
    {
        if (cycles < 300)
            return $"{cycles} charge cycles — low usage, battery should be in good shape.";
        if (cycles < 700)
            return $"{cycles} charge cycles — moderate usage. Keep charges between 20–80%.";
        return $"{cycles} charge cycles — high usage. Battery capacity may be significantly reduced.";
    }
}

public enum MetricStatus
{
    Good,
    Fair,
    Poor,
    Unavailable
}
