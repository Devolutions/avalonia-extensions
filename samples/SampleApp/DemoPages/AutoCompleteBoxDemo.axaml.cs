namespace SampleApp.DemoPages;

using System.Linq;
using Avalonia.Controls;

public partial class AutoCompleteBoxDemo : UserControl
{
  public AutoCompleteBoxDemo()
  {
      this.InitializeComponent();
    this.Animals.ItemsSource = new string[]
        { "cat", "camel", "cow", "chameleon", "mouse", "lion", "zebra", "dog", "frog", "jackal", "puffin", "quokka", "vole", "oryx" }
      .OrderBy(x=>x);
  }
}