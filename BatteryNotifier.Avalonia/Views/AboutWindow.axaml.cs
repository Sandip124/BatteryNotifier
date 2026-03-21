using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BatteryNotifier.Core;
using BatteryNotifier.Core.Services;

namespace BatteryNotifier.Avalonia.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        if (OperatingSystem.IsLinux())
        {
            SystemDecorations = SystemDecorations.None;
            TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        }

        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        VersionText.Text = $"v{Constants.ApplicationVersion}";

        CloseButton.Click += (_, _) => Close();
        ViewSourceButton.Click += OnViewSource;
    }

    /// <summary>
    /// Shows the About window as a standalone window (no owner).
    /// </summary>
    public void ShowStandalone()
    {
        Show();
        Activate();
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        // Auto-check for updates (Chrome-style)
        UpdateStatusText.Text = "Checking for updates...";
        UpdateStatusText.IsVisible = true;

        try
        {
            var result = await UpdateService.Instance.CheckForUpdateManualAsync().ConfigureAwait(false);

            Dispatcher.UIThread.Post(() =>
            {
                switch (result.Status)
                {
                    case CheckStatus.UpdateAvailable when result.Release != null:
                        UpdateStatusText.Text = $"Update available: v{result.Release.TagName?.TrimStart('v')}";
                        UpdateStatusText.Foreground = global::Avalonia.Media.Brushes.DodgerBlue;
                        UpdateStatusText.Cursor = new Cursor(StandardCursorType.Hand);
                        UpdateStatusText.PointerPressed += (_, _) => OpenUrl(result.Release.HtmlUrl);
                        break;
                    case CheckStatus.UpToDate:
                        UpdateStatusText.Text = "You're on the latest version";
                        break;
                    default:
                        UpdateStatusText.IsVisible = false;
                        break;
                }
            });
        }
        catch
        {
            Dispatcher.UIThread.Post(() => UpdateStatusText.IsVisible = false);
        }
    }

    private void DragHandle_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Close();
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
}
