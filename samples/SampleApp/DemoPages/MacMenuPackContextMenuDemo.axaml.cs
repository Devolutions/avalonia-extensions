namespace SampleApp.DemoPages;

using Avalonia.Controls;
using Devolutions.AvaloniaTheme.MacOS.Controls;

public partial class MacMenuPackContextMenuDemo : UserControl
{
  public MacMenuPackContextMenuDemo()
  {
    this.InitializeComponent();
    MacMenuPackStyles.ApplyTo(this.Styles);
  }
}
