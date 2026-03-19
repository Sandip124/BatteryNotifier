using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using BatteryNotifier.Avalonia.ViewModels;
using BatteryNotifier.Core.Services;

namespace BatteryNotifier.Avalonia.Views.Components;

public partial class HealthBottomSheet : UserControl
{
    private static readonly TransformOperations OffScreen = TransformOperations.Parse("translateY(430px)");
    private static readonly TransformOperations OnScreen = TransformOperations.Parse("translateY(0px)");
    private static readonly TimeSpan AnimationDuration = TimeSpan.FromMilliseconds(300);

    private bool _isAnimating;

    public HealthBottomSheet()
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
        if (e.PropertyName != nameof(MainWindowViewModel.IsHealthSheetOpen)) return;
        if (sender is not MainWindowViewModel vm) return;

        if (vm.IsHealthSheetOpen)
            AnimateOpen();
        else
            AnimateClose();
    }

    private void AnimateOpen()
    {
        if (_isAnimating) return;

        BatteryHealthService.Instance.SetActivePolling(true);

        // Reset to off-screen, make visible
        RootGrid.Opacity = 0;
        SheetPanel.RenderTransform = OffScreen;
        IsVisible = true;

        // Animate in on next frame so transitions apply
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

        BatteryHealthService.Instance.SetActivePolling(false);

        // Animate out
        RootGrid.Opacity = 0;
        SheetPanel.RenderTransform = OffScreen;

        // Hide after animation completes
        DispatcherTimer.RunOnce(() =>
        {
            IsVisible = false;
            _isAnimating = false;
        }, AnimationDuration);
    }

    private void Backdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.IsHealthSheetOpen = false;
    }
}