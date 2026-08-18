namespace Devolutions.AvaloniaControls.Controls;

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
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

        if (!this.ShowToolTipWhenTextOverflowing || string.IsNullOrEmpty(text) || availableWidth <= 0)
        {
            ToolTip.SetTip(this.GetToolTipTarget(), null);
            return;
        }

        var layout = new TextLayout(
            text,
            new Typeface(
                textBlock?.FontFamily ?? this.GetValue(TextElement.FontFamilyProperty),
                textBlock?.FontStyle ?? this.GetValue(TextElement.FontStyleProperty),
                textBlock?.FontWeight ?? this.GetValue(TextElement.FontWeightProperty),
                textBlock?.FontStretch ?? FontStretch.Normal),
            textBlock?.FontSize ?? this.GetValue(TextElement.FontSizeProperty),
            textBlock?.Foreground ?? this.GetValue(TextElement.ForegroundProperty),
            textBlock?.TextAlignment ?? TextAlignment.Left,
            TextWrapping.NoWrap,
            TextTrimming.None,
            null,
            textBlock?.FlowDirection ?? this.FlowDirection,
            double.PositiveInfinity,
            double.PositiveInfinity,
            0,
            0,
            0,
            null,
            null);

        Control target = this.GetToolTipTarget();
        ToolTip.SetTip(target, layout.Width > availableWidth + 0.5 ? text : null);
        ToolTip.SetShowDelay(target, 200);
    }

    private Control GetToolTipTarget()
    {
        if (this.FindAncestorOfType<ToggleButton>() is { } toggleButton)
        {
            return toggleButton;
        }

        return (Control?)this.FindAncestorOfType<GroupedListBoxItem>() ?? this;
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
