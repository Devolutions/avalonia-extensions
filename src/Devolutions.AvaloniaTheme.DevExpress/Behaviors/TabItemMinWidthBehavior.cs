// ReSharper disable AccessToStaticMemberViaDerivedType
namespace Devolutions.AvaloniaTheme.DevExpress.Behaviors;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

/// <summary>
/// Behavior that reserves the "selected-state" width for each TabItem so that tabs
/// don't shift when the selected tab gets bold/larger font.
/// </summary>
/// <remarks>
/// After the TabItem is loaded, the behavior temporarily toggles the :selected
/// pseudo-class to trigger all selected-state styles (font weight, size, padding, etc.),
/// measures the content presenter in both states, and sets MinWidth on the ContentPanel
/// to the maximum of the two. This is theme-agnostic — any style changes applied by
/// :selected will be captured automatically.
/// </remarks>
internal static class TabItemMinWidthBehavior
{
    public static readonly AttachedProperty<bool> EnableProperty =
        AvaloniaProperty.RegisterAttached<TabItem, bool>("Enable", typeof(TabItemMinWidthBehavior));

    public static void SetEnable(TabItem element, bool value) => element.SetValue(EnableProperty, value);

    public static bool GetEnable(TabItem element) => element.GetValue(EnableProperty);

    static TabItemMinWidthBehavior()
    {
        EnableProperty.Changed.Subscribe(args =>
        {
            if (args.Sender is TabItem tab)
            {
                bool enable = args.NewValue.GetValueOrDefault<bool>();
                if (enable)
                {
                    Enable(tab);
                }
                else
                {
                    Disable(tab);
                }
            }
        });
    }

    private static void Enable(TabItem tab)
    {
        if (tab.IsLoaded && tab.IsAttachedToVisualTree())
        {
            ScheduleMeasurement(tab);
        }
        else
        {
            tab.Loaded += OnTabLoaded;
        }
    }

    private static void Disable(TabItem tab)
    {
        tab.Loaded -= OnTabLoaded;

        tab.FindContentPresenter()
            ?.SetValue(Panel.MinWidthProperty, AvaloniaProperty.UnsetValue);
    }

    private static void OnTabLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is TabItem tab)
        {
            tab.Loaded -= OnTabLoaded;
            ScheduleMeasurement(tab);
        }
    }

    private static void ScheduleMeasurement(TabItem tab)
    {
        Dispatcher.UIThread.Post(() => MeasureAndSetMinWidth(tab), DispatcherPriority.Loaded);
    }

    private static void MeasureAndSetMinWidth(TabItem tab)
    {
        if (!tab.IsAttachedToVisualTree()) return;

        if (tab.FindContentPresenter() is not ContentPresenter contentPresenter) return;

        bool wasSelected = ((IPseudoClasses)tab.Classes).Contains(":selected");

        try
        {
            // Temporarily apply :selected pseudo-class to trigger all selected-state styles
            // (font weight, font size, padding, etc.) — this is theme-agnostic
            if (!wasSelected)
            {
                ((IPseudoClasses)tab.Classes).Set(":selected", true);
            }

            // Force styles to re-evaluate and measure the content presenter
            contentPresenter.InvalidateMeasure();
            contentPresenter.Measure(Size.Infinity);
            double selectedWidth = contentPresenter.DesiredSize.Width;

            // Measure with normal (unselected) state
            ((IPseudoClasses)tab.Classes).Set(":selected", false);

            contentPresenter.InvalidateMeasure();
            contentPresenter.Measure(Size.Infinity);
            double normalWidth = contentPresenter.DesiredSize.Width;

            // Set MinWidth on the ContentPanel to the maximum of both states
            double minWidth = Math.Max(selectedWidth, normalWidth);
            if (minWidth > 0)
            {
                contentPresenter.MinWidth = minWidth;
            }
        }
        finally
        {
            // Always restore the original :selected state
            ((IPseudoClasses)tab.Classes).Set(":selected", wasSelected);
            contentPresenter.InvalidateMeasure();
        }
    }
    
    private static ContentPresenter? FindContentPresenter(this TabItem tab) => tab.FindDescendantOfType<ContentPresenter>(static c => c.Name == "PART_ContentPresenter");

    private static T? FindDescendantOfType<T>(this Visual visual, Func<T, bool> predicate)
        where T : Visual
    {
        foreach (var child in visual.GetVisualDescendants())
        {
            if (child is T typed && predicate(typed))
            {
                return typed;
            }
        }

        return null;
    }
}
