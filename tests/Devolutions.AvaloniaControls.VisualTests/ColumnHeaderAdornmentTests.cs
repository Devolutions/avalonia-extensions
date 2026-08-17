namespace Devolutions.AvaloniaControls.VisualTests;

using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Devolutions.AvaloniaControls.Controls;
using SampleApp;
using SampleApp.DemoPages;
using SampleApp.ViewModels;

// Covers DevoTreeDataGridExtensions.HeaderRightAdornment. The load-bearing guarantee is that the
// adornment is excluded from the header's desired width, so an Auto-width column never resizes when
// the adornment appears or changes size (DEVEX-303: committing a column search must not shift columns).
[Collection("VisualTests")]
public class ColumnHeaderAdornmentTests
{
    private const double Tolerance = 0.5;

    [AvaloniaTheory]
    [InlineData("MacClassic")]
    [InlineData("DevExpress")]
    [InlineData("Linux")]
    public void Adornment_DoesNotResizeAutoWidthColumn(string themeName)
    {
        SetTheme(themeName);

        Border adornment = new() { Width = 120, Height = 12, Background = Brushes.Red, IsVisible = false };
        TreeDataGrid grid = BuildGrid("Name", GridLength.Auto, adornment);
        Window window = Show(grid);

        double hidden = ColumnWidth(grid);

        adornment.IsVisible = true;
        double shown = ColumnWidth(grid);

        adornment.Width = 260;
        double wider = ColumnWidth(grid);

        window.Close();

        Assert.Equal(hidden, shown, Tolerance);
        Assert.Equal(hidden, wider, Tolerance);
    }

    [AvaloniaTheory]
    [InlineData("MacClassic")]
    [InlineData("DevExpress")]
    [InlineData("Linux")]
    public void Adornment_ReportsMeasuredWidth(string themeName)
    {
        SetTheme(themeName);

        Border adornment = new() { Width = 120, Height = 12, Background = Brushes.Red, IsVisible = false };
        TreeDataGrid grid = BuildGrid("Name", new GridLength(300), adornment);
        Window window = Show(grid);

        double whenHidden = OverflowHeader(grid).AdornmentWidth;

        adornment.IsVisible = true;
        Layout(grid);
        double whenShown = OverflowHeader(grid).AdornmentWidth;

        window.Close();

        Assert.Equal(0, whenHidden, Tolerance);
        Assert.Equal(120, whenShown, Tolerance);
    }

    [AvaloniaTheory]
    [InlineData("MacClassic")]
    [InlineData("DevExpress")]
    [InlineData("Linux")]
    public void Adornment_TrimsCaptionIntoRemainingSpace(string themeName)
    {
        SetTheme(themeName);

        // Sized so the caption fits easily in a 1000px column, yet cannot once 600px is taken away, while
        // leaving the remaining width comfortably positive on every theme's header chrome.
        Border adornment = new() { Width = 800, Height = 12, Background = Brushes.Red, IsVisible = false };
        TreeDataGrid grid = BuildGrid(
            "A considerably longer header caption that keeps going and going and going and still keeps going for a while yet",
            new GridLength(1000),
            adornment);

        Window window = Show(grid, width: 1600);

        bool overflowingBefore = IsOverflowing(grid);

        adornment.IsVisible = true;
        Layout(grid);
        bool overflowingAfter = IsOverflowing(grid);

        TreeDataGridOverflowHeader overflowHeader = OverflowHeader(grid);
        double adornmentRight = adornment.Bounds.Right;
        double headerRight = overflowHeader.Bounds.Width;

        window.Close();

        Assert.False(overflowingBefore);
        Assert.True(overflowingAfter);
        Assert.Equal(headerRight, adornmentRight, Tolerance);
    }

    [AvaloniaTheory]
    [InlineData("MacClassic")]
    [InlineData("DevExpress")]
    [InlineData("Linux")]
    public void StringHeader_StillSizesAutoWidthColumn(string themeName)
    {
        // Guards the pre-existing behaviour the adornment must not disturb: an Auto column still grows
        // to fit its caption. Icon-only Control headers rely on the same contribution.
        SetTheme(themeName);

        TreeDataGrid shortGrid = BuildGrid("X", GridLength.Auto, adornment: null);
        Window shortWindow = Show(shortGrid);
        double shortWidth = ColumnWidth(shortGrid);
        shortWindow.Close();

        SetTheme(themeName);

        TreeDataGrid longGrid = BuildGrid("A considerably longer header caption", GridLength.Auto, adornment: null);
        Window longWindow = Show(longGrid);
        double longWidth = ColumnWidth(longGrid);
        longWindow.Close();

        Assert.True(longWidth > shortWidth + 50, $"expected the caption to widen the column, got {shortWidth} -> {longWidth}");
    }

    [AvaloniaTheory]
    [InlineData("MacClassic")]
    [InlineData("DevExpress")]
    [InlineData("Linux")]
    public void Demo_IndicatorTracksSearchTermWithoutResizingColumn(string themeName)
    {
        SetTheme(themeName);

        TreeDataGridColumnSearchViewModel viewModel = new();
        TreeDataGridColumnSearchDemo page = new() { DataContext = viewModel };

        Window window = new()
        {
            Width = 1100,
            Height = 700,
            Content = page,
        };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        TreeDataGrid grid = page.GetVisualDescendants().OfType<TreeDataGrid>().First();

        double widthBefore = ColumnWidth(grid);
        Border chip = SearchChip(grid);
        bool visibleBefore = chip.IsVisible;

        viewModel.NameSearch = "a-deliberately-long-search-term";
        Layout(grid);

        double widthAfter = ColumnWidth(grid);
        bool visibleAfter = chip.IsVisible;
        string chipText = chip.GetVisualDescendants().OfType<TextBlock>().First().Text ?? string.Empty;
        double adornmentWidth = OverflowHeader(grid).AdornmentWidth;

        window.Close();

        Assert.False(visibleBefore);
        Assert.True(visibleAfter);
        Assert.Equal("a-deliberately-long-search-term", chipText);
        Assert.True(adornmentWidth > 0, "the visible indicator should report a non-zero width");
        Assert.Equal(widthBefore, widthAfter, Tolerance);
    }

    private static Border SearchChip(TreeDataGrid grid) =>
        Header(grid).GetVisualDescendants().OfType<Border>().First(static b => b.Classes.Contains("search-chip"));

    private static void SetTheme(string themeName)
    {
        App.CurrentTheme = null;
        App.SetTheme(themeName switch
        {
            "MacClassic" => new MacOsClassicTheme(),
            "Linux" => new LinuxYaruTheme(),
            "DevExpress" => new DevExpressTheme(),
            _ => throw new ArgumentException($"Unknown theme: {themeName}"),
        });
    }

    private static TreeDataGrid BuildGrid(object header, GridLength columnWidth, Control? adornment)
    {
        TreeDataGrid grid = new() { CanUserSortColumns = true };

        TreeDataGridTemplateColumn column = new()
        {
            Header = header,
            Width = columnWidth,
            CellTemplate = CellTemplate(),
        };

        grid.Columns.Add(column);
        grid.Columns.Add(new TreeDataGridTemplateColumn
        {
            Header = "Filler",
            Width = new GridLength(1, GridUnitType.Star),
            CellTemplate = CellTemplate(),
        });

        if (adornment is not null)
        {
            DevoTreeDataGridExtensions.SetHeaderRightAdornment(grid.Columns[0], adornment);
        }

        List<string> items = ["alpha", "beta", "gamma"];
        grid.ItemsSource = items;

        return grid;
    }

    private static Window Show(TreeDataGrid grid, double width = 700)
    {
        Window window = new()
        {
            Width = width,
            Height = 300,
            Content = grid,
        };

        window.Show();
        Layout(grid);

        return window;
    }

    private static FuncDataTemplate<object> CellTemplate() =>
        new((_, _) => new TextBlock { Text = "cell" }, true);

    private static void Layout(TreeDataGrid grid)
    {
        Dispatcher.UIThread.RunJobs();
        grid.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }

    private static TreeDataGridColumnHeader Header(TreeDataGrid grid) =>
        grid.GetVisualDescendants()
            .OfType<TreeDataGridColumnHeader>()
            .First(static h => h.ColumnIndex == 0);

    private static double ColumnWidth(TreeDataGrid grid)
    {
        Layout(grid);
        return Header(grid).Bounds.Width;
    }

    private static TreeDataGridOverflowHeader OverflowHeader(TreeDataGrid grid) =>
        Header(grid).GetVisualDescendants().OfType<TreeDataGridOverflowHeader>().First();

    private static bool IsOverflowing(TreeDataGrid grid) =>
        OverflowHeader(grid).Classes.Contains(":is-overflowing");
}
