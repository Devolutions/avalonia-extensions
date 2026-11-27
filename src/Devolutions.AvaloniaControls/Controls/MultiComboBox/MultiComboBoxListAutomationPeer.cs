namespace Devolutions.AvaloniaControls.Controls;

using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

// The drop-down's items track their own IsSelected, not the list's selection model.
internal sealed class MultiComboBoxListAutomationPeer(ItemsControl owner) : ItemsControlAutomationPeer(owner), ISelectionProvider
{
    public bool CanSelectMultiple => true;

    public bool IsSelectionRequired => false;

    public IReadOnlyList<AutomationPeer> GetSelection() => SelectedItemPeers((ItemsControl)this.Owner);

    internal static IReadOnlyList<AutomationPeer> SelectedItemPeers(ItemsControl list) =>
        list.GetRealizedContainers().OfType<MultiComboBoxItem>().Where(static item => item.IsSelected).Select(ControlAutomationPeer.CreatePeerForElement).ToList();
}
