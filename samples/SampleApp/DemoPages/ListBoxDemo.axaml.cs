namespace SampleApp.DemoPages;

using System.Linq;
using Avalonia.Controls;

public partial class ListBoxDemo : UserControl
{
    public ListBoxDemo()
    {
        this.InitializeComponent();

        this.animals1.ItemsSource = this.animals2.ItemsSource = new[]
                { "cat", "mouse", "lion", "zebra" }
            .OrderBy(x => x);
        this.animals3.ItemsSource = new[]
                { "cat", "zebra" }
            .OrderBy(x => x);
        this.animals4.ItemsSource = this.animals5.ItemsSource = this.animals6.ItemsSource = new[]
                { "cat", "camel", "cow", "chameleon", "mouse", "lion", "zebra", "tiger", "donkey" }
            .OrderBy(x => x);
    }
}