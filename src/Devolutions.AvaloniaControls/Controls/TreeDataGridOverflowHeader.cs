namespace Devolutions.AvaloniaControls.Controls;

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.VisualTree;

public class TreeDataGridOverflowHeader : Decorator
{
    public static readonly StyledProperty<object?> ContentProperty =
        ContentControl.ContentProperty.AddOwner<TreeDataGridOverflowHeader>();

    public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty =
        ContentControl.ContentTemplateProperty.AddOwner<TreeDataGridOverflowHeader>();

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        TextBlock.ForegroundProperty.AddOwner<TreeDataGridOverflowHeader>();

    public static readonly StyledProperty<FontFamily> FontFamilyProperty =
        TextBlock.FontFamilyProperty.AddOwner<TreeDataGridOverflowHeader>();

    public static readonly StyledProperty<double> FontSizeProperty =
        TextBlock.FontSizeProperty.AddOwner<TreeDataGridOverflowHeader>();

    public static readonly StyledProperty<FontStyle> FontStyleProperty =
        TextBlock.FontStyleProperty.AddOwner<TreeDataGridOverflowHeader>();

    public static readonly StyledProperty<FontWeight> FontWeightProperty =
        TextBlock.FontWeightProperty.AddOwner<TreeDataGridOverflowHeader>();

    public static readonly StyledProperty<FontStretch> FontStretchProperty =
        TextBlock.FontStretchProperty.AddOwner<TreeDataGridOverflowHeader>();

    public static readonly StyledProperty<TextAlignment> TextAlignmentProperty =
        TextBlock.TextAlignmentProperty.AddOwner<TreeDataGridOverflowHeader>();

    public static readonly StyledProperty<TextWrapping> TextWrappingProperty =
        TextBlock.TextWrappingProperty.AddOwner<TreeDataGridOverflowHeader>();

    public static readonly StyledProperty<TextTrimming> TextTrimmingProperty =
        TextBlock.TextTrimmingProperty.AddOwner<TreeDataGridOverflowHeader>();

    public static readonly StyledProperty<TextDecorationCollection?> TextDecorationsProperty =
        TextBlock.TextDecorationsProperty.AddOwner<TreeDataGridOverflowHeader>();

    public static readonly StyledProperty<double> LineHeightProperty =
        TextBlock.LineHeightProperty.AddOwner<TreeDataGridOverflowHeader>();

    public static readonly StyledProperty<double> LetterSpacingProperty =
        TextBlock.LetterSpacingProperty.AddOwner<TreeDataGridOverflowHeader>();

    public static readonly StyledProperty<int> MaxLinesProperty =
        TextBlock.MaxLinesProperty.AddOwner<TreeDataGridOverflowHeader>();

    public static readonly StyledProperty<FontFeatureCollection?> FontFeaturesProperty =
        TextBlock.FontFeaturesProperty.AddOwner<TreeDataGridOverflowHeader>();

    public static readonly StyledProperty<Thickness> InnerContentMarginProperty =
        AvaloniaProperty.Register<TreeDataGridOverflowHeader, Thickness>(nameof(InnerContentMargin));

    public static readonly StyledProperty<bool> ShowToolTipProperty =
        AvaloniaProperty.Register<TreeDataGridOverflowHeader, bool>(nameof(ShowToolTip));

    public static void SetColumnToolTip(TreeDataGridColumn column, object? value) =>
        DevoTreeDataGridExtensions.SetToolTip(column, value);

    public static object? GetColumnToolTip(TreeDataGridColumn column) =>
        DevoTreeDataGridExtensions.GetToolTip(column);

    static TreeDataGridOverflowHeader()
    {
        AffectsMeasure<TreeDataGridOverflowHeader>(ContentProperty, ContentTemplateProperty, FontFamilyProperty,
            FontSizeProperty, FontStyleProperty, FontWeightProperty, FontStretchProperty, TextWrappingProperty,
            TextTrimmingProperty, LineHeightProperty, LetterSpacingProperty, MaxLinesProperty, FontFeaturesProperty,
            PaddingProperty);
        AffectsRender<TreeDataGridOverflowHeader>(ContentProperty, ForegroundProperty, FontFamilyProperty,
            FontSizeProperty, FontStyleProperty, FontWeightProperty, FontStretchProperty, TextAlignmentProperty,
            TextWrappingProperty, TextTrimmingProperty, TextDecorationsProperty, LineHeightProperty,
            LetterSpacingProperty, MaxLinesProperty, FontFeaturesProperty, PaddingProperty, InnerContentMarginProperty);
        AffectsArrange<TreeDataGridOverflowHeader>(InnerContentMarginProperty, ShowToolTipProperty);

        ContentProperty.Changed.Subscribe(args =>
        {
            if (args.Sender is TreeDataGridOverflowHeader header)
            {
                header.UpdateChild();
            }
        });

        ContentTemplateProperty.Changed.Subscribe(args =>
        {
            if (args.Sender is TreeDataGridOverflowHeader header)
            {
                header.UpdateChild();
            }
        });
    }

    public object? Content
    {
        get => this.GetValue(ContentProperty);
        set => this.SetValue(ContentProperty, value);
    }

    public IDataTemplate? ContentTemplate
    {
        get => this.GetValue(ContentTemplateProperty);
        set => this.SetValue(ContentTemplateProperty, value);
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

    public Thickness InnerContentMargin
    {
        get => this.GetValue(InnerContentMarginProperty);
        set => this.SetValue(InnerContentMarginProperty, value);
    }

    public bool ShowToolTip
    {
        get => this.GetValue(ShowToolTipProperty);
        set => this.SetValue(ShowToolTipProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (this.Content is string text)
        {
            Thickness padding = this.Padding;
            var layout = this.CreateTextLayout(text, double.PositiveInfinity, double.PositiveInfinity);
            return new Size(layout.Width + padding.Left + padding.Right, layout.Height + padding.Top + padding.Bottom);
        }

        this.Child?.Measure(availableSize);
        return this.Child?.DesiredSize ?? default;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (this.Content is not string && this.Child is { } child)
        {
            Thickness margin = this.InnerContentMargin;
            var rect = new Rect(
                margin.Left,
                margin.Top,
                Math.Max(0, finalSize.Width - margin.Left - margin.Right),
                Math.Max(0, finalSize.Height - margin.Top - margin.Bottom));
            child.Arrange(rect);
        }

        this.UpdateOverflow(finalSize);
        return finalSize;
    }

    public override void Render(DrawingContext context)
    {
        if (this.Content is not string text)
        {
            return;
        }

        Thickness padding = this.Padding;
        Thickness margin = this.InnerContentMargin;
        double maxWidth = Math.Max(0, this.Bounds.Width - padding.Left - padding.Right - margin.Left - margin.Right);
        double maxHeight = Math.Max(0, this.Bounds.Height - padding.Top - padding.Bottom - margin.Top - margin.Bottom);

        var layout = this.CreateTextLayout(text, maxWidth, maxHeight);
        double x = padding.Left + margin.Left;
        double y = padding.Top + margin.Top + (maxHeight - layout.Height) / 2;
        double scale = LayoutHelper.GetLayoutScale(this);

        layout.Draw(context, new Point(
            LayoutHelper.RoundLayoutValue(x, scale),
            LayoutHelper.RoundLayoutValue(y, scale)));
    }

    private void UpdateChild()
    {
        object? content = this.Content;

        if (content is null or string)
        {
            this.Child = null;
        }
        else if (content is Control control)
        {
            this.Child = control;
        }
        else
        {
            this.Child = new ContentPresenter
            {
                Content = content,
                ContentTemplate = this.ContentTemplate,
            };
        }
    }

    private TextLayout CreateTextLayout(string text, double maxWidth, double maxHeight)
    {
        return new TextLayout(
            text,
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
        string? text = this.Content as string;
        Thickness padding = this.Padding;
        Thickness margin = this.InnerContentMargin;
        double availableWidth = finalSize.Width - padding.Left - padding.Right - margin.Left - margin.Right;
        bool isOverflowing = !string.IsNullOrEmpty(text)
                             && availableWidth > 0
                             && this.CreateTextLayout(text, double.PositiveInfinity, double.PositiveInfinity).Width > availableWidth + 0.5;

        this.PseudoClasses.Set(":is-overflowing", isOverflowing);
        Control toolTipTarget = this.GetToolTipTarget();
        object? customToolTip = this.GetCustomToolTip();
        ToolTip.SetTip(toolTipTarget, customToolTip ?? (isOverflowing && this.ShowToolTip ? text : null));
        ToolTip.SetShowDelay(toolTipTarget, 200);
    }

    private Control GetToolTipTarget()
    {
        TreeDataGridColumnHeader? header = this.FindAncestorOfType<TreeDataGridColumnHeader>();
        return header is not null ? header : this;
    }

    private object? GetCustomToolTip()
    {
        object? columnToolTip = this.GetColumnToolTip();
        if (columnToolTip is not null)
        {
            return columnToolTip;
        }

        return this.Content is Control control ? ToolTip.GetTip(control) : null;
    }

    private object? GetColumnToolTip()
    {
        TreeDataGridColumnHeader? header = this.FindAncestorOfType<TreeDataGridColumnHeader>();
        TreeDataGrid? treeDataGrid = header?.FindAncestorOfType<TreeDataGrid>();

        if (header?.ColumnIndex is not int columnIndex || treeDataGrid is null || columnIndex < 0 || columnIndex >= treeDataGrid.Columns.Count)
        {
            return null;
        }

        return DevoTreeDataGridExtensions.GetToolTip(treeDataGrid.Columns[columnIndex]);
    }
}
