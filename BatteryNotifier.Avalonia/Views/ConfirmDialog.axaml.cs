using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace BatteryNotifier.Avalonia.Views;

/// <summary>
/// Small theme-aware yes/no confirmation shown modally over its owner. Returns <c>true</c> only
/// if the user picks the confirm action; Cancel, Escape, or closing returns <c>false</c>.
/// </summary>
public partial class ConfirmDialog : Window
{
    public ConfirmDialog()
    {
        InitializeComponent();

        if (OperatingSystem.IsLinux())
        {
            SystemDecorations = SystemDecorations.None;
            TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        }
    }

    /// <summary>Shows the dialog modally over <paramref name="owner"/> and awaits the user's choice.</summary>
    public static Task<bool> ShowAsync(Window owner, string title, string message, string confirmText = "Remove")
    {
        var dialog = new ConfirmDialog();
        dialog.TitleText.Text = title;
        dialog.MessageText.Text = message;
        dialog.ConfirmButton.Content = confirmText;
        return dialog.ShowDialog<bool>(owner);
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);

    private void OnConfirm(object? sender, RoutedEventArgs e) => Close(true);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Close(false);
            return;
        }
        base.OnKeyDown(e);
    }
}
