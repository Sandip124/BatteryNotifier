using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using BatteryNotifier.Avalonia.Models;
using BatteryNotifier.Avalonia.ViewModels;

namespace BatteryNotifier.Avalonia.Views.Components;

public partial class PauseDurationSheet : UserControl
{
    private static readonly TransformOperations OffScreen = TransformOperations.Parse("translateY(230px)");
    private static readonly TransformOperations OnScreen = TransformOperations.Parse("translateY(0px)");
    private static readonly TimeSpan AnimationDuration = TimeSpan.FromMilliseconds(280);

    private bool _isAnimating;

    public PauseDurationSheet()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is INotifyPropertyChanged npc)
            npc.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainWindowViewModel.IsPauseSheetOpen)) return;
        if (sender is not MainWindowViewModel vm) return;

        if (vm.IsPauseSheetOpen)
            AnimateOpen();
        else
            AnimateClose();
    }

    private void AnimateOpen()
    {
        if (_isAnimating) return;

        RootGrid.Opacity = 0;
        SheetPanel.RenderTransform = OffScreen;
        IsVisible = true;

        DispatcherTimer.RunOnce(() =>
        {
            RootGrid.Opacity = 1;
            SheetPanel.RenderTransform = OnScreen;
        }, TimeSpan.FromMilliseconds(16));
    }

    private void AnimateClose()
    {
        if (_isAnimating) return;
        _isAnimating = true;

        RootGrid.Opacity = 0;
        SheetPanel.RenderTransform = OffScreen;

        DispatcherTimer.RunOnce(() =>
        {
            IsVisible = false;
            _isAnimating = false;
        }, AnimationDuration);
    }

    private void Backdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.IsPauseSheetOpen = false;
    }

    private void OnOptionClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        if (sender is not Button { Tag: PauseOption option }) return;

        vm.PauseNotificationsCommand.Execute(option.Duration).Subscribe();
        vm.IsPauseSheetOpen = false;
    }
}
