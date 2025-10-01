namespace Devolutions.AvaloniaControls.Controls;

using System.Collections;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;

[TemplatePart("PART_InnerLeftContent", typeof(ItemsControl), IsRequired = true)]
[TemplatePart("PART_InnerRightContent", typeof(ItemsControl), IsRequired = true)]
[TemplatePart("PART_ContentPresenter", typeof(ContentPresenter), IsRequired = true)]
public class ToggleButtonGroupItem : ListBoxItem
{
    public static readonly DirectProperty<ToggleButtonGroupItem, IEnumerable> InnerLeftContentProperty =
        AvaloniaProperty.RegisterDirect<ToggleButtonGroupItem, IEnumerable>(
            nameof(InnerLeftContent),
            static o => o.InnerLeftContent,
            static (o, v) => o.InnerLeftContent = v);

    public static readonly DirectProperty<ToggleButtonGroupItem, IEnumerable> InnerRightContentProperty =
        AvaloniaProperty.RegisterDirect<ToggleButtonGroupItem, IEnumerable>(
            nameof(InnerRightContent),
            static o => o.InnerRightContent,
            static (o, v) => o.InnerRightContent = v);

    public IEnumerable InnerLeftContent { get; set; } = new AvaloniaList<Control>();

    public IEnumerable InnerRightContent { get; set; } = new AvaloniaList<Control>();
}