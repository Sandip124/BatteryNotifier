using System;
using System.Reactive;
using System.Windows.Input;
using ReactiveUI;

namespace BatteryNotifier.Avalonia.ViewModels;

public class SettingsViewModel  : ViewModelBase
{
    private bool _enableNotifications = true;
    private int _batteryThreshold = 20;

    public SettingsViewModel(Action navigateBack)
    {
        BackCommand = ReactiveCommand.Create(navigateBack);
    }

    public ReactiveCommand<Unit, Unit> BackCommand { get; }

    public bool EnableNotifications
    {
        get => _enableNotifications;
        set => this.RaiseAndSetIfChanged(ref _enableNotifications, value);
    }

    public int BatteryThreshold
    {
        get => _batteryThreshold;
        set => this.RaiseAndSetIfChanged(ref _batteryThreshold, value);
    }
}