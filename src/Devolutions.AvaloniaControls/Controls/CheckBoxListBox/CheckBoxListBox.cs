namespace Devolutions.AvaloniaControls.Controls;

using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Metadata;

[TemplatePart("PART_InnerCheckBoxListBox", typeof(InnerCheckBoxListBox), IsRequired = true)]
public partial class CheckBoxListBox : ItemsControl
{
    public static readonly DirectProperty<CheckBoxListBox, IList?> SelectedItemsProperty =
        AvaloniaProperty.RegisterDirect<CheckBoxListBox, IList?>(
            nameof(SelectedItems),
            o => o.SelectedItems,
            (o, v) => o.SelectedItems = v);

    private readonly InnerCheckBoxListBox inner = new()
    {
        Name = "PART_InnerCheckBoxListBox",
        [~BackgroundProperty] = new TemplateBinding(BackgroundProperty),
        [~BackgroundSizingProperty] = new TemplateBinding(BackgroundSizingProperty),
        [~BorderBrushProperty] = new TemplateBinding(BorderBrushProperty),
        [~BorderThicknessProperty] = new TemplateBinding(BorderThicknessProperty),
        [~CornerRadiusProperty] = new TemplateBinding(CornerRadiusProperty),
        [~PaddingProperty] = new TemplateBinding(PaddingProperty),
        [~ItemsSourceProperty] = new TemplateBinding(ItemsSourceProperty),
        [~ListBox.SelectedItemsProperty] = new TemplateBinding(SelectedItemsProperty),
        HorizontalAlignment = HorizontalAlignment.Stretch,
        VerticalAlignment = VerticalAlignment.Stretch,
    };

    public CheckBoxListBox()
    {
        this.Template = new FuncControlTemplate((_, ns) =>
        {
            this.inner.RegisterInNameScope(ns);
            return this.inner;
        });
    }

    [Content]
    public new ItemCollection Items => this.inner.Items;

    public IList? SelectedItems
    {
        get => this.inner.SelectedItems;
        set => this.inner.SelectedItems = value;
    }
}