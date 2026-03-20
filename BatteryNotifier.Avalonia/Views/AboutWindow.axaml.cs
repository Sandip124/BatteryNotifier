using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using BatteryNotifier.Core;

namespace BatteryNotifier.Avalonia.Views;

public partial class AboutWindow : Window
{
    private TaskCompletionSource? _tcs;

    public AboutWindow()
    {
        InitializeComponent();

        if (OperatingSystem.IsLinux())
        {
            SystemDecorations = SystemDecorations.None;
            TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        }
        Deactivated += OnWindowDeactivated;

        VersionText.Text = $"v{Constants.ApplicationVersion}";

        CloseButton.Click += (_, _) => CloseWindow();
        ViewSourceButton.Click += OnViewSource;
    }

    public Task ShowLightDismiss(Window owner)
    {
        _tcs = new TaskCompletionSource();

        if (owner.Position.X > 0 || owner.Position.Y > 0)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            var x = owner.Position.X + (int)((owner.Width - Width) / 2);
            var y = owner.Position.Y + (int)((owner.Height - Height) / 2);
            Position = new PixelPoint(x, y);
        }

        Show(owner);
        return _tcs.Task;
    }

    private void CloseWindow()
    {
        if (_tcs != null && !_tcs.Task.IsCompleted)
            _tcs.TrySetResult();
        Close();
    }

    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        CloseWindow();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            CloseWindow();
            return;
        }
        base.OnKeyDown(e);
    }

    private static void OnViewSource(object? sender, RoutedEventArgs e)
    {
        OpenUrl(Constants.SourceRepositoryUrl);
    }

    private static void OpenUrl(string url)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var psi = new ProcessStartInfo(Constants.ResolveCommand("open")) { UseShellExecute = false };
            psi.ArgumentList.Add(url);
            using var p = Process.Start(psi);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using var p = Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        else
        {
            var psi = new ProcessStartInfo(Constants.ResolveCommand("xdg-open")) { UseShellExecute = false };
            psi.ArgumentList.Add(url);
            using var p = Process.Start(psi);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _tcs?.TrySetResult();
    }
}