namespace SampleApp.DemoPages;

using Avalonia.Controls;
using ViewModels;

public partial class TabControlDemo : UserControl
{
  public TabControlDemo()
  {
    this.InitializeComponent();
    this.DataContext = new TabControlViewModel();
  }
}