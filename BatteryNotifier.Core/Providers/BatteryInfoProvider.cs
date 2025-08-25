using System.Management;

namespace BatteryNotifier.Core.Providers
{
    /// <summary>
    /// Singleton provider that mimics SystemInformation.PowerStatus
    /// but without dependency on System.Windows.Forms.
    /// Uses WMI (Win32_Battery + Power events) instead.
    /// </summary>
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
            var batteryInfo = new BatteryInfo
            {
                BatteryChargeStatus = BatteryChargeStatus.Unknown,
                PowerLineStatus = BatteryPowerLineStatus.Unknown,
                BatteryLifePercent = 0,
                BatteryLifeRemaining = -1 // unknown
            };

            try
            {
                using var batterySearcher = new ManagementObjectSearcher("SELECT * FROM Win32_Battery");
                foreach (var battery in batterySearcher.Get())
                {
                    // Battery percentage
                    if (battery["EstimatedChargeRemaining"] is int chargeRemaining)
                    {
                        batteryInfo.BatteryLifePercent = chargeRemaining / 100f;

                        if (chargeRemaining > 66)
                            batteryInfo.BatteryChargeStatus = BatteryChargeStatus.High;
                        else if (chargeRemaining > 33)
                            batteryInfo.BatteryChargeStatus = BatteryChargeStatus.Low;
                        else if (chargeRemaining > 10)
                            batteryInfo.BatteryChargeStatus = BatteryChargeStatus.Critical;
                        else
                            batteryInfo.BatteryChargeStatus = BatteryChargeStatus.Critical;
                    }

                    // Remaining life (in seconds, if available)
                    if (battery["EstimatedRunTime"] is int runTime && runTime > 0)
                    {
                        batteryInfo.BatteryLifeRemaining = runTime * 60; // minutes → seconds
                    }

                    // Charging state
                    if (battery["BatteryStatus"] is int batteryStatus)
                    {
                        // 1 = Discharging, 2 = AC (charging), 3 = Fully Charged
                        if (batteryStatus == 2)
                        {
                            batteryInfo.BatteryChargeStatus = BatteryChargeStatus.Charging;
                            batteryInfo.PowerLineStatus = BatteryPowerLineStatus.Online;
                        }
                        else if (batteryStatus == 1)
                        {
                            batteryInfo.PowerLineStatus = BatteryPowerLineStatus.Offline;
                        }
                        else if (batteryStatus == 3)
                        {
                            batteryInfo.PowerLineStatus = BatteryPowerLineStatus.Online;
                        }
                    }
                }

                // As a fallback, check power events (if present)
                try
                {
                    using var acSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_PowerManagementEvent");
                    var acAdapters = acSearcher.Get();
                    if (acAdapters.Count > 0 && batteryInfo.PowerLineStatus == BatteryPowerLineStatus.Unknown)
                    {
                        batteryInfo.PowerLineStatus = BatteryPowerLineStatus.Online;
                    }
                }
                catch
                {
                    // ignore failures
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting battery information: {ex.Message}");
                batteryInfo.BatteryChargeStatus = BatteryChargeStatus.Unknown;
                batteryInfo.PowerLineStatus = BatteryPowerLineStatus.Unknown;
            }

            return batteryInfo;
        }
    }
}

public class BatteryInfo
{
    public BatteryChargeStatus BatteryChargeStatus { get; set; }
    public BatteryPowerLineStatus PowerLineStatus { get; set; }

    /// <summary>
    /// Battery percentage in range 0.0–1.0 (like PowerStatus.BatteryLifePercent).
    /// </summary>
    public float BatteryLifePercent { get; set; }

    /// <summary>
    /// Estimated remaining battery life in seconds (-1 = unknown).
    /// </summary>
    public int BatteryLifeRemaining { get; set; }
}


public enum BatteryChargeStatus
{
    High = 1,

    /// <summary>Indicates a low level of battery charge.</summary>
    Low = 2,

    /// <summary>Indicates a critically low level of battery charge.</summary>
    Critical = 4,

    /// <summary>Indicates a battery is charging.</summary>
    Charging = 8,

    /// <summary>Indicates that no battery is present.</summary>
    NoSystemBattery = 128, // 0x00000080

    /// <summary>Indicates an unknown battery condition.</summary>
    Unknown = 255, // 0x000000FF
}

public enum BatteryPowerLineStatus
{
    /// <summary>The system is offline.</summary>
    Offline = 0,

    /// <summary>The system is online.</summary>
    Online = 1,

    /// <summary>The power status of the system is unknown.</summary>
    Unknown = 255, // 0x000000FF
}