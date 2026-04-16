namespace Devolutions.AvaloniaControls.Controls;

using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using System;
using System.Reactive.Disposables;

public class EditableComboBoxHighlightTextBlock : SearchHighlightTextBlock
{
    private CompositeDisposable? bindings;

    protected override Type StyleKeyOverride => typeof(SearchHighlightTextBlock);

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        
        this.bindings?.Dispose();
        this.bindings = new CompositeDisposable();
        
        EditableComboBoxItem? item = this.FindAncestorOfType<EditableComboBoxItem>();
        if (item != null)
        {
            this.bindings.Add(item
                .GetObservable(EditableComboBoxItem.FilterHighlightTextProperty)
                .Subscribe(text => this.Search = text));
            if (!this.IsSet(ContentControl.ContentProperty))
            {
                this.bindings.Add(item
                    .GetObservable(EditableComboBoxItem.ValueProperty)
                    .Subscribe(value => this.Content = value));
            }
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        this.bindings?.Dispose();
        this.bindings = null;
    }
}
