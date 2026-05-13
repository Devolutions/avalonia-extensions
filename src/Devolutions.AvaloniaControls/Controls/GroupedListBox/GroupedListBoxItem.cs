namespace Devolutions.AvaloniaControls.Controls;

using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Media;

/// <summary>
/// Container item used inside a <see cref="GroupedListBox"/>. Extends <see cref="ListBoxItem"/>
/// with two optional content slots, <see cref="InnerLeftContent"/> and <see cref="InnerRightContent"/>,
/// rendered before and after the main content respectively.
/// <para>
/// Additionally exposes group-related state set by the owning <see cref="GroupedListBox"/>:
/// <see cref="IsGroupLeader"/> (renders the collapsible group header above the content),
/// <see cref="GroupKey"/>, <see cref="GroupHeaderText"/>, and <see cref="IsGroupExpanded"/>
/// (controls the visibility of the content row on non-leader siblings of the same group).
/// </para>
/// </summary>
[PseudoClasses(":group-leader", ":group-collapsed")]
public class GroupedListBoxItem : ListBoxItem
{
    public static readonly StyledProperty<object?> InnerLeftContentProperty =
        AvaloniaProperty.Register<GroupedListBoxItem, object?>(nameof(InnerLeftContent));

    public static readonly StyledProperty<object?> InnerRightContentProperty =
        AvaloniaProperty.Register<GroupedListBoxItem, object?>(nameof(InnerRightContent));

    public static readonly StyledProperty<bool> IsGroupLeaderProperty =
        AvaloniaProperty.Register<GroupedListBoxItem, bool>(nameof(IsGroupLeader));

    public static readonly StyledProperty<bool> IsGroupExpandedProperty =
        AvaloniaProperty.Register<GroupedListBoxItem, bool>(nameof(IsGroupExpanded), defaultValue: true);

    public static readonly StyledProperty<string?> GroupKeyProperty =
        AvaloniaProperty.Register<GroupedListBoxItem, string?>(nameof(GroupKey));

    public static readonly StyledProperty<string?> GroupHeaderTextProperty =
        AvaloniaProperty.Register<GroupedListBoxItem, string?>(nameof(GroupHeaderText));

    public static readonly StyledProperty<IBrush?> GroupForegroundProperty =
        AvaloniaProperty.Register<GroupedListBoxItem, IBrush?>(nameof(GroupForeground));

    /// <summary>Callback invoked when this leader's expander toggles. Set by owner.</summary>
    private Action<string, bool>? expansionCallback;

    /// <summary>Guard to suppress callback re-entry during owner-driven propagation.</summary>
    private bool suppressCallback;

    static GroupedListBoxItem()
    {
        IsGroupLeaderProperty.Changed.AddClassHandler<GroupedListBoxItem>((x, e) =>
            x.PseudoClasses.Set(":group-leader", e.GetNewValue<bool>()));
        IsGroupExpandedProperty.Changed.AddClassHandler<GroupedListBoxItem>((x, e) =>
        {
            bool expanded = e.GetNewValue<bool>();
            x.PseudoClasses.Set(":group-collapsed", !expanded);
            if (!x.suppressCallback && x.IsGroupLeader && x.GroupKey is { } key)
            {
                x.expansionCallback?.Invoke(key, expanded);
            }
        });
    }

    /// <summary>Leading content slot (e.g. an icon). Hidden when null.</summary>
    public object? InnerLeftContent
    {
        get => this.GetValue(InnerLeftContentProperty);
        set => this.SetValue(InnerLeftContentProperty, value);
    }

    /// <summary>Trailing content slot (e.g. an accessory glyph). Hidden when null.</summary>
    public object? InnerRightContent
    {
        get => this.GetValue(InnerRightContentProperty);
        set => this.SetValue(InnerRightContentProperty, value);
    }

    /// <summary>True when this container is the first item in its group; renders the group header.</summary>
    public bool IsGroupLeader
    {
        get => this.GetValue(IsGroupLeaderProperty);
        set => this.SetValue(IsGroupLeaderProperty, value);
    }

    /// <summary>Two-way: drives the leader's Expander; mirrored to non-leader siblings by the owner.</summary>
    public bool IsGroupExpanded
    {
        get => this.GetValue(IsGroupExpandedProperty);
        set => this.SetValue(IsGroupExpandedProperty, value);
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

    /// <summary>
    /// Called by the owning <see cref="GroupedListBox"/> to install a callback for leader
    /// expansion changes. Pass <see langword="null"/> for non-leader containers.
    /// </summary>
    internal void SetGroupExpansionCallback(Action<string, bool>? callback) =>
        this.expansionCallback = callback;

    /// <summary>Sets <see cref="IsGroupExpanded"/> without re-invoking the leader callback.</summary>
    internal void SetGroupExpandedSilently(bool expanded)
    {
        this.suppressCallback = true;
        try { this.IsGroupExpanded = expanded; }
        finally { this.suppressCallback = false; }
    }
}
