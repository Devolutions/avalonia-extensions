namespace Devolutions.AvaloniaControls.VisualTests;

using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using SampleApp;
using SampleApp.ControlCatalog;
using SampleApp.Controls;

public class MainWindowTabsTests
{
  [AvaloniaFact]
  public void MainWindow_BuildsControlTabsFromRegistry()
  {
    Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
    App.SetTheme(new MacOsClassicTheme());

    MainWindow window = new();
    TabControl tabControl = window.FindControl<TabControl>("MainTabControl")!;

    List<TabItem> generatedTabs = tabControl.Items
      .OfType<TabItem>()
      .Where(static tabItem => tabItem.Header is SampleItemHeader)
      .ToList();

    Assert.Equal(ControlRegistry.All.Count, generatedTabs.Count);
    Assert.Equal(
      ControlRegistry.All.Select(static control => control.Title),
      generatedTabs.Select(static tabItem => ((SampleItemHeader)tabItem.Header!).Title));

    foreach (ControlCatalogEntry control in ControlRegistry.All)
    {
      TabItem tabItem = Assert.Single(
        generatedTabs,
        tab => ((SampleItemHeader)tab.Header!).Title == control.Title);

      SampleItemHeader header = (SampleItemHeader)tabItem.Header!;
      Assert.Equal(control.ApplicableToCsv, header.ApplicableTo);

      if (control.Source == ControlSource.AvaloniaPro && tabItem.Content is TextBlock)
      {
        continue;
      }

      Control content = Assert.IsAssignableFrom<Control>(tabItem.Content);
      if (control.ViewModelType != null)
      {
        Assert.IsType(control.ViewModelType, content.DataContext);
      }
    }
  }
}
