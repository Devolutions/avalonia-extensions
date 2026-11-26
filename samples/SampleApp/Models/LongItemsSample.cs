namespace SampleApp.Models;

using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Items for the "drop-down width with a few long items" demos: 200 short names, and a much longer one every 40.
/// A virtualized drop-down only builds the rows on screen, so its width changes as the long ones scroll into view.
/// </summary>
public static class LongItemsSample
{
    public static IReadOnlyList<string> Names { get; } = Enumerable.Range(1, 200)
        .Select(static i => i % 40 == 0 ? $"Item {i:000} - {new string('W', i / 4)}" : $"Item {i:000}")
        .ToList();
}
