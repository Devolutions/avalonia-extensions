namespace Devolutions.AvaloniaTheme.DevExpress.Behaviors;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;

internal static class MenuSeparatorAlignmentBehavior
{
    private const string SingleIconSlotClass = "single-icon-slot";

    public static readonly AttachedProperty<bool> EnableProperty =
        AvaloniaProperty.RegisterAttached<Separator, bool>("Enable", typeof(MenuSeparatorAlignmentBehavior));

    /// <summary>
    ///   Marks a menu container as styled by this theme.
    /// </summary>
    /// <remarks>
    ///   Set from the DevExpress menu control themes. Menus styled by another pack (for example
    ///   the standalone MacOS menu pack) resolve their own control themes instead, so they never
    ///   carry this marker and the alignment behavior leaves their separators alone.
    /// </remarks>
    public static readonly AttachedProperty<bool> IsMenuOwnerProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsMenuOwner", typeof(MenuSeparatorAlignmentBehavior));

    private const double DefaultIconColumnWidth = 30;

    private static readonly Thickness defaultMargin = new(7, 1, 0, 1);

    static MenuSeparatorAlignmentBehavior()
    {
        EnableProperty.Changed.Subscribe(args =>
        {
            if (args.Sender is not Separator separator)
            {
                return;
            }

            if (args.NewValue.GetValueOrDefault<bool>())
            {
                separator.Loaded += OnLoaded;
                UpdateMargin(separator);
            }
            else
            {
                separator.Loaded -= OnLoaded;
            }
        });
    }

    public static void SetEnable(Separator element, bool value) => element.SetValue(EnableProperty, value);

    public static bool GetEnable(Separator element) => element.GetValue(EnableProperty);

    public static void SetIsMenuOwner(Control element, bool value) => element.SetValue(IsMenuOwnerProperty, value);

    public static bool GetIsMenuOwner(Control element) => element.GetValue(IsMenuOwnerProperty);

    private static void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is Separator separator)
        {
            UpdateMargin(separator);
        }
    }

    private static void UpdateMargin(Separator separator)
    {
        // The margin is applied as a local value, which outranks every style. Writing it into a
        // menu owned by another pack would override that pack's separator metrics and silently
        // change its menu height, so only touch menus this theme actually styles.
        if (!IsOwnedByThisTheme(separator))
        {
            return;
        }

        Thickness baseMargin = GetResource(separator, "DevExMenuSeparatorMargin", defaultMargin);
        double iconColumnWidth = GetResource(separator, "DevExMenuSeparatorIconColumnWidth", DefaultIconColumnWidth);
        int iconColumnCount = GetIconColumnCount(separator);

        separator.Margin = new Thickness(
            baseMargin.Left + (iconColumnCount * iconColumnWidth),
            baseMargin.Top,
            baseMargin.Right,
            baseMargin.Bottom);
    }

    /// <summary>
    ///   Determines whether the separator lives in a menu styled by this theme.
    /// </summary>
    private static bool IsOwnedByThisTheme(Separator separator)
    {
        ILogical? current = separator.GetLogicalParent();

        while (current is not null)
        {
            if (current is ContextMenu or MenuFlyoutPresenter or Menu or MenuItem)
            {
                return GetIsMenuOwner((Control)current);
            }

            current = current.GetLogicalParent();
        }

        // Not inside a recognised menu container: keep the historical behavior.
        return true;
    }

    private static int GetIconColumnCount(Separator separator)
    {
        ItemsControl? owner = FindOwner(separator);
        if (owner is null)
        {
            return 1;
        }

        var hasToggleColumn = false;
        var hasIconColumn = false;
        bool useSingleIconSlot = owner.Classes.Contains(SingleIconSlotClass);

        foreach (MenuItem menuItem in owner.Items.OfType<MenuItem>())
        {
            if (IsSeparatorMenuItem(menuItem))
            {
                continue;
            }

            hasToggleColumn |= menuItem.ToggleType != MenuItemToggleType.None || menuItem.IsChecked;
            hasIconColumn |= menuItem.Icon is not null;
        }

        if (useSingleIconSlot)
        {
            return hasToggleColumn || hasIconColumn ? 1 : 0;
        }

        return (hasToggleColumn ? 1 : 0) + (hasIconColumn ? 1 : 0);
    }

    private static ItemsControl? FindOwner(Separator separator)
    {
        ItemsControl? owner = separator.FindLogicalAncestorOfType<ItemsControl>();

        return owner is MenuItem menuItem && IsSeparatorMenuItem(menuItem)
            ? menuItem.FindLogicalAncestorOfType<ItemsControl>()
            : owner;
    }

    private static bool IsSeparatorMenuItem(MenuItem menuItem) => menuItem.Header as string == "-";

    private static T GetResource<T>(Control control, string key, T fallback)
    {
        return control.TryFindResource(key, control.ActualThemeVariant, out object? value) && value is T typedValue
            ? typedValue
            : fallback;
    }
}
