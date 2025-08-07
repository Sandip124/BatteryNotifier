using System;
using System.Reactive;
using System.Windows.Input;
using ReactiveUI;

namespace BatteryNotifier.Avalonia.ViewModels;

public class SettingsViewModel  : ViewModelBase
{
    public SettingsViewModel(Action navigateBack)
    {
        BackCommand = ReactiveCommand.Create(navigateBack);
    }

    public ReactiveCommand<Unit, Unit> BackCommand { get; }
    
}