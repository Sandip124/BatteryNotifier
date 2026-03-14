using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;

namespace BatteryNotifier.Avalonia.Controls;

/// <summary>
/// Material 3-inspired discrete slider with:
/// - Tall pill-shaped track (16dp)
/// - Vertical capsule thumb (4dp × 44dp)
/// - Gap around thumb with inside corners
/// - Dot tick marks along the track at each step
/// </summary>
public class MaterialSlider : Control
{
    // ── Styled properties ──────────────────────────────────────

    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<MaterialSlider, double>(nameof(Minimum), 0);

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<MaterialSlider, double>(nameof(Maximum), 100);

    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<MaterialSlider, double>(nameof(Value), 50,
            defaultBindingMode: global::Avalonia.Data.BindingMode.TwoWay,
            coerce: CoerceValue);

    public static readonly StyledProperty<double> StepProperty =
        AvaloniaProperty.Register<MaterialSlider, double>(nameof(Step), 1);

    static MaterialSlider()
    {
        AffectsRender<MaterialSlider>(MinimumProperty, MaximumProperty, ValueProperty, StepProperty);
        FocusableProperty.OverrideDefaultValue<MaterialSlider>(true);
    }

    private static double CoerceValue(AvaloniaObject obj, double value)
    {
        var slider = (MaterialSlider)obj;
        return Math.Clamp(value, slider.Minimum, slider.Maximum);
    }

    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Step
    {
        get => GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }

    // ── Dimensions (M3 spec) ───────────────────────────────────

    private const double TrackHeight = 10;
    private const double TrackCornerRadius = 5;
    private const double ThumbWidth = 5;
    private const double ThumbHeight = 22;
    private const double ThumbCornerRadius = 2.5;
    private const double ThumbGap = 5;
    private const double InsideCornerRadius = 2;
    private const double TickDotRadius = 1.5;
    private const double TickEdgeInset = 4;
    private const double ControlHeight = 28;

    // ── Colors ─────────────────────────────────────────────────

    private record Palette(
        Color ActiveTrack, Color InactiveTrack,
        Color Thumb,
        Color ActiveTick, Color InactiveTick);

    private Palette _palette = DarkPalette;

    private static readonly Palette DarkPalette = new(
        ActiveTrack: Color.Parse("#4CA6FF"),
        InactiveTrack: Color.Parse("#2A2A2A"),
        Thumb: Color.Parse("#4CA6FF"),
        ActiveTick: Color.Parse("#1A1A1A"),
        InactiveTick: Color.Parse("#505050"));

    private static readonly Palette LightPalette = new(
        ActiveTrack: Color.Parse("#2870BD"),
        InactiveTrack: Color.Parse("#D5D5D5"),
        Thumb: Color.Parse("#2870BD"),
        ActiveTick: Color.Parse("#FFFFFF"),
        InactiveTick: Color.Parse("#A0A0A0"));

    // ── Lifecycle ──────────────────────────────────────────────

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
                     (ActualThemeVariant == ThemeVariant.Default && IsSystemDark());
        _palette = isDark ? DarkPalette : LightPalette;
        InvalidateVisual();
    }

    private static bool IsSystemDark()
    {
        var app = Application.Current;
        return app?.ActualThemeVariant == ThemeVariant.Dark;
    }

    // ── Measure ────────────────────────────────────────────────

    protected override Size MeasureOverride(Size availableSize)
    {
        var w = double.IsInfinity(availableSize.Width) ? 100 : availableSize.Width;
        return new Size(w, ControlHeight);
    }

    // ── Input handling ─────────────────────────────────────────

    private bool _isDragging;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _isDragging = true;
        e.Pointer.Capture(this);
        UpdateValueFromPointer(e.GetPosition(this).X);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isDragging)
        {
            UpdateValueFromPointer(e.GetPosition(this).X);
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_isDragging)
        {
            _isDragging = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        var step = Step > 0 ? Step : 1;
        switch (e.Key)
        {
            case Key.Left:
            case Key.Down:
                Value = Math.Max(Minimum, Value - step);
                e.Handled = true;
                break;
            case Key.Right:
            case Key.Up:
                Value = Math.Min(Maximum, Value + step);
                e.Handled = true;
                break;
        }
    }

    private void UpdateValueFromPointer(double x)
    {
        GetThumbRange(out var thumbLeft, out var thumbRight);
        var thumbRange = thumbRight - thumbLeft;
        if (thumbRange <= 0) return;

        var fraction = Math.Clamp((x - thumbLeft) / thumbRange, 0, 1);
        var rawValue = Minimum + fraction * (Maximum - Minimum);

        // Snap to step
        if (Step > 0)
        {
            rawValue = Math.Round(rawValue / Step) * Step;
            rawValue = Math.Clamp(rawValue, Minimum, Maximum);
        }

        Value = rawValue;
    }

    /// <summary>
    /// Returns the X range where the thumb center can travel.
    /// This is inset from the track edges so thumb and dots align.
    /// </summary>
    private void GetThumbRange(out double left, out double right)
    {
        var trackLeft = ThumbWidth / 2 + ThumbGap;
        var trackRight = Bounds.Width - ThumbWidth / 2 - ThumbGap;
        left = trackLeft + TickEdgeInset;
        right = trackRight - TickEdgeInset;
    }

    // ── Rendering ──────────────────────────────────────────────

    public override void Render(DrawingContext context)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        var range = Maximum - Minimum;
        if (range <= 0) return;

        var fraction = (Value - Minimum) / range;
        var centerY = h / 2;

        // Track visual bounds (full pill shape)
        var trackLeft = ThumbWidth / 2 + ThumbGap;
        var trackRight = w - ThumbWidth / 2 - ThumbGap;
        var trackTop = centerY - TrackHeight / 2;

        // Thumb travel range (inset so dots and thumb align)
        GetThumbRange(out var thumbRangeLeft, out var thumbRangeRight);
        var thumbX = thumbRangeLeft + fraction * (thumbRangeRight - thumbRangeLeft);

        // ── Draw inactive track (right of thumb) ──
        var gapRight = thumbX + ThumbWidth / 2 + ThumbGap;
        if (gapRight < trackRight)
        {
            var inactiveGeo = CreateTrackSegment(
                gapRight, trackTop, trackRight - gapRight, TrackHeight,
                leftRadius: InsideCornerRadius, rightRadius: TrackCornerRadius);
            context.DrawGeometry(new SolidColorBrush(_palette.InactiveTrack), null, inactiveGeo);
        }

        // ── Draw active track (left of thumb) ──
        var gapLeft = thumbX - ThumbWidth / 2 - ThumbGap;
        if (gapLeft > trackLeft)
        {
            var activeGeo = CreateTrackSegment(
                trackLeft, trackTop, gapLeft - trackLeft, TrackHeight,
                leftRadius: TrackCornerRadius, rightRadius: InsideCornerRadius);
            context.DrawGeometry(new SolidColorBrush(_palette.ActiveTrack), null, activeGeo);
        }

        // ── Draw tick dots (fixed 10 visual ticks) ──
        const int tickCount = 10;
        {
            for (int i = 0; i <= tickCount; i++)
            {
                var tickFraction = (double)i / tickCount;
                var tickX = thumbRangeLeft + tickFraction * (thumbRangeRight - thumbRangeLeft);

                // Skip ticks inside the thumb gap
                if (tickX > thumbX - ThumbWidth / 2 - ThumbGap + 1 &&
                    tickX < thumbX + ThumbWidth / 2 + ThumbGap - 1)
                    continue;

                var isActive = tickX < thumbX;
                var tickColor = isActive ? _palette.ActiveTick : _palette.InactiveTick;
                context.DrawEllipse(
                    new SolidColorBrush(tickColor), null,
                    new Point(tickX, centerY), TickDotRadius, TickDotRadius);
            }
        }

        // ── Draw thumb (vertical capsule) ──
        var thumbRect = new Rect(
            thumbX - ThumbWidth / 2,
            centerY - ThumbHeight / 2,
            ThumbWidth,
            ThumbHeight);
        context.DrawRectangle(
            new SolidColorBrush(_palette.Thumb), null,
            thumbRect, ThumbCornerRadius, ThumbCornerRadius);
    }

    /// <summary>
    /// Creates a track segment with independent left/right corner radii (pill ends vs inside corners).
    /// </summary>
    private static StreamGeometry CreateTrackSegment(
        double x, double y, double width, double height,
        double leftRadius, double rightRadius)
    {
        var geo = new StreamGeometry();
        using var ctx = geo.Open();

        var lr = Math.Min(leftRadius, height / 2);
        var rr = Math.Min(rightRadius, height / 2);

        // Start at top-left, after the left corner radius
        ctx.BeginFigure(new Point(x + lr, y), true);

        // Top edge → top-right corner
        ctx.LineTo(new Point(x + width - rr, y));
        if (rr > 0)
            ctx.ArcTo(new Point(x + width, y + rr), new Size(rr, rr), 0, false, SweepDirection.Clockwise);

        // Right edge → bottom-right corner
        ctx.LineTo(new Point(x + width, y + height - rr));
        if (rr > 0)
            ctx.ArcTo(new Point(x + width - rr, y + height), new Size(rr, rr), 0, false, SweepDirection.Clockwise);

        // Bottom edge → bottom-left corner
        ctx.LineTo(new Point(x + lr, y + height));
        if (lr > 0)
            ctx.ArcTo(new Point(x, y + height - lr), new Size(lr, lr), 0, false, SweepDirection.Clockwise);

        // Left edge → top-left corner
        ctx.LineTo(new Point(x, y + lr));
        if (lr > 0)
            ctx.ArcTo(new Point(x + lr, y), new Size(lr, lr), 0, false, SweepDirection.Clockwise);

        ctx.EndFigure(true);
        return geo;
    }
}
