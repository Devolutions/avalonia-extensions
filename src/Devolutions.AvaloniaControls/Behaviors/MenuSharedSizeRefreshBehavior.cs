namespace Devolutions.AvaloniaControls.Behaviors;

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;

/// <summary>
/// Works around an Avalonia limitation where a <see cref="Grid"/> shared-size group never
/// shrinks once it has measured a width.
///
/// <para>
/// Menu item templates align their leading toggle (check/radio) and icon columns across
/// sibling items using <c>Grid.IsSharedSizeScope</c> + <c>SharedSizeGroup</c>. When the set of
/// occupied leading columns shrinks — the last checked item is unchecked, the last item with an
/// icon is removed, or the <c>single-icon-slot</c> class is toggled off/on — the vacated column
/// keeps its previously measured width, leaving a phantom empty column that pushes the header
/// text across.
/// </para>
///
/// <para>
/// This behavior is attached to every <see cref="MenuItem"/> (via the control theme). All items
/// that live under the same shared-size scope share a single <see cref="ScopeState"/>, which
/// watches the scope for layout changes and, whenever the "occupied columns" signature changes,
/// rebuilds the scope by toggling <c>Grid.IsSharedSizeScope</c> off and back on. Rebuilding forces
/// a fresh measurement pass so columns that are no longer needed collapse to zero.
/// </para>
/// </summary>
public static class MenuSharedSizeRefreshBehavior
{
    private const string SingleIconSlotClass = "single-icon-slot";

    public static readonly AttachedProperty<bool> EnableProperty =
        AvaloniaProperty.RegisterAttached<MenuItem, bool>("Enable", typeof(MenuSharedSizeRefreshBehavior));

    // One state per shared-size scope owner, shared by every menu item under it.
    private static readonly ConditionalWeakTable<Control, ScopeState> states = new();

    static MenuSharedSizeRefreshBehavior()
    {
        EnableProperty.Changed.Subscribe(static args =>
        {
            if (args.Sender is not MenuItem menuItem)
            {
                return;
            }

            if (args.NewValue.GetValueOrDefault<bool>())
            {
                menuItem.AttachedToVisualTree += OnItemAttached;
                if (menuItem.GetVisualParent() is not null)
                {
                    Register(menuItem);
                }
            }
            else
            {
                menuItem.AttachedToVisualTree -= OnItemAttached;
            }
        });
    }

    public static void SetEnable(MenuItem element, bool value) => element.SetValue(EnableProperty, value);

    public static bool GetEnable(MenuItem element) => element.GetValue(EnableProperty);

    private static void OnItemAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is MenuItem menuItem)
        {
            Register(menuItem);
        }
    }

    private static void Register(MenuItem menuItem)
    {
        Control? scopeOwner = FindScopeOwner(menuItem);
        if (scopeOwner is null)
        {
            return;
        }

        if (!states.TryGetValue(scopeOwner, out _))
        {
            states.Add(scopeOwner, new ScopeState(scopeOwner));
        }
    }

    private static Control? FindScopeOwner(Visual from)
    {
        foreach (Visual ancestor in from.GetVisualAncestors())
        {
            if (ancestor is Control control && Grid.GetIsSharedSizeScope(control))
            {
                return control;
            }
        }

        return null;
    }

    private sealed class ScopeState
    {
        private readonly Control scopeOwner;
        private string lastSignature = string.Empty;
        private DispatcherOperation? scheduled;
        private bool rescoping;

        public ScopeState(Control scopeOwner)
        {
            this.scopeOwner = scopeOwner;
            this.scopeOwner.LayoutUpdated += this.OnLayoutUpdated;
            this.scopeOwner.DetachedFromVisualTree += this.OnDetached;
        }

        private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e)
        {
            this.scopeOwner.LayoutUpdated -= this.OnLayoutUpdated;
            this.scopeOwner.DetachedFromVisualTree -= this.OnDetached;
            this.scheduled?.Abort();
            states.Remove(this.scopeOwner);
        }

        private void OnLayoutUpdated(object? sender, EventArgs e)
        {
            if (this.rescoping)
            {
                return;
            }

            string signature = this.ComputeSignature();
            if (signature == this.lastSignature)
            {
                return;
            }

            this.lastSignature = signature;
            this.ScheduleRescope();
        }

        private string ComputeSignature()
        {
            var builder = new StringBuilder();

            // Whether the single-icon-slot mode is active for this scope (class typically sits on
            // the menu container that is templated with this shared-size scope, or an ancestor).
            bool singleIconSlot = this.scopeOwner.GetVisualAncestors()
                .Prepend(this.scopeOwner)
                .Any(v => v is StyledElement styled and (ContextMenu or Menu or MenuFlyoutPresenter or MenuItem) 
                          && styled.Classes.Contains(SingleIconSlotClass));
            builder.Append(singleIconSlot ? 'S' : '-').Append('|');

            foreach (MenuItem item in this.scopeOwner.GetVisualDescendants().OfType<MenuItem>())
            {
                // Only consider items that belong directly to this scope, not items nested in a
                // deeper scope (their own submenu popup).
                if (FindScopeOwner(item) != this.scopeOwner)
                {
                    continue;
                }

                builder.Append(item.Icon is not null ? 'I' : '-');
                builder.Append(item.IsChecked ? 'C' : '-');
            }

            return builder.ToString();
        }

        private void ScheduleRescope()
        {
            this.scheduled?.Abort();
            this.scheduled = Dispatcher.UIThread.InvokeAsync(this.Rescope, DispatcherPriority.Render);
        }

        private void Rescope()
        {
            this.scheduled = null;

            if (!Grid.GetIsSharedSizeScope(this.scopeOwner))
            {
                return;
            }

            // Drop and recreate the shared-size scope so every column is measured from scratch;
            // columns with no content this pass collapse instead of keeping a stale width.
            this.rescoping = true;
            Grid.SetIsSharedSizeScope(this.scopeOwner, false);
            Dispatcher.UIThread.Post(
                () =>
                {
                    Grid.SetIsSharedSizeScope(this.scopeOwner, true);
                    this.rescoping = false;
                },
                DispatcherPriority.Background);
        }
    }
}
