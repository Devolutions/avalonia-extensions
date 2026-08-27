namespace Devolutions.AvaloniaControls.VisualTests;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using SampleApp;

/// <summary>
///   Covers the macOS rule that a contiguous multi-row selection is drawn as a single rounded
///   shape: only the ends of the run keep their rounding, and the rows between them are squared
///   off. The pseudo-classes come from <c>DataGridSelectionRunBehavior</c> and the radii from the
///   <c>DataGridRow</c> control theme, so these tests exercise both halves together.
/// </summary>
[Collection("VisualTests")]
public class DataGridSelectionRunTests
{
    private const double Radius = 5;

    private static readonly CornerRadius FullyRounded = new(Radius);

    private static readonly CornerRadius TopOnly = new(Radius, Radius, 0, 0);

    private static readonly CornerRadius BottomOnly = new(0, 0, Radius, Radius);

    private static readonly CornerRadius Squared = new(0);

    private sealed record Row(string Name);

    /// <summary>
    ///   Reloads the theme styles so the macOS DataGrid control theme resolves in this test.
    ///   Mirrors the reset other templated tests in this assembly perform.
    /// </summary>
    private static void ResetTheme()
    {
        App.CurrentTheme = null;
        App.SetTheme(new MacOsTheme());
    }

    private static (Window Window, DataGrid Grid, IReadOnlyList<Row> Items) CreateGrid(int rowCount)
    {
        List<Row> items = [.. Enumerable.Range(0, rowCount).Select(i => new Row($"Row {i}"))];

        var grid = new DataGrid
        {
            ItemsSource = items,
            SelectionMode = DataGridSelectionMode.Extended,
            AutoGenerateColumns = true
        };

        // Tall enough that every row is realized, so neighbour lookups are not cut short.
        var window = new Window { Content = grid, Width = 400, Height = 600 };

        return (window, grid, items);
    }

    private static IReadOnlyList<DataGridRow> RealizedRows(DataGrid grid) =>
        [.. grid.GetVisualDescendants().OfType<DataGridRow>().OrderBy(row => row.Index)];

    private static CornerRadius ItemBorderRadius(DataGridRow row) =>
        row.GetVisualDescendants().OfType<Border>().First(border => border.Name == "ItemBorder").CornerRadius;

    [AvaloniaFact]
    public void ContiguousSelectionRoundsOnlyTheEndsOfTheRun()
    {
        ResetTheme();

        (Window window, DataGrid grid, IReadOnlyList<Row> items) = CreateGrid(6);

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            grid.SelectedItems.Add(items[1]);
            grid.SelectedItems.Add(items[2]);
            grid.SelectedItems.Add(items[3]);
            Dispatcher.UIThread.RunJobs();

            IReadOnlyList<DataGridRow> rows = RealizedRows(grid);

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

        (Window window, DataGrid grid, IReadOnlyList<Row> items) = CreateGrid(6);

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            grid.SelectedItems.Add(items[2]);
            Dispatcher.UIThread.RunJobs();

            DataGridRow row = RealizedRows(grid)[2];

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

        (Window window, DataGrid grid, IReadOnlyList<Row> items) = CreateGrid(6);

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            // A gap between the two selected rows means two runs of one, not one run of two.
            grid.SelectedItems.Add(items[1]);
            grid.SelectedItems.Add(items[3]);
            Dispatcher.UIThread.RunJobs();

            IReadOnlyList<DataGridRow> rows = RealizedRows(grid);

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

        (Window window, DataGrid grid, IReadOnlyList<Row> items) = CreateGrid(6);

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            grid.SelectedItems.Add(items[1]);
            grid.SelectedItems.Add(items[2]);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(TopOnly, ItemBorderRadius(RealizedRows(grid)[1]));

            // Dropping back to a single selected row must restore the fully rounded radius
            // rather than leaving a squared-off end behind.
            grid.SelectedItems.Remove(items[2]);
            Dispatcher.UIThread.RunJobs();

            IReadOnlyList<DataGridRow> rows = RealizedRows(grid);

            Assert.DoesNotContain(":sel-run-first", rows[1].Classes);
            Assert.Equal(FullyRounded, ItemBorderRadius(rows[1]));
        }
        finally
        {
            window.Close();
        }
    }
}
