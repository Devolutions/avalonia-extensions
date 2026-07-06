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

  private CompositeDisposable? textBlockBindings;

  private CompositeDisposable? dropDownWidthBindings;

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

  public static readonly StyledProperty<bool> ShowToolTipWhenTextOverflowingProperty =
    AvaloniaProperty.Register<SearchHighlightTextBlock, bool>(nameof(ShowToolTipWhenTextOverflowing));

  public SearchHighlightTextBlock()
  {
    this.GetObservable(ContentProperty).Subscribe(_ => this.UpdateInlines());
    this.GetObservable(SearchProperty).Subscribe(_ => this.UpdateInlines());
    this.GetObservable(HighlightBackgroundProperty).Subscribe(_ => this.UpdateInlines());
    this.GetObservable(HighlightForegroundProperty).Subscribe(_ => this.UpdateInlines());
    this.GetObservable(ShowToolTipWhenTextOverflowingProperty).Subscribe(_ => this.UpdateToolTip());
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

  public bool ShowToolTipWhenTextOverflowing
  {
    get => this.GetValue(ShowToolTipWhenTextOverflowingProperty);
    set => this.SetValue(ShowToolTipWhenTextOverflowingProperty, value);
  }

  protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
  {
    base.OnApplyTemplate(e);
    this.textBlockBindings?.Dispose();
    this.textBlock = e.NameScope.Find<TextBlock>(PART_TextBlock);
    if (this.textBlock != null && !double.IsNaN(this.Width))
    {
      this.textBlock.Width = this.Width;
    }
    this.BindTextBlockBounds();
    this.UpdateInlines();
  }

  protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
  {
    base.OnAttachedToVisualTree(e);

    this.bindings?.Dispose();
    this.bindings = new CompositeDisposable();
    this.BindTextBlockBounds();

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
      this.BindDropDownWidth(editableComboBoxItem);
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
    this.textBlockBindings?.Dispose();
    this.textBlockBindings = null;
    this.dropDownWidthBindings?.Dispose();
    this.dropDownWidthBindings = null;
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
      this.UpdateToolTip();
      return;
    }

    string content = this.Content?.ToString() ?? string.Empty;
    string currentSearch = this.Search ?? string.Empty;

    InlineCollection inlines = this.textBlock.Inlines ??= new InlineCollection();
    inlines.Clear();
    this.textBlock.Text = null;

    if (content.Length == 0)
    {
      this.textBlock.Text = string.Empty;
      this.UpdateToolTip();
      return;
    }

    int highlightIndex = currentSearch.Length > 0
      ? content.IndexOf(currentSearch, StringComparison.OrdinalIgnoreCase)
      : -1;

    if (highlightIndex < 0)
    {
      this.textBlock.Text = content;
      this.UpdateToolTip();
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

    this.UpdateToolTip();
  }

  private void UpdateToolTip()
  {
    if (!this.ShowToolTipWhenTextOverflowing || this.textBlock == null || this.textBlock.Bounds.Width <= 0)
    {
      ToolTip.SetTip(this, null);
      return;
    }

    string content = this.Content?.ToString() ?? string.Empty;
    if (content.Length == 0)
    {
      ToolTip.SetTip(this, null);
      return;
    }

    TextBlock measureBlock = new()
    {
      Text = content,
      FontFamily = this.textBlock.FontFamily,
      FontSize = this.textBlock.FontSize,
      FontStyle = this.textBlock.FontStyle,
      FontWeight = this.textBlock.FontWeight,
      FontStretch = this.textBlock.FontStretch,
      TextWrapping = TextWrapping.NoWrap,
    };
    measureBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

    ToolTip.SetTip(this, measureBlock.DesiredSize.Width > this.textBlock.Bounds.Width + 0.5 ? content : null);
  }

  private void BindDropDownWidth(EditableComboBoxItem editableComboBoxItem)
  {
    if (this.dropDownWidthBindings != null || ItemsControl.ItemsControlFromItemContainer(editableComboBoxItem) is not EditableComboBox.InnerComboBox innerComboBox)
    {
      return;
    }

    void UpdateMaxWidth()
    {
      double maxDropDownWidth = innerComboBox.MaxDropDownWidth;
      double itemWidth = editableComboBoxItem.Bounds.Width;
      double availableWidth = itemWidth > 0 && maxDropDownWidth > 0
        ? Math.Min(itemWidth, maxDropDownWidth)
        : Math.Max(itemWidth, maxDropDownWidth);

      if (double.IsNaN(availableWidth) || double.IsInfinity(availableWidth) || availableWidth <= 0)
      {
        this.ClearValue(WidthProperty);
        this.textBlock?.ClearValue(WidthProperty);
      }
      else
      {
        Thickness padding = editableComboBoxItem.Padding;
        double textWidth = Math.Max(0, availableWidth - padding.Left - padding.Right);
        this.Width = textWidth;
        if (this.textBlock != null)
        {
          this.textBlock.Width = textWidth;
        }
      }

      this.UpdateToolTip();
    }

    void OnInnerComboBoxPropertyChanged(object? _, AvaloniaPropertyChangedEventArgs change)
    {
      if (change.Property.Name == nameof(EditableComboBox.InnerComboBox.MaxDropDownWidth))
      {
        UpdateMaxWidth();
      }
    }

    innerComboBox.PropertyChanged += OnInnerComboBoxPropertyChanged;

    this.dropDownWidthBindings = new CompositeDisposable
    {
      Disposable.Create(() => innerComboBox.PropertyChanged -= OnInnerComboBoxPropertyChanged),
      editableComboBoxItem.GetObservable(PaddingProperty).Subscribe(_ => UpdateMaxWidth()),
      editableComboBoxItem.GetObservable(BoundsProperty).Subscribe(_ => UpdateMaxWidth()),
    };

    UpdateMaxWidth();
  }

  private void BindTextBlockBounds()
  {
    if (this.textBlock == null || this.textBlockBindings != null)
    {
      return;
    }

    this.textBlockBindings = new CompositeDisposable
    {
      this.textBlock.GetObservable(BoundsProperty).Subscribe(_ => this.UpdateToolTip())
    };
  }
}
