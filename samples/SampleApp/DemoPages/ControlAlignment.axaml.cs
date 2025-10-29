namespace SampleApp.DemoPages;

using System.Linq;
using Avalonia.Controls;

public partial class ControlAlignment : UserControl
{
  public ControlAlignment()
  {
    this.InitializeComponent();
    this.Animals.ItemsSource = this.Animals2.ItemsSource = this.Animals3.ItemsSource = this.Animals4.ItemsSource = new[]
        { "cat", "camel", "cow", "chameleon", "mouse", "lion", "zebra" }
      .OrderBy(x => x);

    // Pre-populate horizontal TagInput controls with "A" tag for visibility
    if (this.HorizontalGridTagInput is not null)
    {
      this.HorizontalGridTagInput.Tags.Add("A");
    }

    if (this.HorizontalStackPanelTagInput is not null)
    {
      this.HorizontalStackPanelTagInput.Tags.Add("A");
    }
  }
}