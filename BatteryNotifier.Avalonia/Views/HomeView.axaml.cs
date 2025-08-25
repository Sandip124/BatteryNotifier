using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BatteryNotifier.Core.Logger;
using Serilog;

namespace BatteryNotifier.Avalonia.Views;

public partial class HomeView : UserControl
{
    private readonly ILogger _logger;

    public HomeView()
    {
        InitializeComponent();
        _logger = BatteryNotifierAppLogger.ForContext<HomeView>();

    }
}