using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace SampleApp.DemoPages;

public partial class ColumnSearchAdornment : UserControl
{
    private readonly TextBox? searchBox;

    public ColumnSearchAdornment()
    {
        this.InitializeComponent();

        this.searchBox = this.FindControl<TextBox>("PART_SearchBox");

        // Watch the editor panel, not the TextBox: IsVisible is not inherited, so the box's own IsVisible
        // stays true the whole time and only its parent actually toggles.
        Control? editor = this.FindControl<DockPanel>("PART_Editor");
        editor?.GetObservable(IsVisibleProperty).Subscribe(new AnonymousObserver(this.OnEditorVisibilityChanged));
    }

    // Entering search mode should leave the caret in the field, so the user can type straight after
    // clicking the magnifier, and reopening a committed term preselects it so typing replaces it.
    // Deferred to Loaded priority: at the moment visibility flips the field has not been laid out yet, and
    // focusing a zero-sized control does nothing.
    private void OnEditorVisibilityChanged(bool isVisible)
    {
        if (!isVisible || this.searchBox is null)
        {
            return;
        }

        Dispatcher.UIThread.Post(
            () =>
            {
                if (this.searchBox is not { } box || !box.IsEffectivelyVisible)
                {
                    return;
                }

                box.Focus();
                box.SelectAll();
            },
            DispatcherPriority.Loaded);
    }

    private sealed class AnonymousObserver(Action<bool> onNext) : IObserver<bool>
    {
        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(bool value) => onNext(value);
    }
}
