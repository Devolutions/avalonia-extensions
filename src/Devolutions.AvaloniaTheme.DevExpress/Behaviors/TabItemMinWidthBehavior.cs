// ReSharper disable AccessToStaticMemberViaDerivedType
namespace Devolutions.AvaloniaTheme.DevExpress.Behaviors;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

/// <summary>
/// Behavior that reserves the "selected-state" width for each TabItem so that tabs
/// don't shift when the selected tab gets bold/larger font.
/// </summary>
/// <remarks>
/// After the TabItem is loaded, the behavior finds the PART_ContentPresenter inside
/// the template, temporarily applies the selected-state font properties (Bold, larger
/// font size) as local values, measures the presenter, and sets MinWidth on the
/// ContentPanel to the measured width. This works with arbitrary header templates,
/// not just string headers.
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

        // Clear the MinWidth we set on the ContentPanel
        var contentPanel = tab.FindDescendantOfType<Panel>(static p => p.Name == "ContentPanel");
        contentPanel?.SetValue(Panel.MinWidthProperty, AvaloniaProperty.UnsetValue);
    }

    private static void OnTabLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is TabItem tab)
        {
            tab.Loaded -= OnTabLoaded;
            ScheduleMeasurement(tab);
        }
    }

    /// <summary>
    /// Schedules the measurement for the next render pass so the template is fully
    /// applied and the content presenter has gone through at least one layout pass.
    /// </summary>
    private static void ScheduleMeasurement(TabItem tab)
    {
        Dispatcher.UIThread.Post(() => MeasureAndSetMinWidth(tab), DispatcherPriority.Loaded);
    }

    private static void MeasureAndSetMinWidth(TabItem tab)
    {
        if (!tab.IsAttachedToVisualTree()) return;

        // Find the content presenter inside the TabItem template
        var contentPresenter = tab.FindDescendantOfType<ContentPresenter>(static c => c.Name == "PART_ContentPresenter");

        if (contentPresenter is null) return;

        // Find the ContentPanel that wraps the content presenter
        var contentPanel = tab.FindDescendantOfType<Panel>(static p => p.Name == "ContentPanel");
        if (contentPanel is null) return;

        // Save original values
        bool hadFontWeight = contentPresenter.IsSet(ContentPresenter.FontWeightProperty);
        var originalFontWeight = contentPresenter.FontWeight;
        bool hadFontSize = contentPresenter.IsSet(ContentPresenter.FontSizeProperty);
        var originalFontSize = contentPresenter.FontSize;

        // Look up the selected font size from resources, fall back to 13
        double selectedFontSize = 13;
        if (tab.TryFindResource("TabItemSelectedFontSize", tab.ActualThemeVariant, out var resource)
            && resource is double fontSize)
        {
            selectedFontSize = fontSize;
        }

        try
        {
            // Apply selected-state font properties as local values on the presenter
            contentPresenter.FontWeight = FontWeight.Bold;
            contentPresenter.FontSize = selectedFontSize;

            // Force a measure with unconstrained space
            contentPresenter.InvalidateMeasure();
            contentPresenter.Measure(Size.Infinity);

            double boldWidth = contentPresenter.DesiredSize.Width;

            // Now measure with normal properties to get the base width
            if (hadFontWeight)
            {
                contentPresenter.FontWeight = originalFontWeight;
            }
            else
            {
                contentPresenter.SetValue(ContentPresenter.FontWeightProperty, AvaloniaProperty.UnsetValue);
            }

            if (hadFontSize)
            {
                contentPresenter.FontSize = originalFontSize;
            }
            else
            {
                contentPresenter.SetValue(ContentPresenter.FontSizeProperty, AvaloniaProperty.UnsetValue);
            }

            contentPresenter.InvalidateMeasure();
            contentPresenter.Measure(Size.Infinity);

            double normalWidth = contentPresenter.DesiredSize.Width;

            // Set MinWidth on the ContentPanel to the maximum of bold and normal widths
            double minWidth = Math.Max(boldWidth, normalWidth);
            if (minWidth > 0)
            {
                contentPanel.MinWidth = minWidth;
            }
        }
        finally
        {
            // Ensure we always restore original values even if something goes wrong
            if (hadFontWeight)
            {
                contentPresenter.FontWeight = originalFontWeight;
            }
            else
            {
                contentPresenter.SetValue(ContentPresenter.FontWeightProperty, AvaloniaProperty.UnsetValue);
            }

            if (hadFontSize)
            {
                contentPresenter.FontSize = originalFontSize;
            }
            else
            {
                contentPresenter.SetValue(ContentPresenter.FontSizeProperty, AvaloniaProperty.UnsetValue);
            }

            contentPresenter.InvalidateMeasure();
        }
    }

    /// <summary>
    /// Finds the first descendant of the specified type matching a predicate.
    /// </summary>
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
