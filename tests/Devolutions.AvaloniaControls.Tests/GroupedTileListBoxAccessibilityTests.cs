namespace Devolutions.AvaloniaControls.Tests;

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
        SelectionMode selectionMode = SelectionMode.Single)
    {
        List<Tile> tiles =
        [
            new("Apple", "Fruits"), new("Banana", "Fruits"), new("Cherry", "Fruits"),
            new("Carrot", "Vegetables"), new("Leek", "Vegetables")
        ];
        var list = new GroupedTileListBox
        {
            ItemsSource = tiles,
            SelectionMode = selectionMode,
            GroupSelector = static item => ((Tile)item).Group,
            ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<Tile>((tile, _) => new TextBlock { Text = tile?.Name })
        };
        var before = new Button { Content = "Before" };
        var panel = new DockPanel();
        DockPanel.SetDock(before, Dock.Top);
        panel.Children.Add(before);
        panel.Children.Add(list);

        // Large enough that every tile is realized.
        var window = new Window { Content = panel, Width = 800, Height = 600 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, before, list, tiles);
    }

    private static GroupedTileListBoxItem Container(GroupedTileListBox list, Tile tile) =>
        list.GetVisualDescendants().OfType<GroupedTileListBoxItem>().Single(item => ReferenceEquals(item.Content, tile));

    private static ISelectionItemProvider TileProvider(GroupedTileListBox list, Tile tile) =>
        (ISelectionItemProvider)ControlAutomationPeer.CreatePeerForElement(Container(list, tile));

    private static object? FocusedElement(Window window) => window.FocusManager?.GetFocusedElement();

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
