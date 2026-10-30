namespace Devolutions.AvaloniaControls.Tests;

using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Devolutions.AvaloniaControls.Controls;
using SampleApp;

/// <summary>
///   Covers what a screen reader relies on in <see cref="GroupedTileListBox"/>: the list and its tiles as selectable
///   list items, keyboard focus on the tile the selection moves to, and the group headers as headings.
/// </summary>
[Collection("StylesTest")]
public class GroupedTileListBoxAccessibilityTests
{
    private sealed record Tile(string Name, string Group);

    private static void ResetTheme()
    {
        App.CurrentTheme = null;
        App.SetTheme(new MacOsTheme());
    }

    private static (Window Window, Button Before, GroupedTileListBox List, IReadOnlyList<Tile> Tiles) Create(
        SelectionMode selectionMode = SelectionMode.Single,
        int extraTiles = 0,
        double windowHeight = 600,
        bool grouped = true)
    {
        List<Tile> tiles =
        [
            new("Apple", "Fruits"), new("Banana", "Fruits"), new("Cherry", "Fruits"),
            new("Carrot", "Vegetables"), new("Leek", "Vegetables"),
            .. Enumerable.Range(0, extraTiles).Select(static i => new Tile($"Grain {i}", "Grains"))
        ];
        var list = new GroupedTileListBox
        {
            ItemsSource = tiles,
            SelectionMode = selectionMode,
            GroupSelector = grouped ? static item => ((Tile)item).Group : null,
            ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<Tile>((tile, _) => new TextBlock { Text = tile?.Name })
        };
        var before = new Button { Content = "Before" };
        var after = new Button { Content = "After" };
        var panel = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto") };
        Grid.SetRow(list, 1);
        Grid.SetRow(after, 2);
        panel.Children.Add(before);
        panel.Children.Add(list);
        panel.Children.Add(after);

        // At the default height every tile is realized.
        var window = new Window { Content = panel, Width = 800, Height = windowHeight };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, before, list, tiles);
    }

    private static GroupedTileListBoxItem Container(GroupedTileListBox list, Tile tile) =>
        list.GetVisualDescendants().OfType<GroupedTileListBoxItem>().Single(item => ReferenceEquals(item.Content, tile));

    private static ISelectionItemProvider TileProvider(GroupedTileListBox list, Tile tile) =>
        (ISelectionItemProvider)ControlAutomationPeer.CreatePeerForElement(Container(list, tile));

    private static object? FocusedElement(Window window) => window.FocusManager?.GetFocusedElement();

    private static Button After(Window window) =>
        window.GetVisualDescendants().OfType<Button>().Single(static button => Equals(button.Content, "After"));

    private static void CtrlClick(Window window, Control control)
    {
        Avalonia.Point center = control.TranslatePoint(new Avalonia.Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
        window.MouseDown(center, MouseButton.Left, RawInputModifiers.Control);
        window.MouseUp(center, MouseButton.Left, RawInputModifiers.Control);
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void ListAndTilesAreExposedAsASelectableList()
    {
        ResetTheme();
        (Window window, _, GroupedTileListBox list, IReadOnlyList<Tile> tiles) = Create();
        try
        {
            AutomationPeer listPeer = ControlAutomationPeer.CreatePeerForElement(list);
            Assert.Equal(AutomationControlType.List, listPeer.GetAutomationControlType());

            AutomationPeer tilePeer = ControlAutomationPeer.CreatePeerForElement(Container(list, tiles[1]));
            Assert.Equal(AutomationControlType.ListItem, tilePeer.GetAutomationControlType());

            ((ISelectionItemProvider)tilePeer).Select();
            Dispatcher.UIThread.RunJobs();

            Assert.Same(tiles[1], list.SelectedItem);
            Assert.True(((ISelectionItemProvider)tilePeer).IsSelected);
            AutomationPeer selected = Assert.Single(((ISelectionProvider)listPeer).GetSelection());
            Assert.Same(tilePeer, selected);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void SingleSelectionRejectsAddingASecondTileAndAllowsRemovingTheOnlyOne()
    {
        ResetTheme();
        (Window window, _, GroupedTileListBox list, IReadOnlyList<Tile> tiles) = Create();
        try
        {
            TileProvider(list, tiles[0]).Select();
            Dispatcher.UIThread.RunJobs();

            Assert.Throws<InvalidOperationException>(() => TileProvider(list, tiles[1]).AddToSelection());
            Assert.Same(tiles[0], list.SelectedItem);

            TileProvider(list, tiles[0]).RemoveFromSelection();
            Dispatcher.UIThread.RunJobs();
            Assert.Null(list.SelectedItem);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void AlwaysSelectedRejectsRemovingTheLastSelectedTile()
    {
        ResetTheme();
        (Window window, _, GroupedTileListBox list, IReadOnlyList<Tile> tiles) = Create(SelectionMode.Single | SelectionMode.AlwaysSelected);
        try
        {
            TileProvider(list, tiles[2]).Select();
            Dispatcher.UIThread.RunJobs();

            Assert.Throws<InvalidOperationException>(() => TileProvider(list, tiles[2]).RemoveFromSelection());
            Assert.Same(tiles[2], list.SelectedItem);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void MultipleSelectionAddsAndRemovesTiles()
    {
        ResetTheme();
        (Window window, _, GroupedTileListBox list, IReadOnlyList<Tile> tiles) = Create(SelectionMode.Multiple);
        try
        {
            TileProvider(list, tiles[0]).AddToSelection();
            TileProvider(list, tiles[3]).AddToSelection();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(2, list.SelectedItems?.Count);

            TileProvider(list, tiles[0]).RemoveFromSelection();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal([tiles[3]], list.SelectedItems!.Cast<Tile>());
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ArrowKeysMoveKeyboardFocusToTheSelectedTile()
    {
        ResetTheme();
        (Window window, _, GroupedTileListBox list, IReadOnlyList<Tile> tiles) = Create();
        try
        {
            list.SelectedItem = tiles[0];
            Container(list, tiles[0]).Focus(NavigationMethod.Tab);
            Dispatcher.UIThread.RunJobs();

            window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
            Dispatcher.UIThread.RunJobs();

            Assert.Same(tiles[1], list.SelectedItem);
            Assert.Same(Container(list, tiles[1]), FocusedElement(window));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void UpOnTheFirstRowKeepsFocusOnTheTile()
    {
        ResetTheme();
        (Window window, _, GroupedTileListBox list, IReadOnlyList<Tile> tiles) = Create();
        try
        {
            list.SelectedItem = tiles[0];
            Container(list, tiles[0]).Focus(NavigationMethod.Tab);
            Dispatcher.UIThread.RunJobs();

            window.KeyPress(Key.Up, RawInputModifiers.None, PhysicalKey.ArrowUp, null);
            Dispatcher.UIThread.RunJobs();

            Assert.Same(tiles[0], list.SelectedItem);
            Assert.Same(Container(list, tiles[0]), FocusedElement(window));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TabEntersTheListOnTheSelectedTileAndLeavesItInOneStep()
    {
        ResetTheme();
        (Window window, Button before, GroupedTileListBox list, IReadOnlyList<Tile> tiles) = Create();
        try
        {
            list.SelectedItem = tiles[3];
            Dispatcher.UIThread.RunJobs();
            before.Focus(NavigationMethod.Tab);

            window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
            Dispatcher.UIThread.RunJobs();
            Assert.Same(Container(list, tiles[3]), FocusedElement(window));

            window.KeyPress(Key.Tab, RawInputModifiers.Shift, PhysicalKey.Tab, null);
            Dispatcher.UIThread.RunJobs();
            Assert.Same(before, FocusedElement(window));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TabStillEntersTheListAfterItsTabStopTileScrolledOutOfView()
    {
        ResetTheme();
        (Window window, Button before, GroupedTileListBox list, IReadOnlyList<Tile> tiles) = Create(extraTiles: 200, windowHeight: 250, grouped: false);
        try
        {
            list.SelectedItem = tiles[0];
            Dispatcher.UIThread.RunJobs();
            before.Focus(NavigationMethod.Tab);

            ScrollViewer scrollViewer = list.GetVisualDescendants().OfType<ScrollViewer>().First();
            scrollViewer.Offset = new Avalonia.Vector(0, scrollViewer.Extent.Height);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            Assert.DoesNotContain(list.GetVisualDescendants().OfType<GroupedTileListBoxItem>(), item => ReferenceEquals(item.Content, tiles[0]));

            window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
            Dispatcher.UIThread.RunJobs();
            Dispatcher.UIThread.RunJobs();

            Assert.Same(Container(list, tiles[0]), FocusedElement(window));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TabEntersOnTheNewPrimaryTileWhenAnotherSelectedTileIsPromoted()
    {
        ResetTheme();
        (Window window, Button before, GroupedTileListBox list, IReadOnlyList<Tile> tiles) = Create(SelectionMode.Multiple);
        try
        {
            TileProvider(list, tiles[0]).AddToSelection();
            TileProvider(list, tiles[3]).AddToSelection();
            Dispatcher.UIThread.RunJobs();
            Assert.Same(tiles[3], list.SelectedItem);

            list.SelectedItem = tiles[0];
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(2, list.SelectedItems?.Count);

            before.Focus(NavigationMethod.Tab);
            window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
            Dispatcher.UIThread.RunJobs();

            Assert.Same(Container(list, tiles[0]), FocusedElement(window));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TabLeavesTheListFromAFocusedTileThatIsNotSelected()
    {
        ResetTheme();
        (Window window, _, GroupedTileListBox list, IReadOnlyList<Tile> tiles) = Create(SelectionMode.Multiple);
        try
        {
            CtrlClick(window, Container(list, tiles[0]));
            CtrlClick(window, Container(list, tiles[3]));
            Assert.Equal(2, list.SelectedItems?.Count);

            // Deselects Apple, in the first group, and focuses it; Carrot, in the second group, stays selected.
            CtrlClick(window, Container(list, tiles[0]));
            Assert.Equal([tiles[3]], list.SelectedItems!.Cast<Tile>());
            Assert.Same(Container(list, tiles[0]), FocusedElement(window));

            window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
            Dispatcher.UIThread.RunJobs();

            Assert.Same(After(window), FocusedElement(window));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ArrowKeysMoveFromTheFocusedTileWhenItIsNotTheSelectedOne()
    {
        ResetTheme();
        (Window window, _, GroupedTileListBox list, IReadOnlyList<Tile> tiles) = Create(SelectionMode.Multiple);
        try
        {
            CtrlClick(window, Container(list, tiles[0]));
            CtrlClick(window, Container(list, tiles[3]));
            CtrlClick(window, Container(list, tiles[0]));

            window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
            Dispatcher.UIThread.RunJobs();

            Assert.Same(tiles[1], list.SelectedItem);
            Assert.Same(Container(list, tiles[1]), FocusedElement(window));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void CtrlSpaceTogglesTheFocusedTileAndKeepsFocusOnIt()
    {
        ResetTheme();
        (Window window, _, GroupedTileListBox list, IReadOnlyList<Tile> tiles) = Create(SelectionMode.Multiple);
        try
        {
            CtrlClick(window, Container(list, tiles[0]));
            CtrlClick(window, Container(list, tiles[3]));
            CtrlClick(window, Container(list, tiles[0]));

            window.KeyPress(Key.Space, RawInputModifiers.Control, PhysicalKey.Space, " ");
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(2, list.SelectedItems?.Count);
            Assert.Contains(tiles[0], list.SelectedItems!.Cast<Tile>());
            Assert.Same(Container(list, tiles[0]), FocusedElement(window));

            // Deselecting it again keeps it focused, and the Tab stop, so Tab leaves the list.
            window.KeyPress(Key.Space, RawInputModifiers.Control, PhysicalKey.Space, " ");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal([tiles[3]], list.SelectedItems!.Cast<Tile>());
            Assert.Same(Container(list, tiles[0]), FocusedElement(window));

            window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
            Dispatcher.UIThread.RunJobs();
            Assert.Same(After(window), FocusedElement(window));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TabLeavesTheListAfterADragSelection()
    {
        ResetTheme();
        (Window window, _, GroupedTileListBox list, IReadOnlyList<Tile> tiles) = Create(SelectionMode.Multiple);
        try
        {
            // From empty space below the tiles, up across the second group.
            GroupedTileListBoxItem carrot = Container(list, tiles[3]);
            Point start = list.TranslatePoint(new Point(list.Bounds.Width - 10, list.Bounds.Height - 10), window)!.Value;
            Point end = carrot.TranslatePoint(new Point(carrot.Bounds.Width / 2, carrot.Bounds.Height / 2), window)!.Value;
            window.MouseDown(start, MouseButton.Left, RawInputModifiers.None);
            window.MouseMove(new Point(start.X - 20, start.Y - 20), RawInputModifiers.LeftMouseButton);
            window.MouseMove(end, RawInputModifiers.LeftMouseButton);
            window.MouseUp(end, MouseButton.Left, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();

            Assert.Contains(tiles[3], list.SelectedItems!.Cast<Tile>());
            Assert.IsType<GroupedTileListBoxItem>(FocusedElement(window));

            window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
            Dispatcher.UIThread.RunJobs();

            Assert.Same(After(window), FocusedElement(window));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TabLandsOnTheFirstTileWhenNothingIsSelected()
    {
        ResetTheme();
        (Window window, Button before, GroupedTileListBox list, IReadOnlyList<Tile> tiles) = Create();
        try
        {
            before.Focus(NavigationMethod.Tab);

            window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
            Dispatcher.UIThread.RunJobs();

            Assert.Same(Container(list, tiles[0]), FocusedElement(window));
            Assert.Null(list.SelectedItem);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ADisabledListRefusesSelectionRequests()
    {
        ResetTheme();
        (Window window, _, GroupedTileListBox list, IReadOnlyList<Tile> tiles) = Create(SelectionMode.Multiple);
        try
        {
            list.IsEnabled = false;
            Dispatcher.UIThread.RunJobs();

            Assert.Throws<ElementNotEnabledException>(() => TileProvider(list, tiles[1]).Select());
            Assert.Throws<ElementNotEnabledException>(() => TileProvider(list, tiles[1]).AddToSelection());
            Assert.Null(list.SelectedItem);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void AGroupHeaderWithAHostSetNameDoesNotAnnounceItsTextChanges()
    {
        ResetTheme();
        (Window window, _, GroupedTileListBox list, _) = Create();
        try
        {
            GroupedTileListBoxGroupHeader header = list.GetVisualDescendants().OfType<GroupedTileListBoxGroupHeader>().First();
            AutomationProperties.SetName(header, "Fresh fruit");
            AutomationPeer peer = ControlAutomationPeer.CreatePeerForElement(header);
            List<AutomationProperty> raised = [];
            peer.PropertyChanged += (_, e) => raised.Add(e.Property);

            header.Text = "Fruits and berries";
            Dispatcher.UIThread.RunJobs();

            Assert.DoesNotContain(AutomationElementIdentifiers.NameProperty, raised);
            Assert.Equal("Fresh fruit", peer.GetName());
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void GroupHeadersAreHeadingsNamedAfterTheirGroup()
    {
        ResetTheme();
        (Window window, _, GroupedTileListBox list, _) = Create();
        try
        {
            List<AutomationPeer> headers =
            [
                .. list.GetVisualDescendants().OfType<GroupedTileListBoxGroupHeader>().Select(ControlAutomationPeer.CreatePeerForElement)
            ];

            Assert.Equal(["Fruits", "Vegetables"], headers.Select(static peer => peer.GetName()));
            Assert.All(headers, static peer =>
            {
                Assert.Equal(AutomationControlType.Text, peer.GetAutomationControlType());
                Assert.Equal(2, peer.GetHeadingLevel());
                Assert.Empty(peer.GetChildren());
            });
        }
        finally
        {
            window.Close();
        }
    }
}
