namespace Devolutions.AvaloniaTheme.MacOS.Controls;

using System;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

/// <summary>
/// Commands and platform-aware gestures used in the TextBox context menu flyout
/// that are not directly exposed as public API on <see cref="TextBox"/>.
/// </summary>
public static class TextBoxContextMenuCommands
{
    /// <summary>
    /// Platform-aware gesture for Undo (Cmd+Z on macOS).
    /// Mirrors how <see cref="TextBox.CutGesture"/> works.
    /// </summary>
    public static KeyGesture? UndoGesture =>
        Application.Current?.PlatformSettings?.HotkeyConfiguration.Undo.FirstOrDefault();

    /// <summary>
    /// Platform-aware gesture for Select All (Cmd+A on macOS).
    /// </summary>
    public static KeyGesture? SelectAllGesture =>
        Application.Current?.PlatformSettings?.HotkeyConfiguration.SelectAll.FirstOrDefault();

    /// <summary>
    /// Deletes the currently selected text in the target <see cref="TextBox"/>
    /// (passed as <c>CommandParameter</c>).
    /// Enablement is enforced via a separate XAML <c>IsEnabled</c> binding (CanCut);
    /// <see cref="ICommand.CanExecute"/> always returns <see langword="true"/>.
    /// </summary>
    public static readonly ICommand DeleteSelection = new TextBoxDeleteSelectionCommand();

    private sealed class TextBoxDeleteSelectionCommand : ICommand
    {
        // IsEnabled is controlled via a separate XAML binding (CanCut) — CanExecute is not used.
        public event EventHandler? CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            if (parameter is not TextBox tb) return;
            if (tb.IsReadOnly) return;

            int start = Math.Min(tb.SelectionStart, tb.SelectionEnd);
            int end = Math.Max(tb.SelectionStart, tb.SelectionEnd);
            int length = end - start;

            if (length == 0) return;

            tb.SetCurrentValue(TextBox.TextProperty, (tb.Text ?? string.Empty).Remove(start, length));
            tb.SelectionStart = start;
            tb.SelectionEnd = start;
        }
    }
}
