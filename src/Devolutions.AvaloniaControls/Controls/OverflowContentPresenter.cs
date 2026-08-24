namespace Devolutions.AvaloniaControls.Controls;

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;

public class OverflowContentPresenter : ContentPresenter
{
    private double arrangedWidth;

    private bool hasArranged;

    private double lastTooltipWidth = double.NaN;

    private readonly List<TextBlock> cachedTextBlocks = new();

    private bool textBlocksCached;

    private Control? cachedToolTipTarget;

    private object? appliedToolTip;

    private int? appliedToolTipShowDelay;

    public static readonly StyledProperty<bool> ShowToolTipWhenTextOverflowingProperty =
        AvaloniaProperty.Register<OverflowContentPresenter, bool>(nameof(ShowToolTipWhenTextOverflowing));

    /// <summary>Optional tooltip delay in milliseconds. Null preserves the theme default.</summary>
    public static readonly StyledProperty<int?> ToolTipShowDelayProperty =
        AvaloniaProperty.Register<OverflowContentPresenter, int?>(nameof(ToolTipShowDelay));

    public bool ShowToolTipWhenTextOverflowing
    {
        get => this.GetValue(ShowToolTipWhenTextOverflowingProperty);
        set => this.SetValue(ShowToolTipWhenTextOverflowingProperty, value);
    }

    public int? ToolTipShowDelay
    {
        get => this.GetValue(ToolTipShowDelayProperty);
        set => this.SetValue(ToolTipShowDelayProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ContentProperty || change.Property == ContentTemplateProperty)
        {
            this.ClearTextBlockCache();
            this.InvalidateToolTip();
        }
        else if (change.Property == BoundsProperty
            || change.Property == PaddingProperty
            || change.Property == ShowToolTipWhenTextOverflowingProperty
            || change.Property == ToolTipShowDelayProperty)
        {
            this.InvalidateToolTip();
        }
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        bool widthChanged = !AreClose(this.arrangedWidth, finalSize.Width);
        this.arrangedWidth = finalSize.Width;
        this.hasArranged = true;
        Size size = base.ArrangeOverride(finalSize);
        if (widthChanged)
        {
            this.UpdateToolTip();
        }

        return size;
    }

    private void UpdateToolTip()
    {
        if (!this.hasArranged)
        {
            return;
        }

        double availableWidth = this.arrangedWidth > 0 ? this.arrangedWidth : this.Bounds.Width;
        availableWidth -= this.Padding.Left + this.Padding.Right;

        if (AreClose(this.lastTooltipWidth, availableWidth))
        {
            return;
        }

        this.lastTooltipWidth = availableWidth;

        Control target = this.GetToolTipTarget();
        string? toolTip = TextOverflowToolTip.GetOverflowingText(
            this.GetTextBlocks(),
            this.Content as string,
            availableWidth,
            this.ShowToolTipWhenTextOverflowing,
            true);
        ToolTip.SetTip(target, toolTip);
        if (this.ToolTipShowDelay is { } showDelay)
        {
            ToolTip.SetShowDelay(target, showDelay);
            this.appliedToolTipShowDelay = showDelay;
        }
        else if (this.appliedToolTipShowDelay is not null)
        {
            target.ClearValue(ToolTip.ShowDelayProperty);
            this.appliedToolTipShowDelay = null;
        }

        this.appliedToolTip = toolTip;
    }

    private void InvalidateToolTip()
    {
        this.lastTooltipWidth = double.NaN;
        this.UpdateToolTip();
    }

    private static bool AreClose(double left, double right) => Math.Abs(left - right) < 0.5;

    private Control GetToolTipTarget()
    {
        if (this.cachedToolTipTarget is { } target)
        {
            return target;
        }

        if (this.FindAncestorOfType<ToggleButton>() is { } toggleButton)
        {
            this.cachedToolTipTarget = toggleButton;
            return toggleButton;
        }

        this.cachedToolTipTarget = (Control?)this.FindAncestorOfType<Border>() ?? this;
        return this.cachedToolTipTarget;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        this.ClearOwnedToolTip();
        this.cachedToolTipTarget = null;
        this.hasArranged = false;
        this.InvalidateToolTip();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        this.ClearOwnedToolTip();
        this.cachedToolTipTarget = null;
        this.ClearTextBlockCache();
        this.hasArranged = false;
        this.lastTooltipWidth = double.NaN;

        base.OnDetachedFromVisualTree(e);
    }

    private IReadOnlyList<TextBlock> GetTextBlocks()
    {
        if (!this.textBlocksCached)
        {
            this.textBlocksCached = true;
            FindTextBlocks(this, this.cachedTextBlocks);
            foreach (TextBlock textBlock in this.cachedTextBlocks)
            {
                textBlock.PropertyChanged += this.OnTextBlockPropertyChanged;
                textBlock.DetachedFromVisualTree += this.OnTextBlockDetachedFromVisualTree;
            }
        }

        return this.cachedTextBlocks;
    }

    private void ClearTextBlockCache()
    {
        foreach (TextBlock textBlock in this.cachedTextBlocks)
        {
            textBlock.PropertyChanged -= this.OnTextBlockPropertyChanged;
            textBlock.DetachedFromVisualTree -= this.OnTextBlockDetachedFromVisualTree;
        }

        this.cachedTextBlocks.Clear();
        this.textBlocksCached = false;
    }

    private void OnTextBlockPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == TextBlock.TextProperty
            || change.Property == BoundsProperty
            || change.Property == Visual.IsVisibleProperty
            || change.Property == TextBlock.FontFamilyProperty
            || change.Property == TextBlock.FontSizeProperty
            || change.Property == TextBlock.FontStyleProperty
            || change.Property == TextBlock.FontWeightProperty
            || change.Property == TextBlock.FontStretchProperty
            || change.Property == TextBlock.LetterSpacingProperty
            || change.Property == TextBlock.FontFeaturesProperty)
        {
            this.InvalidateToolTip();
        }
    }

    private void OnTextBlockDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        this.ClearTextBlockCache();
        this.InvalidateToolTip();
    }

    private void ClearOwnedToolTip()
    {
        if (this.cachedToolTipTarget is { } target
            && this.appliedToolTip is { } toolTip
            && ReferenceEquals(ToolTip.GetTip(target), toolTip))
        {
            ToolTip.SetTip(target, null);
        }

        if (this.cachedToolTipTarget is { } targetWithDelay && this.appliedToolTipShowDelay is not null)
        {
            targetWithDelay.ClearValue(ToolTip.ShowDelayProperty);
        }

        this.appliedToolTip = null;
        this.appliedToolTipShowDelay = null;
    }

    private static void FindTextBlocks(Visual visual, List<TextBlock> textBlocks)
    {
        foreach (Visual child in visual.GetVisualChildren())
        {
            if (child is TextBlock textBlock)
            {
                textBlocks.Add(textBlock);
            }

            FindTextBlocks(child, textBlocks);
        }
    }
}
