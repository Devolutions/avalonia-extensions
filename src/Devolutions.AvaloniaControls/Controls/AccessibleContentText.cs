namespace Devolutions.AvaloniaControls.Controls;

using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;

internal static class AccessibleContentText
{
    // Content can be a data object shown through a template or a picture of a control: ToString isn't what users see.
    public static string? Get(ContentControl control)
    {
        if (AutomationProperties.GetName(control) is { Length: > 0 } name)
        {
            return name;
        }

        string text = string.Join(
            " ",
            control.GetVisualDescendants()
                .OfType<TextBlock>()
                .Where(block => !string.IsNullOrWhiteSpace(block.Text) && !IsInsideButton(block, control))
                .Select(static block => block.Text));
        if (text.Length > 0)
        {
            return text;
        }

        if (control.DataContext is Control shown && !ReferenceEquals(shown, control))
        {
            return ControlAutomationPeer.CreatePeerForElement(shown).GetName();
        }

        return control.Content as string;
    }

    private static bool IsInsideButton(Visual block, Visual owner) =>
        block.GetVisualAncestors().TakeWhile(ancestor => !ReferenceEquals(ancestor, owner)).Any(static ancestor => ancestor is Button and not ToggleButton);
}
