using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace BatteryNotifier.Avalonia.Controls;

/// <summary>
/// Renders a 24h battery charge sparkline with filled area, color-coded
/// charging (blue) vs discharging (green/amber), and time labels.
/// </summary>
internal class SparklineControl : Control
{
    // Segment colors: Charging = blue, Discharging = green
    private static readonly Color ChargingDark = Color.FromRgb(66, 165, 245);
    private static readonly Color ChargingLight = Color.FromRgb(30, 136, 229);
    private static readonly Color DischargingDark = Color.FromRgb(102, 187, 106);
    private static readonly Color DischargingLight = Color.FromRgb(56, 142, 60);

    // Label colors
    private static readonly SolidColorBrush LabelBrushDark = new(Color.FromArgb(120, 255, 255, 255));
    private static readonly SolidColorBrush LabelBrushLight = new(Color.FromArgb(120, 0, 0, 0));
    private static readonly SolidColorBrush AxisBrushDark = new(Color.FromArgb(100, 255, 255, 255));
    private static readonly SolidColorBrush AxisBrushLight = new(Color.FromArgb(100, 0, 0, 0));

    // Guide line colors
    private static readonly Color GuideLineDark = Color.FromArgb(30, 255, 255, 255);
    private static readonly Color GuideLineLight = Color.FromArgb(25, 0, 0, 0);

    public static readonly StyledProperty<IReadOnlyList<Core.Models.ChargeHistoryEntry>?> DataPointsProperty =
        AvaloniaProperty.Register<SparklineControl, IReadOnlyList<Core.Models.ChargeHistoryEntry>?>(nameof(DataPoints));

    static SparklineControl()
    {
        AffectsRender<SparklineControl>(DataPointsProperty);
    }

    public IReadOnlyList<Core.Models.ChargeHistoryEntry>? DataPoints
    {
        get => GetValue(DataPointsProperty);
        set => SetValue(DataPointsProperty, value);
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

    private bool IsDark => ActualThemeVariant == ThemeVariant.Dark;

    /// <summary>Bundles chart layout dimensions and time range for passing between draw methods.</summary>
    private readonly record struct ChartArea(double X, double Y, double W, double H, long MinTime, long MaxTime)
    {
        public double Baseline => Y + H;
        public double TimeRange => MaxTime - MinTime;
        public double ToX(long ts) => X + (ts - MinTime) / TimeRange * W;
        public double ToY(byte pct) => Y + H * (1.0 - pct / 100.0);
    }

    public override void Render(DrawingContext context)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        var points = DataPoints;
        if (w <= 0 || h <= 0 || points is not { Count: >= 2 })
            return;

        var isDark = IsDark;
        var padding = new Thickness(30, 8, 8, 18); // left for Y labels, bottom for X labels
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var area = new ChartArea(
            padding.Left, padding.Top,
            w - padding.Left - padding.Right,
            h - padding.Top - padding.Bottom,
            now - 86400, now);
        if (area.W <= 0 || area.H <= 0) return;

        // Filter to 24h window
        var visible = new List<Core.Models.ChargeHistoryEntry>();
        foreach (var p in points)
        {
            if (p.TimestampUnixSeconds >= area.MinTime)
                visible.Add(p);
        }
        if (visible.Count < 2) return;

        DrawGuideLines(context, area, isDark);
        DrawAreaChart(context, visible, area, isDark);
        DrawTimeLabels(context, area, isDark);
        DrawYLabels(context, area, isDark);
    }

    private static void DrawGuideLines(DrawingContext ctx, ChartArea area, bool isDark)
    {
        var lineColor = isDark ? GuideLineDark : GuideLineLight;
        var pen = new Pen(new SolidColorBrush(lineColor), 0.5, new DashStyle([4, 3], 0));

        foreach (var pct in new[] { 25, 50, 75 })
        {
            var ly = area.Y + area.H * (1.0 - pct / 100.0);
            ctx.DrawLine(pen, new Point(area.X, ly), new Point(area.X + area.W, ly));
        }
    }

    private static void DrawAreaChart(DrawingContext ctx,
        List<Core.Models.ChargeHistoryEntry> points, ChartArea area, bool isDark)
    {
        if (area.TimeRange <= 0) return;

        var segments = BuildSegments(points, area);

        foreach (var (seg, charging) in segments)
        {
            var color = GetSegmentColor(charging, isDark);
            DrawSegmentLine(ctx, seg, color);
            DrawSegmentFill(ctx, seg, area.Baseline, color, isDark);
        }
    }

    /// <summary>
    /// Splits points into contiguous segments, breaking on time gaps > 5min
    /// or charging state changes.
    /// </summary>
    private static List<(List<(double x, double y)> pts, bool charging)> BuildSegments(
        List<Core.Models.ChargeHistoryEntry> points, ChartArea area)
    {
        var segments = new List<(List<(double x, double y)>, bool)>();
        var currentSeg = new List<(double x, double y)>();
        var currentCharging = points[0].IsCharging;
        long prevTs = points[0].TimestampUnixSeconds;

        currentSeg.Add((area.ToX(points[0].TimestampUnixSeconds), area.ToY(points[0].Percent)));

        for (int i = 1; i < points.Count; i++)
        {
            var p = points[i];
            var gap = p.TimestampUnixSeconds - prevTs;

            if (gap > 300 || p.IsCharging != currentCharging)
            {
                if (currentSeg.Count >= 2)
                    segments.Add((currentSeg, currentCharging));
                currentSeg = [];
                currentCharging = p.IsCharging;
            }

            currentSeg.Add((area.ToX(p.TimestampUnixSeconds), area.ToY(p.Percent)));
            prevTs = p.TimestampUnixSeconds;
        }
        if (currentSeg.Count >= 2)
            segments.Add((currentSeg, currentCharging));

        return segments;
    }

    private static Color GetSegmentColor(bool charging, bool isDark)
    {
        if (charging)
            return isDark ? ChargingDark : ChargingLight;

        return isDark ? DischargingDark : DischargingLight;
    }

    private static void DrawSegmentLine(DrawingContext ctx,
        List<(double x, double y)> seg, Color color)
    {
        var geo = new StreamGeometry();
        using (var c = geo.Open())
        {
            c.BeginFigure(new Point(seg[0].x, seg[0].y), false);
            for (int i = 1; i < seg.Count; i++)
                c.LineTo(new Point(seg[i].x, seg[i].y));
            c.EndFigure(false);
        }
        ctx.DrawGeometry(null, new Pen(new SolidColorBrush(color), 1.5), geo);
    }

    private static void DrawSegmentFill(DrawingContext ctx,
        List<(double x, double y)> seg, double baseline, Color color, bool isDark)
    {
        var geo = new StreamGeometry();
        using (var c = geo.Open())
        {
            c.BeginFigure(new Point(seg[0].x, baseline), true);
            foreach (var (px, py) in seg)
                c.LineTo(new Point(px, py));
            c.LineTo(new Point(seg[^1].x, baseline));
            c.EndFigure(true);
        }

        var fillTop = Color.FromArgb(isDark ? (byte)50 : (byte)40, color.R, color.G, color.B);
        var fillBot = Color.FromArgb(5, color.R, color.G, color.B);
        ctx.DrawGeometry(new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops = new GradientStops { new(fillTop, 0.0), new(fillBot, 1.0) }
        }, null, geo);
    }

    private static void DrawTimeLabels(DrawingContext ctx, ChartArea area, bool isDark)
    {
        var textColor = isDark ? LabelBrushDark : LabelBrushLight;

        // (label, x-fraction, text-anchor: 0=left, 0.5=center, 1=right)
        var labels = new[] { ("24h ago", 0.0, 0.0), ("12h ago", 0.5, 0.5), ("Now", 1.0, 1.0) };
        var typeface = Typeface.Default;

        foreach (var (text, frac, anchor) in labels)
        {
            var ft = new FormattedText(text, System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, typeface, 9, textColor);
            var lx = area.X + area.W * frac - ft.Width * anchor;
            ctx.DrawText(ft, new Point(lx, area.Y + area.H + 4));
        }
    }

    private static void DrawYLabels(DrawingContext ctx, ChartArea area, bool isDark)
    {
        var textColor = isDark ? AxisBrushDark : AxisBrushLight;
        var typeface = Typeface.Default;

        foreach (var pct in new[] { 0, 25, 50, 75, 100 })
        {
            var text = $"{pct}%";
            var ft = new FormattedText(text, System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, typeface, 8, textColor);
            var ly = area.Y + area.H * (1.0 - pct / 100.0) - ft.Height / 2;
            ctx.DrawText(ft, new Point(area.X - ft.Width - 4, ly));
        }
    }
}
