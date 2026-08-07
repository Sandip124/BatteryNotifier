using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using BatteryNotifier.Avalonia.ViewModels;
using BatteryNotifier.Core.Services;

namespace BatteryNotifier.Avalonia.Views;

public partial class SettingsView : UserControl
{
    private Dictionary<NotificationPosition, Button>? _positionButtons;
    private INotifyPropertyChanged? _subscribedViewModel;

    public SettingsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += (_, _) => UpdateScrollShadows();
    }

    // Show a soft shadow on whichever edge has more content beyond it, hinting the list scrolls.
    private void OnSettingsScrollChanged(object? sender, ScrollChangedEventArgs e) => UpdateScrollShadows();

    private void UpdateScrollShadows()
    {
        if (SettingsScroller is null) return;

        var offset = SettingsScroller.Offset.Y;
        var maxOffset = SettingsScroller.Extent.Height - SettingsScroller.Viewport.Height;

        TopScrollShadow.Opacity = offset > 1 ? 1 : 0;
        BottomScrollShadow.Opacity = offset < maxOffset - 1 ? 1 : 0;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // Unsubscribe from previous DataContext to prevent leak
        if (_subscribedViewModel != null)
        {
            _subscribedViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _subscribedViewModel = null;
        }

        if (DataContext is INotifyPropertyChanged npc)
        {
            npc.PropertyChanged += OnViewModelPropertyChanged;
            _subscribedViewModel = npc;
        }

        BuildPositionMap();
        if (DataContext is SettingsViewModel vm)
            UpdatePositionHighlight(vm.NotificationPosition);
    }

    private void BuildPositionMap()
    {
        _positionButtons = new Dictionary<NotificationPosition, Button>
        {
            [NotificationPosition.TopLeft] = PosTopLeft,
            [NotificationPosition.TopCenter] = PosTopCenter,
            [NotificationPosition.TopRight] = PosTopRight,
            [NotificationPosition.BottomLeft] = PosBottomLeft,
            [NotificationPosition.BottomCenter] = PosBottomCenter,
            [NotificationPosition.BottomRight] = PosBottomRight,
        };
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.NotificationPosition) &&
            sender is SettingsViewModel vm)
        {
            UpdatePositionHighlight(vm.NotificationPosition);
        }
    }

    private void UpdatePositionHighlight(NotificationPosition active)
    {
        if (_positionButtons == null) return;

        foreach (var (pos, btn) in _positionButtons)
        {
            if (pos == active)
                btn.Classes.Add("pos-active");
            else
                btn.Classes.Remove("pos-active");
        }
    }

    private void SettingsTitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var window = TopLevel.GetTopLevel(this) as Window;
            window?.BeginMoveDrag(e);
        }
    }
}
