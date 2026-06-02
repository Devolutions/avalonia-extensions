using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SampleApp.DemoPages;

public partial class DropDownButtonDemo : UserControl
{
    public DropDownButtonDemo()
    {
        this.InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
