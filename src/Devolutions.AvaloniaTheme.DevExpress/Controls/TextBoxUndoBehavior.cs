namespace Devolutions.AvaloniaTheme.DevExpress.Controls;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

/// <summary>
/// Attached behavior that resets the TextBox undo stack once the control is loaded,
/// so that programmatically-set initial text (e.g. via data binding) is not treated
/// as an undoable action by the user.
/// </summary>
/// <remarks>
/// Avalonia records every programmatic <see cref="TextBox.Text"/> assignment (including
/// initial binding evaluations) on the internal undo stack, which causes <see cref="TextBox.CanUndo"/>
/// to be <see langword="true"/> before the user has typed anything. Undoing at that point clears
/// the field entirely and leaves the user with no way to recover via Undo.
/// Toggling <see cref="TextBox.IsUndoEnabled"/> off then on in the <c>Loaded</c> handler
/// flushes the stack cleanly, making the initial text the non-undoable baseline.
/// </remarks>
public static class TextBoxUndoBehavior
{
    public static readonly AttachedProperty<bool> ClearUndoOnLoadedProperty =
        AvaloniaProperty.RegisterAttached<TextBox, bool>(
            "ClearUndoOnLoaded", typeof(TextBoxUndoBehavior));

    static TextBoxUndoBehavior()
    {
        ClearUndoOnLoadedProperty.Changed.AddClassHandler<TextBox>(OnPropertyChanged);
    }

    public static bool GetClearUndoOnLoaded(TextBox element) =>
        element.GetValue(ClearUndoOnLoadedProperty);

    public static void SetClearUndoOnLoaded(TextBox element, bool value) =>
        element.SetValue(ClearUndoOnLoadedProperty, value);

    private static void OnPropertyChanged(TextBox tb, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
            tb.Loaded += OnLoaded;
        else
            tb.Loaded -= OnLoaded;
    }

    private static void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is not TextBox tb) return;

        // Unsubscribe immediately — we only want to clear once on initial load,
        // not on every subsequent re-attach to the visual tree.
        tb.Loaded -= OnLoaded;

        if (tb.IsUndoEnabled)
        {
            // Use SetCurrentValue to avoid overwriting a consumer binding on IsUndoEnabled.
            tb.SetCurrentValue(TextBox.IsUndoEnabledProperty, false);
            tb.SetCurrentValue(TextBox.IsUndoEnabledProperty, true);
        }
    }
}
