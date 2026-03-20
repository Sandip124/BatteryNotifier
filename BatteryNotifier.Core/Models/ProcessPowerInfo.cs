namespace BatteryNotifier.Core.Models;

public readonly record struct ProcessPowerInfo(string Name, double CpuPercent, int Pid);
