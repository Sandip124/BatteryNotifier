using System;
using System.Reactive;
using Avalonia.Media;
using ReactiveUI;

namespace BatteryNotifier.Avalonia.ViewModels;

public sealed class NotificationCardViewModel : ViewModelBase
{
    public string Title { get; }
    public string Message { get; }
    public string BatteryPercent { get; }
    public string AccentColor { get; }
    public Color AccentColorValue { get; }

    public ReactiveCommand<Unit, Unit> DismissCommand { get; }

    public bool ShowPercent { get; }

    public int BatteryLevel { get; }

    private readonly Action<bool> _onDismiss;

    public NotificationCardViewModel(string title, string message, int batteryLevel, string accentColor, Action<bool> onDismiss)
    {
        Title = title;
        Message = message;
        ShowPercent = batteryLevel >= 0;
        BatteryLevel = Math.Clamp(batteryLevel, 0, 100);
        BatteryPercent = batteryLevel >= 0 ? $"{batteryLevel}%" : "";
        AccentColor = accentColor;
        AccentColorValue = Color.Parse(accentColor);
        _onDismiss = onDismiss;
        DismissCommand = ReactiveCommand.Create(() => _onDismiss(true));
    }

    public void Dismiss(bool userInitiated) => _onDismiss(userInitiated);
}
