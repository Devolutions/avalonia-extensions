namespace Devolutions.AvaloniaControls.Behaviors;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Input.Platform;
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
                bool enable = args.NewValue.GetValueOrDefault<bool>();
                if (enable)
                {
                    nud.AddHandler(InputElement.TextInputEvent, OnTextInput, RoutingStrategies.Tunnel);
                    nud.TemplateApplied += OnTemplateApplied;

                    // If template is already applied, hook up now
                    if (nud.GetTemplateChildren().OfType<TextBox>().FirstOrDefault() is { } textBox)
                    {
                        HookTextBox(textBox);
                    }
                }
                else
                {
                    nud.RemoveHandler(InputElement.TextInputEvent, OnTextInput);
                    nud.TemplateApplied -= OnTemplateApplied;
                }
            }
        });
    }

    public static void SetNumericOnly(NumericUpDown element, bool value) => element.SetValue(NumericOnlyProperty, value);

    public static bool GetNumericOnly(NumericUpDown element) => element.GetValue(NumericOnlyProperty);

    private static void OnTemplateApplied(object? sender, TemplateAppliedEventArgs e)
    {
        if (e.NameScope.Find<TextBox>("PART_TextBox") is { } textBox)
        {
            HookTextBox(textBox);
        }
    }

    private static void HookTextBox(TextBox textBox)
    {
        textBox.AddHandler(InputElement.TextInputEvent, OnTextInput, RoutingStrategies.Tunnel);
        textBox.PastingFromClipboard += OnPastingFromClipboard;
    }

    private static void OnTextInput(object? sender, TextInputEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Text)) return;
        e.Text = string.Concat(e.Text.Where(IsValidNumericChar));
    }

    private static void OnPastingFromClipboard(object? sender, RoutedEventArgs e)
    {
        if (sender is not TextBox textBox) return;

        e.Handled = true;

        IClipboard? clipboard = TopLevel.GetTopLevel(textBox)?.Clipboard;
        if (clipboard == null) return;

        _ = HandlePasteAsync(textBox, clipboard);
    }

    private static async Task HandlePasteAsync(TextBox textBox, IClipboard clipboard)
    {
        try
        {
            string? clipboardText = await clipboard.TryGetTextAsync();
            if (string.IsNullOrEmpty(clipboardText)) return;

            string filtered = string.Concat(clipboardText.Where(IsValidNumericChar));
            if (string.IsNullOrEmpty(filtered)) return;

            // Insert filtered text at caret position
            int selectionStart = textBox.SelectionStart;
            int selectionEnd = textBox.SelectionEnd;
            string currentText = textBox.Text ?? string.Empty;

            int start = Math.Min(selectionStart, selectionEnd);
            int end = Math.Max(selectionStart, selectionEnd);

            string newText = currentText[..start] + filtered + currentText[end..];
            textBox.Text = newText;
            textBox.CaretIndex = start + filtered.Length;
        }
        catch
        {
            // Silently ignore clipboard errors
        }
    }

    private static bool IsValidNumericChar(char c) => char.IsDigit(c) || c is '.' or ',' or '-' or '+';
}