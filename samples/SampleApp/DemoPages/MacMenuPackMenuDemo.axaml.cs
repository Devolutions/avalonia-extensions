namespace SampleApp.DemoPages;

using Avalonia.Controls;
using Devolutions.AvaloniaTheme.MacOS.Controls;

public partial class MacMenuPackMenuDemo : UserControl
{
  public MacMenuPackMenuDemo()
  {
    this.InitializeComponent();
    MacMenuPackStyles.ApplyTo(this.Styles);
  }
}
