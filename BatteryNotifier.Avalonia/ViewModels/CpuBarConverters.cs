using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace BatteryNotifier.Avalonia.ViewModels;

/// <summary>
/// Converts CPU percentage (0–100) to a bar width in pixels (max 140px).
/// </summary>
public sealed class CpuBarWidthConverter : IValueConverter
{
    public static readonly CpuBarWidthConverter Instance = new();
    private const double MaxWidth = 140;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double cpu)
            return Math.Clamp(cpu / 100.0 * MaxWidth, 0, MaxWidth);
        return 0.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Converts CPU percentage to a color: green (&lt;20%), amber (20–50%), red (&gt;50%).
/// </summary>
public sealed class CpuBarColorConverter : IValueConverter
{
    public static readonly CpuBarColorConverter Instance = new();

    private static readonly ISolidColorBrush Green = SolidColorBrush.Parse("#00E676");
    private static readonly ISolidColorBrush Amber = SolidColorBrush.Parse("#FFAB00");
    private static readonly ISolidColorBrush Red = SolidColorBrush.Parse("#FF1744");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double cpu)
        {
            return cpu switch
            {
                > 50 => Red,
                > 20 => Amber,
                _ => Green
            };
        }
        return Green;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
