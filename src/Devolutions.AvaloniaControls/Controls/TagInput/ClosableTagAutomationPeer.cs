namespace Devolutions.AvaloniaControls.Controls;

using Avalonia.Automation.Peers;

internal sealed class ClosableTagAutomationPeer(ClosableTag owner) : ContentControlAutomationPeer(owner)
{
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ListItem;

    // ContentControlAutomationPeer leaves its control out of the tree; these are what the user interacts with.
    protected override bool IsContentElementCore() => true;

    protected override bool IsControlElementCore() => true;

    protected override string? GetNameCore() => AccessibleContentText.Get(owner);

    // The tag's text is already its name; only the close button is left to reach.
    protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() =>
        owner.CloseButton is { } close ? [CreatePeerForElement(close)] : [];
}
