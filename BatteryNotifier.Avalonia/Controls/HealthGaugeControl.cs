using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace BatteryNotifier.Avalonia.Controls;

/// <summary>
/// Circular arc gauge (270 degrees, opening at bottom) showing battery health percentage.
/// Color gradient: green → yellow → red along arc. Theme-aware.
/// </summary>
public class HealthGaugeControl : Control
{
    public static readonly StyledProperty<double> HealthPercentProperty =
        AvaloniaProperty.Register<HealthGaugeControl, double>(nameof(HealthPercent), 100,
            defaultBindingMode: global::Avalonia.Data.BindingMode.TwoWay);

    static HealthGaugeControl()
    {
        AffectsRender<HealthGaugeControl>(HealthPercentProperty);
    }

    public double HealthPercent
    {
        get => GetValue(HealthPercentProperty);
        set => SetValue(HealthPercentProperty, value);
    }

    private const double ArcDegrees = 270;
    private const double StartAngle = 135; // degrees from 12 o'clock, clockwise
    private const double TrackThickness = 12;
    private const double ValueThickness = 14;

    private Color _trackColor = Color.Parse("#2A2A2A");
    private Color _textColor = Colors.White;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ActualThemeVariantChanged += (_, _) => UpdateColors();
        UpdateColors();
    }

    private void UpdateColors()
    {
        var isDark = ActualThemeVariant == ThemeVariant.Dark ||
                     (ActualThemeVariant == ThemeVariant.Default && Application.Current?.ActualThemeVariant == ThemeVariant.Dark);
        _trackColor = isDark ? Color.Parse("#2A2A2A") : Color.Parse("#E0E0E0");
        _textColor = isDark ? Colors.White : Color.Parse("#1A1A1A");
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = Math.Min(
            double.IsInfinity(availableSize.Width) ? 200 : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? 200 : availableSize.Height);
        return new Size(size, size);
    }

    public override void Render(DrawingContext context)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        var size = Math.Min(w, h);
        var cx = w / 2;
        var cy = h / 2;
        var radius = (size - ValueThickness) / 2 - 4;

        // Draw track arc
        DrawArc(context, cx, cy, radius, 0, ArcDegrees, TrackThickness,
            new SolidColorBrush(_trackColor));

        // Draw value arc
        var fraction = Math.Clamp(HealthPercent / 100.0, 0, 1);
        var valueDegrees = fraction * ArcDegrees;
        if (valueDegrees > 0.5)
        {
            var color = GetHealthColor(HealthPercent);
            DrawArc(context, cx, cy, radius, 0, valueDegrees, ValueThickness,
                new SolidColorBrush(color));
        }

        // Center text
        var percentText = HealthPercent >= 0 ? $"{HealthPercent:F0}%" : "--";
        var percentFt = new FormattedText(percentText, System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold),
            28, new SolidColorBrush(_textColor));
        context.DrawText(percentFt, new Point(cx - percentFt.Width / 2, cy - percentFt.Height / 2 - 6));

        var labelFt = new FormattedText("Battery Health", System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default),
            11, new SolidColorBrush(Color.FromArgb(180, _textColor.R, _textColor.G, _textColor.B)));
        context.DrawText(labelFt, new Point(cx - labelFt.Width / 2, cy + percentFt.Height / 2 - 6));
    }

    private void DrawArc(DrawingContext context, double cx, double cy, double radius,
        double startDeg, double sweepDeg, double thickness, IBrush brush)
    {
        var geo = new StreamGeometry();
        using var ctx = geo.Open();

        var startRad = (StartAngle + startDeg) * Math.PI / 180;
        var endRad = (StartAngle + startDeg + sweepDeg) * Math.PI / 180;

        var outerRadius = radius;
        var innerRadius = radius - thickness;

        var outerStart = new Point(cx + outerRadius * Math.Cos(startRad), cy + outerRadius * Math.Sin(startRad));
        var outerEnd = new Point(cx + outerRadius * Math.Cos(endRad), cy + outerRadius * Math.Sin(endRad));
        var innerEnd = new Point(cx + innerRadius * Math.Cos(endRad), cy + innerRadius * Math.Sin(endRad));
        var innerStart = new Point(cx + innerRadius * Math.Cos(startRad), cy + innerRadius * Math.Sin(startRad));

        bool isLargeArc = sweepDeg > 180;

        ctx.BeginFigure(outerStart, true);
        ctx.ArcTo(outerEnd, new Size(outerRadius, outerRadius), 0, isLargeArc, SweepDirection.Clockwise);
        ctx.LineTo(innerEnd);
        ctx.ArcTo(innerStart, new Size(innerRadius, innerRadius), 0, isLargeArc, SweepDirection.CounterClockwise);
        ctx.EndFigure(true);

        context.DrawGeometry(brush, null, geo);
    }

    private static Color GetHealthColor(double percent) => percent switch
    {
        >= 80 => Color.Parse("#388E3C"),
        >= 60 => Color.Parse("#F9A825"),
        >= 40 => Color.Parse("#F57A00"),
        _ => Color.Parse("#D32F2F")
    };
}
