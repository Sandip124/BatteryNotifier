using Avalonia.Controls;
using Avalonia.Input;
using BatteryNotifier.Core.Logger;
using BatteryNotifier.Core.Services;
using BatteryNotifier.Core.Utils;
using Serilog;

namespace BatteryNotifier.Avalonia.Views;

public partial class MainWindow : Window
{
    private readonly ILogger _logger;
    private readonly Debouncer _debouncer;

    public MainWindow()
    {
        _debouncer = new Debouncer();
        InitializeComponent();
        _logger = BatteryNotifierAppLogger.ForContext<MainWindow>();
        InitializeServices();
    }

    private void InitializeServices()
    {
        BatteryMonitorService.Instance.BatteryStatusChanged += OnBatteryStatusChanged;
        BatteryMonitorService.Instance.PowerLineStatusChanged += OnPowerLineStatusChanged;
        NotificationService.Instance.NotificationReceived += OnNotificationReceived;
    }

    private void OnBatteryStatusChanged(object? sender, BatteryStatusEventArgs e)
    {
        // Battery UI updates are handled by MainWindowViewModel
    }

    private void OnPowerLineStatusChanged(object? sender, BatteryStatusEventArgs e)
    {
        // Battery UI updates are handled by MainWindowViewModel
    }

    private void OnNotificationReceived(object? sender, NotificationMessage notification)
    {
        // Global notifications are handled by TrayIconService
    }

    private void OnHeaderDrag(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    protected override void OnClosed(System.EventArgs e)
    {
        BatteryMonitorService.Instance.BatteryStatusChanged -= OnBatteryStatusChanged;
        BatteryMonitorService.Instance.PowerLineStatusChanged -= OnPowerLineStatusChanged;
        NotificationService.Instance.NotificationReceived -= OnNotificationReceived;
        _debouncer?.Dispose();
        base.OnClosed(e);
    }
}
