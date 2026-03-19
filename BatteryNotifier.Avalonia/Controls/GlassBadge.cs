using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace BatteryNotifier.Avalonia.Controls;

/// <summary>
/// Renders a battery-shaped glass badge with cylindrical 3D shading,
/// metallic body border, and terminal cap — matching BatteryIndicatorControl aesthetics.
/// </summary>
internal class GlassBadge : Control
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<GlassBadge, string>(nameof(Text), "");

    public static readonly StyledProperty<Color> AccentColorProperty =
        AvaloniaProperty.Register<GlassBadge, Color>(nameof(AccentColor), Colors.DodgerBlue);

    static GlassBadge()
    {
        AffectsRender<GlassBadge>(TextProperty, AccentColorProperty);
    }

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public Color AccentColor
    {
        get => GetValue(AccentColorProperty);
        set => SetValue(AccentColorProperty, value);
    }

    private static readonly RelativePoint RelTop = new(0, 0, RelativeUnit.Relative);
    private static readonly RelativePoint RelBottom = new(0, 1, RelativeUnit.Relative);

    public override void Render(DrawingContext ctx)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        var color = AccentColor;

        // Layout: [body] [gap] [terminal cap]
        var termW = w * 0.055;
        var termGap = w * 0.012;
        var bodyW = w - termW - termGap;
        var cornerR = h * 0.24;

        var bodyRect = new Rect(0, 0, bodyW, h);

        DrawGlassBody(ctx, bodyRect, cornerR, color);
        DrawReflections(ctx, bodyRect, cornerR);
        DrawTerminalCap(ctx, bodyW + termGap, h, termW);
        DrawText(ctx, bodyRect);
    }

    // ── Glass body with metallic edge ──

    private void DrawGlassBody(DrawingContext ctx, Rect rect, double cornerR, Color color)
    {
        // Cylindrical fill gradient
        var bodyBrush = new LinearGradientBrush
        {
            StartPoint = RelTop, EndPoint = RelBottom,
            GradientStops = new GradientStops
            {
                new(Lighten(color, 0.38), 0.0),
                new(Lighten(color, 0.18), 0.15),
                new(color, 0.45),
                new(Darken(color, 0.10), 0.72),
                new(Darken(color, 0.28), 1.0),
            }
        };

        // Metallic edge border — bright top, dark bottom
        var edgePen = new Pen(new LinearGradientBrush
        {
            StartPoint = RelTop, EndPoint = RelBottom,
            GradientStops = new GradientStops
            {
                new(Color.FromArgb(130, 255, 255, 255), 0.0),
                new(Color.FromArgb(60, 255, 255, 255), 0.3),
                new(Color.FromArgb(20, 255, 255, 255), 0.5),
                new(Color.FromArgb(40, 0, 0, 0), 0.8),
                new(Color.FromArgb(70, 0, 0, 0), 1.0),
            }
        }, 1.6);

        ctx.DrawRectangle(bodyBrush, edgePen, rect, cornerR, cornerR);

        // Diffuse interior glow
        var diffuse = new RadialGradientBrush
        {
            Center = new RelativePoint(0.5, 0.35, RelativeUnit.Relative),
            GradientOrigin = new RelativePoint(0.5, 0.3, RelativeUnit.Relative),
            RadiusX = new RelativeScalar(0.7, RelativeUnit.Relative),
            RadiusY = new RelativeScalar(0.6, RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new(Color.FromArgb(22, 255, 255, 255), 0.0),
                new(Color.FromArgb(8, 255, 255, 255), 0.5),
                new(Color.FromArgb(0, 255, 255, 255), 1.0),
            }
        };
        ctx.DrawRectangle(diffuse, null, rect, cornerR, cornerR);
    }

    // ── Reflective layers ──

    private static void DrawReflections(DrawingContext ctx, Rect body, double cornerR)
    {
        var clip = new RectangleGeometry(body, cornerR, cornerR);
        using var _ = ctx.PushGeometryClip(clip);

        var w = body.Width;
        var h = body.Height;
        var insetX = w * 0.08;

        // Specular highlight band — top 38%
        ctx.DrawRectangle(new LinearGradientBrush
        {
            StartPoint = RelTop, EndPoint = RelBottom,
            GradientStops = new GradientStops
            {
                new(Color.FromArgb(75, 255, 255, 255), 0.0),
                new(Color.FromArgb(35, 255, 255, 255), 0.4),
                new(Color.FromArgb(0, 255, 255, 255), 1.0),
            }
        }, null, new Rect(0, 0, w, h * 0.38));

        // Thin bright specular line at top
        DrawHBeam(ctx, new Rect(insetX, h * 0.09, w - insetX * 2, Math.Max(1.2, h * 0.04)),
            wE: 85, wC: 115);

        // Upper broad reflective beam
        DrawHBeam(ctx, new Rect(insetX, h * 0.14, w - insetX * 2, h * 0.30),
            wE: 45, wC: 60);

        // Mid shadow band
        DrawHBeam(ctx, new Rect(insetX * 1.5, h * 0.56, w - insetX * 3, h * 0.10),
            bE: 14, bC: 22);

        // Thin accent shadow
        DrawHBeam(ctx, new Rect(insetX * 2, h * 0.68, w - insetX * 4, Math.Max(1.0, h * 0.035)),
            bE: 8, bC: 15);

        // Bottom reflected light
        ctx.DrawRectangle(new LinearGradientBrush
        {
            StartPoint = RelTop, EndPoint = RelBottom,
            GradientStops = new GradientStops
            {
                new(Color.FromArgb(0, 255, 255, 255), 0.0),
                new(Color.FromArgb(12, 255, 255, 255), 0.5),
                new(Color.FromArgb(24, 255, 255, 255), 1.0),
            }
        }, null, new Rect(0, h * 0.80, w, h * 0.20));

        // Bottom edge highlight
        DrawHBeam(ctx, new Rect(insetX * 1.5, h * 0.88, w - insetX * 3, Math.Max(1.0, h * 0.03)),
            wE: 18, wC: 28);
    }

    // ── Terminal cap — brushed metal nub ──

    private static void DrawTerminalCap(DrawingContext ctx, double x, double bodyH, double termW)
    {
        var termH = bodyH * 0.36;
        var termY = (bodyH - termH) / 2;
        var termRect = new Rect(x, termY, termW, termH);
        var termCornerR = Math.Min(termW * 0.35, termH * 0.25);

        // Metallic cylinder gradient
        var metalBrush = new LinearGradientBrush
        {
            StartPoint = RelTop, EndPoint = RelBottom,
            GradientStops = new GradientStops
            {
                new(Color.FromRgb(155, 160, 172), 0.0),
                new(Color.FromRgb(200, 205, 215), 0.12),
                new(Color.FromRgb(170, 175, 185), 0.30),
                new(Color.FromRgb(130, 135, 145), 0.50),
                new(Color.FromRgb(110, 115, 125), 0.70),
                new(Color.FromRgb(95, 100, 110), 1.0),
            }
        };

        var metalPen = new Pen(new SolidColorBrush(Color.FromArgb(120, 80, 85, 95)), 0.8);
        ctx.DrawRectangle(metalBrush, metalPen, termRect, termCornerR, termCornerR);

        // Specular highlight stripe down the center
        var highlightW = termW * 0.5;
        var highlightRect = new Rect(x + (termW - highlightW) / 2, termY + 1, highlightW, termH - 2);
        ctx.DrawRectangle(new LinearGradientBrush
        {
            StartPoint = RelTop, EndPoint = RelBottom,
            GradientStops = new GradientStops
            {
                new(Color.FromArgb(55, 255, 255, 255), 0.0),
                new(Color.FromArgb(22, 255, 255, 255), 0.3),
                new(Color.FromArgb(0, 255, 255, 255), 0.55),
                new(Color.FromArgb(10, 255, 255, 255), 0.85),
                new(Color.FromArgb(20, 255, 255, 255), 1.0),
            }
        }, null, highlightRect, termCornerR * 0.5, termCornerR * 0.5);
    }

    // ── Centered percentage text ──

    private void DrawText(DrawingContext ctx, Rect bodyRect)
    {
        if (string.IsNullOrEmpty(Text)) return;

        var fontSize = bodyRect.Height * 0.46;
        var typeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);

        // Drop shadow
        var shadowFt = new FormattedText(Text, CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, typeface, fontSize,
            new SolidColorBrush(Color.FromArgb(70, 0, 0, 0)));
        var tx = bodyRect.X + (bodyRect.Width - shadowFt.Width) / 2;
        var ty = bodyRect.Y + (bodyRect.Height - shadowFt.Height) / 2;
        ctx.DrawText(shadowFt, new Point(tx, ty + 1.2));

        // White text
        var ft = new FormattedText(Text, CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, typeface, fontSize, Brushes.White);
        ctx.DrawText(ft, new Point(tx, ty));
    }

    // ── Horizontal beam helper ──

    private static void DrawHBeam(DrawingContext ctx, Rect rect,
        byte wE = 0, byte wC = 0, byte bE = 0, byte bC = 0)
    {
        if (wC > 0 || wE > 0)
        {
            ctx.DrawRectangle(new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new(Color.FromArgb(0, 255, 255, 255), 0.0),
                    new(Color.FromArgb(wE, 255, 255, 255), 0.15),
                    new(Color.FromArgb(wC, 255, 255, 255), 0.5),
                    new(Color.FromArgb(wE, 255, 255, 255), 0.85),
                    new(Color.FromArgb(0, 255, 255, 255), 1.0),
                }
            }, null, rect);
        }

        if (bC > 0 || bE > 0)
        {
            ctx.DrawRectangle(new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new(Color.FromArgb(0, 0, 0, 0), 0.0),
                    new(Color.FromArgb(bE, 0, 0, 0), 0.2),
                    new(Color.FromArgb(bC, 0, 0, 0), 0.5),
                    new(Color.FromArgb(bE, 0, 0, 0), 0.8),
                    new(Color.FromArgb(0, 0, 0, 0), 1.0),
                }
            }, null, rect);
        }
    }

    // ── Color helpers ──

    private static Color Lighten(Color c, double amt) => Color.FromRgb(
        (byte)Math.Min(255, c.R + (255 - c.R) * amt),
        (byte)Math.Min(255, c.G + (255 - c.G) * amt),
        (byte)Math.Min(255, c.B + (255 - c.B) * amt));

    private static Color Darken(Color c, double amt) => Color.FromRgb(
        (byte)(c.R * (1 - amt)),
        (byte)(c.G * (1 - amt)),
        (byte)(c.B * (1 - amt)));
}