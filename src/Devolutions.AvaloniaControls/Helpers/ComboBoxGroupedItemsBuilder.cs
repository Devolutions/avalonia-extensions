namespace Devolutions.AvaloniaControls.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;

using Devolutions.AvaloniaControls.Controls;

/// <summary>
/// Builds a flat display list from source items by grouping them and interleaving
/// non-selectable <see cref="ComboBoxGroupHeader"/> pseudo-items. Shared by <see cref="GroupedComboBox"/>
/// and <see cref="EditableComboBox"/> so both controls group identically.
/// </summary>
internal static class ComboBoxGroupedItemsBuilder
{
    /// <summary>
    /// Groups <paramref name="sourceItems"/> using <paramref name="groupSelector"/>, orders the
    /// groups, then returns a flat list where each group is preceded by a <see cref="ComboBoxGroupHeader"/>.
    /// Items keep their original relative order within a group.
    /// </summary>
    /// <param name="sourceItems">The raw source items, in their original order. <c>null</c> entries are skipped.</param>
    /// <param name="groupSelector">Produces the group key for an item.</param>
    /// <param name="orderSelector">Optional custom ordering key for a group name. When <c>null</c>,
    /// groups keep their first-appearance order (unless <paramref name="orderAlphabetical"/> is set).</param>
    /// <param name="orderAlphabetical">Order groups alphabetically (after <paramref name="orderSelector"/>, if any).</param>
    /// <param name="emptyGroupName">Display name for the empty ("") group key. When <c>null</c>/empty,
    /// items in the empty group render without a header (e.g. the current item shown at the top).</param>
    public static List<object> Build(
        IEnumerable<object?> sourceItems,
        Func<object, string> groupSelector,
        Func<string, object?>? orderSelector,
        bool orderAlphabetical,
        string? emptyGroupName)
    {
        List<(object item, string groupName, int originalIndex)> source =
            sourceItems.TryGetNonEnumeratedCount(out int count) ? new(count) : [];
        int originalIndex = 0;
        foreach (object? item in sourceItems)
        {
            if (item is not null)
            {
                // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
                source.Add((item, groupSelector(item) ?? string.Empty, originalIndex));
            }

            // Increment for every entry (including nulls) so originalIndex reflects the true
            // position in the source collection, not the compacted position.
            ++originalIndex;
        }

        IEnumerable<IGrouping<string, (object item, string groupName, int originalIndex)>> grouped = source.GroupBy(static x => x.groupName);
        grouped = (orderSelector, orderAlphabetical) switch
        {
            ({ } fn, true) => grouped.OrderBy(g => fn(g.Key)).ThenBy(static g => g.Key, StringComparer.Ordinal),
            ({ } fn, false) => grouped.OrderBy(g => fn(g.Key)),
            (null, true) => grouped.OrderBy(static g => g.Key, StringComparer.Ordinal),
            (null, false) => grouped,
        };

        grouped.TryGetNonEnumeratedCount(out var groupedCount);
        List<object> displayItems = new(source.Count + groupedCount);
        bool hasEmptyName = string.IsNullOrEmpty(emptyGroupName);
        foreach (IGrouping<string, (object item, string groupName, int originalIndex)> group in grouped)
        {
            bool isEmptyHeaderless = group.Key.Length == 0 && hasEmptyName;
            if (!isEmptyHeaderless)
            {
                displayItems.Add(new ComboBoxGroupHeader(group.Key.Length == 0 ? emptyGroupName : group.Key));
            }

            foreach ((object item, _, _) in group.OrderBy(static x => x.originalIndex))
            {
                displayItems.Add(item);
            }
        }

        return displayItems;
    }
}
