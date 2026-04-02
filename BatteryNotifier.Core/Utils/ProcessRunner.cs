using System.Diagnostics;
using BatteryNotifier.Core.Logger;
using Serilog;

namespace BatteryNotifier.Core.Utils;

/// <summary>
/// Runs a subprocess with ArgumentList (no shell injection), bounded output, and enforced timeout.
/// </summary>
internal static class ProcessRunner
{
    private static readonly ILogger Logger = BatteryNotifierAppLogger.ForContext("ProcessRunner");

    internal static string Run(string command, params string[] args)
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
        catch (Exception ex)
        {
            Logger.Debug(ex, "Subprocess failed: {Command}", command);
            return string.Empty;
        }
    }
}