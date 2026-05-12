namespace Devolutions.AvaloniaControls.Controls;

using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls.Templates;

/// <summary>
/// Per-group data record consumed by the <see cref="GroupedListBox"/> template's inner
/// <see cref="Avalonia.Controls.ItemsControl"/>. Public so the compiled XAML bindings in the
/// control theme can resolve its properties at compile time.
/// </summary>
public sealed class GroupedListBoxGroupItem : AvaloniaObject
{
    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<GroupedListBoxGroupItem, bool>(nameof(IsExpanded), defaultValue: true);

    public GroupedListBoxGroupItem(
        string key,
        bool hasHeader,
        bool isExpanded,
        IReadOnlyList<object> items,
        IDataTemplate? itemTemplate)
    {
        this.Key = key;
        this.HasHeader = hasHeader;
        this.Items = items;
        this.IsExpanded = isExpanded;
        this.ItemTemplate = itemTemplate;
    }

    /// <summary>Group key string (empty for ungrouped fallback).</summary>
    public string Key { get; }

    /// <summary>Whether this group should render a header (false for ungrouped fallback / empty key).</summary>
    public bool HasHeader { get; }

    /// <summary>Items belonging to this group.</summary>
    public IReadOnlyList<object> Items { get; }

    /// <summary>Item template forwarded from the owning <see cref="GroupedListBox"/>.</summary>
    public IDataTemplate? ItemTemplate { get; }

    /// <summary>Expansion state. Two-way bound to the per-group <see cref="Avalonia.Controls.Expander"/>.</summary>
    public bool IsExpanded
    {
        get => this.GetValue(IsExpandedProperty);
        set => this.SetValue(IsExpandedProperty, value);
    }
}
