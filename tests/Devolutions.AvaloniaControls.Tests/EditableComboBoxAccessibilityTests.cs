namespace Devolutions.AvaloniaControls.Tests;

using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Devolutions.AvaloniaControls.Controls;
using SampleApp;

[Collection("StylesTest")]
public class EditableComboBoxAccessibilityTests
{
    [AvaloniaTheory]
    [InlineData("MacClassic")]
    [InlineData("DevExpress")]
    [InlineData("Linux")]
    [InlineData("WinUI")]
    public void TheFieldIsOneNamedComboBoxHoldingItsEdit(string themeName)
    {
        (Window window, EditableComboBox field, _) = Create(themeName, static field => AutomationProperties.SetName(field, "Folder"));

        try
        {
            AutomationPeer outer = ControlAutomationPeer.CreatePeerForElement(field);
            AutomationPeer combo = ComboPeer(field);

            Assert.False(outer.IsControlElement());
            Assert.Equal(AutomationControlType.ComboBox, combo.GetAutomationControlType());
            Assert.Equal("Folder", combo.GetName());
            Assert.Same(combo, Assert.Single(ControlChildren(outer)));

            AutomationPeer edit = ControlChildren(combo)[0];
            Assert.Equal(AutomationControlType.Edit, edit.GetAutomationControlType());
            Assert.Equal("Folder", edit.GetName());

            Assert.All(
                field.GetVisualDescendants().OfType<ItemsControl>().Where(static slot => slot.Name is "PART_InnerLeftContent" or "PART_InnerLeftOfDropDownArrowContent" or "PART_InnerRightContent"),
                static slot => Assert.Equal(AccessibilityView.Raw, AutomationProperties.GetAccessibilityView(slot)));
        }
        finally
        {
            window.Close();
        }
    }

    // In the themes that show InnerRightContent beside the text.
    [AvaloniaTheory]
    [InlineData("MacClassic")]
    [InlineData("DevExpress")]
    public void ButtonsBesideTheTextSitDirectlyInTheComboBox(string themeName)
    {
        (Window window, EditableComboBox field, Button button) = Create(themeName, static field => AutomationProperties.SetName(field, "Folder"));

        try
        {
            IReadOnlyList<AutomationPeer> children = ControlChildren(ComboPeer(field));
            Assert.Equal(2, children.Count);
            Assert.Equal(AutomationControlType.Edit, children[0].GetAutomationControlType());
            Assert.Same(ControlAutomationPeer.CreatePeerForElement(button), children[1]);
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
    public void TheComboBoxTakesTheFieldsIdElseItsName(string themeName)
    {
        (Window window, EditableComboBox withId, _) = Create(themeName, static field =>
        {
            field.Name = "FolderPicker";
            AutomationProperties.SetAutomationId(field, "Folder");
        });
        (Window nameOnly, EditableComboBox withName, _) = Create(themeName, static field => field.Name = "FolderPicker");
        (Window neither, EditableComboBox withNeither, _) = Create(themeName, static _ => { });

        try
        {
            Assert.Equal("Folder", ComboPeer(withId).GetAutomationId());
            Assert.Equal("FolderPicker", ComboPeer(withName).GetAutomationId());
            Assert.Equal("PART_InnerComboBox", ComboPeer(withNeither).GetAutomationId());

            AutomationProperties.SetAutomationId(withName, "Destination");
            Assert.Equal("Destination", ComboPeer(withName).GetAutomationId());
        }
        finally
        {
            window.Close();
            nameOnly.Close();
            neither.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData("MacClassic")]
    [InlineData("DevExpress")]
    [InlineData("Linux")]
    [InlineData("WinUI")]
    public void TheComboBoxReportsTheFieldsHelpTextAndLabel(string themeName)
    {
        TextBlock label = new() { Text = "Folder" };
        (Window window, EditableComboBox field, _) = Create(themeName, field =>
        {
            AutomationProperties.SetHelpText(field, "Where the entry is saved");
            AutomationProperties.SetLabeledBy(field, label);
        });

        try
        {
            AutomationPeer combo = ComboPeer(field);
            Assert.Equal("Where the entry is saved", combo.GetHelpText());
            Assert.Same(ControlAutomationPeer.CreatePeerForElement(label), combo.GetLabeledBy());

            Assert.Equal("Folder", combo.GetName());
            Assert.Equal("Folder", ControlChildren(combo)[0].GetName());
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
    public void FocusingTheComboBoxFocusesItsEdit(string themeName)
    {
        (Window window, EditableComboBox field, _) = Create(themeName, static field => AutomationProperties.SetName(field, "Folder"));

        try
        {
            AutomationPeer combo = ComboPeer(field);
            Assert.True(combo.IsKeyboardFocusable());

            combo.SetFocus();
            Dispatcher.UIThread.RunJobs();
            Assert.IsType<EditableComboBox.InnerTextBox>(window.FocusManager?.GetFocusedElement());
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
    public void AnAppStyleOnTheInnerComboBoxStillWins(string themeName)
    {
        (Window window, EditableComboBox field, _) = Create(themeName, static field =>
        {
            AutomationProperties.SetName(field, "Folder");
            field.Styles.Add(new Style(static x => x.OfType<EditableComboBox.InnerComboBox>())
            {
                Setters = { new Setter(AutomationProperties.NameProperty, "Destination folder") },
            });
        });

        try
        {
            Assert.Equal("Destination folder", ComboPeer(field).GetName());
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
    public void TheValueIsTheTextAndCanBeSet(string themeName)
    {
        (Window window, EditableComboBox field, _) = Create(themeName, static _ => { });

        try
        {
            IValueProvider value = Assert.IsAssignableFrom<IValueProvider>(ComboPeer(field));
            Assert.False(value.IsReadOnly);

            field.Value = "A folder not in the list";
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("A folder not in the list", value.Value);

            value.SetValue("Beta");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("Beta", field.Value);
            Assert.Equal("Beta", value.Value);
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
    public void ADisabledFieldIsReadOnly(string themeName)
    {
        (Window window, EditableComboBox field, _) = Create(themeName, static field => field.IsEnabled = false);

        try
        {
            IValueProvider value = Assert.IsAssignableFrom<IValueProvider>(ComboPeer(field));
            Assert.True(value.IsReadOnly);
            Assert.Throws<ElementNotEnabledException>(() => value.SetValue("Beta"));
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
    public void ExpandingTheComboBoxOpensTheDropDown(string themeName)
    {
        (Window window, EditableComboBox field, _) = Create(themeName, static _ => { });

        try
        {
            IExpandCollapseProvider expand = Assert.IsAssignableFrom<IExpandCollapseProvider>(ComboPeer(field));
            expand.Expand();
            Dispatcher.UIThread.RunJobs();
            Assert.True(field.IsDropDownOpen);
            Assert.Equal(ExpandCollapseState.Expanded, expand.ExpandCollapseState);

            expand.Collapse();
            Dispatcher.UIThread.RunJobs();
            Assert.False(field.IsDropDownOpen);
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
    public void ValidationErrorsAreReadAsHelpText(string themeName)
    {
        (Window window, EditableComboBox field, _) = Create(themeName, static _ => { });

        try
        {
            AutomationPeer combo = ComboPeer(field);
            DataValidationErrors.SetError(field, new Exception("Choose a folder"));
            Dispatcher.UIThread.RunJobs();
            Assert.Contains("Choose a folder", combo.GetHelpText());

            AutomationProperties.SetHelpText(field, "Where the entry is saved");
            Assert.Equal("Where the entry is saved", combo.GetHelpText());

            AutomationProperties.SetHelpText(field, null);
            DataValidationErrors.SetError(field, null);
            Dispatcher.UIThread.RunJobs();
            Assert.True(string.IsNullOrEmpty(combo.GetHelpText()));
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
    public void AnAppStyledInnerIdSurvivesALaterFieldIdChange(string themeName)
    {
        (Window window, EditableComboBox field, _) = Create(themeName, static field =>
        {
            AutomationProperties.SetAutomationId(field, "Folder");
            field.Styles.Add(new Style(static x => x.OfType<EditableComboBox.InnerComboBox>())
            {
                Setters = { new Setter(AutomationProperties.AutomationIdProperty, "StyledFolder") },
            });
        });

        try
        {
            Assert.Equal("StyledFolder", ComboPeer(field).GetAutomationId());

            AutomationProperties.SetAutomationId(field, "Destination");
            Assert.Equal("StyledFolder", ComboPeer(field).GetAutomationId());
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
    public void AReadOnlyChangeIsAnnounced(string themeName)
    {
        (Window window, EditableComboBox field, _) = Create(themeName, static _ => { });

        try
        {
            AutomationPeer combo = ComboPeer(field);
            List<(object? Old, object? New)> changes = [];
            combo.PropertyChanged += (_, e) =>
            {
                if (e.Property == ValuePatternIdentifiers.IsReadOnlyProperty)
                {
                    changes.Add((e.OldValue, e.NewValue));
                }
            };

            field.IsEnabled = false;
            Dispatcher.UIThread.RunJobs();
            Assert.True(Assert.IsAssignableFrom<IValueProvider>(combo).IsReadOnly);

            field.IsEnabled = true;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal([(false, true), (true, false)], changes);
        }
        finally
        {
            window.Close();
        }
    }

    private static (Window Window, EditableComboBox Field, Button Button) Create(string themeName, Action<EditableComboBox> configure)
    {
        SetTheme(themeName);

        Button button = new() { Content = "Browse" };
        AutomationProperties.SetName(button, "Browse");
        EditableComboBox field = new()
        {
            ItemsSource = new[] { "Alpha", "Beta", "Gamma" },
            InnerRightContent = new Avalonia.Collections.AvaloniaList<Control> { button },
        };
        configure(field);

        Window window = new() { Content = new StackPanel { Children = { field } }, Width = 400, Height = 300 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, field, button);
    }

    private static AutomationPeer ComboPeer(EditableComboBox field) =>
        ControlAutomationPeer.CreatePeerForElement(field.GetVisualDescendants().OfType<EditableComboBox.InnerComboBox>().Single());

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
