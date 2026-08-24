namespace Devolutions.AvaloniaControls.VisualTests;

using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Devolutions.AvaloniaControls.Controls;
using SampleApp;

// Covers DevoTreeDataGridExtensions.HeaderAdornment and HeaderAdornmentPosition. The load-bearing
// guarantee is that the adornment is excluded from the header's desired width, so an Auto-width column
// never resizes when it appears or changes size (DEVEX-303: committing a column search must not shift
// columns).
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

        // The adornment is hosted in an internal clipping border, so its own Bounds are relative to that
        // wrapper; translate into the header's space to check it really is flush right.
        Point? adornmentRight = adornment.TranslatePoint(new Point(adornment.Bounds.Width, 0), overflowHeader);
        double headerRight = overflowHeader.Bounds.Width;

        window.Close();

        Assert.False(overflowingBefore);
        Assert.True(overflowingAfter);
        Assert.NotNull(adornmentRight);
        Assert.Equal(headerRight, adornmentRight!.Value.X, Tolerance);
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
    public void ControlHeader_SizesAutoWidthColumn_FromInitialLayout(string themeName)
    {
        SetTheme(themeName);

        Border narrow = new() { Width = 20, Height = 12, Background = Brushes.Red };
        TreeDataGrid narrowGrid = BuildGrid(narrow, GridLength.Auto, adornment: null);
        Window narrowWindow = Show(narrowGrid);
        double narrowWidth = ColumnWidth(narrowGrid);
        narrowWindow.Close();

        SetTheme(themeName);

        Border wide = new() { Width = 200, Height = 12, Background = Brushes.Red };
        TreeDataGrid wideGrid = BuildGrid(wide, GridLength.Auto, adornment: null);
        Window wideWindow = Show(wideGrid);
        double wideWidth = ColumnWidth(wideGrid);
        wideWindow.Close();

        Assert.True(
            wideWidth - narrowWidth > 150,
            $"a 200px Control header should give a wider Auto column than a 20px one, got {narrowWidth} vs {wideWidth}");
    }

    // DevExpress is deliberately excluded. Its caption is a sibling of HeaderBorder, so that interactive
    // header content works without the full-width border claiming the resize-thumb lane, and in that
    // arrangement a *runtime* size change latches: HeaderContent's DesiredSize is clamped to the width it
    // was offered, so it never changes, so the invalidation stops propagating and TreeDataGrid never
    // recomputes the Auto width. Initial layout is unaffected on all three themes (see the test above),
    // and the search adornment is excluded from desired width by design, so column search does not rely
    // on this. Revisit if a consumer needs a Control header that resizes itself after first layout.
    [AvaloniaTheory]
    [InlineData("MacClassic")]
    [InlineData("Linux")]
    public void ControlHeader_RegrowsAutoWidthColumnAfterResize(string themeName)
    {
        SetTheme(themeName);

        Border header = new() { Width = 20, Height = 12, Background = Brushes.Red };
        TreeDataGrid grid = BuildGrid(header, GridLength.Auto, adornment: null);
        Window window = Show(grid);

        double before = ColumnWidth(grid);
        header.Width = 200;
        double after = ColumnWidth(grid);

        window.Close();

        Assert.True(
            after - before > 150,
            $"a Control header growing 20 -> 200 should widen the Auto column, got {before} -> {after}");
    }

    [AvaloniaTheory]
    [InlineData("MacClassic")]
    [InlineData("DevExpress")]
    [InlineData("Linux")]
    public void Adornment_IsClampedToTheHeaderWidth(string themeName)
    {
        SetTheme(themeName);

        // Width comes from the text, as a real committed-search chip's does, and far exceeds the column.
        // An explicitly-sized control would keep its Width regardless of the arrange slot and only be
        // visually clipped, which would not tell us whether the clamp works.
        TextBlock term = new()
        {
            Text = "a deliberately long committed search term that cannot possibly fit",
            TextTrimming = TextTrimming.CharacterEllipsis,
        };

        Border adornment = new() { Background = Brushes.Red, Child = term };
        TreeDataGrid grid = BuildGrid("Name", new GridLength(200), adornment);
        Window window = Show(grid);

        TreeDataGridOverflowHeader overflowHeader = OverflowHeader(grid);
        double headerWidth = overflowHeader.Bounds.Width;
        double reportedWidth = overflowHeader.AdornmentWidth;
        double arrangedWidth = adornment.Bounds.Width;

        window.Close();

        Assert.True(headerWidth > 0, "the header should have been laid out");
        Assert.True(
            reportedWidth <= headerWidth + Tolerance,
            $"AdornmentWidth {reportedWidth} should be clamped to the header width {headerWidth}");
        Assert.True(
            arrangedWidth <= headerWidth + Tolerance,
            $"arranged width {arrangedWidth} should be clamped to the header width {headerWidth}");
    }

    [AvaloniaTheory]
    [InlineData("MacClassic")]
    [InlineData("DevExpress")]
    [InlineData("Linux")]
    public void Adornment_LeavesTheSortIndicatorLaneOnASortedColumn(string themeName)
    {
        // Regression: clamping only to the header width let a long Right adornment take the whole header, so
        // the theme's -AdornmentWidth shift pushed the sort indicator out of the header and into its
        // neighbour. Adornment_IsClampedToTheHeaderWidth never caught it because an unsorted column
        // reserves no lane.
        SetTheme(themeName);

        TextBlock term = new()
        {
            Text = "a deliberately long committed search term that cannot possibly fit",
            TextTrimming = TextTrimming.CharacterEllipsis,
        };

        // Not hit-testable: the adornment covers the caption at this width, and the sort below needs the
        // click to reach the header. Only layout is under test here.
        Border adornment = new() { Background = Brushes.Red, Child = term, IsHitTestVisible = false };

        TreeDataGrid grid = BuildSortableGrid();
        DevoTreeDataGridExtensions.SetHeaderAdornment(grid.Columns[0], adornment);
        Window window = Show(grid, width: 900);

        TreeDataGridColumnHeader header = Header(grid);
        Point? captionPoint = header.TranslatePoint(new Point(30, header.Bounds.Height / 2), window);

        Assert.NotNull(captionPoint);

        window.MouseDown(captionPoint!.Value, MouseButton.Left);
        window.MouseUp(captionPoint.Value, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        grid.UpdateLayout();

        TreeDataGridOverflowHeader overflowHeader = OverflowHeader(grid);
        object? sortDirection = Header(grid).SortDirection;
        double headerWidth = overflowHeader.Bounds.Width;
        double lane = overflowHeader.InnerContentMargin.Right;
        double reportedWidth = overflowHeader.AdornmentWidth;

        window.Close();

        Assert.NotNull(sortDirection);
        Assert.True(lane > 0, "a sorted column should reserve an indicator lane through InnerContentMargin");
        Assert.True(
            reportedWidth <= headerWidth - lane + Tolerance,
            $"AdornmentWidth {reportedWidth} should leave the {lane}px indicator lane inside the {headerWidth}px header");
    }

    [AvaloniaTheory]
    [InlineData("MacClassic")]
    [InlineData("DevExpress")]
    [InlineData("Linux")]
    public void SortIndicator_IsHiddenWhileAdornmentFillsHeader(string themeName)
    {
        // A full-width adornment (an in-header search field) takes over the cell, so the sort indicator
        // must get out of the way instead of sitting on top of it.
        SetTheme(themeName);

        Border adornment = new() { Background = Brushes.Red, Height = 12 };
        TreeDataGrid grid = BuildGrid("Name", new GridLength(300), adornment);
        Window window = Show(grid);

        Control sortIcon = Header(grid).GetVisualDescendants()
            .OfType<Control>()
            .First(static c => c.Name == "SortIcon");

        // Assert on the slot the theme parks the indicator in, not the Path itself: MacOS leaves the Path
        // IsVisible="False" until a column is actually sorted, so the Path alone says nothing here.
        Control sortSlot = (Control)sortIcon.GetVisualParent()!;

        bool visibleWhileHugging = sortSlot.IsVisible;

        DevoTreeDataGridExtensions.SetHeaderAdornmentPosition(adornment, HeaderAdornmentPosition.Fill);
        Layout(grid);

        bool isFilling = OverflowHeader(grid).AdornmentPosition == HeaderAdornmentPosition.Fill;
        bool visibleWhileFilling = sortSlot.IsVisible;

        window.Close();

        Assert.True(visibleWhileHugging, "the sort indicator slot should be present when the adornment only hugs its content");
        Assert.True(isFilling, "the adornment should report that it is filling the header");
        Assert.False(visibleWhileFilling, "the sort indicator should be hidden while the adornment fills the header");
    }

    [AvaloniaTheory]
    [InlineData("MacClassic")]
    [InlineData("DevExpress")]
    [InlineData("Linux")]
    public void AdornmentContent_ReceivesClicks(string themeName)
    {
        // Without this the in-header column-search affordances are inert. DevExpress used to suppress
        // hit-testing on its header content, so a button in the adornment never saw the click.
        SetTheme(themeName);

        bool clicked = false;
        Button button = new() { Content = "S", Width = 26, Height = 16 };
        button.Click += (_, _) => clicked = true;

        Border adornment = new() { Child = button };
        TreeDataGrid grid = BuildGrid("Name", new GridLength(300), adornment);
        Window window = Show(grid, width: 900);

        Point? centre = button.TranslatePoint(
            new Point(button.Bounds.Width / 2, button.Bounds.Height / 2),
            window);

        Assert.NotNull(centre);

        window.MouseDown(centre!.Value, MouseButton.Left);
        window.MouseUp(centre.Value, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        window.Close();

        Assert.True(clicked, "a button inside the header adornment should receive the click");
    }

    [AvaloniaTheory]
    [InlineData("MacClassic")]
    [InlineData("DevExpress")]
    [InlineData("Linux")]
    public void CaptionClick_StillSorts(string themeName)
    {
        // Interactive header content must not cost the header its sort-on-click: unhandled clicks have to
        // keep bubbling to the TreeDataGridColumnHeader.
        SetTheme(themeName);

        TreeDataGrid grid = BuildSortableGrid();
        Window window = Show(grid, width: 900);

        TreeDataGridColumnHeader header = Header(grid);
        Point? captionPoint = header.TranslatePoint(new Point(30, header.Bounds.Height / 2), window);

        Assert.NotNull(captionPoint);

        window.MouseDown(captionPoint!.Value, MouseButton.Left);
        window.MouseUp(captionPoint.Value, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        grid.UpdateLayout();

        object? sortDirection = Header(grid).SortDirection;

        window.Close();

        Assert.NotNull(sortDirection);
    }

    [AvaloniaFact]
    public void DevExpress_ResizeOverflowLaneStaysGrabbable()
    {
        // DevExpress deliberately overhangs its 11px resize thumb into the next header, and that header's
        // hit-target is inset 6px to leave it grabbable. Interactive header content must not cover it.
        // Only DevExpress overhangs; the other themes keep a 5px thumb inside their own header.
        SetTheme("DevExpress");

        TreeDataGrid grid = BuildSortableGrid();
        Window window = Show(grid, width: 900);

        TreeDataGridColumnHeader second = grid.GetVisualDescendants()
            .OfType<TreeDataGridColumnHeader>()
            .First(static h => h.ColumnIndex == 1);

        Point? lanePoint = second.TranslatePoint(new Point(2, second.Bounds.Height / 2), window);
        Assert.NotNull(lanePoint);

        IInputElement? laneHit = window.InputHitTest(lanePoint!.Value);

        // The thumb's template is an unnamed Border, so identify it from the ancestor chain.
        bool hitsThumb = laneHit is Visual visual && visual.GetSelfAndVisualAncestors().OfType<Thumb>().Any();

        window.Close();

        Assert.True(hitsThumb, "the previous column's resize thumb should still win the 6px overflow lane");
    }

    private static TreeDataGrid BuildSortableGrid()
    {
        TreeDataGrid grid = new() { CanUserSortColumns = true, CanUserResizeColumns = true };

        grid.Columns.Add(new TreeDataGridTemplateColumn
        {
            Header = "Name",
            Width = new GridLength(200),
            CellTemplate = CellTemplate(),
            CanUserSort = true,
            CompareAscending = static (a, b) => string.Compare(a as string, b as string, StringComparison.Ordinal),
            CompareDescending = static (a, b) => string.Compare(b as string, a as string, StringComparison.Ordinal),
        });
        grid.Columns.Add(new TreeDataGridTemplateColumn
        {
            Header = "Filler",
            Width = new GridLength(200),
            CellTemplate = CellTemplate(),
        });

        List<string> items = ["alpha", "beta", "gamma"];
        grid.ItemsSource = items;

        return grid;
    }

    [AvaloniaTheory]
    [InlineData("MacClassic")]
    [InlineData("DevExpress")]
    [InlineData("Linux")]
    public void AdornmentPosition_IsHonouredOnTheColumn(string themeName)
    {
        // RDM builds its columns in code, so the position has to work when set on the column itself and not
        // only on the adornment control.
        SetTheme(themeName);

        Border adornment = new() { Background = Brushes.Red, Height = 12 };
        TreeDataGrid grid = BuildGrid("Name", new GridLength(300), adornment);
        DevoTreeDataGridExtensions.SetHeaderAdornmentPosition(grid.Columns[0], HeaderAdornmentPosition.Fill);

        Window window = Show(grid);

        bool isFilling = OverflowHeader(grid).AdornmentPosition == HeaderAdornmentPosition.Fill;
        double adornmentWidth = OverflowHeader(grid).AdornmentWidth;
        double headerWidth = OverflowHeader(grid).Bounds.Width;

        window.Close();

        Assert.True(isFilling, "a Fill position set on the column should be honoured");
        Assert.Equal(headerWidth, adornmentWidth, Tolerance);
    }

    [AvaloniaTheory]
    [InlineData("MacClassic")]
    [InlineData("DevExpress")]
    [InlineData("Linux")]
    public void HeaderAdornment_SetOnTheColumnAfterLayout_IsPickedUp(string themeName)
    {
        // The adornment lives on the column, a plain AvaloniaObject that nothing in the visual tree
        // observes, so the header has to follow its column to notice one arriving after first layout.
        SetTheme(themeName);

        TreeDataGrid grid = BuildGrid("Caption", new GridLength(300), adornment: null);
        Window window = Show(grid);

        double before = OverflowHeader(grid).AdornmentWidth;

        Border adornment = new() { Width = 40, Height = 12, Background = Brushes.Red };
        DevoTreeDataGridExtensions.SetHeaderAdornment(grid.Columns[0], adornment);
        Layout(grid);

        double after = OverflowHeader(grid).AdornmentWidth;

        window.Close();

        Assert.Equal(0, before, Tolerance);
        Assert.Equal(40, after, Tolerance);
    }

    [AvaloniaTheory]
    [InlineData("MacClassic")]
    [InlineData("DevExpress")]
    [InlineData("Linux")]
    public void AdornmentPosition_SetOnTheColumnAfterLayout_IsPickedUp(string themeName)
    {
        // Same hole as the adornment itself: the class handler only covers a position set on the adornment
        // control, which is in the visual tree. Set on the column it needs the column subscription.
        SetTheme(themeName);

        Border adornment = new() { Width = 40, Height = 12, Background = Brushes.Red };
        TreeDataGrid grid = BuildGrid("Caption", new GridLength(300), adornment);
        Window window = Show(grid);

        double before = OverflowHeader(grid).AdornmentWidth;

        DevoTreeDataGridExtensions.SetHeaderAdornmentPosition(grid.Columns[0], HeaderAdornmentPosition.Fill);
        Layout(grid);

        TreeDataGridOverflowHeader overflowHeader = OverflowHeader(grid);
        bool isFilling = overflowHeader.AdornmentPosition == HeaderAdornmentPosition.Fill;
        double headerWidth = overflowHeader.Bounds.Width;
        double after = overflowHeader.AdornmentWidth;

        window.Close();

        Assert.Equal(40, before, Tolerance);
        Assert.True(isFilling, "a Fill position set on the column after layout should be honoured");
        Assert.Equal(headerWidth, after, Tolerance);
    }

    [AvaloniaTheory]
    [InlineData("MacClassic")]
    [InlineData("DevExpress")]
    [InlineData("Linux")]
    public void PositionChange_DoesNotResizeAnEmptyAutoColumn(string themeName)
    {
        // The themes hide the sort indicator by collapsing a panel that is inside the measured tree, so
        // switching to Fill does lower the header's own desired width by a few px. This pins the part that
        // actually matters: the column does not move. The worst case is used deliberately -- an empty
        // caption and no rows, so nothing but the header can decide the width.
        SetTheme(themeName);

        Border adornment = new() { Width = 40, Height = 12, Background = Brushes.Red };

        TreeDataGrid grid = new() { CanUserSortColumns = true };
        TreeDataGridTemplateColumn column = new()
        {
            Header = string.Empty,
            Width = GridLength.Auto,
            CellTemplate = CellTemplate(),
        };

        grid.Columns.Add(column);
        grid.Columns.Add(new TreeDataGridTemplateColumn
        {
            Header = "Filler",
            Width = new GridLength(1, GridUnitType.Star),
            CellTemplate = CellTemplate(),
        });

        DevoTreeDataGridExtensions.SetHeaderAdornment(column, adornment);
        grid.ItemsSource = new List<string>();

        Window window = Show(grid);

        double rightWidth = ColumnWidth(grid);

        DevoTreeDataGridExtensions.SetHeaderAdornmentPosition(column, HeaderAdornmentPosition.Fill);
        double fillWidth = ColumnWidth(grid);

        window.Close();

        Assert.Equal(rightWidth, fillWidth, Tolerance);
    }

    [AvaloniaTheory]
    [InlineData("MacClassic", 5)]
    [InlineData("DevExpress", 0)]
    [InlineData("Linux", 0)]
    public void FillAdornment_ReachesTheColumnHeaderEdges(string themeName, double trailingChrome)
    {
        // The point of positioning the adornment against the header rather than the caption: a theme's
        // caption inset -- macOS's 18px Padding, DevExpress's trailing gutter, Yaru's leading 5 -- must not
        // hold the adornment off the header's edge. Only structural chrome may, which is why MacClassic
        // stops 5px short: its separator and resizer own a Grid column the adornment must not cross.
        SetTheme(themeName);

        Border adornment = new() { Background = Brushes.Red };
        DevoTreeDataGridExtensions.SetHeaderAdornmentPosition(adornment, HeaderAdornmentPosition.Fill);
        TreeDataGrid grid = BuildGrid("Name", new GridLength(300), adornment);

        Window window = Show(grid);

        TreeDataGridColumnHeader header = Header(grid);
        double headerWidth = header.Bounds.Width;
        Point? left = adornment.TranslatePoint(new Point(0, 0), header);
        Point? right = adornment.TranslatePoint(new Point(adornment.Bounds.Width, 0), header);

        window.Close();

        Assert.NotNull(left);
        Assert.NotNull(right);
        Assert.Equal(0, left!.Value.X, Tolerance);
        Assert.Equal(headerWidth - trailingChrome, right!.Value.X, Tolerance);
    }

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
            DevoTreeDataGridExtensions.SetHeaderAdornment(grid.Columns[0], adornment);
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
