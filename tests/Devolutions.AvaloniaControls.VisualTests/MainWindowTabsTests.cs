namespace Devolutions.AvaloniaControls.VisualTests;

using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using SampleApp;
using SampleApp.ControlCatalog;
using SampleApp.Controls;
using SampleApp.ViewModels;

public class MainWindowTabsTests
{
  [AvaloniaFact]
  public void MainWindow_BuildsControlTabsFromRegistry()
  {
    Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
    App.SetTheme(new MacOsClassicTheme());

    MainWindowViewModel viewModel = new()
    {
      StartupTabTitle = "ComboBox",
    };

    MainWindow window = new()
    {
      DataContext = viewModel,
    };
    window.Show();
    Dispatcher.UIThread.RunJobs();

    TabControl tabControl = window.FindControl<TabControl>("MainTabControl")!;

    TabItem selectedTab = Assert.IsType<TabItem>(tabControl.SelectedItem);
    SampleItemHeader selectedHeader = Assert.IsType<SampleItemHeader>(selectedTab.Header);
    Assert.Equal("ComboBox", selectedHeader.Title);

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

    window.Close();
    Dispatcher.UIThread.RunJobs();
  }
}
