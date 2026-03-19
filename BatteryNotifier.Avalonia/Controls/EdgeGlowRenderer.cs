using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace BatteryNotifier.Avalonia.Controls;

/// <summary>
/// Renders an edge glow effect — 4 gradient rectangles at screen edges with transparent interior.
/// Used by ScreenFlashOverlay for battery notification visual feedback.
/// </summary>
public class EdgeGlowRenderer : Control
{
    public static readonly StyledProperty<Color> GlowColorProperty =
        AvaloniaProperty.Register<EdgeGlowRenderer, Color>(nameof(GlowColor), Colors.Red);

    public static readonly StyledProperty<double> GlowThicknessProperty =
        AvaloniaProperty.Register<EdgeGlowRenderer, double>(nameof(GlowThickness), 60);

    static EdgeGlowRenderer()
    {
        AffectsRender<EdgeGlowRenderer>(GlowColorProperty, GlowThicknessProperty);
    }

    public Color GlowColor
    {
        get => GetValue(GlowColorProperty);
        set => SetValue(GlowColorProperty, value);
    }

    public double GlowThickness
    {
        get => GetValue(GlowThicknessProperty);
        set => SetValue(GlowThicknessProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        var t = Math.Min(GlowThickness, Math.Min(w, h) / 3);
        var baseColor = GlowColor;
        var transparent = Color.FromArgb(0, baseColor.R, baseColor.G, baseColor.B);

        // Top edge
        var topBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops = { new GradientStop(baseColor, 0), new GradientStop(transparent, 1) }
        };
        context.DrawRectangle(topBrush, null, new Rect(0, 0, w, t));

        // Bottom edge
        var bottomBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            GradientStops = { new GradientStop(baseColor, 0), new GradientStop(transparent, 1) }
        };
        context.DrawRectangle(bottomBrush, null, new Rect(0, h - t, w, t));

        // Left edge (full height — overlaps corners for seamless glow)
        var leftBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
            GradientStops = { new GradientStop(baseColor, 0), new GradientStop(transparent, 1) }
        };
        context.DrawRectangle(leftBrush, null, new Rect(0, 0, t, h));

        // Right edge (full height)
        var rightBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            GradientStops = { new GradientStop(baseColor, 0), new GradientStop(transparent, 1) }
        };
        context.DrawRectangle(rightBrush, null, new Rect(w - t, 0, t, h));
    }
}
