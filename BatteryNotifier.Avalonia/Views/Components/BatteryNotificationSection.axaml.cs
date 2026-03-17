using System;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using BatteryNotifier.Avalonia.ViewModels;

namespace BatteryNotifier.Avalonia.Views.Components;

public partial class BatteryNotificationSection : UserControl
{
    private IDisposable? _interactionHandler;

    public BatteryNotificationSection()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        _interactionHandler?.Dispose();
        _interactionHandler = null;

        if (DataContext is BatteryNotificationSectionViewModel vm)
        {
            _interactionHandler = vm.OpenSoundPickerInteraction.RegisterHandler(async ctx =>
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel is not Window ownerWindow)
                {
                    ctx.SetOutput(null);
                    return;
                }

                // Add backdrop overlay (non-interactive — clicks pass through to owner window)
                var backdrop = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(160, 0, 0, 0)),
                    IsHitTestVisible = false
                };

                Panel? overlayHost = null;
                Control? existingContent = null;
                if (ownerWindow.Content is Control content)
                {
                    existingContent = content;
                    overlayHost = new Panel();
                    ownerWindow.Content = null;
                    overlayHost.Children.Add(existingContent);
                    overlayHost.Children.Add(backdrop);
                    ownerWindow.Content = overlayHost;
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
                    // Remove backdrop — restore original content (must run on UI thread)
                    if (overlayHost != null && existingContent != null)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            overlayHost.Children.Clear();
                            ownerWindow.Content = existingContent;
                        });
                    }
                }
            });
        }
    }
}
