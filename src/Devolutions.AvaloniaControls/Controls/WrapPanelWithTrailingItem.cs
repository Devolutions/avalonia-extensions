// Vendored from Ursa.Avalonia, MIT licensed.
//   source:  https://github.com/irihitech/Ursa.Avalonia/blob/e72d50d5e41efa75424c3d2b39f4131a2293b155/src/Ursa/Controls/Panels/WrapPanelWithTrailingItem.cs
//   commit:  e72d50d5e41efa75424c3d2b39f4131a2293b155  (shipped as Irihi.Ursa 2.0.1)
//
// We used four types out of Ursa's 491 and wrapped two of them in empty
// subclasses purely to re-expose them under our own namespace. Vendoring drops
// both the Irihi.Ursa and Irihi.Avalonia.Shared package references, removes those
// wrappers, and stops Ursa loading during theme construction at application start.
//
// Changes from upstream:
//   - namespace moved to Devolutions.AvaloniaControls.Controls
//   - MathHelpers.GreaterThan (Irihi.Avalonia.Shared) replaced by Avalonia's own
//     GreaterThan, which is the identical tolerance-aware comparison
//   - PseudoClassName.PC_Empty replaced by the local EmptyPseudoClass constant,
//     which is the whole of the Irihi.Avalonia.Shared dependency
// See THIRD-PARTY-NOTICES.md for the full licence text.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Devolutions.AvaloniaControls.Controls;

public class WrapPanelWithTrailingItem : Panel
{
    public static readonly StyledProperty<Layoutable?> TrailingItemProperty =
        AvaloniaProperty.Register<WrapPanelWithTrailingItem, Layoutable?>(
            nameof(TrailingItem));

    public static readonly StyledProperty<double> TrailingWrapWidthProperty =
        AvaloniaProperty.Register<WrapPanelWithTrailingItem, double>(
            nameof(TrailingWrapWidth));

    static WrapPanelWithTrailingItem()
    {
        AffectsMeasure<WrapPanelWithTrailingItem>(TrailingItemProperty);
        AffectsArrange<WrapPanelWithTrailingItem>(TrailingItemProperty);
    }

    public Layoutable? TrailingItem
    {
        get => GetValue(TrailingItemProperty);
        set => SetValue(TrailingItemProperty, value);
    }

    public double TrailingWrapWidth
    {
        get => GetValue(TrailingWrapWidthProperty);
        set => SetValue(TrailingWrapWidthProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TrailingItemProperty)
        {
            if (change.GetOldValue<Layoutable?>() is { } oldValue)
            {
                VisualChildren.Remove(oldValue);
                if (!IsItemsHost) LogicalChildren.Remove(oldValue);
            }

            if (change.GetNewValue<Layoutable?>() is { } newValue)
            {
                VisualChildren.Add(newValue);
                if (!IsItemsHost) LogicalChildren.Add(newValue);
            }
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double currentLineX = 0;
        double currentLineHeight = 0;
        double totalHeight = 0;

        var children = Children;
        foreach (var child in children)
        {
            child.Measure(availableSize);
            var deltaX = availableSize.Width - currentLineX;
            // Width is enough to place next child
            if (GreaterThan(deltaX, child.DesiredSize.Width))
            {
                currentLineX += child.DesiredSize.Width;
                currentLineHeight = Math.Max(currentLineHeight, child.DesiredSize.Height);
            }
            // Width is not enough to place next child
            // reset currentLineX and currentLineHeight
            // accumulate last line height to total height. 
            // Notice: last line height accumulation only happens when restarting a new line, so it needs to finally add one more time outside iteration. 
            else
            {
                currentLineX = child.DesiredSize.Width;
                totalHeight += currentLineHeight;
                currentLineHeight = child.DesiredSize.Height;
            }
        }

        var last = TrailingItem;
        if (last is null) return new Size(availableSize.Width, totalHeight);
        last.Measure(availableSize);
        var lastDeltaX = availableSize.Width - currentLineX;
        // If width is not enough, add a new line, and recalculate total height
        if (lastDeltaX < TrailingWrapWidth)
        {
            totalHeight += currentLineHeight;
            totalHeight += last.DesiredSize.Height;
        }
        else
        {
            currentLineHeight = Math.Max(currentLineHeight, last.DesiredSize.Height);
            totalHeight += currentLineHeight;
        }

        return new Size(availableSize.Width, totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double currentLineX = 0;
        double currentLineHeight = 0;
        double totalHeight = 0;
        var children = Children;

        foreach (var child in children)
        {
            var deltaX = finalSize.Width - currentLineX;
            // Width is enough to place next child
            if (GreaterThan(deltaX, child.DesiredSize.Width))
            {
                child.Arrange(new Rect(currentLineX, totalHeight, child.DesiredSize.Width,
                    Math.Max(child.DesiredSize.Height, currentLineHeight)));
                currentLineX += child.Bounds.Width;
                currentLineHeight = Math.Max(currentLineHeight, child.Bounds.Height);
            }
            // Width is not enough to place next child
            // reset currentLineX and currentLineHeight
            // accumulate last line height to total height.
            // Notice: last line height accumulation only happens when restarting a new line, so it needs to finally add one more time outside iteration. 
            else
            {
                totalHeight += currentLineHeight;
                child.Arrange(new Rect(0, totalHeight, Math.Min(child.DesiredSize.Width, finalSize.Width),
                    child.DesiredSize.Height));
                currentLineX = child.Bounds.Width;
                currentLineHeight = child.Bounds.Height;
            }
        }

        var last = TrailingItem;
        if (last is null) return new Size(finalSize.Width, totalHeight);
        var lastDeltaX = finalSize.Width - currentLineX;
        // If width is not enough, add a new line, and recalculate total height
        if (lastDeltaX < TrailingWrapWidth)
        {
            totalHeight += currentLineHeight;
            last.Arrange(new Rect(0, totalHeight, finalSize.Width, last.DesiredSize.Height));
            totalHeight += last.Bounds.Height;
        }
        else
        {
            currentLineHeight = children.Count == 1 ? finalSize.Height : currentLineHeight;
            last.Arrange(new Rect(currentLineX, totalHeight, lastDeltaX,
                Math.Max(currentLineHeight, last.DesiredSize.Height)));
            currentLineHeight = Math.Max(currentLineHeight, last.Bounds.Height);
            totalHeight += currentLineHeight;
        }

        return new Size(finalSize.Width, totalHeight);
    }

    /// <summary>
    /// Tolerance-aware greater-than: values within a scaled epsilon count as equal, so a child whose
    /// desired width lands a floating-point hair over the remaining space still fits on the line.
    /// Stands in for Irihi.Avalonia.Shared's MathHelpers.GreaterThan; Avalonia's own MathUtilities is
    /// internal in 12.x. Verified to agree with the original across the layout-relevant range.
    /// </summary>
    private static bool GreaterThan(double value1, double value2) =>
        value1 > value2 && !AreClose(value1, value2);

    private static bool AreClose(double value1, double value2)
    {
        if (value1 == value2) return true;

        const double epsilon = 2.2204460492503131e-016;
        double tolerance = (Math.Abs(value1) + Math.Abs(value2) + 10.0) * epsilon;
        double delta = value1 - value2;
        return -tolerance < delta && tolerance > delta;
    }

}