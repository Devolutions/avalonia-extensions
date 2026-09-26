namespace SampleApp.Controls;

using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

public partial class SampleItemHeader : UserControl, INotifyPropertyChanged
{
  public static readonly StyledProperty<string?> TitleProperty =
    AvaloniaProperty.Register<SampleItemHeader, string?>(nameof(Title));

  public static readonly StyledProperty<string?> StatusSymbolProperty =
    AvaloniaProperty.Register<SampleItemHeader, string?>(nameof(StatusSymbol), string.Empty);

  public static readonly StyledProperty<string?> StatusTooltipProperty =
    AvaloniaProperty.Register<SampleItemHeader, string?>(nameof(StatusTooltip));

  /// <summary>
  /// True when the status symbol is inherited from a reference theme (↔️ in the catalog),
  /// rather than set for the current theme directly. Rendered dimmed.
  /// </summary>
  public static readonly StyledProperty<bool> IsStatusInheritedProperty =
    AvaloniaProperty.Register<SampleItemHeader, bool>(nameof(IsStatusInherited));

  public static readonly StyledProperty<string?> SourceBadgeTextProperty =
    AvaloniaProperty.Register<SampleItemHeader, string?>(nameof(SourceBadgeText));

  public static readonly StyledProperty<IBrush?> SourceBadgeBackgroundProperty =
    AvaloniaProperty.Register<SampleItemHeader, IBrush?>(nameof(SourceBadgeBackground));

  public SampleItemHeader()
  {
    this.InitializeComponent();
  }

  public string? Title
  {
    get => this.GetValue(TitleProperty);
    set => this.SetValue(TitleProperty, value);
  }

  public string? StatusSymbol
  {
    get => this.GetValue(StatusSymbolProperty);
    set => this.SetValue(StatusSymbolProperty, value);
  }

  public bool HasStatusSymbol => !string.IsNullOrWhiteSpace(this.StatusSymbol);

  public string? StatusTooltip
  {
    get => this.GetValue(StatusTooltipProperty);
    set => this.SetValue(StatusTooltipProperty, value);
  }

  public bool IsStatusInherited
  {
    get => this.GetValue(IsStatusInheritedProperty);
    set => this.SetValue(IsStatusInheritedProperty, value);
  }

  public double StatusSymbolOpacity => this.IsStatusInherited ? 0.3 : 1.0;

  public string? SourceBadgeText
  {
    get => this.GetValue(SourceBadgeTextProperty);
    set => this.SetValue(SourceBadgeTextProperty, value);
  }

  public IBrush? SourceBadgeBackground
  {
    get => this.GetValue(SourceBadgeBackgroundProperty);
    set => this.SetValue(SourceBadgeBackgroundProperty, value);
  }

  public bool HasSourceBadge => !string.IsNullOrWhiteSpace(this.SourceBadgeText);

  public new event PropertyChangedEventHandler? PropertyChanged;

  protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
  {
    base.OnPropertyChanged(change);

    if (change.Property == StatusSymbolProperty)
    {
      this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.HasStatusSymbol)));
    }

    if (change.Property == IsStatusInheritedProperty)
    {
      this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.StatusSymbolOpacity)));
    }

    if (change.Property == SourceBadgeTextProperty)
    {
      this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.HasSourceBadge)));
    }
  }
}