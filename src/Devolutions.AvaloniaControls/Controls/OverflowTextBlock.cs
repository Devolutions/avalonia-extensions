namespace Devolutions.AvaloniaControls.Controls;

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.VisualTree;

public class OverflowTextBlock : Control
{
    public static readonly StyledProperty<string?> TextProperty =
        TextBlock.TextProperty.AddOwner<OverflowTextBlock>();

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        TextBlock.ForegroundProperty.AddOwner<OverflowTextBlock>();

    public static readonly StyledProperty<FontFamily> FontFamilyProperty =
        TextBlock.FontFamilyProperty.AddOwner<OverflowTextBlock>();

    public static readonly StyledProperty<double> FontSizeProperty =
        TextBlock.FontSizeProperty.AddOwner<OverflowTextBlock>();

    public static readonly StyledProperty<FontStyle> FontStyleProperty =
        TextBlock.FontStyleProperty.AddOwner<OverflowTextBlock>();

    public static readonly StyledProperty<FontWeight> FontWeightProperty =
        TextBlock.FontWeightProperty.AddOwner<OverflowTextBlock>();

    public static readonly StyledProperty<FontStretch> FontStretchProperty =
        TextBlock.FontStretchProperty.AddOwner<OverflowTextBlock>();

    public static readonly StyledProperty<TextAlignment> TextAlignmentProperty =
        TextBlock.TextAlignmentProperty.AddOwner<OverflowTextBlock>();

    public static readonly StyledProperty<TextWrapping> TextWrappingProperty =
        TextBlock.TextWrappingProperty.AddOwner<OverflowTextBlock>();

    public static readonly StyledProperty<TextTrimming> TextTrimmingProperty =
        TextBlock.TextTrimmingProperty.AddOwner<OverflowTextBlock>();

    public static readonly StyledProperty<TextDecorationCollection?> TextDecorationsProperty =
        TextBlock.TextDecorationsProperty.AddOwner<OverflowTextBlock>();

    public static readonly StyledProperty<double> LineHeightProperty =
        TextBlock.LineHeightProperty.AddOwner<OverflowTextBlock>();

    public static readonly StyledProperty<double> LetterSpacingProperty =
        TextBlock.LetterSpacingProperty.AddOwner<OverflowTextBlock>();

    public static readonly StyledProperty<int> MaxLinesProperty =
        TextBlock.MaxLinesProperty.AddOwner<OverflowTextBlock>();

    public static readonly StyledProperty<FontFeatureCollection?> FontFeaturesProperty =
        TextBlock.FontFeaturesProperty.AddOwner<OverflowTextBlock>();

    public static readonly StyledProperty<Thickness> PaddingProperty =
        TextBlock.PaddingProperty.AddOwner<OverflowTextBlock>();

    public static readonly StyledProperty<Thickness> InnerTextMarginProperty =
        AvaloniaProperty.Register<OverflowTextBlock, Thickness>(nameof(InnerTextMargin));

    public static readonly StyledProperty<bool> ShowToolTipProperty =
        AvaloniaProperty.Register<OverflowTextBlock, bool>(nameof(ShowToolTip));

    static OverflowTextBlock()
    {
        AffectsMeasure<OverflowTextBlock>(TextProperty, FontFamilyProperty, FontSizeProperty, FontStyleProperty,
            FontWeightProperty, FontStretchProperty, TextWrappingProperty, TextTrimmingProperty, LineHeightProperty,
            LetterSpacingProperty, MaxLinesProperty, FontFeaturesProperty, PaddingProperty);
        AffectsRender<OverflowTextBlock>(TextProperty, ForegroundProperty, FontFamilyProperty, FontSizeProperty,
            FontStyleProperty, FontWeightProperty, FontStretchProperty, TextAlignmentProperty, TextWrappingProperty,
            TextTrimmingProperty, TextDecorationsProperty, LineHeightProperty, LetterSpacingProperty, MaxLinesProperty,
            FontFeaturesProperty, PaddingProperty, InnerTextMarginProperty);
        AffectsArrange<OverflowTextBlock>(InnerTextMarginProperty, ShowToolTipProperty);
    }

    public string? Text
    {
        get => this.GetValue(TextProperty);
        set => this.SetValue(TextProperty, value);
    }

    public IBrush? Foreground
    {
        get => this.GetValue(ForegroundProperty);
        set => this.SetValue(ForegroundProperty, value);
    }

    public FontFamily FontFamily
    {
        get => this.GetValue(FontFamilyProperty);
        set => this.SetValue(FontFamilyProperty, value);
    }

    public double FontSize
    {
        get => this.GetValue(FontSizeProperty);
        set => this.SetValue(FontSizeProperty, value);
    }

    public FontStyle FontStyle
    {
        get => this.GetValue(FontStyleProperty);
        set => this.SetValue(FontStyleProperty, value);
    }

    public FontWeight FontWeight
    {
        get => this.GetValue(FontWeightProperty);
        set => this.SetValue(FontWeightProperty, value);
    }

    public FontStretch FontStretch
    {
        get => this.GetValue(FontStretchProperty);
        set => this.SetValue(FontStretchProperty, value);
    }

    public TextAlignment TextAlignment
    {
        get => this.GetValue(TextAlignmentProperty);
        set => this.SetValue(TextAlignmentProperty, value);
    }

    public TextWrapping TextWrapping
    {
        get => this.GetValue(TextWrappingProperty);
        set => this.SetValue(TextWrappingProperty, value);
    }

    public TextTrimming TextTrimming
    {
        get => this.GetValue(TextTrimmingProperty);
        set => this.SetValue(TextTrimmingProperty, value);
    }

    public TextDecorationCollection? TextDecorations
    {
        get => this.GetValue(TextDecorationsProperty);
        set => this.SetValue(TextDecorationsProperty, value);
    }

    public double LineHeight
    {
        get => this.GetValue(LineHeightProperty);
        set => this.SetValue(LineHeightProperty, value);
    }

    public double LetterSpacing
    {
        get => this.GetValue(LetterSpacingProperty);
        set => this.SetValue(LetterSpacingProperty, value);
    }

    public int MaxLines
    {
        get => this.GetValue(MaxLinesProperty);
        set => this.SetValue(MaxLinesProperty, value);
    }

    public FontFeatureCollection? FontFeatures
    {
        get => this.GetValue(FontFeaturesProperty);
        set => this.SetValue(FontFeaturesProperty, value);
    }

    public Thickness Padding
    {
        get => this.GetValue(PaddingProperty);
        set => this.SetValue(PaddingProperty, value);
    }

    public Thickness InnerTextMargin
    {
        get => this.GetValue(InnerTextMarginProperty);
        set => this.SetValue(InnerTextMarginProperty, value);
    }

    public bool ShowToolTip
    {
        get => this.GetValue(ShowToolTipProperty);
        set => this.SetValue(ShowToolTipProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        Thickness padding = this.Padding;
        var layout = this.CreateTextLayout(double.PositiveInfinity, double.PositiveInfinity);
        return new Size(layout.Width + padding.Left + padding.Right, layout.Height + padding.Top + padding.Bottom);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        this.UpdateOverflow(finalSize);
        return finalSize;
    }

    public override void Render(DrawingContext context)
    {
        Thickness padding = this.Padding;
        Thickness margin = this.InnerTextMargin;
        double maxWidth = Math.Max(0, this.Bounds.Width - padding.Left - padding.Right - margin.Left - margin.Right);
        double maxHeight = Math.Max(0, this.Bounds.Height - padding.Top - padding.Bottom - margin.Top - margin.Bottom);

        var layout = this.CreateTextLayout(maxWidth, maxHeight);
        layout.Draw(context, new Point(padding.Left + margin.Left, padding.Top + margin.Top));
    }

    private TextLayout CreateTextLayout(double maxWidth, double maxHeight)
    {
        return new TextLayout(
            this.Text ?? string.Empty,
            new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch),
            this.FontSize,
            this.Foreground,
            this.TextAlignment,
            this.TextWrapping,
            this.TextTrimming,
            this.TextDecorations,
            this.FlowDirection,
            maxWidth,
            maxHeight,
            this.LineHeight,
            this.LetterSpacing,
            this.MaxLines,
            this.FontFeatures,
            null);
    }

    private void UpdateOverflow(Size finalSize)
    {
        string? text = this.Text;
        Thickness padding = this.Padding;
        Thickness margin = this.InnerTextMargin;
        double availableWidth = finalSize.Width - padding.Left - padding.Right - margin.Left - margin.Right;
        bool isOverflowing = !string.IsNullOrEmpty(text)
                             && availableWidth > 0
                             && this.CreateTextLayout(double.PositiveInfinity, double.PositiveInfinity).Width > availableWidth + 0.5;

        this.PseudoClasses.Set(":is-overflowing", isOverflowing);
        var toolTipTarget = this.GetToolTipTarget();
        ToolTip.SetTip(toolTipTarget, isOverflowing && this.ShowToolTip ? text : null);
        ToolTip.SetShowDelay(toolTipTarget, 200);
    }

    private Control GetToolTipTarget()
    {
        TreeDataGridColumnHeader? header = this.FindAncestorOfType<TreeDataGridColumnHeader>();
        return header is not null ? header : this;
    }
}
