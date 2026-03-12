using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace BatteryNotifier.Core.Providers
{
    public sealed class BatteryInfoProvider
    {
        private static readonly Lazy<BatteryInfoProvider> _instance =
            new(() => new BatteryInfoProvider());

        public static BatteryInfoProvider Instance => _instance.Value;

        private BatteryInfoProvider()
        {
        }

        public BatteryInfo GetBatteryInfo()
        {
            try
            {
                if (OperatingSystem.IsWindows())
                    return GetBatteryInfoWindows();
                if (OperatingSystem.IsMacOS())
                    return GetBatteryInfoMacOS();
                if (OperatingSystem.IsLinux())
                    return GetBatteryInfoLinux();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting battery information: {ex.Message}");
            }

            return new BatteryInfo
            {
                BatteryChargeStatus = BatteryChargeStatus.Unknown,
                PowerLineStatus = BatteryPowerLineStatus.Unknown,
                BatteryLifePercent = 0,
                BatteryLifeRemaining = -1
            };
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

            try
            {
                // Use System.Management WMI on Windows
                var batterySearcherType = Type.GetType("System.Management.ManagementObjectSearcher, System.Management");
                if (batterySearcherType == null) return info;

                using var searcher = Activator.CreateInstance(batterySearcherType, "SELECT * FROM Win32_Battery") as IDisposable;
                var getMethod = batterySearcherType.GetMethod("Get", Type.EmptyTypes);
                if (getMethod == null || searcher == null) return info;

                var results = getMethod.Invoke(searcher, null) as System.Collections.IEnumerable;
                if (results == null) return info;

                foreach (var battery in results)
                {
                    var indexer = battery.GetType().GetProperty("Item", new[] { typeof(string) });
                    if (indexer == null) continue;

                    if (indexer.GetValue(battery, new object[] { "EstimatedChargeRemaining" }) is int chargeRemaining)
                    {
                        info.BatteryLifePercent = chargeRemaining / 100f;
                        info.BatteryChargeStatus = chargeRemaining > 66
                            ? BatteryChargeStatus.High
                            : chargeRemaining > 33
                                ? BatteryChargeStatus.Low
                                : BatteryChargeStatus.Critical;
                    }

                    if (indexer.GetValue(battery, new object[] { "EstimatedRunTime" }) is int runTime && runTime > 0)
                    {
                        info.BatteryLifeRemaining = runTime * 60;
                    }

                    if (indexer.GetValue(battery, new object[] { "BatteryStatus" }) is int batteryStatus)
                    {
                        if (batteryStatus == 2)
                        {
                            info.BatteryChargeStatus = BatteryChargeStatus.Charging;
                            info.PowerLineStatus = BatteryPowerLineStatus.Online;
                        }
                        else if (batteryStatus == 1)
                        {
                            info.PowerLineStatus = BatteryPowerLineStatus.Offline;
                        }
                        else if (batteryStatus == 3)
                        {
                            info.PowerLineStatus = BatteryPowerLineStatus.Online;
                        }
                    }

                    if (battery is IDisposable d) d.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting Windows battery information: {ex.Message}");
            }

            return info;
        }

        private static BatteryInfo GetBatteryInfoMacOS()
        {
            var info = new BatteryInfo
            {
                BatteryChargeStatus = BatteryChargeStatus.Unknown,
                PowerLineStatus = BatteryPowerLineStatus.Unknown,
                BatteryLifePercent = 0,
                BatteryLifeRemaining = -1
            };

            try
            {
                var output = RunProcess("pmset", "-g batt");
                if (string.IsNullOrWhiteSpace(output))
                    return info;

                // Parse "pmset -g batt" output:
                // Now drawing from 'AC Power'
                //  -InternalBattery-0 (id=...)	72%; charging; 1:23 remaining present: true
                var lines = output.Split('\n');

                // Check power source from first line
                if (lines.Length > 0)
                {
                    var firstLine = lines[0];
                    if (firstLine.Contains("AC Power", StringComparison.OrdinalIgnoreCase))
                        info.PowerLineStatus = BatteryPowerLineStatus.Online;
                    else if (firstLine.Contains("Battery Power", StringComparison.OrdinalIgnoreCase))
                        info.PowerLineStatus = BatteryPowerLineStatus.Offline;
                }

                // Parse battery line
                for (int i = 1; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (!line.Contains("InternalBattery", StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Extract percentage
                    var percentMatch = Regex.Match(line, @"(\d+)%");
                    if (percentMatch.Success && int.TryParse(percentMatch.Groups[1].Value, out int percent))
                    {
                        info.BatteryLifePercent = percent / 100f;
                        info.BatteryChargeStatus = percent > 66
                            ? BatteryChargeStatus.High
                            : percent > 33
                                ? BatteryChargeStatus.Low
                                : BatteryChargeStatus.Critical;
                    }

                    // Check charging status
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

                    // Extract time remaining (e.g., "1:23 remaining")
                    var timeMatch = Regex.Match(line, @"(\d+):(\d+) remaining");
                    if (timeMatch.Success &&
                        int.TryParse(timeMatch.Groups[1].Value, out int hours) &&
                        int.TryParse(timeMatch.Groups[2].Value, out int minutes))
                    {
                        info.BatteryLifeRemaining = (hours * 3600) + (minutes * 60);
                    }

                    break; // only need first battery
                }

                // If no battery line found, might be a desktop Mac
                if (!output.Contains("InternalBattery", StringComparison.OrdinalIgnoreCase))
                {
                    info.BatteryChargeStatus = BatteryChargeStatus.NoSystemBattery;
                    info.PowerLineStatus = BatteryPowerLineStatus.Online;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting macOS battery information: {ex.Message}");
            }

            return info;
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

            try
            {
                // Try /sys/class/power_supply/
                var powerSupplyDir = "/sys/class/power_supply";
                if (!Directory.Exists(powerSupplyDir))
                {
                    info.BatteryChargeStatus = BatteryChargeStatus.NoSystemBattery;
                    return info;
                }

                string? batteryPath = null;
                foreach (var dir in Directory.GetDirectories(powerSupplyDir))
                {
                    var typePath = Path.Combine(dir, "type");
                    if (File.Exists(typePath) &&
                        File.ReadAllText(typePath).Trim().Equals("Battery", StringComparison.OrdinalIgnoreCase))
                    {
                        batteryPath = dir;
                        break;
                    }
                }

                if (batteryPath == null)
                {
                    info.BatteryChargeStatus = BatteryChargeStatus.NoSystemBattery;
                    info.PowerLineStatus = BatteryPowerLineStatus.Online;
                    return info;
                }

                // Read capacity
                var capacityPath = Path.Combine(batteryPath, "capacity");
                if (File.Exists(capacityPath) &&
                    int.TryParse(File.ReadAllText(capacityPath).Trim(), out int capacity))
                {
                    info.BatteryLifePercent = capacity / 100f;
                    info.BatteryChargeStatus = capacity > 66
                        ? BatteryChargeStatus.High
                        : capacity > 33
                            ? BatteryChargeStatus.Low
                            : BatteryChargeStatus.Critical;
                }

                // Read status
                var statusPath = Path.Combine(batteryPath, "status");
                if (File.Exists(statusPath))
                {
                    var status = File.ReadAllText(statusPath).Trim();
                    switch (status.ToLowerInvariant())
                    {
                        case "charging":
                            info.BatteryChargeStatus = BatteryChargeStatus.Charging;
                            info.PowerLineStatus = BatteryPowerLineStatus.Online;
                            break;
                        case "discharging":
                            info.PowerLineStatus = BatteryPowerLineStatus.Offline;
                            break;
                        case "full":
                            info.PowerLineStatus = BatteryPowerLineStatus.Online;
                            break;
                        case "not charging":
                            info.PowerLineStatus = BatteryPowerLineStatus.Online;
                            break;
                    }
                }

                // Estimate time remaining from energy/power readings
                info.BatteryLifeRemaining = EstimateLinuxTimeRemaining(batteryPath, info);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting Linux battery information: {ex.Message}");
            }

            return info;
        }

        /// <summary>
        /// Estimates time remaining in seconds from sysfs energy_now/power_now or charge_now/current_now.
        /// Returns -1 if estimation is not possible.
        /// </summary>
        private static int EstimateLinuxTimeRemaining(string batteryPath, BatteryInfo info)
        {
            if (info.BatteryChargeStatus == BatteryChargeStatus.Charging)
                return -1; // Can't estimate charge time reliably

            try
            {
                // Try energy-based (µWh / µW)
                var energyNowPath = Path.Combine(batteryPath, "energy_now");
                var powerNowPath = Path.Combine(batteryPath, "power_now");
                if (File.Exists(energyNowPath) && File.Exists(powerNowPath))
                {
                    if (long.TryParse(File.ReadAllText(energyNowPath).Trim(), out long energyNow) &&
                        long.TryParse(File.ReadAllText(powerNowPath).Trim(), out long powerNow) &&
                        powerNow > 0)
                    {
                        return (int)((double)energyNow / powerNow * 3600);
                    }
                }

                // Fallback: charge-based (µAh / µA)
                var chargeNowPath = Path.Combine(batteryPath, "charge_now");
                var currentNowPath = Path.Combine(batteryPath, "current_now");
                if (File.Exists(chargeNowPath) && File.Exists(currentNowPath))
                {
                    if (long.TryParse(File.ReadAllText(chargeNowPath).Trim(), out long chargeNow) &&
                        long.TryParse(File.ReadAllText(currentNowPath).Trim(), out long currentNow) &&
                        currentNow > 0)
                    {
                        return (int)((double)chargeNow / currentNow * 3600);
                    }
                }
            }
            catch
            {
                // Best effort
            }

            return -1;
        }

        private static string RunProcess(string fileName, string arguments)
        {
            try
            {
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(3000);
                return output;
            }
            catch
            {
                return string.Empty;
            }
        }

        public bool HasBattery()
        {
            var info = GetBatteryInfo();
            return info.BatteryChargeStatus != BatteryChargeStatus.NoSystemBattery &&
                   info.BatteryChargeStatus != BatteryChargeStatus.Unknown;
        }
    }
}

public class BatteryInfo
{
    public BatteryChargeStatus BatteryChargeStatus { get; set; }
    public BatteryPowerLineStatus PowerLineStatus { get; set; }
    public float BatteryLifePercent { get; set; }
    public int BatteryLifeRemaining { get; set; }
}


public enum BatteryChargeStatus
{
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
