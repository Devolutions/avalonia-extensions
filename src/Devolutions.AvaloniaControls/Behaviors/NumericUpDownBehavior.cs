namespace Devolutions.AvaloniaControls.Behaviors;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

public static class NumericUpDownBehavior
{
    public static readonly AttachedProperty<bool> NumericOnlyProperty =
        AvaloniaProperty.RegisterAttached<NumericUpDown, bool>("NumericOnly", typeof(NumericUpDownBehavior));

    static NumericUpDownBehavior()
    {
        NumericOnlyProperty.Changed.Subscribe(args =>
        {
            if (args.Sender is NumericUpDown nud)
            {
                var enable = args.NewValue.GetValueOrDefault<bool>();
                if (enable)
                {
                    nud.AddHandler(InputElement.TextInputEvent, OnTextInput, RoutingStrategies.Tunnel);
                }
                else
                {
                    nud.RemoveHandler(InputElement.TextInputEvent, OnTextInput);
                }
            }
        });
    }

    public static void SetNumericOnly(NumericUpDown element, bool value) => element.SetValue(NumericOnlyProperty, value);

    public static bool GetNumericOnly(NumericUpDown element) => element.GetValue(NumericOnlyProperty);

    private static void OnTextInput(object? sender, TextInputEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Text)) return;

        foreach (var c in e.Text)
        {
            if (!IsValidNumericChar(c))
            {
                e.Handled = true;
                return;
            }
        }
    }

    private static bool IsValidNumericChar(char c) => char.IsDigit(c) || c is '.' or ',' or '-' or '+';
}