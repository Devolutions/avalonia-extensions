namespace Devolutions.AvaloniaControls.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;

using Devolutions.AvaloniaControls.Controls;

using Extensions;

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
    /// <param name="orderSelector">Optional ordering key for a group, evaluated against a representative item
    /// of that group. The returned value is compared as-is (keep it a raw comparable such as an <see cref="int"/>
    /// so numeric orders sort numerically). When <c>null</c>, groups keep their first-appearance order
    /// (unless <paramref name="orderAlphabetical"/> is set).</param>
    /// <param name="orderAlphabetical">Order groups alphabetically (after <paramref name="orderSelector"/>, if any).</param>
    /// <param name="emptyGroupName">Display name for the empty ("") group key. When <c>null</c>/empty,
    /// items in the empty group render without a header (e.g. the current item shown at the top).</param>
    /// <param name="headerFactory">Optional factory for the header pseudo-item, given the group's display name
    /// and its full ordered membership. When <c>null</c>, a plain <see cref="ComboBoxGroupHeader"/> (text only)
    /// is produced. Callers that need the header to carry its group's items (e.g. a group-level select-all
    /// checkbox) supply a factory here.</param>
    /// <param name="itemFilter">Optional per-item display filter. When supplied, <paramref name="sourceItems"/>
    /// is treated as the full, unfiltered set: each group's header still carries the full membership, but only
    /// items passing the filter are emitted into the display list, and a group with no visible items is dropped
    /// entirely (its header included). When <c>null</c>, every item is emitted (the original behavior).</param>
    public static List<object> Build(
        IEnumerable<object?> sourceItems,
        Func<object, string> groupSelector,
        Func<object, object?>? orderSelector,
        bool orderAlphabetical,
        string? emptyGroupName,
        Func<string, IReadOnlyList<object>, object>? headerFactory = null,
        Func<object, bool>? itemFilter = null)
    {
        headerFactory ??= static (name, _) => new ComboBoxGroupHeader(name);

        // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
        IEnumerable<IGrouping<string, (object item, string groupName, int originalIndex)>> grouped = sourceItems
            .SkipNulls()
            .Select((x, i) => (item: x, groupName: groupSelector(x) ?? string.Empty, originalIndex: i))
            .GroupBy(static x => x.groupName);
        grouped = (orderSelector, orderAlphabetical) switch
        {
            ({ } fn, true) => grouped.OrderBy(g => fn(g.First().item)).ThenBy(static g => g.Key, StringComparer.Ordinal),
            ({ } fn, false) => grouped.OrderBy(g => fn(g.First().item)),
            (null, true) => grouped.OrderBy(static g => g.Key, StringComparer.Ordinal),
            (null, false) => grouped,
        };

        sourceItems.TryGetNonEnumeratedCount(out var itemsCount);
        grouped.TryGetNonEnumeratedCount(out var groupedCount);
        List<object> displayItems = new(itemsCount + groupedCount);
        bool hasEmptyName = string.IsNullOrEmpty(emptyGroupName);
        foreach (IGrouping<string, (object item, string groupName, int originalIndex)> group in grouped)
        {
            List<object> fullItems = group.OrderBy(static x => x.originalIndex).Select(static x => x.item).ToList();
            List<object> visibleItems = itemFilter is null
                ? fullItems
                : fullItems.Where(itemFilter).ToList();

            // When filtering, a group with no visible items drops out entirely (its header included).
            if (itemFilter is not null && visibleItems.Count == 0)
            {
                continue;
            }

            bool isEmptyHeaderless = group.Key.Length == 0 && hasEmptyName;
            if (!isEmptyHeaderless)
            {
                string displayName = group.Key.Length == 0 ? emptyGroupName! : group.Key;
                displayItems.Add(headerFactory(displayName, fullItems));
            }

            displayItems.AddRange(visibleItems);
        }

        return displayItems;
    }
}
