using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Devolutions.AvaloniaTheme.MacOS.Internal;

internal class SplitButtonFlyoutPresenterThemeBehavior
{
    private const string PressedClass = "splitbutton-pressed";

    public static readonly AttachedProperty<double> MenuFlyoutHorizontalOffsetProperty =
        AvaloniaProperty.RegisterAttached<SplitButtonFlyoutPresenterThemeBehavior, SplitButton, double>(
            "MenuFlyoutHorizontalOffset");

    static SplitButtonFlyoutPresenterThemeBehavior()
    {
        MenuFlyoutHorizontalOffsetProperty.Changed.AddClassHandler<SplitButton>(OnBehaviorChanged);
    }

    public static void SetMenuFlyoutHorizontalOffset(AvaloniaObject element, double value)
    {
        element.SetValue(MenuFlyoutHorizontalOffsetProperty, value);
    }

    public static double GetMenuFlyoutHorizontalOffset(AvaloniaObject element)
    {
        return element.GetValue(MenuFlyoutHorizontalOffsetProperty);
    }

    private static void OnBehaviorChanged(SplitButton splitButton, AvaloniaPropertyChangedEventArgs args)
    {
        splitButton.PropertyChanged -= OnSplitButtonPropertyChanged;
        splitButton.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
        splitButton.RemoveHandler(InputElement.PointerReleasedEvent, OnPointerReleased);
        splitButton.RemoveHandler(InputElement.PointerCaptureLostEvent, OnPointerCaptureLost);

        splitButton.PropertyChanged += OnSplitButtonPropertyChanged;
        splitButton.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Bubble, true);
        splitButton.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Bubble, true);
        splitButton.AddHandler(InputElement.PointerCaptureLostEvent, OnPointerCaptureLost, RoutingStrategies.Bubble, true);

        ApplySettingsToFlyout(splitButton);
    }

    private static void OnPointerPressed(object? sender, PointerPressedEventArgs args)
    {
        if (sender is not SplitButton splitButton)
        {
            return;
        }

        if (!args.GetCurrentPoint(splitButton).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (!splitButton.Classes.Contains(PressedClass))
        {
            splitButton.Classes.Add(PressedClass);
        }
    }

    private static void OnPointerReleased(object? sender, PointerReleasedEventArgs args)
    {
        ClearPressedClass(sender as SplitButton);
    }

    private static void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs args)
    {
        ClearPressedClass(sender as SplitButton);
    }

    private static void ClearPressedClass(SplitButton? splitButton)
    {
        if (splitButton?.Classes.Contains(PressedClass) == true)
        {
            splitButton.Classes.Remove(PressedClass);
        }
    }

    private static void OnSplitButtonPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs args)
    {
        if (sender is not SplitButton splitButton || args.Property != SplitButton.FlyoutProperty)
        {
            return;
        }

        ApplySettingsToFlyout(splitButton);
    }

    private static void ApplySettingsToFlyout(SplitButton splitButton)
    {
        if (splitButton.Flyout is not MenuFlyout menuFlyout)
        {
            return;
        }

        menuFlyout.HorizontalOffset = GetMenuFlyoutHorizontalOffset(splitButton);
    }
}






