#if ENABLE_ACCELERATE
namespace Devolutions.AvaloniaControls.VisualTests;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Selection;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using SampleApp;

/// <summary>
///   Covers the macOS rule that a contiguous multi-row selection is drawn as a single rounded
///   shape: only the ends of the run keep their rounding, and the rows between them are squared
///   off. The pseudo-classes come from <c>TreeDataGridSelectionRunBehavior</c> and the radii from
///   the <c>TreeDataGridRow</c> control theme, so these tests exercise both halves together.
/// </summary>
[Collection("VisualTests")]
public class TreeDataGridSelectionRunTests
{
    private const double Radius = 5;

    private static readonly CornerRadius FullyRounded = new(Radius);

    private static readonly CornerRadius TopOnly = new(Radius, Radius, 0, 0);

    private static readonly CornerRadius BottomOnly = new(0, 0, Radius, Radius);

    private static readonly CornerRadius Squared = new(0);

    private sealed record Node(string Name);

    /// <summary>
    ///   Reloads the theme styles so the macOS TreeDataGrid control theme resolves in this test.
    ///   Mirrors the reset other templated tests in this assembly perform.
    /// </summary>
    private static void ResetTheme()
    {
        App.CurrentTheme = null;
        App.SetTheme(new MacOsTheme());
    }

    private static (Window Window, TreeDataGrid Grid, FlatTreeDataGridSource<Node> Source) CreateGrid(int rowCount)
    {
        List<Node> items = [.. Enumerable.Range(0, rowCount).Select(i => new Node($"Row {i}"))];

        var source = new FlatTreeDataGridSource<Node>(items);
        source.WithTextColumn("Name", x => x.Name);
        source.Selection = new TreeDataGridRowSelectionModel<Node>(source) { SingleSelect = false };

        var grid = new TreeDataGrid { Source = source };

        // Tall enough that every row is realized, so neighbour lookups are not cut short.
        var window = new Window { Content = grid, Width = 400, Height = 600 };

        return (window, grid, source);
    }

    private static IReadOnlyList<TreeDataGridRow> RealizedRows(TreeDataGrid grid) =>
        [.. grid.GetVisualDescendants().OfType<TreeDataGridRow>().OrderBy(row => row.RowIndex)];

    private static CornerRadius ItemBorderRadius(TreeDataGridRow row) =>
        row.GetVisualDescendants().OfType<Border>().First(border => border.Name == "ItemBorder").CornerRadius;

    private static void Select(FlatTreeDataGridSource<Node> source, params int[] indexes)
    {
        foreach (int index in indexes)
        {
            source.RowSelection!.Select(new IndexPath(index));
        }
    }

    [AvaloniaFact]
    public void ContiguousSelectionRoundsOnlyTheEndsOfTheRun()
    {
        ResetTheme();

        (Window window, TreeDataGrid grid, FlatTreeDataGridSource<Node> source) = CreateGrid(6);

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            Select(source, 1, 2, 3);
            Dispatcher.UIThread.RunJobs();

            IReadOnlyList<TreeDataGridRow> rows = RealizedRows(grid);

            Assert.True(rows.Count >= 6, $"expected all 6 rows realized, got {rows.Count}");

            Assert.DoesNotContain(":sel-run-first", rows[0].Classes);
            Assert.Contains(":sel-run-first", rows[1].Classes);
            Assert.Contains(":sel-run-middle", rows[2].Classes);
            Assert.Contains(":sel-run-last", rows[3].Classes);
            Assert.DoesNotContain(":sel-run-last", rows[4].Classes);

            // The run must read as one shape: rounded at the top, square through the middle,
            // rounded at the bottom.
            Assert.Equal(TopOnly, ItemBorderRadius(rows[1]));
            Assert.Equal(Squared, ItemBorderRadius(rows[2]));
            Assert.Equal(BottomOnly, ItemBorderRadius(rows[3]));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void LoneSelectedRowStaysFullyRounded()
    {
        ResetTheme();

        (Window window, TreeDataGrid grid, FlatTreeDataGridSource<Node> source) = CreateGrid(6);

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            Select(source, 2);
            Dispatcher.UIThread.RunJobs();

            TreeDataGridRow row = RealizedRows(grid)[2];

            Assert.DoesNotContain(":sel-run-first", row.Classes);
            Assert.DoesNotContain(":sel-run-middle", row.Classes);
            Assert.DoesNotContain(":sel-run-last", row.Classes);
            Assert.Equal(FullyRounded, ItemBorderRadius(row));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void SeparatedSelectionsEachStayFullyRounded()
    {
        ResetTheme();

        (Window window, TreeDataGrid grid, FlatTreeDataGridSource<Node> source) = CreateGrid(6);

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            // A gap between the two selected rows means two runs of one, not one run of two.
            Select(source, 1, 3);
            Dispatcher.UIThread.RunJobs();

            IReadOnlyList<TreeDataGridRow> rows = RealizedRows(grid);

            Assert.Equal(FullyRounded, ItemBorderRadius(rows[1]));
            Assert.Equal(FullyRounded, ItemBorderRadius(rows[3]));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ShrinkingSelectionClearsRunClasses()
    {
        ResetTheme();

        (Window window, TreeDataGrid grid, FlatTreeDataGridSource<Node> source) = CreateGrid(6);

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            Select(source, 1, 2);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(TopOnly, ItemBorderRadius(RealizedRows(grid)[1]));

            // Dropping back to a single selected row must restore the fully rounded radius
            // rather than leaving a squared-off end behind.
            source.RowSelection!.Deselect(new IndexPath(2));
            Dispatcher.UIThread.RunJobs();

            IReadOnlyList<TreeDataGridRow> rows = RealizedRows(grid);

            Assert.DoesNotContain(":sel-run-first", rows[1].Classes);
            Assert.Equal(FullyRounded, ItemBorderRadius(rows[1]));
        }
        finally
        {
            window.Close();
        }
    }
}
#endif
