using Avalonia.Controls;
using System.Linq;

namespace SampleApp.DemoPages;

public partial class ScrollBarDemo : UserControl
{
    public ScrollBarDemo()
    {
        InitializeComponent();
        
        DataContext = new 
        {
            LongOptions = Enumerable.Range(1, 100).Select(i => $"Option {i}").ToList(),
            MediumOptions = Enumerable.Range(1, 40).Select(i => $"Option {i}").ToList(),
            ShortOptions = Enumerable.Range(1, 10).Select(i => $"Option {i}").ToList()
        };
    }
}
