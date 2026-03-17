using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace BatteryNotifier.Avalonia.Controls;

/// <summary>
/// Custom-rendered battery indicator with cylindrical 3D appearance.
/// Glass body with diffuse reflections, cylindrical fill bar,
/// metallic terminal cap, and status badge overlay.
/// </summary>
internal class BatteryIndicatorControl : Control
{
    // ── Styled properties ────────────────────────────────────────

    public static readonly StyledProperty<double> PercentageProperty =
        AvaloniaProperty.Register<BatteryIndicatorControl, double>(nameof(Percentage), 50);

    public static readonly StyledProperty<bool> IsChargingProperty =
        AvaloniaProperty.Register<BatteryIndicatorControl, bool>(nameof(IsCharging));

    static BatteryIndicatorControl()
    {
        AffectsRender<BatteryIndicatorControl>(PercentageProperty, IsChargingProperty);
    }

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

    // ── Lifecycle ────────────────────────────────────────────────

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

    // ── Theme ────────────────────────────────────────────────────

    private bool IsDark => ActualThemeVariant == ThemeVariant.Dark;
    private ThemePalette Palette => IsDark ? DarkPalette : LightPalette;

    // ── Render ───────────────────────────────────────────────────

    public override void Render(DrawingContext ctx)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        var pct = Math.Clamp(Percentage, 0, 100) / 100.0;
        var p = Palette;

        var badgeRadius = h * 0.18;
        var batteryH = h - badgeRadius * 0.50;

        // Proportional layout
        var termW = w * 0.050;
        var termGap = w * 0.008;
        var bodyW = w - termW - termGap;
        var cornerR = batteryH * 0.22; // cylindrical roundness
        var inset = Math.Max(2, batteryH * 0.075);

        var bodyRect = new Rect(0, 0, bodyW, batteryH);
        var innerRect = bodyRect.Deflate(inset);

        // Draw layers back-to-front
        DrawAmbientGlow(ctx, w, h);
        DrawGlassBody(ctx, bodyRect, cornerR, p);
        DrawInnerWell(ctx, innerRect, cornerR * 0.65, p);
        DrawFill(ctx, innerRect, cornerR * 0.65, pct);
        DrawGlassReflections(ctx, bodyRect, cornerR);
        DrawTerminalCap(ctx, bodyW + termGap, batteryH, termW, p);
        DrawStatusBadge(ctx, bodyRect, badgeRadius, p);
    }

    // ── Layer 0: Ambient glow ────────────────────────────────────

    private void DrawAmbientGlow(DrawingContext ctx, double w, double h)
    {
        var color = IsCharging ? AppGreen : GetFillColor(Percentage);
        var alpha = (byte)(IsDark ? 25 : 16);

        var cx = w * 0.47;
        var cy = h * 0.5;
        var radius = Math.Max(w, h) * 2.0;

        var brush = new RadialGradientBrush
        {
            Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            GradientOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new(Color.FromArgb(alpha, color.R, color.G, color.B), 0.0),
                new(Color.FromArgb((byte)(alpha * 0.5), color.R, color.G, color.B), 0.35),
                new(Color.FromArgb(0, color.R, color.G, color.B), 0.85),
            }
        };

        ctx.DrawRectangle(brush, null,
            new Rect(cx - radius, cy - radius, radius * 2, radius * 2));
    }

    // ── Layer 1: Glass cylinder body ─────────────────────────────
    //
    // Simulates a horizontal glass cylinder using vertical gradients:
    // bright specular at top → transparent mid → subtle reflected light at bottom.

    private static void DrawGlassBody(DrawingContext ctx, Rect rect, double cornerR, ThemePalette p)
    {
        // Outer cylinder edge — subtle border
        var outerPen = new Pen(new LinearGradientBrush
        {
            StartPoint = RelStart, EndPoint = RelEnd,
            GradientStops = new GradientStops
            {
                new(p.GlassEdgeTop, 0.0),
                new(p.GlassEdgeMid, 0.5),
                new(p.GlassEdgeBottom, 1.0),
            }
        }, 1.8);

        // Glass body fill — cylindrical shading with transparency
        var glassFill = new LinearGradientBrush
        {
            StartPoint = RelStart, EndPoint = RelEnd,
            GradientStops = new GradientStops
            {
                new(p.GlassTop, 0.0),
                new(p.GlassUpper, 0.15),
                new(p.GlassMid, 0.45),
                new(p.GlassLower, 0.75),
                new(p.GlassBottom, 1.0),
            }
        };

        ctx.DrawRectangle(glassFill, outerPen, rect, cornerR, cornerR);

        // Diffuse interior glow — gives depth to the glass
        var diffuse = new RadialGradientBrush
        {
            Center = new RelativePoint(0.5, 0.35, RelativeUnit.Relative),
            GradientOrigin = new RelativePoint(0.5, 0.3, RelativeUnit.Relative),
            RadiusX = new RelativeScalar(0.7, RelativeUnit.Relative),
            RadiusY = new RelativeScalar(0.6, RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new(Color.FromArgb(p.DiffuseAlpha, 255, 255, 255), 0.0),
                new(Color.FromArgb((byte)(p.DiffuseAlpha * 0.3), 255, 255, 255), 0.5),
                new(Color.FromArgb(0, 255, 255, 255), 1.0),
            }
        };
        ctx.DrawRectangle(diffuse, null, rect, cornerR, cornerR);
    }

    // ── Layer 2: Inner well (recessed cylinder interior) ──────────

    private static void DrawInnerWell(DrawingContext ctx, Rect rect, double cornerR, ThemePalette p)
    {
        var bg = new LinearGradientBrush
        {
            StartPoint = RelStart, EndPoint = RelEnd,
            GradientStops = new GradientStops
            {
                new(p.WellTop, 0.0),
                new(p.WellMid, 0.4),
                new(p.WellBottom, 1.0),
            }
        };

        ctx.DrawRectangle(bg, null, rect, cornerR, cornerR);

        // Top shadow — recessed edge
        var shadowRect = new Rect(rect.X, rect.Y, rect.Width, rect.Height * 0.15);
        var clip = new RectangleGeometry(rect, cornerR, cornerR);
        using (ctx.PushGeometryClip(clip))
        {
            ctx.DrawRectangle(new LinearGradientBrush
            {
                StartPoint = RelStart, EndPoint = RelEnd,
                GradientStops = new GradientStops
                {
                    new(Color.FromArgb(p.WellShadowAlpha, 0, 0, 0), 0.0),
                    new(Color.FromArgb(0, 0, 0, 0), 1.0),
                }
            }, null, shadowRect);
        }
    }

    // ── Layer 3: Cylindrical fill bar ────────────────────────────

    private void DrawFill(DrawingContext ctx, Rect inner, double cornerR, double pct)
    {
        var color = GetFillColor(Percentage);

        // Background tint across the entire well
        var tintClip = new RectangleGeometry(inner, cornerR, cornerR);
        ctx.DrawGeometry(new SolidColorBrush(Color.FromArgb(30, color.R, color.G, color.B)),
            null, tintClip);

        var fillW = inner.Width * pct;
        if (fillW < 1) return;

        var fillRect = new Rect(inner.X, inner.Y, fillW, inner.Height);
        var fillCornerR = Math.Min(cornerR, fillW * 0.5);

        // Cylindrical fill gradient: light top → saturated mid → dark bottom
        var fillBrush = new LinearGradientBrush
        {
            StartPoint = RelStart, EndPoint = RelEnd,
            GradientStops = new GradientStops
            {
                new(Lighten(color, 0.42), 0.0),
                new(Lighten(color, 0.20), 0.15),
                new(color, 0.45),
                new(Darken(color, 0.08), 0.72),
                new(Darken(color, 0.22), 1.0),
            }
        };

        using (ctx.PushGeometryClip(tintClip))
        {
            ctx.DrawRectangle(fillBrush, null, fillRect, fillCornerR, fillCornerR);

            // Specular highlight on fill top
            var highlightH = Math.Max(1.5, inner.Height * 0.28);
            var highlightRect = new Rect(fillRect.X + 1, fillRect.Y, fillRect.Width - 2, highlightH);
            ctx.DrawRectangle(new LinearGradientBrush
            {
                StartPoint = RelStart, EndPoint = RelEnd,
                GradientStops = new GradientStops
                {
                    new(Color.FromArgb(90, 255, 255, 255), 0.0),
                    new(Color.FromArgb(30, 255, 255, 255), 0.5),
                    new(Color.FromArgb(0, 255, 255, 255), 1.0),
                }
            }, null, highlightRect, fillCornerR, fillCornerR);

            // Bottom shadow on fill
            var shadowH = Math.Max(1.5, inner.Height * 0.18);
            var shadowRect = new Rect(fillRect.X + 1, fillRect.Bottom - shadowH, fillRect.Width - 2, shadowH);
            ctx.DrawRectangle(new LinearGradientBrush
            {
                StartPoint = RelStart, EndPoint = RelEnd,
                GradientStops = new GradientStops
                {
                    new(Color.FromArgb(0, 0, 0, 0), 0.0),
                    new(Color.FromArgb(40, 0, 0, 0), 1.0),
                }
            }, null, shadowRect, fillCornerR, fillCornerR);

            // Left edge glow on fill (cylinder rim light)
            var edgeW = Math.Max(1.5, fillRect.Width * 0.06);
            ctx.DrawRectangle(new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new(Color.FromArgb(55, 255, 255, 255), 0.0),
                    new(Color.FromArgb(0, 255, 255, 255), 1.0),
                }
            }, null, new Rect(fillRect.X, fillRect.Y, edgeW, fillRect.Height), fillCornerR, 0);
        }
    }

    // ── Layer 4: Glass reflections over entire body ───────────────

    private static void DrawGlassReflections(DrawingContext ctx, Rect body, double cornerR)
    {
        var clip = new RectangleGeometry(body, cornerR, cornerR);
        using var _ = ctx.PushGeometryClip(clip);

        var insetX = body.Width * 0.06;

        // Primary specular band — top third of cylinder
        var specularH = body.Height * 0.32;
        ctx.DrawRectangle(new LinearGradientBrush
        {
            StartPoint = RelStart, EndPoint = RelEnd,
            GradientStops = new GradientStops
            {
                new(Color.FromArgb(55, 255, 255, 255), 0.0),
                new(Color.FromArgb(30, 255, 255, 255), 0.35),
                new(Color.FromArgb(0, 255, 255, 255), 1.0),
            }
        }, null, new Rect(body.X, body.Y, body.Width, specularH));

        // Thin bright specular line at top edge
        ctx.DrawRectangle(new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new(Color.FromArgb(0, 255, 255, 255), 0.0),
                new(Color.FromArgb(80, 255, 255, 255), 0.2),
                new(Color.FromArgb(100, 255, 255, 255), 0.5),
                new(Color.FromArgb(80, 255, 255, 255), 0.8),
                new(Color.FromArgb(0, 255, 255, 255), 1.0),
            }
        }, null, new Rect(body.X + insetX, body.Y + body.Height * 0.08,
                          body.Width - insetX * 2, Math.Max(1.2, body.Height * 0.03)));

        // ── Reflective beams (horizontal light bands across the glass) ──

        // Upper beam — soft broad reflection sitting in the top-mid zone
        DrawReflectiveBeam(ctx,
            new Rect(body.X + insetX, body.Y + body.Height * 0.14,
                     body.Width - insetX * 2, Math.Max(1.5, body.Height * 0.34)),
            45, 60);

        // Lower shadow beam — darker band in the bottom-mid zone
        var shadowY = body.Y + body.Height * 0.68;
        var shadowH = Math.Max(2.0, body.Height * 0.10);
        DrawReflectiveBeam(ctx,
            new Rect(body.X + insetX * 1.5, shadowY,
                     body.Width - insetX * 3, shadowH),
            0, 0, 10, 16);

        // Thin accent shadow line just below
        DrawReflectiveBeam(ctx,
            new Rect(body.X + insetX * 2, shadowY + shadowH + body.Height * 0.03,
                     body.Width - insetX * 4, Math.Max(1.0, body.Height * 0.035)),
            0, 0, 6, 12);

        // Bottom reflected light — subtle upward glow
        var reflH = body.Height * 0.15;
        ctx.DrawRectangle(new LinearGradientBrush
        {
            StartPoint = RelStart, EndPoint = RelEnd,
            GradientStops = new GradientStops
            {
                new(Color.FromArgb(0, 255, 255, 255), 0.0),
                new(Color.FromArgb(12, 255, 255, 255), 0.5),
                new(Color.FromArgb(22, 255, 255, 255), 1.0),
            }
        }, null, new Rect(body.X, body.Bottom - reflH, body.Width, reflH));
    }

    /// <summary>
    /// Draws a horizontal reflective beam — a light or shadow band that fades
    /// at both horizontal edges, simulating a light source reflection on glass.
    /// Pass whiteEdge/whiteCenter for bright beams, blackEdge/blackCenter for shadow beams.
    /// </summary>
    private static void DrawReflectiveBeam(DrawingContext ctx, Rect rect,
        byte whiteEdge = 0, byte whiteCenter = 0,
        byte blackEdge = 0, byte blackCenter = 0)
    {
        if (whiteCenter > 0 || whiteEdge > 0)
        {
            ctx.DrawRectangle(new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new(Color.FromArgb(0, 255, 255, 255), 0.0),
                    new(Color.FromArgb(whiteEdge, 255, 255, 255), 0.15),
                    new(Color.FromArgb(whiteCenter, 255, 255, 255), 0.5),
                    new(Color.FromArgb(whiteEdge, 255, 255, 255), 0.85),
                    new(Color.FromArgb(0, 255, 255, 255), 1.0),
                }
            }, null, rect);
        }

        if (blackCenter > 0 || blackEdge > 0)
        {
            ctx.DrawRectangle(new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new(Color.FromArgb(0, 0, 0, 0), 0.0),
                    new(Color.FromArgb(blackEdge, 0, 0, 0), 0.2),
                    new(Color.FromArgb(blackCenter, 0, 0, 0), 0.5),
                    new(Color.FromArgb(blackEdge, 0, 0, 0), 0.8),
                    new(Color.FromArgb(0, 0, 0, 0), 1.0),
                }
            }, null, rect);
        }
    }

    // ── Layer 5: Metallic terminal cap ───────────────────────────
    //
    // Small cylindrical nub with brushed-metal vertical gradient.

    private static void DrawTerminalCap(DrawingContext ctx, double x, double bodyH,
        double termW, ThemePalette p)
    {
        var termH = bodyH * 0.34;
        var termY = (bodyH - termH) / 2;
        var termRect = new Rect(x, termY, termW, termH);
        var termCornerR = Math.Min(termW * 0.35, termH * 0.25);

        // Metallic cylinder gradient
        var metalBrush = new LinearGradientBrush
        {
            StartPoint = RelStart, EndPoint = RelEnd,
            GradientStops = new GradientStops
            {
                new(p.MetalTop, 0.0),
                new(p.MetalHighlight, 0.12),
                new(p.MetalUpper, 0.30),
                new(p.MetalMid, 0.50),
                new(p.MetalLower, 0.70),
                new(p.MetalBottom, 1.0),
            }
        };

        var metalPen = new Pen(new SolidColorBrush(p.MetalEdge), 0.8);
        ctx.DrawRectangle(metalBrush, metalPen, termRect, termCornerR, termCornerR);

        // Specular highlight stripe
        var highlightW = termW * 0.5;
        var highlightRect = new Rect(x + (termW - highlightW) / 2, termY + 1, highlightW, termH - 2);
        ctx.DrawRectangle(new LinearGradientBrush
        {
            StartPoint = RelStart, EndPoint = RelEnd,
            GradientStops = new GradientStops
            {
                new(Color.FromArgb(50, 255, 255, 255), 0.0),
                new(Color.FromArgb(20, 255, 255, 255), 0.3),
                new(Color.FromArgb(0, 255, 255, 255), 0.6),
                new(Color.FromArgb(8, 255, 255, 255), 0.85),
                new(Color.FromArgb(18, 255, 255, 255), 1.0),
            }
        }, null, highlightRect, termCornerR * 0.5, termCornerR * 0.5);
    }

    // ── Layer 6: Status badge ────────────────────────────────────

    private void DrawStatusBadge(DrawingContext ctx, Rect bodyRect, double radius, ThemePalette p)
    {
        var cx = bodyRect.Width * 0.5;
        var cy = bodyRect.Bottom - radius * 0.45;
        var shadowBrush = new SolidColorBrush(Color.FromArgb(p.BadgeShadowAlpha, 0, 0, 0));
        var badgeColor = GetBadgeColor();
        const double shadowOff = 1.5;

        if (IsLowOrCritical)
        {
            DrawTriangle(ctx, cx, cy + shadowOff, radius + shadowOff, shadowBrush, null);
            DrawTriangleBadge(ctx, cx, cy, radius, badgeColor);
        }
        else
        {
            ctx.DrawEllipse(shadowBrush, null,
                new Point(cx, cy + shadowOff), radius + shadowOff, radius + shadowOff);
            DrawCircleBadge(ctx, cx, cy, radius, badgeColor);
        }
    }

    private static void DrawTriangle(DrawingContext ctx, double cx, double cy,
        double r, IBrush? fill, IPen? pen)
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
        ctx.DrawGeometry(fill, pen, geo);
    }

    private void DrawCircleBadge(DrawingContext ctx, double cx, double cy, double r, Color color)
    {
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)), 1.5);
        ctx.DrawEllipse(new SolidColorBrush(color), pen, new Point(cx, cy), r, r);

        if (IsCharging)
            DrawBadgeIcon(ctx, cx, cy, r, BoltGeo, BoltGoldBrush);
        else
            DrawBadgeIcon(ctx, cx, cy, r, CheckGeo);
    }

    private void DrawTriangleBadge(DrawingContext ctx, double cx, double cy, double r, Color color)
    {
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)), 1.5)
            { LineJoin = PenLineJoin.Round };
        DrawTriangle(ctx, cx, cy, r, new SolidColorBrush(color), pen);

        if (IsCharging)
            DrawBadgeIcon(ctx, cx, cy, r, BoltGeo, BoltGoldBrush);
        else
            DrawBadgeIcon(ctx, cx, cy, r, ExclamationGeo);
    }

    // ── Badge icons (loaded from Icons.axaml resource dictionary) ─────

    private static Geometry? _boltGeo;
    private static Geometry? _checkGeo;
    private static Geometry? _exclamationGeo;

    private static Geometry GetBadgeIcon(string key)
    {
        if (Application.Current!.TryFindResource(key, out var res) && res is Geometry geo)
            return geo;
        return new StreamGeometry();
    }

    private static Geometry BoltGeo => _boltGeo ??= GetBadgeIcon("Icon.LightningFill");
    private static Geometry CheckGeo => _checkGeo ??= GetBadgeIcon("Icon.CheckFat");
    private static Geometry ExclamationGeo => _exclamationGeo ??= GetBadgeIcon("Icon.ExclamationMarkFill");

    /// <summary>
    /// Draws a pre-parsed icon geometry scaled and centered inside the badge.
    /// </summary>
    private static void DrawBadgeIcon(DrawingContext ctx, double cx, double cy, double r,
        Geometry geo, IBrush? brush = null)
    {
        var bounds = geo.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        var targetSize = r * 1.2;
        var scale = targetSize / Math.Max(bounds.Width, bounds.Height);
        var geoCx = bounds.X + bounds.Width / 2;
        var geoCy = bounds.Y + bounds.Height / 2;

        using (ctx.PushTransform(
            Matrix.CreateTranslation(-geoCx, -geoCy) *
            Matrix.CreateScale(scale, scale) *
            Matrix.CreateTranslation(cx, cy)))
        {
            ctx.DrawGeometry(brush ?? Brushes.White, null, geo);
        }
    }

    // ── State helpers ────────────────────────────────────────────

    private bool IsLowOrCritical => Percentage < 40 && !IsCharging;

    private Color GetBadgeColor() => IsCharging ? AppGreen : Percentage switch
    {
        >= 60 => AppGreen,
        >= 40 => BadgeBlue,
        >= 15 => BadgeAmber,
        _     => BadgeRed,
    };

    private static Color GetFillColor(double pct) => pct switch
    {
        >= 60 => AppGreen,
        >= 40 => BadgeBlue,
        >= 15 => BadgeAmber,
        _     => BadgeRed,
    };

    // ── Colors ───────────────────────────────────────────────────

    private static readonly Color AppGreen   = Color.FromRgb(44, 162, 60);
    private static readonly Color BadgeBlue  = Color.FromRgb(60, 130, 210);
    private static readonly Color BadgeAmber = Color.FromRgb(225, 168, 32);
    private static readonly Color BadgeRed   = Color.FromRgb(220, 55, 42);
    private static readonly SolidColorBrush BoltGoldBrush = new(Color.FromRgb(0xFF, 0xD7, 0x60));

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

    // ── Theme palettes ───────────────────────────────────────────

    private sealed record ThemePalette
    {
        // Glass body
        public required Color GlassTop, GlassUpper, GlassMid, GlassLower, GlassBottom;
        public required Color GlassEdgeTop, GlassEdgeMid, GlassEdgeBottom;
        public required byte DiffuseAlpha;
        // Inner well
        public required Color WellTop, WellMid, WellBottom;
        public required byte WellShadowAlpha;
        // Metallic terminal
        public required Color MetalTop, MetalHighlight, MetalUpper, MetalMid, MetalLower, MetalBottom;
        public required Color MetalEdge;
        // Badge
        public required byte BadgeShadowAlpha;
    }

    private static readonly ThemePalette DarkPalette = new()
    {
        // Glass: transparent-ish dark cylinder with subtle edges
        GlassTop    = Color.FromArgb(160, 75, 78, 88),
        GlassUpper  = Color.FromArgb(140, 58, 62, 72),
        GlassMid    = Color.FromArgb(130, 42, 46, 56),
        GlassLower  = Color.FromArgb(140, 50, 54, 64),
        GlassBottom = Color.FromArgb(155, 62, 66, 76),
        GlassEdgeTop    = Color.FromArgb(120, 140, 145, 160),
        GlassEdgeMid    = Color.FromArgb(80, 90, 95, 110),
        GlassEdgeBottom = Color.FromArgb(100, 110, 115, 130),
        DiffuseAlpha = 22,
        // Inner well: deep dark recess
        WellTop    = Color.FromRgb(14, 16, 24),
        WellMid    = Color.FromRgb(24, 27, 36),
        WellBottom = Color.FromRgb(20, 22, 30),
        WellShadowAlpha = 40,
        // Metal terminal: brushed steel
        MetalTop       = Color.FromRgb(155, 160, 172),
        MetalHighlight = Color.FromRgb(200, 205, 215),
        MetalUpper     = Color.FromRgb(170, 175, 185),
        MetalMid       = Color.FromRgb(130, 135, 145),
        MetalLower     = Color.FromRgb(110, 115, 125),
        MetalBottom    = Color.FromRgb(95, 100, 110),
        MetalEdge      = Color.FromArgb(120, 80, 85, 95),
        BadgeShadowAlpha = 45,
    };

    private static readonly ThemePalette LightPalette = new()
    {
        // Glass: bright translucent cylinder
        GlassTop    = Color.FromArgb(180, 235, 238, 244),
        GlassUpper  = Color.FromArgb(160, 220, 224, 232),
        GlassMid    = Color.FromArgb(145, 208, 212, 220),
        GlassLower  = Color.FromArgb(155, 215, 218, 226),
        GlassBottom = Color.FromArgb(170, 225, 228, 236),
        GlassEdgeTop    = Color.FromArgb(140, 195, 200, 210),
        GlassEdgeMid    = Color.FromArgb(100, 175, 180, 192),
        GlassEdgeBottom = Color.FromArgb(120, 185, 190, 200),
        DiffuseAlpha = 35,
        // Inner well: light recess
        WellTop    = Color.FromRgb(200, 204, 214),
        WellMid    = Color.FromRgb(218, 222, 230),
        WellBottom = Color.FromRgb(210, 214, 222),
        WellShadowAlpha = 20,
        // Metal terminal: polished chrome
        MetalTop       = Color.FromRgb(225, 228, 235),
        MetalHighlight = Color.FromRgb(248, 250, 255),
        MetalUpper     = Color.FromRgb(232, 235, 242),
        MetalMid       = Color.FromRgb(200, 204, 212),
        MetalLower     = Color.FromRgb(185, 190, 198),
        MetalBottom    = Color.FromRgb(172, 178, 186),
        MetalEdge      = Color.FromArgb(100, 160, 165, 178),
        BadgeShadowAlpha = 25,
    };
}
