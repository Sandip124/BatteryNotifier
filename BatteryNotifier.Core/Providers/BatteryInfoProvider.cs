using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using BatteryNotifier.Core.Logger;
using Serilog;

namespace BatteryNotifier.Core.Providers;

public sealed class BatteryInfoProvider
{
    private static readonly Lazy<BatteryInfoProvider> _instance =
        new(() => new BatteryInfoProvider());

    public static BatteryInfoProvider Instance => _instance.Value;

    private static readonly ILogger Logger = BatteryNotifierAppLogger.ForContext("BatteryInfoProvider");

    private BatteryInfoProvider()
    {
    }

    public static BatteryInfo GetBatteryInfo()
    {
        if (OperatingSystem.IsWindows())
            return GetBatteryInfoWindows();
        if (OperatingSystem.IsMacOS())
            return GetBatteryInfoMacOs();
        if (OperatingSystem.IsLinux())
            return GetBatteryInfoLinux();

        return new BatteryInfo
        {
            BatteryChargeStatus = BatteryChargeStatus.Unknown,
            PowerLineStatus = BatteryPowerLineStatus.Unknown,
            BatteryLifePercent = 0,
            BatteryLifeRemaining = -1
        };
    }

    private static BatteryInfo GetBatteryInfoLinux()
    {
        var info = new BatteryInfo
        {
            BatteryChargeStatus = BatteryChargeStatus.Unknown,
            PowerLineStatus = BatteryPowerLineStatus.Unknown,
            BatteryLifePercent = 0,
            BatteryLifeRemaining = -1
        };

        const string root = "/sys/class/power_supply";
        try
        {
            if (!Directory.Exists(root))
            {
                Logger.Warning("Linux battery: {Root} not present", root);
                return info;
            }

            var batteryDir = FindLinuxBattery(root);
            if (batteryDir == null)
            {
                info.BatteryChargeStatus = BatteryChargeStatus.NoSystemBattery;
                info.PowerLineStatus = BatteryPowerLineStatus.Online;
                Logger.Information("Linux battery: no Battery-type supply under {Root} — treating as desktop", root);
                return info;
            }

            var capacity = ReadIntSys(batteryDir, "capacity");
            if (capacity is >= 0 and <= 100)
            {
                info.BatteryLifePercent = capacity.Value / 100f;
            }
            else
            {
                var nowCap = ReadLongSys(batteryDir, "energy_now") ?? ReadLongSys(batteryDir, "charge_now");
                var fullCap = ReadLongSys(batteryDir, "energy_full") ?? ReadLongSys(batteryDir, "charge_full");
                if (nowCap is >= 0 && fullCap is > 0)
                    info.BatteryLifePercent = Math.Clamp((float)nowCap.Value / fullCap.Value, 0f, 1f);
            }

            var status = ReadSys(batteryDir, "status") ?? "Unknown";
            var acOnline = ReadLinuxAcOnline(root);

            info.PowerLineStatus = acOnline switch
            {
                true => BatteryPowerLineStatus.Online,
                false => BatteryPowerLineStatus.Offline,
                _ => status.Equals("Discharging", StringComparison.OrdinalIgnoreCase)
                    ? BatteryPowerLineStatus.Offline
                    : BatteryPowerLineStatus.Online
            };

            info.BatteryChargeStatus = status switch
            {
                "Charging" => BatteryChargeStatus.Charging,
                "Full" => BatteryChargeStatus.High,
                _ => DeriveChargeStatusFromPercent(info.BatteryLifePercent)
            };

            info.BatteryLifeRemaining = EstimateLinuxSecondsRemaining(batteryDir, status);

            var full = ReadLongSys(batteryDir, "energy_full") ?? ReadLongSys(batteryDir, "charge_full");
            var design = ReadLongSys(batteryDir, "energy_full_design") ?? ReadLongSys(batteryDir, "charge_full_design");
            int? healthPct = full is > 0 && design is > 0
                ? (int)Math.Round(100.0 * full.Value / design.Value)
                : null;

            LogBatteryDiag("Linux /sys/class/power_supply", info,
                $"name={Path.GetFileName(batteryDir)} capacity={capacity} status={status} ac={acOnline} full={full} design={design} healthPct={healthPct}");
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Linux battery: failed to read {Root}", root);
        }

        return info;
    }

    // Logs the battery-fetch approach + values, but only when they change (polling runs every ~1s).
    private static string _lastBatteryDiag = "";
    private static void LogBatteryDiag(string approach, BatteryInfo info, string? extra = null)
    {
        var msg = $"{approach} → percent={info.BatteryLifePercent:P0} charge={info.BatteryChargeStatus} " +
                  $"power={info.PowerLineStatus} secondsLeft={info.BatteryLifeRemaining}" +
                  (extra != null ? " | " + extra : string.Empty);

        if (msg == _lastBatteryDiag) return;
        _lastBatteryDiag = msg;
        Logger.Information("Battery {Diag}", msg);
    }

    private static string? FindLinuxBattery(string root)
    {
        foreach (var dir in Directory.EnumerateDirectories(root))
        {
            var type = ReadSys(dir, "type");
            if (string.Equals(type, "Battery", StringComparison.OrdinalIgnoreCase))
                return dir;
        }
        return null;
    }

    private static bool? ReadLinuxAcOnline(string root)
    {
        bool? result = null;
        foreach (var dir in Directory.EnumerateDirectories(root))
        {
            var type = ReadSys(dir, "type");
            if (!string.Equals(type, "Mains", StringComparison.OrdinalIgnoreCase)) continue;

            var online = ReadIntSys(dir, "online");
            if (online == 1) return true;
            if (online == 0) result = false;
        }
        return result;
    }

    private static int EstimateLinuxSecondsRemaining(string dir, string status)
    {
        if (!status.Equals("Discharging", StringComparison.OrdinalIgnoreCase)) return -1;

        var now = ReadLongSys(dir, "energy_now") ?? ReadLongSys(dir, "charge_now");
        var rate = ReadLongSys(dir, "power_now") ?? ReadLongSys(dir, "current_now");
        if (now is > 0 && rate is > 0)
            return (int)(3600.0 * now.Value / rate.Value);

        return -1;
    }

    private static string? ReadSys(string dir, string file)
    {
        var path = Path.Combine(dir, file);
        try { return File.Exists(path) ? File.ReadAllText(path).Trim() : null; }
        catch (Exception ex) { Logger.Debug(ex, "Linux battery: cannot read {Path}", path); return null; }
    }

    private static int? ReadIntSys(string dir, string file) =>
        int.TryParse(ReadSys(dir, file), out var v) ? v : null;

    private static long? ReadLongSys(string dir, string file) =>
        long.TryParse(ReadSys(dir, file), out var v) ? v : null;

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

        LogBatteryDiag("Windows GetSystemPowerStatus", info,
            $"acLine={ps.ACLineStatus} flag={ps.BatteryFlag} rawPct={ps.BatteryLifePercent}");
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

        LogBatteryDiag("macOS pmset", info);
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