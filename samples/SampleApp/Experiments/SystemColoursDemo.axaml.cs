namespace SampleApp.Experiments;

using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;

public partial class SystemColoursDemo : UserControl
{
  public SystemColoursDemo()
  {
    this.InitializeComponent();
  }

  public List<double> OpacityValues { get; } = Enumerable.Range(1, 100).Select(x => x / 100.0).ToList();
}