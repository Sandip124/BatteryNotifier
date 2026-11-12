using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;

namespace BatteryNotifier.Avalonia.Controls;

/// <summary>
/// Slider with tick dots and enlarged thumb hit areas (Material-inspired).
/// Dual-handle range by default (battery alert ranges); set <see cref="IsRange"/> = false for a
/// single-thumb slider driven by <see cref="Value"/> (e.g. alert volume).
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

    /// <summary>When false, renders a single-thumb slider driven by <see cref="Value"/>.</summary>
    public static readonly StyledProperty<bool> IsRangeProperty =
        AvaloniaProperty.Register<RangeSlider, bool>(nameof(IsRange), true);

    /// <summary>Single-thumb value (used when <see cref="IsRange"/> is false).</summary>
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<RangeSlider, double>(nameof(Value), 0,
            defaultBindingMode: global::Avalonia.Data.BindingMode.TwoWay,
            coerce: CoerceValue);

    static RangeSlider()
    {
        AffectsRender<RangeSlider>(MinimumProperty, MaximumProperty, LowerValueProperty, UpperValueProperty,
            MinimumGapProperty, IsRangeProperty, ValueProperty);
        FocusableProperty.OverrideDefaultValue<RangeSlider>(true);
    }

    private static double CoerceValue(AvaloniaObject obj, double value)
    {
        var slider = (RangeSlider)obj;
        return Math.Clamp(value, slider.Minimum, slider.Maximum);
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
    public bool IsRange { get => GetValue(IsRangeProperty); set => SetValue(IsRangeProperty, value); }
    public double Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    // ── Dimensions ──────────────────────────────────────────────

    private const double TrackHeight = 10;
    private const double TrackCornerRadius = 5;
    private const double ThumbWidth = 5;
    private const double ThumbHeight = 22;
    private const double ThumbCornerRadius = 2.5;
    private const double ThumbPressShrink = 4; // height reduction while a thumb is held
    private const double ThumbGap = 5;
    private const double ThumbEdgeGap = ThumbWidth / 2 + ThumbGap; // clearance between a thumb and adjacent track
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
        var basePalette = isDark ? DarkPalette : LightPalette;

        // Use the OS system accent color (follows live changes on all platforms)
        if (this.TryFindResource(isDark ? "SystemAccentColorLight1" : "SystemAccentColorDark1", ActualThemeVariant, out var colorRes) &&
            colorRes is Color accent)
        {
            var accentDarker = Color.FromArgb(255,
                (byte)Math.Max(accent.R - 30, 0),
                (byte)Math.Max(accent.G - 30, 0),
                (byte)Math.Max(accent.B - 30, 0));
            _palette = basePalette with
            {
                ActiveTrack = accent,
                Thumb = accent,
                TickDotActive = accentDarker
            };
        }
        else
        {
            _palette = basePalette;
        }

        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var w = double.IsInfinity(availableSize.Width) ? 100 : availableSize.Width;
        return new Size(w, ControlHeight);
    }

    // ── Input handling ──────────────────────────────────────────

    private enum DragTarget { None, Lower, Upper, Single }
    private DragTarget _dragTarget;
    private static readonly Cursor HandCursor = new(StandardCursorType.Hand);

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pos = e.GetPosition(this);
        _dragTarget = IsRange ? GetNearestThumb(pos.X, pos.Y) : DragTarget.Single;
        if (_dragTarget != DragTarget.None)
        {
            e.Pointer.Capture(this);
            UpdateFromPointer(pos.X);
            InvalidateVisual(); // show the "held" thumb cue
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var pos = e.GetPosition(this);

        if (_dragTarget != DragTarget.None)
        {
            UpdateFromPointer(pos.X);
            e.Handled = true;
            return;
        }

        Cursor = GetHoveredThumb(pos.X, pos.Y) != DragTarget.None ? HandCursor : null;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_dragTarget != DragTarget.None)
        {
            _dragTarget = DragTarget.None;
            e.Pointer.Capture(null);
            InvalidateVisual(); // restore full-height thumb
            e.Handled = true;
        }
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        Cursor = null;
    }

    /// <summary>Nearest thumb within the hit radius of the pointer, else None (for the hover cursor).</summary>
    private DragTarget GetHoveredThumb(double x, double y)
    {
        GetThumbRange(out var left, out var right);
        var range = Maximum - Minimum;
        if (range <= 0) return DragTarget.None;

        var centerY = Bounds.Height / 2;
        double Dist(double val)
        {
            var tx = Lerp((val - Minimum) / range, left, right);
            return Math.Sqrt((x - tx) * (x - tx) + (y - centerY) * (y - centerY));
        }

        if (!IsRange)
            return Dist(Value) <= ThumbHitRadius ? DragTarget.Single : DragTarget.None;

        var dl = Dist(LowerValue);
        var du = Dist(UpperValue);
        if (Math.Min(dl, du) > ThumbHitRadius) return DragTarget.None;
        return dl <= du ? DragTarget.Lower : DragTarget.Upper;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        var step = e.Key is Key.Left or Key.Down ? -1
            : e.Key is Key.Right or Key.Up ? 1
            : 0;
        if (step == 0) return;

        if (!IsRange)
        {
            Value = Math.Clamp(Value + step, Minimum, Maximum);
            e.Handled = true;
            return;
        }

        var target = _dragTarget != DragTarget.None ? _dragTarget : DragTarget.Lower;
        if (target == DragTarget.Lower)
            LowerValue = Math.Clamp(LowerValue + step, Minimum, UpperValue - MinimumGap);
        else
            UpperValue = Math.Clamp(UpperValue + step, LowerValue + MinimumGap, Maximum);
        e.Handled = true;
    }

    private DragTarget GetNearestThumb(double x, double y)
    {
        GetThumbRange(out var left, out var right);
        var range = Maximum - Minimum;
        if (range <= 0) return DragTarget.None;

        var centerY = Bounds.Height / 2;
        var lowerX = Lerp((LowerValue - Minimum) / range, left, right);
        var upperX = Lerp((UpperValue - Minimum) / range, left, right);

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

        if (_dragTarget == DragTarget.Single)
            Value = Math.Clamp(rawValue, Minimum, Maximum);
        else if (_dragTarget == DragTarget.Lower)
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

        // Paint the full bounds transparent so the whole control is hit-testable — otherwise only
        // the drawn pixels (track / thumbs / dots) receive clicks, leaving dead gaps between them.
        context.FillRectangle(Brushes.Transparent, new Rect(0, 0, w, h));

        var range = Maximum - Minimum;
        if (range <= 0) return;

        var centerY = h / 2;
        var trackLeft = ThumbWidth / 2 + ThumbGap;
        var trackRight = w - ThumbWidth / 2 - ThumbGap;
        var trackTop = centerY - TrackHeight / 2;

        GetThumbRange(out var rangeLeft, out var rangeRight);

        // Unified model: the active band spans [loFrac, hiFrac]. Range mode has a lower thumb;
        // single mode fills from the track start (loFrac = 0) with no lower thumb.
        var hasLowerThumb = IsRange;
        var loFrac = hasLowerThumb ? (LowerValue - Minimum) / range : 0.0;
        var hiFrac = ((hasLowerThumb ? UpperValue : Value) - Minimum) / range;

        var loX = Lerp(loFrac, rangeLeft, rangeRight);
        var hiX = Lerp(hiFrac, rangeLeft, rangeRight);

        // Left inactive segment (only when a lower thumb splits it from the active band)
        if (hasLowerThumb)
            DrawSegment(context, trackLeft, loX - ThumbEdgeGap, trackTop,
                TrackCornerRadius, InsideCornerRadius, _palette.InactiveTrack);

        // Active segment: lower thumb (or track start in single mode) → upper thumb
        var activeLeft = hasLowerThumb ? loX + ThumbEdgeGap : trackLeft;
        var activeLeftRadius = hasLowerThumb ? InsideCornerRadius : TrackCornerRadius;
        DrawSegment(context, activeLeft, hiX - ThumbEdgeGap, trackTop,
            activeLeftRadius, InsideCornerRadius, _palette.ActiveTrack);

        // Right inactive segment
        DrawSegment(context, hiX + ThumbEdgeGap, trackRight, trackTop,
            InsideCornerRadius, TrackCornerRadius, _palette.InactiveTrack);

        DrawTickDots(context, rangeLeft, rangeRight, centerY, loFrac, hiFrac);

        if (hasLowerThumb)
            DrawThumb(context, loX, centerY, pressed: _dragTarget == DragTarget.Lower);

        var hiTarget = hasLowerThumb ? DragTarget.Upper : DragTarget.Single;
        DrawThumb(context, hiX, centerY, pressed: _dragTarget == hiTarget);
    }

    private static double Lerp(double frac, double left, double right) => left + frac * (right - left);

    private void DrawSegment(DrawingContext context, double leftX, double rightX, double trackTop,
        double leftRadius, double rightRadius, Color color)
    {
        if (rightX <= leftX) return;
        var geo = CreateTrackSegment(leftX, trackTop, rightX - leftX, TrackHeight, leftRadius, rightRadius);
        context.DrawGeometry(new SolidColorBrush(color), null, geo);
    }

    /// <summary>
    /// Draws the tick dots. A dot is active when its fraction is within the active band
    /// [loFrac, hiFrac]; dots overlapping either thumb position are skipped. (Single mode passes
    /// loFrac = 0, so the "lower thumb" sits at the track edge where no dot is drawn.)
    /// </summary>
    private void DrawTickDots(DrawingContext context, double rangeLeft, double rangeRight,
        double centerY, double loFrac, double hiFrac)
    {
        var range = Maximum - Minimum;
        if (range <= 0) return;

        var inactiveBrush = new SolidColorBrush(_palette.TickDot);
        var activeBrush = new SolidColorBrush(_palette.TickDotActive);
        var loX = Lerp(loFrac, rangeLeft, rangeRight);
        var hiX = Lerp(hiFrac, rangeLeft, rangeRight);

        for (var val = Minimum + TickInterval; val < Maximum; val += TickInterval)
        {
            var frac = (val - Minimum) / range;
            var x = Lerp(frac, rangeLeft, rangeRight);

            if (Math.Abs(x - loX) < ThumbWidth + ThumbGap) continue;
            if (Math.Abs(x - hiX) < ThumbWidth + ThumbGap) continue;

            var isActive = frac >= loFrac && frac <= hiFrac;
            context.DrawEllipse(isActive ? activeBrush : inactiveBrush, null,
                new Point(x, centerY), TickDotRadius, TickDotRadius);
        }
    }

    private void DrawThumb(DrawingContext context, double x, double centerY, bool pressed = false)
    {
        // Slightly shorter thumb while held — a subtle "pressed" cue. The clickable area is the
        // virtual ThumbHitRadius, so shrinking the visual doesn't affect grabbing it.
        var height = pressed ? ThumbHeight - ThumbPressShrink : ThumbHeight;
        var rect = new Rect(x - ThumbWidth / 2, centerY - height / 2, ThumbWidth, height);
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
