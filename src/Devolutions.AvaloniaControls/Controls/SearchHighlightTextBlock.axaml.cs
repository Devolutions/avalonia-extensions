namespace Devolutions.AvaloniaControls.Controls;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using System;
using System.Reactive.Disposables;
using Avalonia.VisualTree;

public class SearchHighlightTextBlock : ContentControl
{
  private const string PART_TextBlock = "PART_TextBlock";

  private CompositeDisposable? bindings;

  // Tracks whether Content was set by our ValueProperty subscription (not by explicit XAML assignment).
  // Required to correctly handle VSP container recycling: when the container is detached from the
  // visual tree, we must clear Content so that IsSet(ContentProperty) resets to false, allowing
  // OnAttachedToVisualTree to re-establish the subscription with the recycled container's new Value.
  private bool contentManagedBySubscription;

  private TextBlock? textBlock;

  public static readonly DirectProperty<SearchHighlightTextBlock, string?> SearchProperty =
    AvaloniaProperty.RegisterDirect<SearchHighlightTextBlock, string?>(nameof(Search), static o => o.Search, static (o, v) => o.Search = v);

  public static readonly StyledProperty<IBrush?> HighlightBackgroundProperty =
    AvaloniaProperty.Register<SearchHighlightTextBlock, IBrush?>(nameof(HighlightBackground));

  public static readonly StyledProperty<IBrush?> HighlightForegroundProperty =
    AvaloniaProperty.Register<SearchHighlightTextBlock, IBrush?>(nameof(HighlightForeground));

  public SearchHighlightTextBlock()
  {
    this.GetObservable(ContentProperty).Subscribe(_ => this.UpdateInlines());
    this.GetObservable(SearchProperty).Subscribe(_ => this.UpdateInlines());
    this.GetObservable(HighlightBackgroundProperty).Subscribe(_ => this.UpdateInlines());
    this.GetObservable(HighlightForegroundProperty).Subscribe(_ => this.UpdateInlines());
  }

  public string? Search
  {
      get;
      set => this.SetAndRaise(SearchProperty, ref field, value);
  }

  public IBrush? HighlightBackground
  {
    get => this.GetValue(HighlightBackgroundProperty);
    set => this.SetValue(HighlightBackgroundProperty, value);
  }

  public IBrush? HighlightForeground
  {
    get => this.GetValue(HighlightForegroundProperty);
    set => this.SetValue(HighlightForegroundProperty, value);
  }

  protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
  {
    base.OnApplyTemplate(e);
    this.textBlock = e.NameScope.Find<TextBlock>(PART_TextBlock);
    this.UpdateInlines();
  }

  protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
  {
    base.OnAttachedToVisualTree(e);

    this.bindings?.Dispose();
    this.bindings = new CompositeDisposable();

    if (this.FindAncestorOfType<EditableComboBoxItem>() is {} editableComboBoxItem)
    {
      this.bindings.Add(editableComboBoxItem
        .GetObservable(EditableComboBoxItem.FilterHighlightTextProperty)
        .Subscribe(text => this.Search = text));
      if (!this.IsSet(ContentControl.ContentProperty) || this.contentManagedBySubscription)
      {
        this.contentManagedBySubscription = true;
        this.bindings.Add(editableComboBoxItem
          .GetObservable(EditableComboBoxItem.ValueProperty)
          .Subscribe(value => this.Content = value));
      }
    }

    if (this.FindAncestorOfType<MultiComboBoxItem>() is { } multiComboBoxItem)
    {
        this.bindings.Add(multiComboBoxItem
            .GetObservable(MultiComboBoxItem.FilterValueProperty)
            .Subscribe(value => this.Search = value));
    }
  }

  protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
  {
    base.OnDetachedFromVisualTree(e);
    this.bindings?.Dispose();
    this.bindings = null;
    if (this.contentManagedBySubscription)
    {
      // Clear Content so that IsSet(ContentProperty) resets to false on next OnAttachedToVisualTree.
      // This ensures recycled VSP containers re-subscribe to the new container's ValueProperty.
      this.ClearValue(ContentProperty);
    }
  }

  private void UpdateInlines()
  {
    if (this.textBlock == null)
    {
      return;
    }

    string content = this.Content?.ToString() ?? string.Empty;
    string currentSearch = this.Search ?? string.Empty;

    InlineCollection inlines = this.textBlock.Inlines ??= new InlineCollection();
    inlines.Clear();

    if (content.Length == 0)
    {
      return;
    }

    int highlightIndex = currentSearch.Length > 0
      ? content.IndexOf(currentSearch, StringComparison.OrdinalIgnoreCase)
      : -1;

    if (highlightIndex < 0)
    {
      inlines.Add(new Run(content));
      return;
    }

    if (highlightIndex > 0)
    {
      inlines.Add(new Run(content[..highlightIndex]));
    }

    Run highlighted = new Run(content.Substring(highlightIndex, currentSearch.Length))
    {
      Background = this.HighlightBackground,
      Foreground = this.HighlightForeground,
    };
    inlines.Add(highlighted);

    int rightStart = highlightIndex + currentSearch.Length;
    if (rightStart < content.Length)
    {
      inlines.Add(new Run(content[rightStart..]));
    }
  }
}
