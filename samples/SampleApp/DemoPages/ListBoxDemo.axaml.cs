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
        this.animals4.ItemsSource = this.animals5.ItemsSource = this.animals6.ItemsSource = this.animals7.ItemsSource = new[]
                { "cat", "camel", "cow", "chameleon", "mouse", "lion", "zebra", "tiger", "donkey" }
            .OrderBy(x => x);
        this.animals8.ItemsSource = new[]
        {
            new Animal { Name = "Lion", Region = "Africa" },
            new Animal { Name = "Panda", Region = "Asia" },
            new Animal { Name = "Kangaroo", Region = "Australia" },
            new Animal { Name = "Polar Bear", Region = "Arctic" }
        };
    }

    public class Animal
    {
        public string Name { get; init; } = "";

        public string Region { get; init; } = "";
    }
}