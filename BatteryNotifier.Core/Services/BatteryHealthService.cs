using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using BatteryNotifier.Core.Logger;
using BatteryNotifier.Core.Models;
using Serilog;

namespace BatteryNotifier.Core.Services;

/// <summary>
/// Polls battery health metrics at 60s intervals.
/// Windows: WMI root\WMI classes. macOS: ioreg -r -c AppleSmartBattery.
/// </summary>
public sealed class BatteryHealthService : IDisposable
{
    private static readonly Lazy<BatteryHealthService> _instance = new(() => new BatteryHealthService());
    public static BatteryHealthService Instance => _instance.Value;

    private static readonly ILogger Logger = BatteryNotifierAppLogger.ForContext("BatteryHealthService");
    private CancellationTokenSource? _cts;
    private bool _disposed;
    private bool _activePolling;

    /// <summary>Background poll every 15 min. Active poll every 30s when dashboard is open.</summary>
    private static readonly TimeSpan BackgroundInterval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ActiveInterval = TimeSpan.FromSeconds(30);

    public BatteryHealthInfo? LatestHealth { get; private set; }
    public event EventHandler<BatteryHealthInfo>? HealthUpdated;

    private BatteryHealthService()
    {
        _cts = new CancellationTokenSource();
        _ = RunPollingAsync(_cts.Token);
    }

    /// <summary>
    /// Call when the health dashboard becomes visible — switches to 30s active polling.
    /// </summary>
    public void SetActivePolling(bool active)
    {
        _activePolling = active;
    }

    private async Task RunPollingAsync(CancellationToken ct)
    {
        // Delay initial fetch so it doesn't block app startup
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(10), ct).ConfigureAwait(false);
            FetchAndPublish();
        }
        catch (OperationCanceledException) { return; }
        catch (Exception ex) { Logger.Warning(ex, "Initial health check failed"); }

        // Adaptive polling loop
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var interval = _activePolling ? ActiveInterval : BackgroundInterval;
                await Task.Delay(interval, ct).ConfigureAwait(false);
                FetchAndPublish();
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { Logger.Warning(ex, "Health poll failed"); }
        }
    }

    private void FetchAndPublish()
    {
        var info = FetchHealthInfo();
        LatestHealth = info;
        HealthUpdated?.Invoke(this, info);
    }

    public BatteryHealthInfo Refresh()
    {
        var info = FetchHealthInfo();
        LatestHealth = info;
        HealthUpdated?.Invoke(this, info);
        return info;
    }

    private static BatteryHealthInfo FetchHealthInfo()
    {
        if (OperatingSystem.IsMacOS())
            return FetchMacHealth();
        if (OperatingSystem.IsLinux())
            return FetchLinuxHealth();
#if WINDOWS
        return FetchWindowsHealth();
#else
        return new BatteryHealthInfo();
#endif
    }

    private static BatteryHealthInfo FetchMacHealth()
    {
        var info = new BatteryHealthInfo();

        try
        {
            var output = RunProcess("ioreg", "-r", "-c", "AppleSmartBattery");
            if (string.IsNullOrWhiteSpace(output)) return info;

            info.CycleCount = ParseInt(output, "\"CycleCount\"\\s*=\\s*(\\d+)");
            info.DesignCycleCount = ParseInt(output, "\"DesignCycleCount9C\"\\s*=\\s*(\\d+)");

            // Temperature is in decikelvin (e.g. 3026 = 302.6K = 29.45°C)
            var tempRaw = ParseDouble(output, "\"Temperature\"\\s*=\\s*(\\d+)");
            if (tempRaw.HasValue)
                info.TemperatureCelsius = Math.Round(tempRaw.Value / 10.0 - 273.15, 1);

            // Voltage is in millivolts
            info.VoltageVolts = ParseDouble(output, "\"Voltage\"\\s*=\\s*(\\d+)") / 1000.0;

            // TimeRemaining is in minutes; 65535 = N/A sentinel
            var timeMin = ParseInt(output, "\"TimeRemaining\"\\s*=\\s*(\\d+)");
            if (timeMin.HasValue && timeMin.Value != 65535)
                info.TimeRemainingSeconds = timeMin.Value * 60;

            // Amperage is unsigned 64-bit; negative values (discharge) wrap around.
            // Parse as ulong then cast to signed long to get actual mA.
            var amperageRaw = ParseULong(output, "\"Amperage\"\\s*=\\s*(\\d+)");
            double? amperageMa = amperageRaw.HasValue ? (double)unchecked((long)amperageRaw.Value) : null;

            // AppleRawMaxCapacity is actual mAh capacity (MaxCapacity is always 100% on Apple Silicon)
            var rawMaxCap = ParseDouble(output, "\"AppleRawMaxCapacity\"\\s*=\\s*(\\d+)");
            var designCap = ParseDouble(output, "\"DesignCapacity\"\\s*=\\s*(\\d+)");

            if (rawMaxCap.HasValue && designCap.HasValue && designCap.Value > 0)
            {
                info.HealthPercent = Math.Round(rawMaxCap.Value / designCap.Value * 100, 1);
            }

            // Convert amperage mA to watts: W = |mA| × V / 1000
            if (amperageMa.HasValue && info.VoltageVolts.HasValue)
            {
                info.PowerRateWatts = Math.Round(
                    Math.Abs(amperageMa.Value) * info.VoltageVolts.Value / 1000.0, 2);
            }
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to parse macOS battery health");
        }

        return info;
    }

#if WINDOWS
    private static BatteryHealthInfo FetchWindowsHealth()
    {
        var info = new BatteryHealthInfo();

        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher("root\\WMI",
                "SELECT DesignedCapacity FROM BatteryStaticData");
            long designCap = 0;
            foreach (System.Management.ManagementObject obj in searcher.Get())
            {
                designCap = Convert.ToInt64(obj["DesignedCapacity"]);
                break;
            }

            using var fullSearcher = new System.Management.ManagementObjectSearcher("root\\WMI",
                "SELECT FullChargedCapacity FROM BatteryFullChargedCapacity");
            long fullCap = 0;
            foreach (System.Management.ManagementObject obj in fullSearcher.Get())
            {
                fullCap = Convert.ToInt64(obj["FullChargedCapacity"]);
                break;
            }

            if (designCap > 0 && fullCap > 0)
                info.HealthPercent = Math.Round((double)fullCap / designCap * 100, 1);

            using var cycleSearcher = new System.Management.ManagementObjectSearcher("root\\WMI",
                "SELECT CycleCount FROM BatteryCycleCount");
            foreach (System.Management.ManagementObject obj in cycleSearcher.Get())
            {
                info.CycleCount = Convert.ToInt32(obj["CycleCount"]);
                break;
            }

            using var statusSearcher = new System.Management.ManagementObjectSearcher("root\\WMI",
                "SELECT Voltage, Temperature, DischargeRate FROM BatteryStatus WHERE Voltage > 0");
            foreach (System.Management.ManagementObject obj in statusSearcher.Get())
            {
                var voltage = Convert.ToInt32(obj["Voltage"]);
                if (voltage > 0) info.VoltageVolts = voltage / 1000.0;

                var temp = Convert.ToInt32(obj["Temperature"]);
                if (temp > 0) info.TemperatureCelsius = (temp - 2732) / 10.0; // decikelvin → celsius

                var rate = Convert.ToInt32(obj["DischargeRate"]);
                if (rate != 0) info.PowerRateWatts = Math.Abs(rate) / 1000.0;

                break;
            }
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to query Windows battery health via WMI");
        }

        return info;
    }
#endif

    private static BatteryHealthInfo FetchLinuxHealth()
    {
        var info = new BatteryHealthInfo();

        try
        {
            // Linux exposes battery info via /sys/class/power_supply/BAT*
            var batDir = FindLinuxBatteryDirectory();
            if (batDir == null) return info;

            var energyFull = ReadSysfsLong(Path.Combine(batDir, "energy_full"))
                          ?? ReadSysfsLong(Path.Combine(batDir, "charge_full"));
            var energyDesign = ReadSysfsLong(Path.Combine(batDir, "energy_full_design"))
                            ?? ReadSysfsLong(Path.Combine(batDir, "charge_full_design"));

            if (energyFull.HasValue && energyDesign.HasValue && energyDesign.Value > 0)
                info.HealthPercent = Math.Round((double)energyFull.Value / energyDesign.Value * 100, 1);

            var cycleCount = ReadSysfsLong(Path.Combine(batDir, "cycle_count"));
            if (cycleCount.HasValue && cycleCount.Value > 0)
                info.CycleCount = (int)cycleCount.Value;

            // Temperature: reported in tenths of degree Celsius
            var temp = ReadSysfsLong(Path.Combine(batDir, "temp"));
            if (temp.HasValue)
                info.TemperatureCelsius = temp.Value / 10.0;

            // Voltage: reported in microvolts
            var voltageNow = ReadSysfsLong(Path.Combine(batDir, "voltage_now"));
            if (voltageNow.HasValue)
                info.VoltageVolts = voltageNow.Value / 1_000_000.0;

            // Power: energy-based supplies report power_now in microwatts,
            // charge-based supplies report current_now in microamps
            var powerNow = ReadSysfsLong(Path.Combine(batDir, "power_now"));
            if (powerNow.HasValue)
            {
                info.PowerRateWatts = Math.Round(Math.Abs(powerNow.Value) / 1_000_000.0, 2);
            }
            else
            {
                var currentNow = ReadSysfsLong(Path.Combine(batDir, "current_now"));
                if (currentNow.HasValue && voltageNow.HasValue)
                {
                    info.PowerRateWatts = Math.Round(
                        Math.Abs(currentNow.Value) * voltageNow.Value / 1e12, 2);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to parse Linux battery health");
        }

        return info;
    }

    private static string? FindLinuxBatteryDirectory()
    {
        const string basePath = "/sys/class/power_supply";
        if (!Directory.Exists(basePath)) return null;

        // Look for BAT0, BAT1, etc.
        foreach (var dir in Directory.GetDirectories(basePath))
        {
            var name = Path.GetFileName(dir);
            if (name.StartsWith("BAT", StringComparison.OrdinalIgnoreCase))
            {
                var typePath = Path.Combine(dir, "type");
                if (File.Exists(typePath))
                {
                    var type = File.ReadAllText(typePath).Trim();
                    if (type.Equals("Battery", StringComparison.OrdinalIgnoreCase))
                        return dir;
                }
            }
        }

        return null;
    }

    private static long? ReadSysfsLong(string path)
    {
        if (!File.Exists(path)) return null;
        var text = File.ReadAllText(path).Trim();
        return long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var val)
            ? val : null;
    }

    private static int? ParseInt(string text, string pattern)
    {
        var m = Regex.Match(text, pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
        return m.Success && int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    private static double? ParseDouble(string text, string pattern)
    {
        var m = Regex.Match(text, pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
        return m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    private static ulong? ParseULong(string text, string pattern)
    {
        var m = Regex.Match(text, pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
        return m.Success && ulong.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    private static string RunProcess(string command, params string[] args)
    {
        try
        {
            using var process = new Process();
            var psi = new ProcessStartInfo
            {
                FileName = Constants.ResolveCommand(command),
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            foreach (var arg in args)
                psi.ArgumentList.Add(arg);
            process.StartInfo = psi;
            process.Start();

            var output = process.StandardOutput.ReadToEnd();
            if (output.Length > Constants.MaxProcessOutputLength)
                output = output[..Constants.MaxProcessOutputLength];

            if (!process.WaitForExit(Constants.ProcessTimeoutMs) && !process.HasExited)
                process.Kill();
            return output;
        }
        catch
        {
            return string.Empty;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        HealthUpdated = null;
    }
}
