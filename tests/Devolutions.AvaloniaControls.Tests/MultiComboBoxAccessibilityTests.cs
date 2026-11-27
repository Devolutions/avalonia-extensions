namespace Devolutions.AvaloniaControls.Tests;

using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Devolutions.AvaloniaControls.Controls;
using SampleApp;

[Collection("StylesTest")]
public class MultiComboBoxAccessibilityTests
{
    private static readonly string[] Levels = ["Error", "Warning", "Info", "Debug"];

    [AvaloniaTheory]
    [InlineData("MacClassic")]
    [InlineData("DevExpress")]
    [InlineData("Linux")]
    [InlineData("WinUI")]
    public void TheFieldIsANamedComboBoxThatOpens(string themeName)
    {
        (Window window, MultiComboBox box) = Create(themeName);

        try
        {
            AutomationPeer combo = ControlAutomationPeer.CreatePeerForElement(box);
            Assert.Equal(AutomationControlType.ComboBox, combo.GetAutomationControlType());
            Assert.Equal("Level", combo.GetName());
            Assert.Equal("LevelPicker", combo.GetAutomationId());

            IExpandCollapseProvider expand = Assert.IsAssignableFrom<IExpandCollapseProvider>(combo);
            Assert.Equal(ExpandCollapseState.Collapsed, expand.ExpandCollapseState);
            expand.Expand();
            Dispatcher.UIThread.RunJobs();
            Assert.True(box.IsDropDownOpen);
            Assert.Equal(ExpandCollapseState.Expanded, expand.ExpandCollapseState);
            expand.Collapse();
            Dispatcher.UIThread.RunJobs();
            Assert.False(box.IsDropDownOpen);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData("MacClassic")]
    [InlineData("DevExpress")]
    [InlineData("Linux")]
    [InlineData("WinUI")]
    public void ItsValueIsTheSelectionAndReadOnly(string themeName)
    {
        (Window window, MultiComboBox box) = Create(themeName);

        try
        {
            IValueProvider value = Assert.IsAssignableFrom<IValueProvider>(ControlAutomationPeer.CreatePeerForElement(box));
            Assert.True(value.IsReadOnly);
            Assert.Equal("Error, Warning", value.Value);
            Assert.Throws<InvalidOperationException>(() => value.SetValue("Info"));

            box.SelectedItems!.Add("Info");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("Error, Warning, Info", value.Value);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData("MacClassic")]
    [InlineData("DevExpress")]
    [InlineData("Linux")]
    [InlineData("WinUI")]
    public void EachChipIsReadableAndItsRemoveButtonSaysWhatItRemoves(string themeName)
    {
        (Window window, MultiComboBox box) = Create(themeName);

        try
        {
            IReadOnlyList<AutomationPeer> chips = ControlChildren(ControlAutomationPeer.CreatePeerForElement(box));
            Assert.Equal(["Error", "Warning"], chips.Select(static chip => chip.GetName()));
            Assert.All(chips, static chip => Assert.Equal(AutomationControlType.ListItem, chip.GetAutomationControlType()));

            AutomationPeer remove = Assert.Single(ControlChildren(chips[0]));
            Assert.Equal(AutomationControlType.Button, remove.GetAutomationControlType());
            Assert.Equal("Remove Error", remove.GetName());

            Assert.IsAssignableFrom<IInvokeProvider>(remove).Invoke();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(["Warning"], box.SelectedItems!.Cast<string>());
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData("MacClassic")]
    [InlineData("DevExpress")]
    [InlineData("Linux")]
    [InlineData("WinUI")]
    public void TheRemoveButtonsNameCanBeTranslated(string themeName)
    {
        (Window window, MultiComboBox box) = Create(themeName, static box => box.Styles.Add(new Style(static x => x.OfType<ClosableTag>())
        {
            Setters = { new Setter(ClosableTag.RemoveTextProperty, "Retirer {0}") },
        }));

        try
        {
            AutomationPeer chip = ControlChildren(ControlAutomationPeer.CreatePeerForElement(box))[0];
            Assert.Equal("Retirer Error", ControlChildren(chip)[0].GetName());
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData("MacClassic")]
    [InlineData("DevExpress")]
    [InlineData("Linux")]
    [InlineData("WinUI")]
    public void ChipsOfItemControlsAreNamedByTheItem(string themeName)
    {
        (Window window, MultiComboBox box) = Create(themeName, static box =>
        {
            box.ItemsSource = null;
            box.SelectedItems!.Clear();
            box.Items.Add(new MultiComboBoxItem { Content = "Error", IsSelected = true });
            box.Items.Add(new MultiComboBoxItem { Content = "Warning" });
        });

        try
        {
            AutomationPeer chip = Assert.Single(ControlChildren(ControlAutomationPeer.CreatePeerForElement(box)));
            Assert.Equal("Error", chip.GetName());
            Assert.Equal("Remove Error", ControlChildren(chip)[0].GetName());
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData("MacClassic")]
    [InlineData("DevExpress")]
    [InlineData("Linux")]
    [InlineData("WinUI")]
    public void DropDownItemsReportAndChangeTheirOwnState(string themeName)
    {
        (Window window, MultiComboBox box) = Create(themeName);

        try
        {
            Open(box);
            AutomationPeer list = ControlAutomationPeer.CreatePeerForElement(DropDown(box).OfType<ItemsControl>().Single(static list => list.Name == "PART_ItemsList"));
            Assert.Equal(AutomationControlType.List, list.GetAutomationControlType());
            Assert.Equal("Level", list.GetName());
            ISelectionProvider selection = Assert.IsAssignableFrom<ISelectionProvider>(list);
            Assert.True(selection.CanSelectMultiple);

            IReadOnlyList<AutomationPeer> items = ControlChildren(list);
            Assert.Equal(Levels, items.Select(static item => item.GetName()));
            Assert.All(items, static item => Assert.Empty(ControlChildren(item)));
            Assert.Equal([true, true, false, false], items.Select(static item => ((ISelectionItemProvider)item).IsSelected));
            Assert.Equal(items.Take(2), selection.GetSelection());

            List<object?> changes = [];
            items[2].PropertyChanged += (_, e) =>
            {
                if (e.Property == SelectionItemPatternIdentifiers.IsSelectedProperty)
                {
                    changes.Add(e.NewValue);
                }
            };

            ((ISelectionItemProvider)items[2]).AddToSelection();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(["Error", "Warning", "Info"], box.SelectedItems!.Cast<string>());
            ((ISelectionItemProvider)items[0]).RemoveFromSelection();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(["Warning", "Info"], box.SelectedItems!.Cast<string>());
            ((ISelectionItemProvider)items[3]).Select();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(["Debug"], box.SelectedItems!.Cast<string>());
            Assert.Equal([true, false], changes);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData("MacClassic")]
    [InlineData("DevExpress")]
    [InlineData("Linux")]
    [InlineData("WinUI")]
    public void SelectAllIsAThreeStateCheckBox(string themeName)
    {
        (Window window, MultiComboBox box) = Create(themeName, static box => box.SelectAllText = "Select all");

        try
        {
            Open(box);
            AutomationPeer selectAll = ControlAutomationPeer.CreatePeerForElement(DropDown(box).OfType<MultiComboBoxSelectAllItem>().Single());
            Assert.Equal(AutomationControlType.CheckBox, selectAll.GetAutomationControlType());
            Assert.Equal("Select all", selectAll.GetName());
            Assert.Empty(ControlChildren(selectAll));

            IToggleProvider toggle = Assert.IsAssignableFrom<IToggleProvider>(selectAll);
            Assert.Equal(ToggleState.Indeterminate, toggle.ToggleState);
            toggle.Toggle();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(ToggleState.On, toggle.ToggleState);
            Assert.Equal(Levels, box.SelectedItems!.Cast<string>());
            toggle.Toggle();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(ToggleState.Off, toggle.ToggleState);
            Assert.Empty(box.SelectedItems!);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData("MacClassic")]
    [InlineData("DevExpress")]
    [InlineData("Linux")]
    [InlineData("WinUI")]
    public void GroupHeadersAreThreeStateCheckBoxes(string themeName)
    {
        (Window window, MultiComboBox box) = Create(themeName, static box => box.GroupSelector = static item => (string)item is "Error" or "Warning" ? "Problems" : "Details");

        try
        {
            Open(box);
            MultiComboBoxGroupHeaderItem[] headers = DropDown(box).OfType<MultiComboBoxGroupHeaderItem>().ToArray();
            AutomationPeer problems = ControlAutomationPeer.CreatePeerForElement(headers.Single(static header => AutomationText(header) == "Problems"));
            AutomationPeer details = ControlAutomationPeer.CreatePeerForElement(headers.Single(static header => AutomationText(header) == "Details"));
            Assert.Equal(AutomationControlType.CheckBox, problems.GetAutomationControlType());
            Assert.Equal(ToggleState.On, ((IToggleProvider)problems).ToggleState);
            Assert.Equal(ToggleState.Off, ((IToggleProvider)details).ToggleState);

            ((IToggleProvider)details).Toggle();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(Levels.Order(), box.SelectedItems!.Cast<string>().Order());
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData("MacClassic")]
    [InlineData("DevExpress")]
    [InlineData("Linux")]
    [InlineData("WinUI")]
    public void TheFilterBoxIsNamedByItsText(string themeName)
    {
        (Window window, MultiComboBox box) = Create(themeName, static box => box.ShowFilter = true);

        try
        {
            Open(box);
            TextBox filter = DropDown(box).OfType<TextBox>().Single(static box => box.Name == "PART_FilterTextBox");
            Assert.Equal("Find items", ControlAutomationPeer.CreatePeerForElement(filter).GetName());

            box.FilterText = "Search levels";
            Assert.Equal("Search levels", ControlAutomationPeer.CreatePeerForElement(filter).GetName());
        }
        finally
        {
            window.Close();
        }
    }

    // TagInput shares the chips.
    [AvaloniaTheory]
    [InlineData("MacClassic")]
    [InlineData("DevExpress")]
    [InlineData("Linux")]
    [InlineData("WinUI")]
    public void TagInputChipsAreReadableWithNamedRemoveButtons(string themeName)
    {
        SetTheme(themeName);
        TagInput input = new() { Tags = new Avalonia.Collections.AvaloniaList<string> { "urgent", "billing" } };
        Window window = new() { Content = input, Width = 500, Height = 200 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        try
        {
            ClosableTag[] tags = input.GetVisualDescendants().OfType<ClosableTag>().ToArray();
            Assert.Equal(["urgent", "billing"], tags.Select(static tag => ControlAutomationPeer.CreatePeerForElement(tag).GetName()));
            AutomationPeer remove = Assert.Single(ControlChildren(ControlAutomationPeer.CreatePeerForElement(tags[0])));
            Assert.Equal("Remove urgent", remove.GetName());
        }
        finally
        {
            window.Close();
        }
    }

    private static (Window Window, MultiComboBox Box) Create(string themeName, Action<MultiComboBox>? configure = null)
    {
        SetTheme(themeName);

        MultiComboBox box = new() { Name = "LevelPicker", ItemsSource = Levels };
        AutomationProperties.SetName(box, "Level");
        box.SelectedItems!.Add("Error");
        box.SelectedItems.Add("Warning");
        configure?.Invoke(box);

        Window window = new() { Content = new StackPanel { Children = { box } }, Width = 500, Height = 400 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, box);
    }

    private static void Open(MultiComboBox box)
    {
        Assert.IsAssignableFrom<IExpandCollapseProvider>(ControlAutomationPeer.CreatePeerForElement(box)).Expand();
        Dispatcher.UIThread.RunJobs();
    }

    private static IEnumerable<Visual> DropDown(MultiComboBox box) =>
        box.GetVisualDescendants().OfType<Popup>().Single().Child?.GetVisualDescendants() ?? [];

    private static string? AutomationText(Control control) => ControlAutomationPeer.CreatePeerForElement(control).GetName();

    private static IReadOnlyList<AutomationPeer> ControlChildren(AutomationPeer peer)
    {
        List<AutomationPeer> result = [];
        foreach (AutomationPeer child in peer.GetChildren())
        {
            if (child.IsControlElement())
            {
                result.Add(child);
            }
            else
            {
                result.AddRange(ControlChildren(child));
            }
        }

        return result;
    }

    private static void SetTheme(string themeName)
    {
        App.CurrentTheme = null;
        App.SetTheme(themeName switch
        {
            "MacClassic" => new MacOsClassicTheme(),
            "Linux" => new LinuxYaruTheme(),
            "DevExpress" => new DevExpressTheme(),
            "WinUI" => new WinUiClassicTheme(),
            _ => throw new ArgumentException($"Unknown theme: {themeName}"),
        });
    }
}
