using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Transformation;
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

        ThemeSegments.SizeChanged += (_, _) => MoveThemeIndicator(CurrentThemeIndex, animate: false);
    }

    private int CurrentThemeIndex => (DataContext as SettingsViewModel)?.ThemeIndex ?? 1;

    private void MoveThemeIndicator(int index, bool animate)
    {
        var segment = index switch
        {
            0 => ThemeLight,
            2 => ThemeDark,
            _ => ThemeSystem,
        };

        var width = segment.Bounds.Width;
        if (width <= 0) return;

        var target = TransformOperations.Parse($"translateX({segment.Bounds.X}px)");

        if (!animate)
        {
            var transitions = ThemeIndicator.Transitions;
            ThemeIndicator.Transitions = null;
            ThemeIndicator.Width = width;
            ThemeIndicator.RenderTransform = target;
            ThemeIndicator.Transitions = transitions;
            return;
        }

        ThemeIndicator.Width = width;
        ThemeIndicator.RenderTransform = target;
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
        {
            UpdatePositionHighlight(vm.NotificationPosition);
            MoveThemeIndicator(vm.ThemeIndex, animate: false);
        }
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
        if (sender is not SettingsViewModel vm) return;

        if (e.PropertyName == nameof(SettingsViewModel.NotificationPosition))
            UpdatePositionHighlight(vm.NotificationPosition);
        else if (e.PropertyName == nameof(SettingsViewModel.ThemeIndex))
            MoveThemeIndicator(vm.ThemeIndex, animate: true);
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
