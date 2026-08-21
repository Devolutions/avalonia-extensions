namespace SampleApp.DemoPages;

using Avalonia.Controls;
using Devolutions.AvaloniaTheme.MacOS.Controls;

public partial class MacMenuPackAbout : UserControl
{
  public MacMenuPackAbout()
  {
    this.InitializeComponent();
    MacMenuPackStyles.ApplyTo(this.Styles);
  }
}
