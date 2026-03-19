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

    public NotificationCardViewModel(string title, string message, int batteryLevel, string accentColor, Action onDismiss)
    {
        Title = title;
        Message = message;
        ShowPercent = batteryLevel >= 0;
        BatteryPercent = batteryLevel >= 0 ? $"{batteryLevel}%" : "";
        AccentColor = accentColor;
        AccentColorValue = Color.Parse(accentColor);
        DismissCommand = ReactiveCommand.Create(onDismiss);
    }
}
