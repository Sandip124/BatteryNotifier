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

    // Gradient brushes depend only on GlowColor (their geometry is relative to each drawn rect),
    // so they're cached and rebuilt only when the color changes — keeping per-frame renders cheap
    // while GlowThickness animates.
    private Color _cachedColor;
    private bool _brushesValid;
    private IBrush? _top, _bottom, _left, _right, _cornerTL, _cornerTR, _cornerBL, _cornerBR;

    public override void Render(DrawingContext context)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        var t = Math.Min(GlowThickness, Math.Min(w, h) / 3);
        if (t <= 0) return;

        EnsureBrushes();

        // Straight bands over the middle spans (corners are drawn separately).
        if (w - 2 * t > 0)
        {
            context.DrawRectangle(_top, null, new Rect(t, 0, w - 2 * t, t));
            context.DrawRectangle(_bottom, null, new Rect(t, h - t, w - 2 * t, t));
        }
        if (h - 2 * t > 0)
        {
            context.DrawRectangle(_left, null, new Rect(0, t, t, h - 2 * t));
            context.DrawRectangle(_right, null, new Rect(w - t, t, t, h - 2 * t));
        }

        // Rounded inner corners: a radial gradient centred at each inner corner (opaque at radius
        // t → clear at the centre). The arc of radius t forms the rounded inner edge; the region
        // beyond it (toward the square outer corner) stays fully opaque.
        context.DrawRectangle(_cornerTL, null, new Rect(0, 0, t, t));
        context.DrawRectangle(_cornerTR, null, new Rect(w - t, 0, t, t));
        context.DrawRectangle(_cornerBL, null, new Rect(0, h - t, t, t));
        context.DrawRectangle(_cornerBR, null, new Rect(w - t, h - t, t, t));
    }

    private void EnsureBrushes()
    {
        if (_brushesValid && _cachedColor == GlowColor) return;

        var outer = GlowColor;
        var inner = Color.FromArgb(0, outer.R, outer.G, outer.B);

        _top = Linear(outer, inner, (0, 0), (0, 1));
        _bottom = Linear(outer, inner, (0, 1), (0, 0));
        _left = Linear(outer, inner, (0, 0), (1, 0));
        _right = Linear(outer, inner, (1, 0), (0, 0));
        _cornerTL = Corner(outer, inner, (1, 1));
        _cornerTR = Corner(outer, inner, (0, 1));
        _cornerBL = Corner(outer, inner, (1, 0));
        _cornerBR = Corner(outer, inner, (0, 0));

        _cachedColor = outer;
        _brushesValid = true;
    }

    private static LinearGradientBrush Linear(Color outer, Color inner,
        (double x, double y) from, (double x, double y) to) => new()
    {
        StartPoint = new RelativePoint(from.x, from.y, RelativeUnit.Relative),
        EndPoint = new RelativePoint(to.x, to.y, RelativeUnit.Relative),
        GradientStops = { new GradientStop(outer, 0), new GradientStop(inner, 1) },
    };

    private static RadialGradientBrush Corner(Color outer, Color inner, (double x, double y) innerCorner)
    {
        var center = new RelativePoint(innerCorner.x, innerCorner.y, RelativeUnit.Relative);
        return new RadialGradientBrush
        {
            Center = center,
            GradientOrigin = center,
            RadiusX = new RelativeScalar(1, RelativeUnit.Relative),
            RadiusY = new RelativeScalar(1, RelativeUnit.Relative),
            GradientStops = { new GradientStop(inner, 0), new GradientStop(outer, 1) },
        };
    }
}
