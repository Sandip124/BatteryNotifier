using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using BatteryNotifier.Core.Logger;
using BatteryNotifier.Core.Models;
using BatteryNotifier.Core.Utils;
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
        DetectCannotSustainLoad(info);
        LatestHealth = info;
        HealthUpdated?.Invoke(this, info);
    }

    public BatteryHealthInfo Refresh()
    {
        var info = FetchHealthInfo();
        DetectCannotSustainLoad(info);
        LatestHealth = info;
        HealthUpdated?.Invoke(this, info);
        return info;
    }

    /// <summary>
    /// Detects if the battery cannot sustain the device without charger.
    /// Signals: OS reports 0 seconds remaining while battery is present,
    /// or discharge rate is extremely high relative to capacity.
    /// </summary>
    private static void DetectCannotSustainLoad(BatteryHealthInfo info)
    {
        var store = Store.BatteryManagerStore.Instance;
        if (store.HasNoBattery || store.IsUnknown) return;

        // If plugged in and OS reports 0 seconds battery life — battery can't sustain on its own
        if (store.IsPluggedIn && store.BatteryLifeRemaining == 0 && store.BatteryLifePercent > 0)
        {
            info.CannotSustainLoad = true;
            return;
        }

        // If not plugged in and battery percentage is > 0 but time remaining is 0
        // (OS knows the battery drains instantly)
        if (!store.IsPluggedIn && store.BatteryLifePercent > 10 && store.BatteryLifeRemaining == 0)
        {
            info.CannotSustainLoad = true;
        }
    }

    private static BatteryHealthInfo FetchHealthInfo()
    {
        if (OperatingSystem.IsMacOS())
            return FetchMacHealth();
        if (OperatingSystem.IsLinux())
            return FetchLinuxHealth();
#if WINDOWS
        if (OperatingSystem.IsWindows())
            return FetchWindowsHealth();
#endif
        return new BatteryHealthInfo();
    }

    private static BatteryHealthInfo FetchMacHealth()
    {
        var info = new BatteryHealthInfo();

        try
        {
            var output = ProcessRunner.Run("ioreg", "-r", "-c", "AppleSmartBattery");
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
    // ── Windows: IOCTL_BATTERY via DeviceIoControl (no elevation required) ──

    private static readonly Guid GUID_DEVINTERFACE_BATTERY =
        new("72631e54-78a4-11d0-bcf7-00aa00b7b32a");

    private const uint IOCTL_BATTERY_QUERY_TAG = 0x00294040;
    private const uint IOCTL_BATTERY_QUERY_INFORMATION = 0x00294044;
    private const uint IOCTL_BATTERY_QUERY_STATUS = 0x0029404C;

    private const uint DIGCF_PRESENT = 0x02;
    private const uint DIGCF_DEVICEINTERFACE = 0x10;
    private const int INVALID_HANDLE_VALUE = -1;

    private enum BATTERY_QUERY_INFORMATION_LEVEL
    {
        BatteryInformation = 0,
        BatteryTemperature = 2,
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BATTERY_INFORMATION
    {
        public uint Capabilities;
        public byte Technology;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] Reserved;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] Chemistry;
        public uint DesignedCapacity;      // mWh
        public uint FullChargedCapacity;   // mWh
        public uint DefaultAlert1;
        public uint DefaultAlert2;
        public uint CriticalBias;
        public uint CycleCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BATTERY_QUERY_INFORMATION
    {
        public uint BatteryTag;
        public BATTERY_QUERY_INFORMATION_LEVEL InformationLevel;
        public uint AtRate;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BATTERY_WAIT_STATUS
    {
        public uint BatteryTag;
        public uint Timeout;
        public uint PowerState;
        public uint LowCapacity;
        public uint HighCapacity;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BATTERY_STATUS
    {
        public uint PowerState;
        public uint Capacity;     // mWh
        public uint Voltage;      // mV
        public int Rate;          // mW (negative = discharging)
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SP_DEVICE_INTERFACE_DATA
    {
        public uint cbSize;
        public Guid InterfaceClassGuid;
        public uint Flags;
        public IntPtr Reserved;
    }

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern IntPtr SetupDiGetClassDevs(
        ref Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent, uint Flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiEnumDeviceInterfaces(
        IntPtr DeviceInfoSet, IntPtr DeviceInfoData, ref Guid InterfaceClassGuid,
        uint MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiGetDeviceInterfaceDetail(
        IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData,
        IntPtr DeviceInterfaceDetailData, uint DeviceInterfaceDetailDataSize,
        out uint RequiredSize, IntPtr DeviceInfoData);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr CreateFile(
        string lpFileName, uint dwDesiredAccess, uint dwShareMode,
        IntPtr lpSecurityAttributes, uint dwCreationDisposition,
        uint dwFlagsAndAttributes, IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeviceIoControl(
        IntPtr hDevice, uint dwIoControlCode,
        ref uint lpInBuffer, uint nInBufferSize,
        out uint lpOutBuffer, uint nOutBufferSize,
        out uint lpBytesReturned, IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeviceIoControl(
        IntPtr hDevice, uint dwIoControlCode,
        ref BATTERY_QUERY_INFORMATION lpInBuffer, uint nInBufferSize,
        out BATTERY_INFORMATION lpOutBuffer, uint nOutBufferSize,
        out uint lpBytesReturned, IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeviceIoControl(
        IntPtr hDevice, uint dwIoControlCode,
        ref BATTERY_QUERY_INFORMATION lpInBuffer, uint nInBufferSize,
        out uint lpOutBuffer, uint nOutBufferSize,
        out uint lpBytesReturned, IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeviceIoControl(
        IntPtr hDevice, uint dwIoControlCode,
        ref BATTERY_WAIT_STATUS lpInBuffer, uint nInBufferSize,
        out BATTERY_STATUS lpOutBuffer, uint nOutBufferSize,
        out uint lpBytesReturned, IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);

    private static BatteryHealthInfo FetchWindowsHealth()
    {
        var info = new BatteryHealthInfo();

        try
        {
            FetchViaIoctl(info);
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "IOCTL battery query failed, trying WMI fallback");
            try { FetchViaWmi(info); }
            catch (Exception wmiEx) { Logger.Warning(wmiEx, "WMI battery query also failed"); }
        }

        return info;
    }

    private static void FetchViaIoctl(BatteryHealthInfo info)
    {
        var guid = GUID_DEVINTERFACE_BATTERY;
        var hDevInfo = SetupDiGetClassDevs(ref guid, IntPtr.Zero, IntPtr.Zero,
            DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);

        if (hDevInfo == new IntPtr(INVALID_HANDLE_VALUE))
            throw new InvalidOperationException("SetupDiGetClassDevs failed");

        try
        {
            var diData = new SP_DEVICE_INTERFACE_DATA
            {
                cbSize = (uint)Marshal.SizeOf<SP_DEVICE_INTERFACE_DATA>()
            };

            // Get the first battery device
            if (!SetupDiEnumDeviceInterfaces(hDevInfo, IntPtr.Zero, ref guid, 0, ref diData))
                throw new InvalidOperationException("No battery device found");

            var devicePath = GetDevicePath(hDevInfo, ref diData);
            if (devicePath == null)
                throw new InvalidOperationException("Failed to get battery device path");

            // Open battery device handle
            const uint GENERIC_READ = 0x80000000;
            const uint GENERIC_WRITE = 0x40000000;
            const uint FILE_SHARE_READ_WRITE = 0x03;
            const uint OPEN_EXISTING = 3;

            var hBattery = CreateFile(devicePath, GENERIC_READ | GENERIC_WRITE,
                FILE_SHARE_READ_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);

            if (hBattery == new IntPtr(INVALID_HANDLE_VALUE))
                throw new InvalidOperationException("Failed to open battery device");

            try
            {
                // Step 1: Get battery tag
                uint waitTimeout = 0;
                if (!DeviceIoControl(hBattery, IOCTL_BATTERY_QUERY_TAG,
                        ref waitTimeout, sizeof(uint),
                        out uint batteryTag, sizeof(uint),
                        out _, IntPtr.Zero) || batteryTag == 0)
                    throw new InvalidOperationException("Failed to get battery tag");

                // Step 2: Get BATTERY_INFORMATION (design capacity, full charge, cycle count)
                QueryBatteryInformation(hBattery, batteryTag, info);

                // Step 3: Get BATTERY_STATUS (voltage, rate)
                QueryBatteryStatus(hBattery, batteryTag, info);

                // Step 4: Try to get temperature (not all drivers support this)
                QueryBatteryTemperature(hBattery, batteryTag, info);
            }
            finally
            {
                CloseHandle(hBattery);
            }
        }
        finally
        {
            SetupDiDestroyDeviceInfoList(hDevInfo);
        }
    }

    private static void QueryBatteryInformation(IntPtr hBattery, uint batteryTag, BatteryHealthInfo info)
    {
        var query = new BATTERY_QUERY_INFORMATION
        {
            BatteryTag = batteryTag,
            InformationLevel = BATTERY_QUERY_INFORMATION_LEVEL.BatteryInformation
        };

        if (!DeviceIoControl(hBattery, IOCTL_BATTERY_QUERY_INFORMATION,
                ref query, (uint)Marshal.SizeOf(query),
                out BATTERY_INFORMATION batInfo, (uint)Marshal.SizeOf<BATTERY_INFORMATION>(),
                out _, IntPtr.Zero))
            return;

        Logger.Debug("IOCTL BatteryInformation: DesignCap={Design}, FullCap={Full}, Cycles={Cycles}",
            batInfo.DesignedCapacity, batInfo.FullChargedCapacity, batInfo.CycleCount);

        if (batInfo.DesignedCapacity > 0 && batInfo.FullChargedCapacity > 0)
            info.HealthPercent = Math.Round((double)batInfo.FullChargedCapacity / batInfo.DesignedCapacity * 100, 1);

        if (batInfo.CycleCount > 0)
            info.CycleCount = (int)batInfo.CycleCount;
    }

    private static void QueryBatteryStatus(IntPtr hBattery, uint batteryTag, BatteryHealthInfo info)
    {
        var waitStatus = new BATTERY_WAIT_STATUS { BatteryTag = batteryTag };

        if (!DeviceIoControl(hBattery, IOCTL_BATTERY_QUERY_STATUS,
                ref waitStatus, (uint)Marshal.SizeOf(waitStatus),
                out BATTERY_STATUS status, (uint)Marshal.SizeOf<BATTERY_STATUS>(),
                out _, IntPtr.Zero))
            return;

        Logger.Debug("IOCTL BatteryStatus: Voltage={Voltage}mV, Rate={Rate}mW, Capacity={Cap}mWh",
            status.Voltage, status.Rate, status.Capacity);

        if (status.Voltage > 0)
            info.VoltageVolts = status.Voltage / 1000.0;

        if (status.Rate != 0)
            info.PowerRateWatts = Math.Round(Math.Abs(status.Rate) / 1000.0, 2);
    }

    private static void QueryBatteryTemperature(IntPtr hBattery, uint batteryTag, BatteryHealthInfo info)
    {
        var query = new BATTERY_QUERY_INFORMATION
        {
            BatteryTag = batteryTag,
            InformationLevel = BATTERY_QUERY_INFORMATION_LEVEL.BatteryTemperature
        };

        // Many drivers don't support temperature — silently ignore failure
        if (DeviceIoControl(hBattery, IOCTL_BATTERY_QUERY_INFORMATION,
                ref query, (uint)Marshal.SizeOf(query),
                out uint tempDecikelvin, sizeof(uint),
                out _, IntPtr.Zero) && tempDecikelvin > 0)
        {
            info.TemperatureCelsius = Math.Round((tempDecikelvin / 10.0) - 273.15, 1);
        }
    }

    private static string? GetDevicePath(IntPtr hDevInfo, ref SP_DEVICE_INTERFACE_DATA diData)
    {
        // First call to get required size
        SetupDiGetDeviceInterfaceDetail(hDevInfo, ref diData, IntPtr.Zero, 0,
            out uint requiredSize, IntPtr.Zero);

        if (requiredSize == 0) return null;

        var detailDataPtr = Marshal.AllocHGlobal((int)requiredSize);
        try
        {
            // cbSize = sizeof(SP_DEVICE_INTERFACE_DETAIL_DATA) which is
            // sizeof(DWORD) + sizeof(TCHAR) = 5 on x86, 8 on x64 (struct packing)
            Marshal.WriteInt32(detailDataPtr, IntPtr.Size == 8 ? 8 : 5);

            if (!SetupDiGetDeviceInterfaceDetail(hDevInfo, ref diData, detailDataPtr,
                    requiredSize, out _, IntPtr.Zero))
                return null;

            // DevicePath starts at offset 4 (after cbSize DWORD)
            return Marshal.PtrToStringAuto(detailDataPtr + 4);
        }
        finally
        {
            Marshal.FreeHGlobal(detailDataPtr);
        }
    }

    /// <summary>WMI fallback for machines where IOCTL fails.</summary>
    private static void FetchViaWmi(BatteryHealthInfo info)
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

        if (designCap > 0 && fullCap > 0 && !info.HealthPercent.HasValue)
            info.HealthPercent = Math.Round((double)fullCap / designCap * 100, 1);

        using var cycleSearcher = new System.Management.ManagementObjectSearcher("root\\WMI",
            "SELECT CycleCount FROM BatteryCycleCount");
        foreach (System.Management.ManagementObject obj in cycleSearcher.Get())
        {
            if (!info.CycleCount.HasValue)
                info.CycleCount = Convert.ToInt32(obj["CycleCount"]);
            break;
        }
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
