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

    public static readonly DirectProperty<TreeDataGridOverflowHeader, double> AdornmentWidthProperty =
        AvaloniaProperty.RegisterDirect<TreeDataGridOverflowHeader, double>(
            nameof(AdornmentWidth),
            static o => o.AdornmentWidth);

    public static readonly DirectProperty<TreeDataGridOverflowHeader, bool> IsAdornmentFillingProperty =
        AvaloniaProperty.RegisterDirect<TreeDataGridOverflowHeader, bool>(
            nameof(IsAdornmentFilling),
            static o => o.IsAdornmentFilling);

    private Control? adornment;

    private object? adornmentSource;

    private Control? adornmentContent;

    private double adornmentWidth;

    private bool isAdornmentFilling;

    private double adornmentNaturalWidth;

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

        ContentProperty.Changed.AddClassHandler<TreeDataGridOverflowHeader>((control, _) => control.UpdateChild());
        ContentTemplateProperty.Changed.AddClassHandler<TreeDataGridOverflowHeader>((control, _) => control.UpdateChild());

        // The position is resolved during arrange, so a change has to schedule one; otherwise nothing
        // else may invalidate layout and the adornment keeps its previous placement.
        DevoTreeDataGridExtensions.HeaderAdornmentPositionProperty.Changed.AddClassHandler<AvaloniaObject>(
            static (target, _) => (target as Control)?.FindAncestorOfType<TreeDataGridOverflowHeader>()?.InvalidateArrange());
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

    /// <summary>
    /// Width the right-hand adornment occupies, or 0 when there is none. Themes bind this to offset the
    /// sort indicator so the adornment sits to its right.
    /// </summary>
    public double AdornmentWidth => this.adornmentWidth;

    /// <summary>
    /// True while the adornment is spanning the whole header (see
    /// <c>DevoTreeDataGridExtensions.FillsHeaderWidth</c>). Themes bind this to hide the sort indicator,
    /// which would otherwise sit on top of a full-width adornment such as a search field.
    /// </summary>
    public bool IsAdornmentFilling => this.isAdornmentFilling;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        this.UpdateAdornment();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        // Column headers are recycled, so release the adornment: a Control can only have one parent,
        // and the next column this header serves may supply a different one.
        this.DetachAdornment();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        this.MeasureAdornment();

        if (this.Content is string text)
        {
            Thickness padding = this.Padding;
            var layout = this.CreateTextLayout(text, double.PositiveInfinity, double.PositiveInfinity);
            return new Size(layout.Width + padding.Left + padding.Right, layout.Height + padding.Top + padding.Bottom);
        }

        // Measured unconstrained, matching the string path above, which reports its natural width and
        // ignores availableSize. Passing availableSize through instead lets Avalonia clamp the child to the
        // column's current width, which self-locks an Auto column: the clamped desired size reproduces the
        // width it was clamped by, so the column can never grow to fit its header. Arrange re-measures
        // against the real width so content can still trim.
        this.Child?.Measure(Size.Infinity);
        return this.Child?.DesiredSize ?? default;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double adornmentWidth = this.ArrangeAdornment(finalSize);

        if (this.Content is not string && this.Child is { } child)
        {
            Thickness margin = this.InnerContentMargin;
            var rect = new Rect(
                margin.Left,
                margin.Top,
                Math.Max(0, finalSize.Width - margin.Left - margin.Right - adornmentWidth),
                Math.Max(0, finalSize.Height - margin.Top - margin.Bottom));

            // Deliberately NOT re-measured against rect here, unlike the adornment. The child's desired
            // size is what an Auto column sizes from, so shrinking it to the width it was just given makes
            // the column self-lock at its current width.
            child.Arrange(rect);
        }

        this.UpdateOverflow(finalSize);
        return finalSize;
    }

    // Measured at its natural size but NEVER folded into this control's desired size. That is what keeps
    // a variable-width adornment from resizing an Auto-width column when it appears or changes.
    private void MeasureAdornment()
    {
        if (this.adornment is not { } adorn)
        {
            this.adornmentNaturalWidth = 0;
            return;
        }

        adorn.Measure(Size.Infinity);
        this.adornmentNaturalWidth = adorn.DesiredSize.Width;
    }

    // Sits flush right, so the sort indicator (which themes shift left by AdornmentWidth) lands to its
    // left. Never wider than the header: a long term is re-measured against what is actually available so
    // its content can trim, instead of spilling over neighbouring columns.
    private double ArrangeAdornment(Size finalSize)
    {
        if (this.adornment is not { } adorn)
        {
            this.SetAndRaise(AdornmentWidthProperty, ref this.adornmentWidth, 0);
            this.SetAndRaise(IsAdornmentFillingProperty, ref this.isAdornmentFilling, false);
            return 0;
        }

        bool fills = this.ResolveAdornmentPosition() == HeaderAdornmentPosition.Fill;
        this.SetAndRaise(IsAdornmentFillingProperty, ref this.isAdornmentFilling, fills);
        double width = fills
            ? finalSize.Width
            : Math.Min(this.adornmentNaturalWidth, finalSize.Width);

        if (fills || width < this.adornmentNaturalWidth)
        {
            adorn.Measure(new Size(width, finalSize.Height));
        }

        adorn.Arrange(new Rect(Math.Max(0, finalSize.Width - width), 0, width, finalSize.Height));

        // Report the effective width, not the natural one, so a clamped adornment cannot push a theme's
        // sort indicator off the left edge of the header.
        this.SetAndRaise(AdornmentWidthProperty, ref this.adornmentWidth, width);

        return width;
    }

    public override void Render(DrawingContext context)
    {
        if (this.Content is not string text)
        {
            return;
        }

        Thickness padding = this.Padding;
        Thickness margin = this.InnerContentMargin;
        double maxWidth = Math.Max(0, this.Bounds.Width - padding.Left - padding.Right - margin.Left - margin.Right - this.adornmentWidth);
        double maxHeight = Math.Max(0, this.Bounds.Height - padding.Top - padding.Bottom - margin.Top - margin.Bottom);

        TextLayout layout = this.CreateTextLayout(text, maxWidth, maxHeight);
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

        // Setting Child clears LogicalChildren, and a new Content may mean a new column, so re-resolve.
        this.UpdateAdornment();
    }

    // A value on the adornment control wins over the column's: the column is a plain AvaloniaObject with no
    // DataContext, so only the control can carry a binding that flips the position at runtime.
    private HeaderAdornmentPosition ResolveAdornmentPosition()
    {
        if (this.adornmentContent is { } content
            && content.IsSet(DevoTreeDataGridExtensions.HeaderAdornmentPositionProperty))
        {
            return DevoTreeDataGridExtensions.GetHeaderAdornmentPosition(content);
        }

        return this.TryGetOwningColumn() is { } column
            ? DevoTreeDataGridExtensions.GetHeaderAdornmentPosition(column)
            : HeaderAdornmentPosition.Right;
    }

    private void UpdateAdornment()
    {
        object? source = this.TryGetOwningColumn() is { } column
            ? DevoTreeDataGridExtensions.GetHeaderAdornment(column)
            : null;

        if (!ReferenceEquals(source, this.adornmentSource))
        {
            this.DetachAdornment();

            this.adornmentSource = source;

            // Hosted inside a clipping border: headers deliberately run with ClipToBounds="False" (the
            // resize thumb overhangs), so without this an over-long adornment paints across its
            // neighbours. The consumer's own control is left untouched.
            this.adornmentContent = source switch
            {
                null => null,
                Control control => control,
                _ => new ContentPresenter { Content = source },
            };

            this.adornment = this.adornmentContent is { } content
                ? new Border { ClipToBounds = true, Child = content }
                : null;
        }

        this.EnsureAdornmentAttached();
    }

    private void EnsureAdornmentAttached()
    {
        if (this.adornment is not { } adorn)
        {
            return;
        }

        if (!this.LogicalChildren.Contains(adorn))
        {
            ((ISetLogicalParent)adorn).SetParent(this);
            this.LogicalChildren.Add(adorn);
        }

        if (!this.VisualChildren.Contains(adorn))
        {
            this.VisualChildren.Add(adorn);
        }

        this.InvalidateMeasure();
    }

    private void DetachAdornment()
    {
        if (this.adornment is not { } adorn)
        {
            return;
        }

        this.VisualChildren.Remove(adorn);
        this.LogicalChildren.Remove(adorn);
        ((ISetLogicalParent)adorn).SetParent(null);

        // Hand the consumer's control back, so the next header to serve this column can re-host it.
        if (adorn is Border { Child: not null } border)
        {
            border.Child = null;
        }

        this.adornment = null;
        this.adornmentContent = null;
        this.adornmentSource = null;
        this.adornmentNaturalWidth = 0;
        this.SetAndRaise(AdornmentWidthProperty, ref this.adornmentWidth, 0);
        this.SetAndRaise(IsAdornmentFillingProperty, ref this.isAdornmentFilling, false);
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
        var text = this.Content as string;
        Thickness padding = this.Padding;
        Thickness margin = this.InnerContentMargin;
        double availableWidth = finalSize.Width - padding.Left - padding.Right - margin.Left - margin.Right - this.adornmentWidth;
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
        var header = this.FindAncestorOfType<TreeDataGridColumnHeader>();
        return header is not null ? header : this;
    }

    private object? GetCustomToolTip()
    {
        if (this.GetColumnToolTip() is {} columnToolTip)
        {
            return columnToolTip;
        }

        return this.Content is Control control ? ToolTip.GetTip(control) : null;
    }

    private object? GetColumnToolTip() =>
        this.TryGetOwningColumn() is { } column ? DevoTreeDataGridExtensions.GetToolTip(column) : null;

    private TreeDataGridColumn? TryGetOwningColumn()
    {
        var header = this.FindAncestorOfType<TreeDataGridColumnHeader>();
        var treeDataGrid = header?.FindAncestorOfType<TreeDataGrid>();

        if (header?.ColumnIndex is not int columnIndex || treeDataGrid is null || columnIndex < 0 || columnIndex >= treeDataGrid.Columns.Count)
        {
            return null;
        }

        return treeDataGrid.Columns[columnIndex];
    }
}
