using Avalonia.Controls;
using Avalonia.Controls.Templates;
using BatteryNotifier.Avalonia.ViewModels;
using BatteryNotifier.Avalonia.Views;

namespace BatteryNotifier.Avalonia;

/// <summary>
/// Maps view models to their views with direct constructor calls rather than
/// reflection (Type.GetType by name). The Release publish uses
/// PublishTrimmed + TrimMode=full, which removes any view only reachable via
/// a name string — that silently dropped SettingsView from published builds.
/// Add a line here for each view model rendered through a ContentControl.
/// </summary>
public sealed class ViewLocator : IDataTemplate
{
    public Control? Build(object? data)
    {
        if (data is null)
            return null;

        Control? control = data switch
        {
            SettingsViewModel => new SettingsView(),
            _ => null,
        };

        if (control is null)
            return new TextBlock { Text = "Not Found: " + data.GetType().FullName };

        control.DataContext = data;
        return control;
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
