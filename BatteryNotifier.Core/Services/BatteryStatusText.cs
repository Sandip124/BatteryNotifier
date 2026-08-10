using BatteryNotifier.Core.Store;
using BatteryNotifier.Core.Utils;

namespace BatteryNotifier.Core.Services;

/// <summary>
/// Builds the main window's contextual battery status line and time-remaining text.
/// Keeps the copy and level thresholds out of the view model.
/// </summary>
public static class BatteryStatusText
{
    // Charge-level thresholds (%) that switch the status-line copy.
    private const int UnplugPct = 80;
    private const int HighChargePct = 70;
    private const int MidChargePct = 50;
    private const int CriticalPct = 5;
    private const int LowPct = 20;
    private const int HalfPct = 50;

    /// <summary>
    /// One contextual line combining status, time, and a charge tip.
    /// e.g. "1h 23m to full · Unplug at 80%" / "2h 15m remaining" / "Unplug now — extend battery lifespan".
    /// </summary>
    public static string BuildStatusLine(BatteryManagerStore store) 
    {
        if (store.HasNoBattery || store.IsUnknown) return string.Empty;

        var pct = (int)store.BatteryLifePercent;
        var time = FormatTimeShort(store);

        return (store.IsCharging || store.IsPluggedIn)
            ? ChargingStatusLine(pct, time)
            : DischargingStatusLine(pct, time);
    }

    /// <summary>Short status label, e.g. "Charging" / "Plugged In" / "Discharging".</summary>
    public static string StatusLabel(BatteryManagerStore store) =>
        store.HasNoBattery ? "No Battery"
        : store.IsUnknown ? "Unknown"
        : store.IsCharging ? "Charging"
        : store.IsPluggedIn ? "Plugged In"
        : "Discharging";

    /// <summary>Longer time-remaining phrase for the dedicated time label.</summary>
    public static string FormatTimeRemaining(BatteryManagerStore store)
    {
        var timeStr = TimeFormat.HoursMinutes(store.BatteryLifeRemainingInSeconds);
        return store.IsCharging
            ? $"{timeStr} to full charge"
            : $"{timeStr} of battery remaining";
    }

    private static string ChargingStatusLine(int pct, string? time)
    {
        if (pct >= UnplugPct) return "Unplug now — extend battery lifespan";
        if (pct >= HighChargePct) return WithTime(time, "to full · Unplug at 80%", "Unplug at 80% for longevity");
        if (pct >= MidChargePct) return WithTime(time, "to full", "Optimal range is 20–80%");
        return WithTime(time, "to full", "Charging — avoid draining below 20%");
    }

    private static string DischargingStatusLine(int pct, string? time)
    {
        if (pct <= CriticalPct) return WithTime(time, "left · Plug in now", "Critical — plug in now");
        if (pct <= LowPct) return WithTime(time, "left · Plug in soon", "Low — plug in soon");
        if (pct <= HalfPct) return WithTime(time, "remaining", "Keep above 20% for battery health");
        return WithTime(time, "remaining", "Battery in good shape");
    }

    private static string WithTime(string? time, string suffix, string fallback) =>
        time != null ? $"{time} {suffix}" : fallback;

    private static string? FormatTimeShort(BatteryManagerStore store) =>
        store.BatteryLifeRemaining <= 0 ? null : TimeFormat.HoursMinutes(store.BatteryLifeRemainingInSeconds);
}
