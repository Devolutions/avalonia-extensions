namespace SampleApp.DemoPages;

using Avalonia.Controls;
using Avalonia.Interactivity;

public partial class SearchHighlightTextBlockDemo : UserControl
{
  public SearchHighlightTextBlockDemo()
  {
    this.InitializeComponent();
  }

  private void OnClearSearchClicked(object? sender, RoutedEventArgs e)
  {
    this.SearchBox.Text = string.Empty;
  }
}
