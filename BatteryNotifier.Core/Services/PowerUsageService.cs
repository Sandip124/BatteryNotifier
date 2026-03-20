using System.Diagnostics;
using System.Globalization;
using BatteryNotifier.Core.Logger;
using BatteryNotifier.Core.Models;
using Serilog;

namespace BatteryNotifier.Core.Services;

/// <summary>
/// Polls top CPU-consuming processes at adaptive intervals.
/// macOS/Linux: ps command. Windows: Process.GetProcesses() with CPU delta.
/// </summary>
public sealed class PowerUsageService : IDisposable
{
    private static readonly Lazy<PowerUsageService> _instance = new(() => new PowerUsageService());
    public static PowerUsageService Instance => _instance.Value;

    private static readonly ILogger Logger = BatteryNotifierAppLogger.ForContext("PowerUsageService");
    private CancellationTokenSource? _cts;
    private bool _disposed;
    private bool _activePolling;

    private static readonly TimeSpan BackgroundInterval = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan ActiveInterval = TimeSpan.FromSeconds(15);

    private static readonly HashSet<string> SystemProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "kernel_task", "launchd", "svchost", "systemd", "idle",
        "WindowServer", "loginwindow", "init", "kthreadd",
        "System Idle Process", "System", "Registry"
    };

    public IReadOnlyList<ProcessPowerInfo>? LatestProcesses { get; private set; }
    public event EventHandler<IReadOnlyList<ProcessPowerInfo>>? ProcessesUpdated;

    private PowerUsageService()
    {
        _cts = new CancellationTokenSource();
        _ = RunPollingAsync(_cts.Token);
    }

    public void SetActivePolling(bool active)
    {
        _activePolling = active;
    }

    private async Task RunPollingAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct).ConfigureAwait(false);
            FetchAndPublish();
        }
        catch (OperationCanceledException) { return; }
        catch (Exception ex) { Logger.Warning(ex, "Initial power usage check failed"); }

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var interval = _activePolling ? ActiveInterval : BackgroundInterval;
                await Task.Delay(interval, ct).ConfigureAwait(false);
                FetchAndPublish();
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { Logger.Warning(ex, "Power usage poll failed"); }
        }
    }

    private void FetchAndPublish()
    {
        var processes = FetchTopProcesses();
        LatestProcesses = processes;
        ProcessesUpdated?.Invoke(this, processes);
    }

    private static IReadOnlyList<ProcessPowerInfo> FetchTopProcesses()
    {
        if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
            return FetchViaPs();
#if WINDOWS
        if (OperatingSystem.IsWindows())
            return FetchViaProcessApi();
#endif
        return [];
    }

    private static IReadOnlyList<ProcessPowerInfo> FetchViaPs()
    {
        var args = OperatingSystem.IsMacOS()
            ? new[] { "-eo", "pid,%cpu,comm", "-r" }
            : new[] { "-eo", "pid,%cpu,comm", "--sort=-%cpu" };

        var output = RunProcess("ps", args);
        if (string.IsNullOrWhiteSpace(output))
            return [];

        return FilterAndSort(ParsePsOutput(output));
    }

    internal static List<ProcessPowerInfo> ParsePsOutput(string output)
    {
        var results = new List<ProcessPowerInfo>();
        var lines = output.AsSpan();

        // Skip header line
        var firstNewline = lines.IndexOf('\n');
        if (firstNewline < 0) return results;
        lines = lines[(firstNewline + 1)..];

        while (lines.Length > 0)
        {
            var lineEnd = lines.IndexOf('\n');
            var line = lineEnd >= 0 ? lines[..lineEnd] : lines;
            lines = lineEnd >= 0 ? lines[(lineEnd + 1)..] : default;

            line = line.Trim();
            if (line.IsEmpty) continue;

            // Parse: PID  %CPU  COMM
            // PID field
            var rest = line;
            var spaceIdx = rest.IndexOf(' ');
            if (spaceIdx < 0) continue;

            var pidSpan = rest[..spaceIdx];
            if (!int.TryParse(pidSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pid))
                continue;

            // Skip whitespace to %CPU
            rest = rest[(spaceIdx + 1)..].TrimStart();
            spaceIdx = rest.IndexOf(' ');
            if (spaceIdx < 0) continue;

            var cpuSpan = rest[..spaceIdx];
            if (!double.TryParse(cpuSpan, NumberStyles.Float, CultureInfo.InvariantCulture, out var cpu))
                continue;

            // Rest is command path
            var comm = rest[(spaceIdx + 1)..].TrimStart();
            if (comm.IsEmpty) continue;

            // Extract just the process name from the full path
            var lastSlash = comm.LastIndexOf('/');
            var name = lastSlash >= 0 ? comm[(lastSlash + 1)..] : comm;

            if (name.IsEmpty) continue;

            results.Add(new ProcessPowerInfo(name.ToString(), cpu, pid));
        }

        return results;
    }

#if WINDOWS
    private static IReadOnlyList<ProcessPowerInfo> FetchViaProcessApi()
    {
        try
        {
            // Snapshot 1: capture CPU times
            var snapshot1 = new Dictionary<int, (string Name, TimeSpan Cpu)>();
            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    snapshot1[p.Id] = (p.ProcessName, p.TotalProcessorTime);
                }
                catch { /* Access denied for some system processes */ }
                finally { p.Dispose(); }
            }

            // Wait 1 second for delta
            Thread.Sleep(1000);

            // Snapshot 2: compute delta
            var results = new List<ProcessPowerInfo>();
            var cpuCount = Environment.ProcessorCount;
            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    if (snapshot1.TryGetValue(p.Id, out var prev))
                    {
                        var delta = (p.TotalProcessorTime - prev.Cpu).TotalMilliseconds;
                        var cpuPercent = Math.Round(delta / 1000.0 / cpuCount * 100, 1);
                        results.Add(new ProcessPowerInfo(prev.Name, cpuPercent, p.Id));
                    }
                }
                catch { /* Access denied */ }
                finally { p.Dispose(); }
            }

            return FilterAndSort(results);
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to fetch Windows process CPU usage");
            return [];
        }
    }
#endif

    private static IReadOnlyList<ProcessPowerInfo> FilterAndSort(List<ProcessPowerInfo> processes)
    {
        var selfPid = Environment.ProcessId;

        return processes
            .Where(p => p.Pid != selfPid
                     && p.CpuPercent >= 1.0
                     && !SystemProcesses.Contains(p.Name))
            .OrderByDescending(p => p.CpuPercent)
            .Take(5)
            .ToList();
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
        ProcessesUpdated = null;
    }
}
