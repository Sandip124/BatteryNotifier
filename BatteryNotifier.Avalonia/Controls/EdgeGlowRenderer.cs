using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace BatteryNotifier.Avalonia.Controls;

/// <summary>
/// Renders an edge glow — four gradient bands fading inward from the (square) screen edges, with
/// the four corners drawn as radial gradients so the glow's <b>inner</b> edge is rounded while the
/// outer edge stays flush to the rectangular screen. Corners blend seamlessly with the straight
/// bands (matching alpha along the shared boundaries). Used by ScreenFlashOverlay.
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
        var outer = GlowColor;
        var inner = Color.FromArgb(0, outer.R, outer.G, outer.B);

        // Straight bands over the middle spans (corners are drawn separately).
        if (w - 2 * t > 0)
        {
            context.DrawRectangle(Linear(outer, inner, (0, 0), (0, 1)), null, new Rect(t, 0, w - 2 * t, t));       // top
            context.DrawRectangle(Linear(outer, inner, (0, 1), (0, 0)), null, new Rect(t, h - t, w - 2 * t, t));   // bottom
        }
        if (h - 2 * t > 0)
        {
            context.DrawRectangle(Linear(outer, inner, (0, 0), (1, 0)), null, new Rect(0, t, t, h - 2 * t));       // left
            context.DrawRectangle(Linear(outer, inner, (1, 0), (0, 0)), null, new Rect(w - t, t, t, h - 2 * t));   // right
        }

        // Rounded inner corners: a radial gradient centred at each inner corner (opaque at radius
        // t → clear at the centre). The arc of radius t forms the rounded inner edge; the region
        // beyond it (toward the square outer corner) stays fully opaque.
        DrawCorner(context, new Rect(0, 0, t, t), innerCorner: (1, 1), outer, inner);           // top-left
        DrawCorner(context, new Rect(w - t, 0, t, t), innerCorner: (0, 1), outer, inner);       // top-right
        DrawCorner(context, new Rect(0, h - t, t, t), innerCorner: (1, 0), outer, inner);       // bottom-left
        DrawCorner(context, new Rect(w - t, h - t, t, t), innerCorner: (0, 0), outer, inner);   // bottom-right
    }

    private static LinearGradientBrush Linear(Color outer, Color inner,
        (double x, double y) from, (double x, double y) to) => new()
    {
        StartPoint = new RelativePoint(from.x, from.y, RelativeUnit.Relative),
        EndPoint = new RelativePoint(to.x, to.y, RelativeUnit.Relative),
        GradientStops = { new GradientStop(outer, 0), new GradientStop(inner, 1) },
    };

    private static void DrawCorner(DrawingContext context, Rect square,
        (double x, double y) innerCorner, Color outer, Color inner)
    {
        var center = new RelativePoint(innerCorner.x, innerCorner.y, RelativeUnit.Relative);
        var brush = new RadialGradientBrush
        {
            Center = center,
            GradientOrigin = center,
            RadiusX = new RelativeScalar(1, RelativeUnit.Relative),
            RadiusY = new RelativeScalar(1, RelativeUnit.Relative),
            GradientStops = { new GradientStop(inner, 0), new GradientStop(outer, 1) },
        };
        context.DrawRectangle(brush, null, square);
    }
}
