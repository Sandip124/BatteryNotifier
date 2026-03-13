using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Controls;
using Avalonia.Threading;

namespace BatteryNotifier.Avalonia.Controls;

/// <summary>
/// Custom-rendered battery indicator with glossy 3D appearance.
/// Theme-aware with sine-curve capsule geometry, ambient glow, glass reflections,
/// and status badge overlay.
/// </summary>
public class BatteryIndicatorControl : Control
{
    // ── Styled properties ────────────────────────────────────────

    public static readonly StyledProperty<double> PercentageProperty =
        AvaloniaProperty.Register<BatteryIndicatorControl, double>(nameof(Percentage), 50);

    public static readonly StyledProperty<bool> IsChargingProperty =
        AvaloniaProperty.Register<BatteryIndicatorControl, bool>(nameof(IsCharging));

    // Pulse animation state
    private DispatcherTimer? _pulseTimer;
    private double _pulsePhase;
    private IDisposable? _visibilitySub;

    static BatteryIndicatorControl()
    {
        AffectsRender<BatteryIndicatorControl>(PercentageProperty, IsChargingProperty);
        IsChargingProperty.Changed.AddClassHandler<BatteryIndicatorControl>(
            (c, _) => c.UpdatePulseAnimation());
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

        // Track parent window visibility — pauses pulse when window is hidden
        if (VisualRoot is Window window)
        {
            _visibilitySub = window.GetObservable(Window.IsVisibleProperty)
                .Subscribe(_ => UpdatePulseAnimation());
        }

        UpdatePulseAnimation();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ActualThemeVariantChanged -= OnThemeChanged;
        _visibilitySub?.Dispose();
        _visibilitySub = null;
        StopPulseAnimation();
        base.OnDetachedFromVisualTree(e);
    }

    private void OnThemeChanged(object? sender, EventArgs e) => InvalidateVisual();

    private void UpdatePulseAnimation()
    {
        var isVisible = VisualRoot is Window { IsVisible: true };
        if (IsCharging && isVisible)
            StartPulseAnimation();
        else
            StopPulseAnimation();
    }

    private void StartPulseAnimation()
    {
        if (_pulseTimer != null) return;
        _pulseTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) }; // ~25 fps
        _pulseTimer.Tick += OnPulseTick;
        _pulseTimer.Start();
    }

    private void StopPulseAnimation()
    {
        if (_pulseTimer == null) return;
        _pulseTimer.Tick -= OnPulseTick;
        _pulseTimer.Stop();
        _pulseTimer = null;
        _pulsePhase = 0;
        InvalidateVisual();
    }

    private void OnPulseTick(object? sender, EventArgs e)
    {
        _pulsePhase += 0.04; // ~2.5 second full cycle
        if (_pulsePhase > Math.PI * 2) _pulsePhase -= Math.PI * 2;
        InvalidateVisual();
    }

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
        var termW = w * 0.045;
        var termGap = w * 0.006;
        var bodyW = w - termW - termGap;
        var bulge = batteryH * 0.06;
        var inset = Math.Max(2, batteryH * 0.065);

        var bodyRect = new Rect(bulge, 0, bodyW - bulge * 2, batteryH);
        var bodyGeo = CreateSineCapsule(bodyRect, bulge);
        var innerRect = bodyRect.Deflate(inset);
        var innerBulge = bulge * 0.85;
        var innerGeo = CreateSineCapsule(innerRect, innerBulge);

        // Draw layers back-to-front
        DrawAmbientGlow(ctx, w, h);
        DrawBodyShell(ctx, bodyGeo, bodyRect, p);
        DrawInnerWell(ctx, innerGeo, innerRect, p);
        DrawFill(ctx, innerRect, innerGeo, innerBulge, pct);
        DrawGlassReflection(ctx, innerRect, innerGeo);
        DrawTerminal(ctx, bodyW + termGap, batteryH, termW, p);
        DrawStatusBadge(ctx, bodyRect, badgeRadius, p);
    }

    // ── Geometry: Sine-curve capsule ─────────────────────────────
    //
    // Barrel/fisheye shape: left and right edges bow outward following
    // sin(π·t). Corners use cubic Bezier curves for smooth transitions.

    private static StreamGeometry CreateSineCapsule(Rect rect, double bulge)
        => CreateSineCapsuleAsymmetric(rect, bulge, bulge);

    private static StreamGeometry CreateSineCapsuleAsymmetric(Rect rect,
        double leftBulge, double rightBulge, double cornerFactor = 0.12)
    {
        var geo = new StreamGeometry();
        using var c = geo.Open();

        const int segments = 24;
        const int skip = 2;
        double tSkip = (double)skip / segments;

        var x = rect.X;
        var y = rect.Y;
        var w = rect.Width;
        var h = rect.Height;
        var cr = Math.Min(h * cornerFactor, w * 0.3);

        // Start at top-left, go clockwise
        c.BeginFigure(new Point(x, y + cr), true);

        // Top-left corner
        c.CubicBezierTo(new Point(x, y), new Point(x, y), new Point(x + cr, y));

        // Top edge
        c.LineTo(new Point(x + w - cr, y));

        // Top-right corner → right sine curve
        double trT = tSkip;
        c.CubicBezierTo(
            new Point(x + w, y),
            new Point(x + w, y + cr * 0.4),
            new Point(x + w + rightBulge * Math.Sin(Math.PI * trT), y + trT * h));

        // Right edge (sine bulge)
        for (int i = skip + 1; i <= segments - skip; i++)
        {
            double t = (double)i / segments;
            c.LineTo(new Point(x + w + rightBulge * Math.Sin(Math.PI * t), y + t * h));
        }

        // Bottom-right corner
        double brT = 1.0 - tSkip;
        c.LineTo(new Point(x + w + rightBulge * Math.Sin(Math.PI * brT), y + brT * h));
        c.CubicBezierTo(
            new Point(x + w, y + h - cr * 0.4),
            new Point(x + w, y + h),
            new Point(x + w - cr, y + h));

        // Bottom edge
        c.LineTo(new Point(x + cr, y + h));

        // Bottom-left corner → left sine curve
        double blT = tSkip;
        c.CubicBezierTo(
            new Point(x, y + h),
            new Point(x, y + h - cr * 0.4),
            new Point(x - leftBulge * Math.Sin(Math.PI * blT), y + h - blT * h));

        // Left edge (sine bulge)
        for (int i = skip + 1; i <= segments - skip; i++)
        {
            double t = (double)i / segments;
            c.LineTo(new Point(x - leftBulge * Math.Sin(Math.PI * t), y + h - t * h));
        }

        // Close: left sine curve → top-left start
        double tlT = 1.0 - tSkip;
        c.LineTo(new Point(x - leftBulge * Math.Sin(Math.PI * tlT), y + tSkip * h));
        c.CubicBezierTo(
            new Point(x, y + cr * 0.4),
            new Point(x, y),
            new Point(x, y + cr));

        c.EndFigure(true);
        return geo;
    }

    // ── Layer 0: Ambient glow ────────────────────────────────────

    private void DrawAmbientGlow(DrawingContext ctx, double w, double h)
    {
        // Charging: always green (full battery shade) with pulse animation
        // Discharging: color based on current battery percentage, static
        var color = IsCharging ? AppGreen : GetFillColor(Percentage);
        var baseAlpha = IsDark ? 25.0 : 16.0;

        var alpha = (byte)(IsCharging
            ? baseAlpha * (1.0 + 0.5 * Math.Sin(_pulsePhase))
            : baseAlpha);

        var cx = w * 0.47;
        var cy = h * 0.5;
        var radiusScale = IsCharging ? 2.0 + 0.15 * Math.Sin(_pulsePhase) : 2.0;
        var radius = Math.Max(w, h) * radiusScale;

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

    // ── Layer 1: Metallic shell ──────────────────────────────────

    private static void DrawBodyShell(DrawingContext ctx, Geometry geo, Rect rect, ThemePalette p)
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
        ctx.DrawGeometry(shell, null, geo);

        // Subtle top-edge highlight
        using (ctx.PushGeometryClip(geo))
        {
            ctx.DrawRectangle(
                new SolidColorBrush(Color.FromArgb(p.HighlightAlpha, 255, 255, 255)), null,
                new Rect(rect.X + rect.Width * 0.08, rect.Y + 1, rect.Width * 0.84, 1.2));
        }
    }

    // ── Layer 2: Inner well ──────────────────────────────────────

    private static void DrawInnerWell(DrawingContext ctx, Geometry geo, Rect rect, ThemePalette p)
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
        ctx.DrawGeometry(bg, null, geo);

        using (ctx.PushGeometryClip(geo))
        {
            ctx.DrawRectangle(
                new SolidColorBrush(Color.FromArgb(p.WellShadowAlpha, 0, 0, 0)), null,
                new Rect(rect.X, rect.Y, rect.Width, rect.Height * 0.12));
        }
    }

    // ── Layer 3: Colored fill bar ────────────────────────────────

    private void DrawFill(DrawingContext ctx, Rect inner, Geometry clipGeo,
        double innerBulge, double pct)
    {
        var color = GetFillColor(Percentage);

        // Background tint across the entire well
        ctx.DrawGeometry(new SolidColorBrush(Color.FromArgb(30, color.R, color.G, color.B)),
            null, clipGeo);

        var fillW = inner.Width * pct;
        if (fillW < 1) return;

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

        // Fill capsule: left always bulges, right lerps from inward to outward
        var fillRect = new Rect(inner.X, inner.Y, fillW, inner.Height);
        var fillBulge = innerBulge * 1.4;
        var leftBulge = fillBulge;
        var rightBulge = fillBulge * (2 * pct - 1);
        var fillGeo = CreateSineCapsuleAsymmetric(fillRect, leftBulge, rightBulge, 0.05);

        using (ctx.PushGeometryClip(clipGeo))
        {
            ctx.DrawGeometry(fillBrush, null, fillGeo);
            DrawFillInnerGlow(ctx, fillGeo, fillRect, inner, leftBulge, rightBulge);
        }
    }

    private static void DrawFillInnerGlow(DrawingContext ctx, Geometry fillGeo,
        Rect fillRect, Rect inner, double leftBulge, double rightBulge)
    {
        // Top highlight
        var topH = Math.Max(1.5, inner.Height * 0.12);
        var topGeo = CreateSineCapsuleAsymmetric(
            new Rect(fillRect.X, fillRect.Y, fillRect.Width, topH),
            leftBulge, rightBulge * (topH / inner.Height), 0.05);
        using (ctx.PushGeometryClip(fillGeo))
        {
            ctx.DrawGeometry(new LinearGradientBrush
            {
                StartPoint = RelStart, EndPoint = RelEnd,
                GradientStops = new GradientStops
                {
                    new(Color.FromArgb(80, 255, 255, 255), 0.0),
                    new(Color.FromArgb(0, 255, 255, 255), 1.0),
                }
            }, null, topGeo);
        }

        // Bottom shadow
        var botH = Math.Max(1.5, inner.Height * 0.10);
        var botGeo = CreateSineCapsuleAsymmetric(
            new Rect(fillRect.X, fillRect.Bottom - botH, fillRect.Width, botH),
            leftBulge, rightBulge * (botH / inner.Height), 0.05);
        using (ctx.PushGeometryClip(fillGeo))
        {
            ctx.DrawGeometry(new LinearGradientBrush
            {
                StartPoint = RelStart, EndPoint = RelEnd,
                GradientStops = new GradientStops
                {
                    new(Color.FromArgb(0, 0, 0, 0), 0.0),
                    new(Color.FromArgb(50, 0, 0, 0), 1.0),
                }
            }, null, botGeo);
        }

        // Left edge glow
        var leftW = Math.Max(1.5, fillRect.Width * 0.08);
        var leftGeo = CreateSineCapsuleAsymmetric(
            new Rect(fillRect.X, fillRect.Y, leftW, fillRect.Height),
            leftBulge, 0, 0.05);
        using (ctx.PushGeometryClip(fillGeo))
        {
            ctx.DrawGeometry(new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new(Color.FromArgb(60, 255, 255, 255), 0.0),
                    new(Color.FromArgb(0, 255, 255, 255), 1.0),
                }
            }, null, leftGeo);
        }
    }

    // ── Layer 4: Glass reflection beams ──────────────────────────

    private static void DrawGlassReflection(DrawingContext ctx, Rect inner, Geometry clipGeo)
    {
        using var _ = ctx.PushGeometryClip(clipGeo);
        var insetX = inner.Width * 0.06;

        // Top beam: bright specular highlight
        DrawBeam(ctx,
            new Rect(inner.X + insetX, inner.Y + inner.Height * 0.12,
                     inner.Width - insetX * 2, Math.Max(1.5, inner.Height * 0.40)),
            90, 110);

        // Bottom shadow beam
        var bottomY = inner.Y + inner.Height * 0.74;
        var bottomH = Math.Max(2.5, inner.Height * 0.12);
        DrawShadowBeam(ctx,
            new Rect(inner.X + insetX * 1.5, bottomY,
                     inner.Width - insetX * 3, bottomH),
            18, 28);

        // Thin accent shadow beam
        DrawShadowBeam(ctx,
            new Rect(inner.X + insetX * 2, bottomY + bottomH + inner.Height * 0.03,
                     inner.Width - insetX * 4, Math.Max(1.0, inner.Height * 0.04)),
            12, 20);

        // Soft top glow
        ctx.DrawRectangle(new LinearGradientBrush
        {
            StartPoint = RelStart, EndPoint = RelEnd,
            GradientStops = new GradientStops
            {
                new(Color.FromArgb(30, 255, 255, 255), 0.0),
                new(Color.FromArgb(8, 255, 255, 255), 0.6),
                new(Color.FromArgb(0, 255, 255, 255), 1.0),
            }
        }, null, new Rect(inner.X, inner.Y, inner.Width, inner.Height * 0.30));
    }

    private static void DrawBeam(DrawingContext ctx, Rect rect, byte edgeAlpha, byte centerAlpha)
    {
        ctx.DrawRectangle(new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new(Color.FromArgb(0, 255, 255, 255), 0.0),
                new(Color.FromArgb(edgeAlpha, 255, 255, 255), 0.15),
                new(Color.FromArgb(centerAlpha, 255, 255, 255), 0.5),
                new(Color.FromArgb(edgeAlpha, 255, 255, 255), 0.85),
                new(Color.FromArgb(0, 255, 255, 255), 1.0),
            }
        }, null, rect);
    }

    private static void DrawShadowBeam(DrawingContext ctx, Rect rect, byte edgeAlpha, byte centerAlpha)
    {
        ctx.DrawRectangle(new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new(Color.FromArgb(0, 0, 0, 0), 0.0),
                new(Color.FromArgb(edgeAlpha, 0, 0, 0), 0.2),
                new(Color.FromArgb(centerAlpha, 0, 0, 0), 0.5),
                new(Color.FromArgb(edgeAlpha, 0, 0, 0), 0.8),
                new(Color.FromArgb(0, 0, 0, 0), 1.0),
            }
        }, null, rect);
    }

    // ── Layer 5: Terminal cap ────────────────────────────────────

    private static void DrawTerminal(DrawingContext ctx, double x, double bodyH,
        double termW, ThemePalette p)
    {
        var termH = bodyH * 0.34;
        var termY = (bodyH - termH) / 2;
        var termRect = new Rect(x, termY, termW, termH);
        var termGeo = CreateSineCapsuleAsymmetric(termRect, 0, termW * 0.2);

        ctx.DrawGeometry(new LinearGradientBrush
        {
            StartPoint = RelStart, EndPoint = RelEnd,
            GradientStops = new GradientStops
            {
                new(p.TermTop, 0.0),
                new(p.TermMid, 0.45),
                new(p.TermBottom, 1.0),
            }
        }, null, termGeo);
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
            DrawBoltIcon(ctx, cx, cy, r);
        else
            DrawCheckmarkIcon(ctx, cx, cy, r);
    }

    private void DrawTriangleBadge(DrawingContext ctx, double cx, double cy, double r, Color color)
    {
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)), 1.5)
            { LineJoin = PenLineJoin.Round };
        DrawTriangle(ctx, cx, cy, r, new SolidColorBrush(color), pen);

        if (IsCharging)
            DrawBoltIcon(ctx, cx, cy, r);
        else
            DrawExclamationIcon(ctx, cx, cy, r);
    }

    // ── Badge icons ──────────────────────────────────────────────

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
        ctx.DrawGeometry(null,
            new Pen(Brushes.White, r * 0.18) { LineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round },
            geo);
    }

    private static void DrawExclamationIcon(DrawingContext ctx, double cx, double cy, double r)
    {
        var strokeW = r * 0.16;
        var lineH = r * 0.50;
        var lineTop = cy - lineH * 0.45;

        var geo = new StreamGeometry();
        using (var c = geo.Open())
        {
            c.BeginFigure(new Point(cx, lineTop), false);
            c.LineTo(new Point(cx, lineTop + lineH));
        }
        ctx.DrawGeometry(null, new Pen(Brushes.White, strokeW * 2) { LineCap = PenLineCap.Round }, geo);

        var dotR = strokeW * 1.1;
        ctx.DrawEllipse(Brushes.White, null, new Point(cx, lineTop + lineH + dotR * 2.8), dotR, dotR);
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

    private static readonly Color AppGreen   = Color.FromRgb(59, 175, 74);
    private static readonly Color BadgeBlue  = Color.FromRgb(74, 144, 226);
    private static readonly Color BadgeAmber = Color.FromRgb(240, 180, 41);
    private static readonly Color BadgeRed   = Color.FromRgb(234, 67, 53);

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

    private record ThemePalette
    {
        public required Color ShellTop, ShellUpper, ShellMid, ShellLower, ShellBottom;
        public required byte HighlightAlpha;
        public required Color WellTop, WellMid, WellBottom;
        public required byte WellShadowAlpha;
        public required Color TermTop, TermMid, TermBottom;
        public required byte BadgeShadowAlpha;
    }

    private static readonly ThemePalette DarkPalette = new()
    {
        ShellTop    = Color.FromRgb(220, 222, 228),
        ShellUpper  = Color.FromRgb(192, 194, 202),
        ShellMid    = Color.FromRgb(168, 170, 178),
        ShellLower  = Color.FromRgb(148, 150, 158),
        ShellBottom = Color.FromRgb(132, 134, 142),
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
}
