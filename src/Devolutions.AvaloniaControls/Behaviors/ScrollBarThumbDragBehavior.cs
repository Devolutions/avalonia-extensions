namespace Devolutions.AvaloniaControls.Behaviors;

using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;

/// <summary>
/// Behavior that adds a ":thumb-dragging" pseudoclass to a ScrollBar when its thumb is being dragged.
/// This allows styling the ScrollBar differently during thumb drag (e.g., keeping arrows visible).
/// </summary>
public static class ScrollBarThumbDragBehavior
{
    private const string ThumbDraggingPseudoClass = ":thumb-dragging";
    private static readonly PropertyInfo? PseudoClassesProperty;

    static ScrollBarThumbDragBehavior()
    {
        // Get the protected PseudoClasses property via reflection
        PseudoClassesProperty = typeof(StyledElement).GetProperty(
            "PseudoClasses",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        EnableProperty.Changed.Subscribe(args =>
        {
            if (args.Sender is ScrollBar scrollBar)
            {
                bool enable = args.NewValue.GetValueOrDefault<bool>();
                if (enable)
                {
                    Enable(scrollBar);
                }
                else
                {
                    Disable(scrollBar);
                }
            }
        });
    }

    public static readonly AttachedProperty<bool> EnableProperty =
        AvaloniaProperty.RegisterAttached<ScrollBar, bool>("Enable", typeof(ScrollBarThumbDragBehavior));

    public static void SetEnable(ScrollBar element, bool value) => element.SetValue(EnableProperty, value);

    public static bool GetEnable(ScrollBar element) => element.GetValue(EnableProperty);

    private static void Enable(ScrollBar scrollBar)
    {
        // Wait for template to be applied
        if (scrollBar.IsLoaded)
        {
            SetupThumbHandlers(scrollBar);
        }
        else
        {
            scrollBar.Loaded += OnScrollBarLoaded;
        }
    }

    private static void Disable(ScrollBar scrollBar)
    {
        scrollBar.Loaded -= OnScrollBarLoaded;
        SetPseudoClass(scrollBar, ThumbDraggingPseudoClass, false);
        
        // Remove handlers from thumb if it exists
        if (FindThumb(scrollBar) is { } thumb)
        {
            thumb.RemoveHandler(InputElement.PointerPressedEvent, OnThumbPointerPressed);
            thumb.RemoveHandler(InputElement.PointerReleasedEvent, OnThumbPointerReleased);
        }
    }

    private static void OnScrollBarLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is ScrollBar scrollBar)
        {
            scrollBar.Loaded -= OnScrollBarLoaded;
            SetupThumbHandlers(scrollBar);
        }
    }

    private static void SetupThumbHandlers(ScrollBar scrollBar)
    {
        if (FindThumb(scrollBar) is { } thumb)
        {
            thumb.AddHandler(InputElement.PointerPressedEvent, OnThumbPointerPressed, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            thumb.AddHandler(InputElement.PointerReleasedEvent, OnThumbPointerReleased, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        }
    }

    private static void OnThumbPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Thumb thumb && FindScrollBar(thumb) is { } scrollBar)
        {
            SetPseudoClass(scrollBar, ThumbDraggingPseudoClass, true);
        }
    }

    private static void OnThumbPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is Thumb thumb && FindScrollBar(thumb) is { } scrollBar)
        {
            SetPseudoClass(scrollBar, ThumbDraggingPseudoClass, false);
        }
    }

    private static void SetPseudoClass(StyledElement element, string pseudoClass, bool value)
    {
        if (PseudoClassesProperty?.GetValue(element) is IPseudoClasses pseudoClasses)
        {
            pseudoClasses.Set(pseudoClass, value);
        }
    }

    private static Thumb? FindThumb(ScrollBar scrollBar)
    {
        // Find the Thumb in the ScrollBar's visual tree
        return scrollBar.GetVisualDescendants()
            .OfType<Thumb>()
            .FirstOrDefault();
    }

    private static ScrollBar? FindScrollBar(Thumb thumb)
    {
        // Walk up the visual tree to find the parent ScrollBar
        return thumb.FindAncestorOfType<ScrollBar>();
    }
}
