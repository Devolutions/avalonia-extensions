namespace Devolutions.AvaloniaControls.Controls;

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;

public class OverflowContentPresenter : ContentPresenter
{
    private double arrangedWidth;

    private double lastTooltipWidth = double.NaN;

    public static readonly StyledProperty<bool> ShowToolTipWhenTextOverflowingProperty =
        AvaloniaProperty.Register<OverflowContentPresenter, bool>(nameof(ShowToolTipWhenTextOverflowing));

    public bool ShowToolTipWhenTextOverflowing
    {
        get => this.GetValue(ShowToolTipWhenTextOverflowingProperty);
        set => this.SetValue(ShowToolTipWhenTextOverflowingProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ContentProperty || change.Property == BoundsProperty || change.Property == ShowToolTipWhenTextOverflowingProperty)
        {
            this.InvalidateToolTip();
        }
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        bool widthChanged = !AreClose(this.arrangedWidth, finalSize.Width);
        this.arrangedWidth = finalSize.Width;
        Size size = base.ArrangeOverride(finalSize);
        if (widthChanged)
        {
            this.UpdateToolTip();
        }

        return size;
    }

    private void UpdateToolTip()
    {
        double availableWidth = this.arrangedWidth > 0 ? this.arrangedWidth : this.Bounds.Width;
        availableWidth -= this.Padding.Left + this.Padding.Right;

        TextBlock? textBlock = this.FindTextBlock();
        string? text = textBlock?.Text ?? (this.Content as string);
        if (textBlock?.Bounds.Width > 0)
        {
            availableWidth = Math.Min(availableWidth, textBlock.Bounds.Width);
        }

        if (AreClose(this.lastTooltipWidth, availableWidth))
        {
            return;
        }

        this.lastTooltipWidth = availableWidth;

        Control target = this.GetToolTipTarget();
        TextOverflowToolTip.Update(target, textBlock, text, availableWidth, this.ShowToolTipWhenTextOverflowing, true);
    }

    private void InvalidateToolTip()
    {
        this.lastTooltipWidth = double.NaN;
        this.UpdateToolTip();
    }

    private static bool AreClose(double left, double right) => Math.Abs(left - right) < 0.5;

    private Control GetToolTipTarget()
    {
        if (this.FindAncestorOfType<ToggleButton>() is { } toggleButton)
        {
            return toggleButton;
        }

        return (Control?)this.FindAncestorOfType<Border>() ?? this;
    }

    private TextBlock? FindTextBlock()
    {
        foreach (Visual child in this.GetVisualChildren())
        {
            if (child is TextBlock textBlock)
            {
                return textBlock;
            }

            if (FindTextBlock(child) is { } descendant)
            {
                return descendant;
            }
        }

        return null;
    }

    private static TextBlock? FindTextBlock(Visual visual)
    {
        foreach (Visual child in visual.GetVisualChildren())
        {
            if (child is TextBlock textBlock)
            {
                return textBlock;
            }

            if (FindTextBlock(child) is { } descendant)
            {
                return descendant;
            }
        }

        return null;
    }
}
