namespace Devolutions.AvaloniaControls.Controls;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

/// <summary>
/// Non-selectable header container inserted into a <see cref="GroupedComboBox"/> popup.
/// </summary>
public class GroupedComboBoxHeaderItem : ComboBoxItem
{
    public static readonly StyledProperty<string?> HeaderTextProperty =
        AvaloniaProperty.Register<GroupedComboBoxHeaderItem, string?>(nameof(HeaderText));

    public static readonly StyledProperty<IBrush?> HeaderForegroundProperty =
        AvaloniaProperty.Register<GroupedComboBoxHeaderItem, IBrush?>(nameof(HeaderForeground));

    public static readonly StyledProperty<Thickness> HeaderMarginProperty =
        AvaloniaProperty.Register<GroupedComboBoxHeaderItem, Thickness>(nameof(HeaderMargin), new Thickness(4, 6, 8, 4), inherits: true);

    public GroupedComboBoxHeaderItem()
    {
        this.Focusable = false;
        this.IsEnabled = false;
    }

    public string? HeaderText
    {
        get => this.GetValue(HeaderTextProperty);
        set => this.SetValue(HeaderTextProperty, value);
    }

    public IBrush? HeaderForeground
    {
        get => this.GetValue(HeaderForegroundProperty);
        set => this.SetValue(HeaderForegroundProperty, value);
    }

    public Thickness HeaderMargin
    {
        get => this.GetValue(HeaderMarginProperty);
        set => this.SetValue(HeaderMarginProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(GroupedComboBoxHeaderItem);
}
