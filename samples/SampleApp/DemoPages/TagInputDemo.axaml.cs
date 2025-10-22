namespace SampleApp.DemoPages;

using Avalonia.Controls;

public partial class TagInputDemo : UserControl
{
  public TagInputDemo()
  {
    this.InitializeComponent();

    // Pre-populate some example tags
    if (this.PrePopulatedTagInput is not null)
    {
      this.PrePopulatedTagInput.Tags.Add("Development");
      this.PrePopulatedTagInput.Tags.Add("Design");
      this.PrePopulatedTagInput.Tags.Add("Testing");
    }

    // Pre-populate disabled TagInput
    if (this.DisabledTagInput is not null)
    {
      this.DisabledTagInput.Tags.Add("Read-only");
      this.DisabledTagInput.Tags.Add("Disabled");
    }
  }
}
