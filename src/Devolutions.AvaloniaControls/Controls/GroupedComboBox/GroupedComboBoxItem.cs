namespace Devolutions.AvaloniaControls.Controls;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Media;

/// <summary>
/// Container item used inside a <see cref="GroupedComboBox"/>.
/// </summary>
[PseudoClasses(":group-leader")]
public class GroupedComboBoxItem : ComboBoxItem
{
    public static readonly StyledProperty<bool> IsGroupLeaderProperty =
        AvaloniaProperty.Register<GroupedComboBoxItem, bool>(nameof(IsGroupLeader));

    public static readonly StyledProperty<string?> GroupKeyProperty =
        AvaloniaProperty.Register<GroupedComboBoxItem, string?>(nameof(GroupKey));

    public static readonly StyledProperty<string?> GroupHeaderTextProperty =
        AvaloniaProperty.Register<GroupedComboBoxItem, string?>(nameof(GroupHeaderText));

    public static readonly StyledProperty<IBrush?> GroupForegroundProperty =
        AvaloniaProperty.Register<GroupedComboBoxItem, IBrush?>(nameof(GroupForeground));

    public static readonly StyledProperty<Thickness> HeaderMarginProperty =
        AvaloniaProperty.Register<GroupedComboBoxItem, Thickness>(nameof(HeaderMargin));

    static GroupedComboBoxItem()
    {
        IsGroupLeaderProperty.Changed.AddClassHandler<GroupedComboBoxItem>((x, e) =>
            x.PseudoClasses.Set(":group-leader", e.GetNewValue<bool>()));
    }

    public bool IsGroupLeader
    {
        get => this.GetValue(IsGroupLeaderProperty);
        set => this.SetValue(IsGroupLeaderProperty, value);
    }

    public string? GroupKey
    {
        get => this.GetValue(GroupKeyProperty);
        set => this.SetValue(GroupKeyProperty, value);
    }

    public string? GroupHeaderText
    {
        get => this.GetValue(GroupHeaderTextProperty);
        set => this.SetValue(GroupHeaderTextProperty, value);
    }

    public IBrush? GroupForeground
    {
        get => this.GetValue(GroupForegroundProperty);
        set => this.SetValue(GroupForegroundProperty, value);
    }

    public Thickness HeaderMargin
    {
        get => this.GetValue(HeaderMarginProperty);
        set => this.SetValue(HeaderMarginProperty, value);
    }

    protected override System.Type StyleKeyOverride => typeof(GroupedComboBoxItem);
}
