using System;
using ReactiveUI;

namespace BatteryNotifier.Avalonia.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private ViewModelBase _currentView;

    public MainWindowViewModel()
    {
        // Start with main view
        CurrentView = CreateMainViewModel();
    }

    public ViewModelBase CurrentView
    {
        get => _currentView;
        set => this.RaiseAndSetIfChanged(ref _currentView, value);
    }

    private HomeViewModel CreateMainViewModel()
    {
        var mainVm = new HomeViewModel();
        mainVm.NavigateToSettings.Subscribe(_ => NavigateToSettings());
        return mainVm;
    }

    private void NavigateToSettings()
    {
        CurrentView = new SettingsViewModel(NavigateToMain);
    }

    private void NavigateToMain()
    {
        CurrentView = CreateMainViewModel();
    }
}