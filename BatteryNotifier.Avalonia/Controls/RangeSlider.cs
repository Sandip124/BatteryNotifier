using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;

namespace BatteryNotifier.Avalonia.Controls;

/// <summary>
/// Dual-handle range slider for battery alert ranges.
/// Material-inspired with tick dots and enlarged thumb hit areas.
/// </summary>
public class RangeSlider : Control
{
    // ── Styled properties ──────────────────────────────────────

    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<RangeSlider, double>(nameof(Minimum), 0);

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<RangeSlider, double>(nameof(Maximum), 100);

    public static readonly StyledProperty<double> LowerValueProperty =
        AvaloniaProperty.Register<RangeSlider, double>(nameof(LowerValue), 0,
            defaultBindingMode: global::Avalonia.Data.BindingMode.TwoWay,
            coerce: CoerceLowerValue);

    public static readonly StyledProperty<double> UpperValueProperty =
        AvaloniaProperty.Register<RangeSlider, double>(nameof(UpperValue), 100,
            defaultBindingMode: global::Avalonia.Data.BindingMode.TwoWay,
            coerce: CoerceUpperValue);

    public static readonly StyledProperty<double> MinimumGapProperty =
        AvaloniaProperty.Register<RangeSlider, double>(nameof(MinimumGap), 5);

    static RangeSlider()
    {
        AffectsRender<RangeSlider>(MinimumProperty, MaximumProperty, LowerValueProperty, UpperValueProperty, MinimumGapProperty);
        FocusableProperty.OverrideDefaultValue<RangeSlider>(true);
    }

    private static double CoerceLowerValue(AvaloniaObject obj, double value)
    {
        var slider = (RangeSlider)obj;
        var max = slider.UpperValue - slider.MinimumGap;
        return Math.Clamp(value, slider.Minimum, Math.Max(slider.Minimum, max));
    }

    private static double CoerceUpperValue(AvaloniaObject obj, double value)
    {
        var slider = (RangeSlider)obj;
        var min = slider.LowerValue + slider.MinimumGap;
        return Math.Clamp(value, Math.Min(slider.Maximum, min), slider.Maximum);
    }

    public double Minimum { get => GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public double Maximum { get => GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
    public double LowerValue { get => GetValue(LowerValueProperty); set => SetValue(LowerValueProperty, value); }
    public double UpperValue { get => GetValue(UpperValueProperty); set => SetValue(UpperValueProperty, value); }
    public double MinimumGap { get => GetValue(MinimumGapProperty); set => SetValue(MinimumGapProperty, value); }

    // ── Dimensions ──────────────────────────────────────────────

    private const double TrackHeight = 10;
    private const double TrackCornerRadius = 5;
    private const double ThumbWidth = 5;
    private const double ThumbHeight = 22;
    private const double ThumbCornerRadius = 2.5;
    private const double ThumbGap = 5;
    private const double InsideCornerRadius = 2;
    private const double ControlHeight = 32;
    private const double EdgeInset = 4;
    private const double ThumbHitRadius = 18; // virtual hit area radius around each thumb
    private const double TickDotRadius = 1.8;
    private const int TickInterval = 10; // dots every 10%

    // ── Colors ──────────────────────────────────────────────────

    private sealed record Palette(Color ActiveTrack, Color InactiveTrack, Color Thumb, Color TickDot, Color TickDotActive);

    private Palette _palette = DarkPalette;

    private static readonly Palette DarkPalette = new(
        ActiveTrack: Color.Parse("#4CA6FF"),
        InactiveTrack: Color.Parse("#2A2A2A"),
        Thumb: Color.Parse("#4CA6FF"),
        TickDot: Color.Parse("#444444"),
        TickDotActive: Color.Parse("#2A7ACC"));

    private static readonly Palette LightPalette = new(
        ActiveTrack: Color.Parse("#2870BD"),
        InactiveTrack: Color.Parse("#D5D5D5"),
        Thumb: Color.Parse("#2870BD"),
        TickDot: Color.Parse("#BEBEBE"),
        TickDotActive: Color.Parse("#1A5A9E"));

    // ── Lifecycle ───────────────────────────────────────────────

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ActualThemeVariantChanged += OnThemeChanged;
        UpdatePalette();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ActualThemeVariantChanged -= OnThemeChanged;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnThemeChanged(object? sender, EventArgs e) => UpdatePalette();

    private void UpdatePalette()
    {
        var isDark = ActualThemeVariant == ThemeVariant.Dark ||
                     (ActualThemeVariant == ThemeVariant.Default && Application.Current?.ActualThemeVariant == ThemeVariant.Dark);
        _palette = isDark ? DarkPalette : LightPalette;
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var w = double.IsInfinity(availableSize.Width) ? 100 : availableSize.Width;
        return new Size(w, ControlHeight);
    }

    // ── Input handling ──────────────────────────────────────────

    private enum DragTarget { None, Lower, Upper }
    private DragTarget _dragTarget;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pos = e.GetPosition(this);
        _dragTarget = GetNearestThumb(pos.X, pos.Y);
        if (_dragTarget != DragTarget.None)
        {
            e.Pointer.Capture(this);
            UpdateFromPointer(pos.X);
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_dragTarget != DragTarget.None)
        {
            UpdateFromPointer(e.GetPosition(this).X);
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_dragTarget != DragTarget.None)
        {
            _dragTarget = DragTarget.None;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        var target = _dragTarget != DragTarget.None ? _dragTarget : DragTarget.Lower;
        switch (e.Key)
        {
            case Key.Left:
            case Key.Down:
                if (target == DragTarget.Lower) LowerValue = Math.Max(Minimum, LowerValue - 1);
                else UpperValue = Math.Max(LowerValue + MinimumGap, UpperValue - 1);
                e.Handled = true;
                break;
            case Key.Right:
            case Key.Up:
                if (target == DragTarget.Lower) LowerValue = Math.Min(UpperValue - MinimumGap, LowerValue + 1);
                else UpperValue = Math.Min(Maximum, UpperValue + 1);
                e.Handled = true;
                break;
        }
    }

    private DragTarget GetNearestThumb(double x, double y)
    {
        GetThumbRange(out var left, out var right);
        var range = Maximum - Minimum;
        if (range <= 0) return DragTarget.None;

        var centerY = Bounds.Height / 2;
        var lowerX = left + ((LowerValue - Minimum) / range) * (right - left);
        var upperX = left + ((UpperValue - Minimum) / range) * (right - left);

        // Distance from pointer to each thumb center (2D)
        var distLower = Math.Sqrt((x - lowerX) * (x - lowerX) + (y - centerY) * (y - centerY));
        var distUpper = Math.Sqrt((x - upperX) * (x - upperX) + (y - centerY) * (y - centerY));

        // Only register if within the virtual hit radius
        var minDist = Math.Min(distLower, distUpper);
        if (minDist > ThumbHitRadius * 2)
        {
            // Click is far from both thumbs — pick nearest by X only
            return Math.Abs(x - lowerX) <= Math.Abs(x - upperX) ? DragTarget.Lower : DragTarget.Upper;
        }

        return distLower <= distUpper ? DragTarget.Lower : DragTarget.Upper;
    }

    private void UpdateFromPointer(double x)
    {
        GetThumbRange(out var left, out var right);
        var thumbRange = right - left;
        if (thumbRange <= 0) return;

        var fraction = Math.Clamp((x - left) / thumbRange, 0, 1);
        var rawValue = Minimum + fraction * (Maximum - Minimum);
        rawValue = Math.Round(rawValue);

        if (_dragTarget == DragTarget.Lower)
            LowerValue = Math.Clamp(rawValue, Minimum, UpperValue - MinimumGap);
        else if (_dragTarget == DragTarget.Upper)
            UpperValue = Math.Clamp(rawValue, LowerValue + MinimumGap, Maximum);
    }

    private void GetThumbRange(out double left, out double right)
    {
        var trackLeft = ThumbWidth / 2 + ThumbGap;
        var trackRight = Bounds.Width - ThumbWidth / 2 - ThumbGap;
        left = trackLeft + EdgeInset;
        right = trackRight - EdgeInset;
    }

    // ── Rendering ───────────────────────────────────────────────

    public override void Render(DrawingContext context)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        var range = Maximum - Minimum;
        if (range <= 0) return;

        var lowerFrac = (LowerValue - Minimum) / range;
        var upperFrac = (UpperValue - Minimum) / range;
        var centerY = h / 2;

        var trackLeft = ThumbWidth / 2 + ThumbGap;
        var trackRight = w - ThumbWidth / 2 - ThumbGap;
        var trackTop = centerY - TrackHeight / 2;

        GetThumbRange(out var thumbRangeLeft, out var thumbRangeRight);
        var lowerX = thumbRangeLeft + lowerFrac * (thumbRangeRight - thumbRangeLeft);
        var upperX = thumbRangeLeft + upperFrac * (thumbRangeRight - thumbRangeLeft);

        // Left inactive segment
        var lowerGapLeft = lowerX - ThumbWidth / 2 - ThumbGap;
        if (lowerGapLeft > trackLeft)
        {
            var geo = CreateTrackSegment(trackLeft, trackTop, lowerGapLeft - trackLeft, TrackHeight,
                TrackCornerRadius, InsideCornerRadius);
            context.DrawGeometry(new SolidColorBrush(_palette.InactiveTrack), null, geo);
        }

        // Active middle segment
        var activeLeft = lowerX + ThumbWidth / 2 + ThumbGap;
        var activeRight = upperX - ThumbWidth / 2 - ThumbGap;
        if (activeRight > activeLeft)
        {
            var geo = CreateTrackSegment(activeLeft, trackTop, activeRight - activeLeft, TrackHeight,
                InsideCornerRadius, InsideCornerRadius);
            context.DrawGeometry(new SolidColorBrush(_palette.ActiveTrack), null, geo);
        }

        // Right inactive segment
        var upperGapRight = upperX + ThumbWidth / 2 + ThumbGap;
        if (upperGapRight < trackRight)
        {
            var geo = CreateTrackSegment(upperGapRight, trackTop, trackRight - upperGapRight, TrackHeight,
                InsideCornerRadius, TrackCornerRadius);
            context.DrawGeometry(new SolidColorBrush(_palette.InactiveTrack), null, geo);
        }

        // Tick dots
        DrawTickDots(context, thumbRangeLeft, thumbRangeRight, centerY, lowerFrac, upperFrac);

        // Thumbs
        DrawThumb(context, lowerX, centerY);
        DrawThumb(context, upperX, centerY);
    }

    private void DrawTickDots(DrawingContext context, double rangeLeft, double rangeRight,
        double centerY, double lowerFrac, double upperFrac)
    {
        var range = Maximum - Minimum;
        if (range <= 0) return;

        var inactiveBrush = new SolidColorBrush(_palette.TickDot);
        var activeBrush = new SolidColorBrush(_palette.TickDotActive);

        for (var val = Minimum + TickInterval; val < Maximum; val += TickInterval)
        {
            var frac = (val - Minimum) / range;
            var x = rangeLeft + frac * (rangeRight - rangeLeft);

            // Skip dots that overlap with thumbs
            var lowerX = rangeLeft + lowerFrac * (rangeRight - rangeLeft);
            var upperX = rangeLeft + upperFrac * (rangeRight - rangeLeft);
            if (Math.Abs(x - lowerX) < ThumbWidth + ThumbGap) continue;
            if (Math.Abs(x - upperX) < ThumbWidth + ThumbGap) continue;

            var isActive = frac >= lowerFrac && frac <= upperFrac;
            context.DrawEllipse(isActive ? activeBrush : inactiveBrush, null,
                new Point(x, centerY), TickDotRadius, TickDotRadius);
        }
    }

    private void DrawThumb(DrawingContext context, double x, double centerY)
    {
        var rect = new Rect(x - ThumbWidth / 2, centerY - ThumbHeight / 2, ThumbWidth, ThumbHeight);
        context.DrawRectangle(new SolidColorBrush(_palette.Thumb), null, rect, ThumbCornerRadius, ThumbCornerRadius);
    }

    private static StreamGeometry CreateTrackSegment(
        double x, double y, double width, double height,
        double leftRadius, double rightRadius)
    {
        var geo = new StreamGeometry();
        using var ctx = geo.Open();

        var lr = Math.Min(leftRadius, height / 2);
        var rr = Math.Min(rightRadius, height / 2);

        ctx.BeginFigure(new Point(x + lr, y), true);
        ctx.LineTo(new Point(x + width - rr, y));
        if (rr > 0) ctx.ArcTo(new Point(x + width, y + rr), new Size(rr, rr), 0, false, SweepDirection.Clockwise);
        ctx.LineTo(new Point(x + width, y + height - rr));
        if (rr > 0) ctx.ArcTo(new Point(x + width - rr, y + height), new Size(rr, rr), 0, false, SweepDirection.Clockwise);
        ctx.LineTo(new Point(x + lr, y + height));
        if (lr > 0) ctx.ArcTo(new Point(x, y + height - lr), new Size(lr, lr), 0, false, SweepDirection.Clockwise);
        ctx.LineTo(new Point(x, y + lr));
        if (lr > 0) ctx.ArcTo(new Point(x + lr, y), new Size(lr, lr), 0, false, SweepDirection.Clockwise);
        ctx.EndFigure(true);

        return geo;
    }
}
