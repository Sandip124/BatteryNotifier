using System;
using System.ComponentModel;
using BatteryNotifier.Avalonia.ViewModels;

namespace BatteryNotifier.Avalonia.Services
{
    public interface INavigationService
    {
        event EventHandler<ViewModelBase> NavigationRequested;
        void NavigateTo<T>() where T : ViewModelBase, new();
        void NavigateTo(ViewModelBase viewModel);
    }

    public class NavigationService : INavigationService
    {
        public event EventHandler<ViewModelBase>? NavigationRequested;

        public void NavigateTo<T>() where T : ViewModelBase, new()
        {
            NavigateTo(new T());
        }

        public void NavigateTo(ViewModelBase viewModel)
        {
            NavigationRequested?.Invoke(this, viewModel);
        }
    }
}