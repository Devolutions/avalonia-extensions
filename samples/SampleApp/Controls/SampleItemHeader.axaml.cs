namespace SampleApp.Controls;

using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;

public partial class SampleItemHeader : UserControl, INotifyPropertyChanged
{
  public static readonly StyledProperty<string?> TitleProperty =
    AvaloniaProperty.Register<SampleItemHeader, string?>(nameof(Title));

  public static readonly StyledProperty<string?> StatusSymbolProperty =
    AvaloniaProperty.Register<SampleItemHeader, string?>(nameof(StatusSymbol), string.Empty);

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

  public new event PropertyChangedEventHandler? PropertyChanged;

  protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
  {
    base.OnPropertyChanged(change);

    if (change.Property == StatusSymbolProperty)
    {
      this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.HasStatusSymbol)));
    }
  }
}