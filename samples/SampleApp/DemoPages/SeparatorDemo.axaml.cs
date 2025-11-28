using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SampleApp.DemoPages;

public partial class SeparatorDemo : UserControl
{
    public SeparatorDemo()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
