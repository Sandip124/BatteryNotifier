using System.Globalization;
#if WINDOWS
using System.Runtime.InteropServices;
#endif
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

    private static void DetectCannotSustainLoad(BatteryHealthInfo info)
    {
        if (OperatingSystem.IsMacOS()) return;

        var store = Store.BatteryManagerStore.Instance;
        if (store.HasNoBattery || store.IsUnknown) return;
        if (info.HealthPercent.HasValue) return;
        if (store.IsPluggedIn) return;

        if (store.BatteryLifePercent > 10 && store.BatteryLifeRemaining == 0)
            info.CannotSustainLoad = true;
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

            var tempRaw = ParseDouble(output, "\"Temperature\"\\s*=\\s*(\\d+)");
            if (tempRaw.HasValue)
                info.TemperatureCelsius = Math.Round(tempRaw.Value / 10.0 - 273.15, 1);

            info.VoltageVolts = ParseDouble(output, "\"Voltage\"\\s*=\\s*(\\d+)") / 1000.0;

            var timeMin = ParseInt(output, "\"TimeRemaining\"\\s*=\\s*(\\d+)");
            if (timeMin.HasValue && timeMin.Value != 65535)
                info.TimeRemainingSeconds = timeMin.Value * 60;
            
            var amperageRaw = ParseULong(output, "\"Amperage\"\\s*=\\s*(\\d+)");
            double? amperageMa = amperageRaw.HasValue ? (double)unchecked((long)amperageRaw.Value) : null;

            var rawMaxCap = ParseDouble(output, "\"AppleRawMaxCapacity\"\\s*=\\s*(\\d+)")
                         ?? ParseDouble(output, "\"FullChargeCapacity\"\\s*=\\s*(\\d+)");
            var designCap = ParseDouble(output, "\"DesignCapacity\"\\s*=\\s*(\\d+)");

            if (rawMaxCap.HasValue && designCap.HasValue && designCap.Value > 0)
            {
                info.HealthPercent = Math.Round(rawMaxCap.Value / designCap.Value * 100, 1);
            }

            if (amperageMa.HasValue && info.VoltageVolts.HasValue)
            {
                info.PowerRateWatts = Math.Round(
                    Math.Abs(amperageMa.Value) * info.VoltageVolts.Value / 1000.0, 2);
            }

            var permanentFailure = ParseInt(output, "\"PermanentFailureStatus\"\\s*=\\s*(\\d+)");
            if (permanentFailure is > 0)
                info.CannotSustainLoad = true;
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to parse macOS battery health");
        }

        return info;
    }

#if WINDOWS

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
        public uint DesignedCapacity;      
        public uint FullChargedCapacity;   
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
        public uint Capacity;     
        public uint Voltage;      
        public int Rate;          
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

            if (!SetupDiEnumDeviceInterfaces(hDevInfo, IntPtr.Zero, ref guid, 0, ref diData))
                throw new InvalidOperationException("No battery device found");

            var devicePath = GetDevicePath(hDevInfo, ref diData);
            if (devicePath == null)
                throw new InvalidOperationException("Failed to get battery device path");

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
                uint waitTimeout = 0;
                if (!DeviceIoControl(hBattery, IOCTL_BATTERY_QUERY_TAG,
                        ref waitTimeout, sizeof(uint),
                        out uint batteryTag, sizeof(uint),
                        out _, IntPtr.Zero) || batteryTag == 0)
                    throw new InvalidOperationException("Failed to get battery tag");

                QueryBatteryInformation(hBattery, batteryTag, info);

                QueryBatteryStatus(hBattery, batteryTag, info);

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
        SetupDiGetDeviceInterfaceDetail(hDevInfo, ref diData, IntPtr.Zero, 0,
            out uint requiredSize, IntPtr.Zero);

        if (requiredSize == 0) return null;

        var detailDataPtr = Marshal.AllocHGlobal((int)requiredSize);
        try
        {
            Marshal.WriteInt32(detailDataPtr, IntPtr.Size == 8 ? 8 : 5);

            if (!SetupDiGetDeviceInterfaceDetail(hDevInfo, ref diData, detailDataPtr,
                    requiredSize, out _, IntPtr.Zero))
                return null;

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
            var batDir = FindLinuxBatteryDirectory();
            if (batDir == null) return info;

            var energyFull = ReadSysfsLong(batDir, "energy_full") ?? ReadSysfsLong(batDir, "charge_full");
            var energyDesign = ReadSysfsLong(batDir, "energy_full_design") ?? ReadSysfsLong(batDir, "charge_full_design");
            if (energyFull.HasValue && energyDesign is > 0)
                info.HealthPercent = Math.Round((double)energyFull.Value / energyDesign.Value * 100, 1);

            var cycleCount = ReadSysfsLong(batDir, "cycle_count");
            if (cycleCount is > 0)
                info.CycleCount = (int)cycleCount.Value;

            var temp = ReadSysfsLong(batDir, "temp");
            if (temp.HasValue)
                info.TemperatureCelsius = temp.Value / 10.0;

            var voltageNow = ReadSysfsLong(batDir, "voltage_now");
            if (voltageNow.HasValue)
                info.VoltageVolts = voltageNow.Value / 1_000_000.0;

            info.PowerRateWatts = ReadLinuxPowerRateWatts(batDir, voltageNow);
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to parse Linux battery health");
        }

        return info;
    }

    /// <summary>power_now is already in watts; otherwise derive it from current_now × voltage_now.</summary>
    private static double? ReadLinuxPowerRateWatts(string batDir, long? voltageNow)
    {
        var powerNow = ReadSysfsLong(batDir, "power_now");
        if (powerNow.HasValue)
            return Math.Round(Math.Abs(powerNow.Value) / 1_000_000.0, 2);

        var currentNow = ReadSysfsLong(batDir, "current_now");
        return currentNow.HasValue && voltageNow.HasValue
            ? Math.Round(Math.Abs(currentNow.Value) * voltageNow.Value / 1e12, 2)
            : null;
    }

    private static string? FindLinuxBatteryDirectory()
    {
        const string basePath = "/sys/class/power_supply";
        if (!Directory.Exists(basePath)) return null;

        return Directory.GetDirectories(basePath).FirstOrDefault(IsBatteryDirectory);
    }

    private static bool IsBatteryDirectory(string dir) =>
        Path.GetFileName(dir).StartsWith("BAT", StringComparison.OrdinalIgnoreCase) &&
        ReadSysfsText(Path.Combine(dir, "type")) is { } type &&
        type.Equals("Battery", StringComparison.OrdinalIgnoreCase);

    private static string? ReadSysfsText(string path) =>
        File.Exists(path) ? File.ReadAllText(path).Trim() : null;

    private static long? ReadSysfsLong(string batDir, string fileName) =>
        ReadSysfsLong(Path.Combine(batDir, fileName));

    private static long? ReadSysfsLong(string path) =>
        ReadSysfsText(path) is { } text &&
        long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var val)
            ? val
            : null;

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
