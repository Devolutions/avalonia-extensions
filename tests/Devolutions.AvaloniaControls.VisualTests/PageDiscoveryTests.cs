using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using SampleApp.PageCatalog;
using Xunit;

namespace Devolutions.AvaloniaControls.VisualTests;

public class PageDiscoveryTests
{
  [Fact]
  public void VisualDiscovery_OnlyReturnsRegistryBackedTestableDemoPages()
  {
    List<object?[]> discoveredCases = VisualRegressionTests.GetDemoPages().ToList();
    Assert.NotEmpty(discoveredCases);

    foreach (object?[] testCase in discoveredCases)
    {
      Type pageType = Assert.IsAssignableFrom<Type>(testCase[0]);
      string themeName = Assert.IsType<string>(testCase[1]);
      Type? viewModelType = testCase[2] as Type;

      Assert.EndsWith("Demo", pageType.Name, StringComparison.Ordinal);
      Assert.True(pageType.IsSubclassOf(typeof(UserControl)));
      Assert.False(pageType.IsAbstract);

      ThemeId themeId = ThemeIds.Parse(themeName);
      PageCatalogEntry entry = Assert.Single(PageRegistry.All, page => page.PageType == pageType && page.ViewModelType == viewModelType);
      Assert.True(entry.ShouldTest(themeId));
    }
  }
}
