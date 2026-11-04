namespace Devolutions.AvaloniaTheme.WinUI.Internal;

using Avalonia;
using Avalonia.Controls;

public static class ListBoxScrollBehavior
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<ListBox, bool>("IsEnabled", typeof(ListBoxScrollBehavior));

    public static readonly AttachedProperty<WrapPanel?> RealizedWrapPanelProperty =
        AvaloniaProperty.RegisterAttached<ListBox, WrapPanel?>("RealizedWrapPanel", typeof(ListBoxScrollBehavior));

    static ListBoxScrollBehavior()
    {
        IsEnabledProperty.Changed.AddClassHandler<ListBox>((list, args) =>
        {
            if (args.GetNewValue<bool>())
            {
                list.LayoutUpdated += OnLayoutUpdated;
                UpdatePanel(list);
            }
            else
            {
                list.LayoutUpdated -= OnLayoutUpdated;
                list.ClearValue(RealizedWrapPanelProperty);
            }
        });
    }

    public static bool GetIsEnabled(ListBox list) => list.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(ListBox list, bool value) => list.SetValue(IsEnabledProperty, value);
    public static WrapPanel? GetRealizedWrapPanel(ListBox list) => list.GetValue(RealizedWrapPanelProperty);

    private static void OnLayoutUpdated(object? sender, EventArgs args)
    {
        if (sender is ListBox list)
            UpdatePanel(list);
    }

    // ItemsPanelRoot is a CLR property without change notification; publish its realized panel after layout.
    private static void UpdatePanel(ListBox list) =>
        list.SetValue(RealizedWrapPanelProperty, list.ItemsPanelRoot as WrapPanel);
}
