namespace Devolutions.AvaloniaControls.VisualTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
      StartupPageTitle = startupPageTitle,
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

    foreach (PageCatalogEntry page in PageRegistry.All)
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

    PageCatalogEntry cachedPage = PageRegistry.All.First(page => page.Source != ControlSource.AvaloniaPro);
    PageCatalogEntry otherPage = PageRegistry.All.First(page =>
      page.Source != ControlSource.AvaloniaPro &&
      !string.Equals(page.Key, cachedPage.Key, StringComparison.OrdinalIgnoreCase));

    Assert.True(window.TrySelectPageByTitle(cachedPage.Title));
    object? firstInstance = contentHost.Content;
    Assert.NotNull(firstInstance);
    Assert.True(window.TrySelectPageByTitle(otherPage.Title));
    Assert.True(window.TrySelectPageByTitle(cachedPage.Title));
    Assert.Same(firstInstance, contentHost.Content);

    window.Close();
    Dispatcher.UIThread.RunJobs();
  }

  [AvaloniaFact]
  public void MainWindowNavigationBuilder_MapsStatusAndSourceMetadata()
  {
    Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
    App.SetTheme(new MacOsClassicTheme());

    PageCatalogEntry proEntry = CreateTestEntry("Header Pro", ControlSource.AvaloniaPro, "🚧");
    SampleApp.Controls.SampleItemHeader proHeader = CreateHeader(proEntry);
    Assert.Equal("🚧", proHeader.StatusSymbol);
    Assert.Equal(PageRegistry.GetStatusDescription("🚧"), proHeader.StatusTooltip);
    Assert.Equal("Pro", proHeader.SourceBadgeText);
    Assert.NotNull(proHeader.SourceBadgeBackground);

    PageCatalogEntry devoEntry = CreateTestEntry("Header Devo", ControlSource.Devolutions, "✅");
    SampleApp.Controls.SampleItemHeader devoHeader = CreateHeader(devoEntry);
    Assert.Equal("Devo", devoHeader.SourceBadgeText);
    Assert.NotNull(devoHeader.SourceBadgeBackground);
  }

  private static PageCatalogEntry CreateTestEntry(string key, ControlSource? source, string statusSymbol)
  {
    Dictionary<ThemeId, string> statusByTheme = ThemeIds.All.ToDictionary(static themeId => themeId, _ => statusSymbol);
    return new PageCatalogEntry(
      key: key,
      section: "Control Demos",
      title: key,
      pageType: typeof(UserControl),
      source: source,
      categoryPath: ["Input"],
      statusByTheme: statusByTheme);
  }

  private static SampleApp.Controls.SampleItemHeader CreateHeader(PageCatalogEntry entry)
  {
    Type builderType = Type.GetType("SampleApp.MainWindowNavigationBuilder, SampleApp", throwOnError: true)!;
    MethodInfo createHeader = builderType.GetMethod("CreateHeader", BindingFlags.Static | BindingFlags.Public)!;
    return (SampleApp.Controls.SampleItemHeader)createHeader.Invoke(null, [entry])!;
  }
}
