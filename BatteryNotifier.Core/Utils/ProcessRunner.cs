using System.Diagnostics;

namespace BatteryNotifier.Core.Utils;

/// <summary>
/// Runs a subprocess with ArgumentList (no shell injection), bounded output, and enforced timeout.
/// </summary>
internal static class ProcessRunner
{
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
        catch
        {
            return string.Empty;
        }
    }
}
