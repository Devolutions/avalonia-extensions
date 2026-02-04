namespace Devolutions.AvaloniaControls.Controls;

using System.Collections;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Input;

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

    public static readonly StyledProperty<Thickness> LeftContentPaddingProperty =
        AvaloniaProperty.Register<ToggleButtonGroupItem, Thickness>(nameof(LeftContentPadding), new Thickness(0));

    public static readonly StyledProperty<Thickness> RightContentPaddingProperty =
        AvaloniaProperty.Register<ToggleButtonGroupItem, Thickness>(nameof(RightContentPadding), new Thickness(0));

    public IEnumerable InnerLeftContent { get; set; } = new AvaloniaList<Control>();

    public IEnumerable InnerRightContent { get; set; } = new AvaloniaList<Control>();

    public Thickness LeftContentPadding
    {
        get => this.GetValue(LeftContentPaddingProperty);
        set => this.SetValue(LeftContentPaddingProperty, value);
    }

    public Thickness RightContentPadding
    {
        get => this.GetValue(RightContentPaddingProperty);
        set => this.SetValue(RightContentPaddingProperty, value);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (this.Parent is not ToggleButtonGroup toggleButtonGroup)
        {
            return;
        }

        if (toggleButtonGroup.ItemClickCommand is { } command && command.CanExecute(this))
        {
            command.Execute(this);
        }
    }
}