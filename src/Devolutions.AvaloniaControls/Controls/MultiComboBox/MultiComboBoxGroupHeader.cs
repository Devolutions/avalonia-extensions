namespace Devolutions.AvaloniaControls.Controls;

using System.Collections.Generic;

/// <summary>
/// Non-selectable header pseudo-item interleaved into a <see cref="MultiComboBox"/> drop-down.
/// Unlike <see cref="ComboBoxGroupHeader"/> (text only), it also carries its group's full,
/// unfiltered membership so the header's leading check box can select/deselect the whole group
/// and reflect a mixed (indeterminate) state — even while the drop-down shows only a filtered subset.
/// </summary>
internal sealed class MultiComboBoxGroupHeader
{
    public MultiComboBoxGroupHeader(string? text, IReadOnlyList<object> items)
    {
        this.Text = text;
        this.Items = items;
    }

    public string? Text { get; }

    /// <summary>The full (unfiltered) set of items belonging to this group.</summary>
    public IReadOnlyList<object> Items { get; }

    public override string? ToString() => this.Text;
}
