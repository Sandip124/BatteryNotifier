using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace BatteryNotifier.Core.Providers;

public sealed class BatteryInfoProvider
{
    private static readonly Lazy<BatteryInfoProvider> _instance =
        new(() => new BatteryInfoProvider());

    public static BatteryInfoProvider Instance => _instance.Value;

    private BatteryInfoProvider()
    {
    }

    public static BatteryInfo GetBatteryInfo()
    {
        if (OperatingSystem.IsWindows())
            return GetBatteryInfoWindows();
        if (OperatingSystem.IsMacOS())
            return GetBatteryInfoMacOs();

        return new BatteryInfo
        {
            BatteryChargeStatus = BatteryChargeStatus.Unknown,
            PowerLineStatus = BatteryPowerLineStatus.Unknown,
            BatteryLifePercent = 0,
            BatteryLifeRemaining = -1
        };
    }

    // ── Windows: kernel32 GetSystemPowerStatus ──
    // More reliable than WMI Win32_Battery for real-time battery state.
    // WMI BatteryStatus values (1=Other,2=Unknown,6=Charging) are often
    // misinterpreted and slow to query; GetSystemPowerStatus is instant.

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemPowerStatus(out SystemPowerStatus status);

    [StructLayout(LayoutKind.Sequential)]
    private struct SystemPowerStatus
    {
        public byte ACLineStatus; // 0=Offline, 1=Online, 255=Unknown
        public byte BatteryFlag; // 1=High, 2=Low, 4=Critical, 8=Charging, 128=NoBattery, 255=Unknown
        public byte BatteryLifePercent; // 0–100, or 255=Unknown
        public byte SystemStatusFlag; // 0 or 1 (power saver)
        public int BatteryLifeTime; // Seconds remaining, or -1
        public int BatteryFullLifeTime; // Seconds to full charge, or -1
    }

    private static BatteryInfo GetBatteryInfoWindows()
    {
        var info = new BatteryInfo
        {
            BatteryChargeStatus = BatteryChargeStatus.Unknown,
            PowerLineStatus = BatteryPowerLineStatus.Unknown,
            BatteryLifePercent = 0,
            BatteryLifeRemaining = -1
        };

        if (!GetSystemPowerStatus(out var ps))
            return info;

        // No battery installed
        if ((ps.BatteryFlag & 128) != 0)
        {
            info.BatteryChargeStatus = BatteryChargeStatus.NoSystemBattery;
            info.PowerLineStatus = BatteryPowerLineStatus.Online;
            return info;
        }

        // AC line status
        info.PowerLineStatus = ps.ACLineStatus switch
        {
            0 => BatteryPowerLineStatus.Offline,
            1 => BatteryPowerLineStatus.Online,
            _ => BatteryPowerLineStatus.Unknown
        };

        // Battery percentage
        if (ps.BatteryLifePercent is >= 0 and <= 100)
        {
            info.BatteryLifePercent = ps.BatteryLifePercent / 100f;
        }

        // Charge status from BatteryFlag.
        // 255 = Unknown — must check before individual bits since 255 has all bits set.
        // 0   = No flags — derive from percentage (common when plugged in, not actively charging).
        if (ps.BatteryFlag == 255)
        {
            info.BatteryChargeStatus = DeriveChargeStatusFromPercent(info.BatteryLifePercent);
        }
        else if ((ps.BatteryFlag & 8) != 0)
        {
            info.BatteryChargeStatus = BatteryChargeStatus.Charging;
        }
        else if ((ps.BatteryFlag & 4) != 0)
        {
            info.BatteryChargeStatus = BatteryChargeStatus.Critical;
        }
        else if ((ps.BatteryFlag & 2) != 0)
        {
            info.BatteryChargeStatus = BatteryChargeStatus.Low;
        }
        else if ((ps.BatteryFlag & 1) != 0)
        {
            info.BatteryChargeStatus = BatteryChargeStatus.High;
        }
        else
        {
            info.BatteryChargeStatus = DeriveChargeStatusFromPercent(info.BatteryLifePercent);
        }

        // Time remaining (seconds), only valid when discharging
        if (ps.BatteryLifeTime >= 0)
        {
            info.BatteryLifeRemaining = ps.BatteryLifeTime;
        }

        return info;
    }

    private static BatteryChargeStatus DeriveChargeStatusFromPercent(float lifePercent)
    {
        var pct = (int)(lifePercent * 100);

        if (pct > 66) return BatteryChargeStatus.High;
        if (pct > 33) return BatteryChargeStatus.Low;
        return BatteryChargeStatus.Critical;
    }

    private static BatteryInfo GetBatteryInfoMacOs()
    {
        var info = new BatteryInfo
        {
            BatteryChargeStatus = BatteryChargeStatus.Unknown,
            PowerLineStatus = BatteryPowerLineStatus.Unknown,
            BatteryLifePercent = 0,
            BatteryLifeRemaining = -1
        };

        // Parse "pmset -g batt" output:
        // Now drawing from 'AC Power'
        //  -InternalBattery-0 (id=...)	72%; charging; 1:23 remaining present: true
        var output = RunProcess("pmset", "-g batt");
        if (string.IsNullOrWhiteSpace(output))
            return info;

        var lines = output.Split('\n');

        ParsePowerSource(lines, info);

        var batteryLine = FindBatteryLine(lines);
        if (batteryLine != null)
            ParseBatteryLine(batteryLine, info);
        else if (!output.Contains("InternalBattery", StringComparison.OrdinalIgnoreCase))
        {
            // No battery line found — desktop Mac
            info.BatteryChargeStatus = BatteryChargeStatus.NoSystemBattery;
            info.PowerLineStatus = BatteryPowerLineStatus.Online;
        }

        return info;
    }

    private static void ParsePowerSource(string[] lines, BatteryInfo info)
    {
        if (lines.Length == 0) return;

        var firstLine = lines[0];
        if (firstLine.Contains("AC Power", StringComparison.OrdinalIgnoreCase))
            info.PowerLineStatus = BatteryPowerLineStatus.Online;
        else if (firstLine.Contains("Battery Power", StringComparison.OrdinalIgnoreCase))
            info.PowerLineStatus = BatteryPowerLineStatus.Offline;
    }

    private static string? FindBatteryLine(string[] lines)
    {
        for (int i = 1; i < lines.Length; i++)
        {
            if (lines[i].Contains("InternalBattery", StringComparison.OrdinalIgnoreCase))
                return lines[i];
        }

        return null;
    }

    private static readonly Regex BatteryPercentRegex = new(@"(\d+)%", RegexOptions.Compiled, TimeSpan.FromSeconds(1));
    private static readonly Regex TimeRemainingRegex = new(@"(\d+):(\d+) remaining", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    private static void ParseBatteryLine(string line, BatteryInfo info)
    {
        ParseBatteryPercent(line, info);
        ParseChargingStatus(line, info);
        ParseTimeRemaining(line, info);
    }

    private static void ParseBatteryPercent(string line, BatteryInfo info)
    {
        var match = BatteryPercentRegex.Match(line);
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out int percent))
            return;

        info.BatteryLifePercent = percent / 100f;
        info.BatteryChargeStatus = DeriveChargeStatusFromPercent(info.BatteryLifePercent);
    }

    private static void ParseChargingStatus(string line, BatteryInfo info)
    {
        if (line.Contains("charging", StringComparison.OrdinalIgnoreCase) &&
            !line.Contains("discharging", StringComparison.OrdinalIgnoreCase) &&
            !line.Contains("not charging", StringComparison.OrdinalIgnoreCase))
        {
            info.BatteryChargeStatus = BatteryChargeStatus.Charging;
            info.PowerLineStatus = BatteryPowerLineStatus.Online;
        }
        else if (line.Contains("charged", StringComparison.OrdinalIgnoreCase))
        {
            info.PowerLineStatus = BatteryPowerLineStatus.Online;
        }
    }

    private static void ParseTimeRemaining(string line, BatteryInfo info)
    {
        var match = TimeRemainingRegex.Match(line);
        if (match.Success &&
            int.TryParse(match.Groups[1].Value, out int hours) &&
            int.TryParse(match.Groups[2].Value, out int minutes))
        {
            info.BatteryLifeRemaining = (hours * 3600) + (minutes * 60);
        }
    }

    private static string RunProcess(string fileName, string arguments)
    {
        try
        {
            using var process = new Process();
            var psi = new ProcessStartInfo
            {
                FileName = Constants.ResolveCommand(fileName),
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            // Use ArgumentList for safe argument passing (no shell injection)
            foreach (var arg in arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                psi.ArgumentList.Add(arg);
            process.StartInfo = psi;
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            if (!process.WaitForExit(Constants.ProcessTimeoutShortMs) && !process.HasExited)
                process.Kill();
            return output;
        }
        catch
        {
            return string.Empty;
        }
    }

    public static bool HasBattery()
    {
        var info = GetBatteryInfo();
        return info.BatteryChargeStatus != BatteryChargeStatus.NoSystemBattery &&
               info.BatteryChargeStatus != BatteryChargeStatus.Unknown;
    }
}

public sealed class BatteryInfo
{
    public BatteryChargeStatus BatteryChargeStatus { get; set; }
    public BatteryPowerLineStatus PowerLineStatus { get; set; }
    public float BatteryLifePercent { get; set; }
    public int BatteryLifeRemaining { get; set; }
}

public enum BatteryChargeStatus
{
    None = 0,
    High = 1,
    Low = 2,
    Critical = 4,
    Charging = 8,
    NoSystemBattery = 128,
    Unknown = 255,
}

public enum BatteryPowerLineStatus
{
    Offline = 0,
    Online = 1,
    Unknown = 255,
}