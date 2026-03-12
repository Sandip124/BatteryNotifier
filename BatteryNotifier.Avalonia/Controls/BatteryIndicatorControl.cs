using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace BatteryNotifier.Avalonia.Controls;

/// <summary>
/// Custom-rendered battery indicator with glossy 3D appearance matching the app asset style.
/// Theme-aware: adapts shell, well, and border colors for light and dark modes.
/// Shows exact charge level with state-colored fill and a status badge at bottom-center.
/// </summary>
public class BatteryIndicatorControl : Control
{
    public static readonly StyledProperty<double> PercentageProperty =
        AvaloniaProperty.Register<BatteryIndicatorControl, double>(nameof(Percentage), 50);

    public static readonly StyledProperty<bool> IsChargingProperty =
        AvaloniaProperty.Register<BatteryIndicatorControl, bool>(nameof(IsCharging));

    static BatteryIndicatorControl()
    {
        AffectsRender<BatteryIndicatorControl>(PercentageProperty, IsChargingProperty);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ActualThemeVariantChanged += OnThemeChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ActualThemeVariantChanged -= OnThemeChanged;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnThemeChanged(object? sender, EventArgs e) => InvalidateVisual();

    public double Percentage
    {
        get => GetValue(PercentageProperty);
        set => SetValue(PercentageProperty, value);
    }

    public bool IsCharging
    {
        get => GetValue(IsChargingProperty);
        set => SetValue(IsChargingProperty, value);
    }

    // ── Theme detection ──────────────────────────────────────────

    private bool IsDark => ActualThemeVariant == ThemeVariant.Dark;

    private ThemePalette Palette => IsDark ? DarkPalette : LightPalette;

    // ── Render ────────────────────────────────────────────────────

    public override void Render(DrawingContext ctx)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        var pct = Math.Clamp(Percentage, 0, 100) / 100.0;
        var p = Palette;

        // Reserve space at bottom for badge overhang
        var badgeRadius = h * 0.18;
        var badgeOverhang = badgeRadius * 0.50;
        var batteryH = h - badgeOverhang;

        // ── Proportional layout ──
        var termW = w * 0.045;
        var termGap = w * 0.006;
        var bodyW = w - termW - termGap;
        var cr = Math.Min(bodyW, batteryH) * 0.15;
        var bw = Math.Max(1.5, Math.Min(bodyW, batteryH) * 0.03);
        var inset = bw + Math.Max(2, batteryH * 0.045);

        var bodyRect = new Rect(0, 0, bodyW, batteryH);
        var bodyRR = new RoundedRect(bodyRect, cr);
        var innerRect = bodyRect.Deflate(inset);
        var innerCR = Math.Max(0, cr - inset);
        var innerRR = new RoundedRect(innerRect, innerCR);

        DrawBodyShell(ctx, bodyRR, bw, p);
        DrawInnerWell(ctx, innerRR, p);
        DrawFill(ctx, innerRect, innerRR, pct);
        DrawGlassReflection(ctx, innerRect, innerRR);
        DrawTerminal(ctx, bodyW + termGap, batteryH, termW, bw, cr, p);
        DrawStatusBadge(ctx, bodyRect, badgeRadius, p);
    }

    // ── Layer 1: Metallic shell ──────────────────────────────────

    private static void DrawBodyShell(DrawingContext ctx, RoundedRect rr, double bw, ThemePalette p)
    {
        var shell = new LinearGradientBrush
        {
            StartPoint = RelStart, EndPoint = RelEnd,
            GradientStops = new GradientStops
            {
                new(p.ShellTop, 0.0),
                new(p.ShellUpper, 0.12),
                new(p.ShellMid, 0.45),
                new(p.ShellLower, 0.80),
                new(p.ShellBottom, 1.0),
            }
        };
        ctx.DrawRectangle(shell, null, rr);

        var pen = new Pen(new SolidColorBrush(p.Border), bw);
        ctx.DrawRectangle(null, pen, rr);

        var highlightRect = new Rect(rr.Rect.X + bw * 2, rr.Rect.Y + bw * 0.5,
                                      rr.Rect.Width - bw * 4, bw * 0.6);
        ctx.DrawRectangle(new SolidColorBrush(Color.FromArgb(p.HighlightAlpha, 255, 255, 255)), null, highlightRect);
    }

    // ── Layer 2: Inner well ──────────────────────────────────────

    private static void DrawInnerWell(DrawingContext ctx, RoundedRect rr, ThemePalette p)
    {
        var bg = new LinearGradientBrush
        {
            StartPoint = RelStart, EndPoint = RelEnd,
            GradientStops = new GradientStops
            {
                new(p.WellTop, 0.0),
                new(p.WellMid, 0.35),
                new(p.WellBottom, 1.0),
            }
        };
        ctx.DrawRectangle(bg, null, rr);

        var shadowRect = new Rect(rr.Rect.X, rr.Rect.Y, rr.Rect.Width, rr.Rect.Height * 0.12);
        using (ctx.PushClip(rr))
        {
            ctx.DrawRectangle(new SolidColorBrush(Color.FromArgb(p.WellShadowAlpha, 0, 0, 0)), null, shadowRect);
        }
    }

    // ── Layer 3: Colored fill bar ────────────────────────────────

    private void DrawFill(DrawingContext ctx, Rect inner, RoundedRect innerRR, double pct)
    {
        var fillW = inner.Width * pct;
        if (fillW < 1) return;

        var fillRect = new Rect(inner.X, inner.Y, fillW, inner.Height);
        var color = GetFillColor(Percentage);

        var fillBrush = new LinearGradientBrush
        {
            StartPoint = RelStart, EndPoint = RelEnd,
            GradientStops = new GradientStops
            {
                new(Lighten(color, 0.38), 0.0),
                new(Lighten(color, 0.16), 0.18),
                new(color, 0.48),
                new(Darken(color, 0.10), 0.78),
                new(Darken(color, 0.25), 1.0),
            }
        };

        using (ctx.PushClip(innerRR))
        {
            ctx.DrawRectangle(fillBrush, null, fillRect);

            var edgeH = Math.Max(1.5, inner.Height * 0.06);
            ctx.DrawRectangle(
                new SolidColorBrush(Color.FromArgb(55, 255, 255, 255)), null,
                new Rect(inner.X, inner.Y, fillW, edgeH));
        }
    }

    // ── Layer 4: Glass reflection ────────────────────────────────

    private static void DrawGlassReflection(DrawingContext ctx, Rect inner, RoundedRect innerRR)
    {
        var glassH = inner.Height * 0.40;
        var glassRect = new Rect(inner.X, inner.Y, inner.Width, glassH);

        var glass = new LinearGradientBrush
        {
            StartPoint = RelStart, EndPoint = RelEnd,
            GradientStops = new GradientStops
            {
                new(Color.FromArgb(60, 255, 255, 255), 0.0),
                new(Color.FromArgb(22, 255, 255, 255), 0.55),
                new(Color.FromArgb(0, 255, 255, 255), 1.0),
            }
        };

        using (ctx.PushClip(innerRR))
        {
            ctx.DrawRectangle(glass, null, glassRect);
        }
    }

    // ── Layer 5: Terminal cap ────────────────────────────────────

    private static void DrawTerminal(DrawingContext ctx, double x, double bodyH,
        double termW, double bw, double bodyCR, ThemePalette p)
    {
        var termH = bodyH * 0.34;
        var termY = (bodyH - termH) / 2;
        var termCR = Math.Min(termW * 0.4, bodyCR * 0.5);

        var termRect = new Rect(x, termY, termW, termH);
        var termRR = new RoundedRect(termRect,
            new CornerRadius(0, termCR, termCR, 0));

        var brush = new LinearGradientBrush
        {
            StartPoint = RelStart, EndPoint = RelEnd,
            GradientStops = new GradientStops
            {
                new(p.TermTop, 0.0),
                new(p.TermMid, 0.45),
                new(p.TermBottom, 1.0),
            }
        };

        var pen = new Pen(new SolidColorBrush(p.Border), bw * 0.7);
        ctx.DrawRectangle(brush, pen, termRR);
    }

    // ── Layer 6: Status badge at bottom-center ──────────────────

    private void DrawStatusBadge(DrawingContext ctx, Rect bodyRect, double radius, ThemePalette p)
    {
        var cx = bodyRect.Width * 0.5;
        var cy = bodyRect.Bottom - radius * 0.45;

        var badgeColor = GetBadgeColor();

        // Drop shadow
        ctx.DrawEllipse(
            new SolidColorBrush(Color.FromArgb(p.BadgeShadowAlpha, 0, 0, 0)), null,
            new Point(cx, cy + 1.5), radius + 1.5, radius + 1.5);

        if (IsLowOrCritical)
            DrawTriangleBadge(ctx, cx, cy, radius, badgeColor);
        else
            DrawCircleBadge(ctx, cx, cy, radius, badgeColor);
    }

    private void DrawCircleBadge(DrawingContext ctx, double cx, double cy, double r, Color color)
    {
        var brush = new SolidColorBrush(color);
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)), 1.5);
        ctx.DrawEllipse(brush, pen, new Point(cx, cy), r, r);

        if (IsCharging)
            DrawBoltIcon(ctx, cx, cy, r);
        else
            DrawCheckmarkIcon(ctx, cx, cy, r);
    }

    private static void DrawTriangleBadge(DrawingContext ctx, double cx, double cy, double r, Color color)
    {
        var triH = r * 2.0;
        var triW = triH * 1.15;
        var topY = cy - triH * 0.52;
        var botY = cy + triH * 0.48;

        var geo = new StreamGeometry();
        using (var c = geo.Open())
        {
            c.BeginFigure(new Point(cx, topY), true);
            c.LineTo(new Point(cx + triW / 2, botY));
            c.LineTo(new Point(cx - triW / 2, botY));
            c.EndFigure(true);
        }

        var brush = new SolidColorBrush(color);
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)), 1.5)
            { LineJoin = PenLineJoin.Round };
        ctx.DrawGeometry(brush, pen, geo);

        DrawExclamationIcon(ctx, cx, cy, r);
    }

    private static void DrawCheckmarkIcon(DrawingContext ctx, double cx, double cy, double r)
    {
        var s = r * 0.50;
        var geo = new StreamGeometry();
        using (var c = geo.Open())
        {
            c.BeginFigure(new Point(cx - s * 0.65, cy + s * 0.05), false);
            c.LineTo(new Point(cx - s * 0.08, cy + s * 0.55));
            c.LineTo(new Point(cx + s * 0.70, cy - s * 0.42));
        }
        var pen = new Pen(Brushes.White, r * 0.18)
            { LineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };
        ctx.DrawGeometry(null, pen, geo);
    }

    private static void DrawExclamationIcon(DrawingContext ctx, double cx, double cy, double r)
    {
        var white = new SolidColorBrush(Colors.White);
        var strokeW = r * 0.16;

        var lineH = r * 0.50;
        var lineTop = cy - lineH * 0.45;
        var pen = new Pen(white, strokeW * 2) { LineCap = PenLineCap.Round };
        var geo = new StreamGeometry();
        using (var c = geo.Open())
        {
            c.BeginFigure(new Point(cx, lineTop), false);
            c.LineTo(new Point(cx, lineTop + lineH));
        }
        ctx.DrawGeometry(null, pen, geo);

        var dotR = strokeW * 1.1;
        ctx.DrawEllipse(white, null,
            new Point(cx, lineTop + lineH + dotR * 2.8), dotR, dotR);
    }

    private static void DrawBoltIcon(DrawingContext ctx, double cx, double cy, double r)
    {
        var s = r * 1.1;
        var bx = cx - s * 0.5;
        var by = cy - s * 0.5;

        var geo = new StreamGeometry();
        using (var c = geo.Open())
        {
            c.BeginFigure(new Point(bx + s * 0.55, by), true);
            c.LineTo(new Point(bx + s * 0.18, by + s * 0.47));
            c.LineTo(new Point(bx + s * 0.45, by + s * 0.47));
            c.LineTo(new Point(bx + s * 0.32, by + s));
            c.LineTo(new Point(bx + s * 0.82, by + s * 0.53));
            c.LineTo(new Point(bx + s * 0.55, by + s * 0.53));
            c.EndFigure(true);
        }
        ctx.DrawGeometry(Brushes.White, null, geo);
    }

    // ── State helpers ───────────────────────────────────────────

    private bool IsLowOrCritical => Percentage < 40 && !IsCharging;

    private Color GetBadgeColor()
    {
        if (IsCharging)
            return AppGreen;

        return Percentage switch
        {
            >= 60 => AppGreen,
            >= 40 => BadgeBlue,
            >= 15 => BadgeAmber,
            _     => BadgeRed,
        };
    }

    private static Color GetFillColor(double pct) => pct switch
    {
        >= 60 => AppGreen,
        >= 40 => BadgeBlue,
        >= 15 => BadgeAmber,
        _     => BadgeRed,
    };

    // ── Color palette — extracted from app icon & asset PNGs ────

    private static readonly Color AppGreen   = Color.FromRgb(59, 175, 74);   // #3BAF4A
    private static readonly Color BadgeBlue  = Color.FromRgb(74, 144, 226);  // #4A90E2
    private static readonly Color BadgeAmber = Color.FromRgb(240, 180, 41);  // #F0B429
    private static readonly Color BadgeRed   = Color.FromRgb(234, 67, 53);   // #EA4335

    // ── Theme palettes ──────────────────────────────────────────

    private record ThemePalette
    {
        // Shell gradient (top to bottom)
        public required Color ShellTop, ShellUpper, ShellMid, ShellLower, ShellBottom;
        public required Color Border;
        public required byte HighlightAlpha;

        // Inner well gradient
        public required Color WellTop, WellMid, WellBottom;
        public required byte WellShadowAlpha;

        // Terminal cap gradient
        public required Color TermTop, TermMid, TermBottom;

        // Badge
        public required byte BadgeShadowAlpha;
    }

    private static readonly ThemePalette DarkPalette = new()
    {
        ShellTop    = Color.FromRgb(220, 222, 228),
        ShellUpper  = Color.FromRgb(192, 194, 202),
        ShellMid    = Color.FromRgb(168, 170, 178),
        ShellLower  = Color.FromRgb(148, 150, 158),
        ShellBottom = Color.FromRgb(132, 134, 142),
        Border      = Color.FromRgb(88, 90, 98),
        HighlightAlpha = 50,

        WellTop    = Color.FromRgb(18, 20, 28),
        WellMid    = Color.FromRgb(30, 33, 42),
        WellBottom = Color.FromRgb(25, 28, 36),
        WellShadowAlpha = 35,

        TermTop    = Color.FromRgb(200, 202, 210),
        TermMid    = Color.FromRgb(160, 162, 170),
        TermBottom = Color.FromRgb(138, 140, 148),

        BadgeShadowAlpha = 45,
    };

    private static readonly ThemePalette LightPalette = new()
    {
        ShellTop    = Color.FromRgb(245, 246, 248),
        ShellUpper  = Color.FromRgb(228, 230, 235),
        ShellMid    = Color.FromRgb(210, 212, 218),
        ShellLower  = Color.FromRgb(198, 200, 206),
        ShellBottom = Color.FromRgb(188, 190, 196),
        Border      = Color.FromRgb(165, 168, 178),
        HighlightAlpha = 80,

        WellTop    = Color.FromRgb(215, 218, 225),
        WellMid    = Color.FromRgb(228, 230, 235),
        WellBottom = Color.FromRgb(220, 222, 228),
        WellShadowAlpha = 18,

        TermTop    = Color.FromRgb(238, 240, 244),
        TermMid    = Color.FromRgb(210, 212, 218),
        TermBottom = Color.FromRgb(195, 198, 205),

        BadgeShadowAlpha = 25,
    };

    // ── Color helpers ───────────────────────────────────────────

    private static Color Lighten(Color c, double amt) => Color.FromRgb(
        (byte)Math.Min(255, c.R + (255 - c.R) * amt),
        (byte)Math.Min(255, c.G + (255 - c.G) * amt),
        (byte)Math.Min(255, c.B + (255 - c.B) * amt));

    private static Color Darken(Color c, double amt) => Color.FromRgb(
        (byte)(c.R * (1 - amt)),
        (byte)(c.G * (1 - amt)),
        (byte)(c.B * (1 - amt)));

    private static readonly RelativePoint RelStart = new(0, 0, RelativeUnit.Relative);
    private static readonly RelativePoint RelEnd = new(0, 1, RelativeUnit.Relative);
}
