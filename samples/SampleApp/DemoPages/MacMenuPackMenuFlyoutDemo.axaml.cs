namespace SampleApp.DemoPages;

using Avalonia.Controls;
using Devolutions.AvaloniaTheme.MacOS.Controls;

public partial class MacMenuPackMenuFlyoutDemo : UserControl
{
  public MacMenuPackMenuFlyoutDemo()
  {
    this.InitializeComponent();
    MacMenuPackStyles.ApplyTo(this.Styles);
  }
}
