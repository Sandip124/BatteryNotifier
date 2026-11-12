namespace BatteryNotifier.Core.Utils;

/// <summary>Shared human-readable time formatting.</summary>
public static class TimeFormat
{
    /// <summary>Compact hours/minutes, e.g. "1h 23m" or "23m".</summary>
    public static string HoursMinutes(TimeSpan ts)
    {
        var h = (int)ts.TotalHours;
        return h > 0 ? $"{h}h {ts.Minutes}m" : $"{ts.Minutes}m";
    }

    /// <summary>Relative "time ago": "Just now" / "42s ago" / "5m ago".</summary>
    public static string Ago(TimeSpan elapsed)
    {
        if (elapsed.TotalSeconds < 10) return "Just now";
        if (elapsed.TotalMinutes < 1) return $"{(int)elapsed.TotalSeconds}s ago";
        return $"{(int)elapsed.TotalMinutes}m ago";
    }

    /// <summary>Countdown form: rounds minutes up, drops "0m" on whole hours. "1h 5m" / "1h" / "5m".</summary>
    public static string Countdown(TimeSpan d)
    {
        var totalMinutes = (int)Math.Ceiling(d.TotalMinutes);
        var hours = totalMinutes / 60;
        var minutes = totalMinutes % 60;

        if (hours > 0)
            return minutes > 0 ? $"{hours}h {minutes}m" : $"{hours}h";
        return $"{minutes}m";
    }
}
