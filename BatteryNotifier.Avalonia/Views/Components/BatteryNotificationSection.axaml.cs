using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
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

        // Dispose previous handler to prevent duplicate registrations
        _interactionHandler?.Dispose();
        _interactionHandler = null;

        if (DataContext is BatteryNotificationSectionViewModel vm)
        {
            _interactionHandler = vm.BrowseSoundInteraction.RegisterHandler(async ctx =>
            {
                var path = await BrowseAudioFile().ConfigureAwait(false);
                ctx.SetOutput(path);
            });
        }
    }

    private async Task<string?> BrowseAudioFile()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return null;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Sound File",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Audio Files")
                {
                    Patterns = ["*.wav", "*.mp3", "*.m4a", "*.wma"]
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = ["*.*"]
                }
            ]
        }).ConfigureAwait(false);

        return files.Count > 0 ? files[0].Path.LocalPath : null;
    }
}
