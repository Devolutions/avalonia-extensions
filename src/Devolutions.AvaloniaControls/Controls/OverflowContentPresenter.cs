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

    public static readonly StyledProperty<bool> ShowToolTipWhenTextOverflowingProperty =
        AvaloniaProperty.Register<OverflowContentPresenter, bool>(nameof(ShowToolTipWhenTextOverflowing));

    public OverflowContentPresenter()
    {
        this.GetObservable(ContentProperty).Subscribe(_ => this.UpdateToolTip());
        this.GetObservable(BoundsProperty).Subscribe(_ => this.UpdateToolTip());
        this.GetObservable(ShowToolTipWhenTextOverflowingProperty).Subscribe(_ => this.UpdateToolTip());
    }

    public bool ShowToolTipWhenTextOverflowing
    {
        get => this.GetValue(ShowToolTipWhenTextOverflowingProperty);
        set => this.SetValue(ShowToolTipWhenTextOverflowingProperty, value);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        this.arrangedWidth = finalSize.Width;
        Size size = base.ArrangeOverride(finalSize);
        this.UpdateToolTip();
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

        Control target = this.GetToolTipTarget();
        TextOverflowToolTip.Update(target, textBlock, text, availableWidth, this.ShowToolTipWhenTextOverflowing, true);
    }

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
