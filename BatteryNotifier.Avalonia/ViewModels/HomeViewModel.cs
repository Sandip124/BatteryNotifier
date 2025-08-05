using System;
using System.Reactive;
using System.Windows.Input;
using ReactiveUI;

namespace BatteryNotifier.Avalonia.ViewModels
{
    public class HomeViewModel : ViewModelBase
    {
        public HomeViewModel()
        {
            NavigateToSettingsCommand = ReactiveCommand.Create(() => { });
            NavigateToSettings = NavigateToSettingsCommand;
        }

        public ReactiveCommand<Unit, Unit> NavigateToSettingsCommand { get; }
        public IObservable<Unit> NavigateToSettings { get; }
    }
}