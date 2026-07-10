namespace Devolutions.AvaloniaControls.VisualTests;

using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using SampleApp;
using SampleApp.PageCatalog;
using SampleApp.ViewModels;

public class MainWindowNavigationTests
{
  [AvaloniaFact]
  public void MainWindow_BuildsNavigationTreeFromRegistry()
  {
    Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
    App.SetTheme(new MacOsClassicTheme());
    string startupPageTitle = PageRegistry.All.First().Title;

    MainWindowViewModel viewModel = new()
    {
      StartupTabTitle = startupPageTitle,
    };

    MainWindow window = new()
    {
      DataContext = viewModel,
    };
    window.Show();
    Dispatcher.UIThread.RunJobs();

    Assert.Equal(startupPageTitle, window.GetSelectedPageTitle());
    ContentControl contentHost = window.FindControl<ContentControl>("MainPageHost")!;
    Assert.NotNull(contentHost.Content);

    foreach (PageCatalogEntry page in PageRegistry.ControlDemos)
    {
      Assert.True(window.TrySelectPageByTitle(page.Title));
      Assert.Equal(page.Title, window.GetSelectedPageTitle());

      if (page.Source == ControlSource.AvaloniaPro && contentHost.Content is TextBlock)
      {
        continue;
      }

      Control content = Assert.IsAssignableFrom<Control>(contentHost.Content);
      if (page.ViewModelType != null)
      {
        Assert.IsType(page.ViewModelType, content.DataContext);
      }
    }

    PageCatalogEntry cachedPage = PageRegistry.ControlDemos.First(page => page.Source != ControlSource.AvaloniaPro);
    PageCatalogEntry otherPage = PageRegistry.ControlDemos.First(page =>
      page.Source != ControlSource.AvaloniaPro &&
      !string.Equals(page.Key, cachedPage.Key, StringComparison.OrdinalIgnoreCase));

    Assert.True(window.TrySelectPageByTitle(cachedPage.Title));
    object? firstInstance = contentHost.Content;
    Assert.NotNull(firstInstance);
    Assert.True(window.TrySelectPageByTitle(otherPage.Title));
    Assert.True(window.TrySelectPageByTitle(cachedPage.Title));
    Assert.Same(firstInstance, contentHost.Content);

    var sectionGroup = PageRegistry.All
      .GroupBy(page => page.Section)
      .First(group =>
        !string.Equals(group.Key, PageRegistry.ControlDemosSection, StringComparison.OrdinalIgnoreCase) &&
        group.Count() > 1);

    string targetSection = sectionGroup.Key;
    string targetPageTitle = sectionGroup.First().Title;
    Assert.False(window.IsSectionExpanded(targetSection));
    Assert.True(window.TrySelectPageByTitle(targetPageTitle));
    Assert.Equal(targetPageTitle, window.GetSelectedPageTitle());
    Assert.True(window.IsSectionExpanded(targetSection));

    window.Close();
    Dispatcher.UIThread.RunJobs();
  }
}
