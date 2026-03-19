using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using BatteryNotifier.Avalonia.ViewModels;

namespace BatteryNotifier.Avalonia.Views.Components;

public partial class AlertRow : UserControl
{
    private IDisposable? _interactionHandler;

    public AlertRow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        _interactionHandler?.Dispose();
        _interactionHandler = null;

        if (DataContext is AlertRowViewModel vm)
        {
            _interactionHandler = vm.OpenSoundPickerInteraction.RegisterHandler(async ctx =>
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel is not Window ownerWindow)
                {
                    ctx.SetOutput(null);
                    return;
                }

                // Find the root panel to overlay into — avoids reparenting content
                // which would reset the ScrollViewer position
                var rootPanel = FindRootPanel(ownerWindow);
                Border? backdrop = null;

                if (rootPanel != null)
                {
                    backdrop = new Border
                    {
                        Background = new SolidColorBrush(Color.FromArgb(160, 0, 0, 0)),
                        IsHitTestVisible = false,
                        Margin = new Thickness(-4), // extend past window padding
                        ZIndex = 100
                    };
                    rootPanel.Children.Add(backdrop);
                }

                try
                {
                    var (settingsValue, title) = ctx.Input;
                    var pickerVm = new SoundPickerViewModel(settingsValue, title);
                    var pickerWindow = new SoundPickerWindow
                    {
                        DataContext = pickerVm
                    };

                    var result = await pickerWindow.ShowLightDismiss(ownerWindow);
                    ctx.SetOutput(result);
                }
                finally
                {
                    if (rootPanel != null && backdrop != null)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            rootPanel.Children.Remove(backdrop);
                        });
                    }
                }
            });
        }
    }

    /// <summary>
    /// Walks the window content tree to find a Panel we can add an overlay child to.
    /// </summary>
    private static Panel? FindRootPanel(Window window)
    {
        if (window.Content is Panel panel)
            return panel;

        // The MainWindow structure is Border > Border > Grid — dig into it
        if (window.Content is Decorator decorator)
        {
            var child = decorator.Child;
            while (child is Decorator d)
                child = d.Child;
            if (child is Panel p)
                return p;
        }

        return null;
    }
}
