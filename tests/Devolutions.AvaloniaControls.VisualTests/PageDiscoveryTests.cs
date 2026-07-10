using System;
using System.Collections.Generic;
using System.Linq;
using SampleApp.PageCatalog;
using Xunit;

namespace Devolutions.AvaloniaControls.VisualTests;

public class PageDiscoveryTests
{
  private static readonly ThemeId[] VisualDiscoveryThemes =
  [
    ThemeId.MacClassic,
    ThemeId.LiquidGlass,
    ThemeId.Linux,
    ThemeId.DevExpress,
  ];

  [Fact]
  public void VisualDiscovery_OnlyReturnsRegistryBackedTestablePages()
  {
    HashSet<string> discoveredCases = VisualRegressionTests.GetTestPages()
      .Select(static testCase =>
      {
        Type pageType = Assert.IsAssignableFrom<Type>(testCase[0]);
        string themeName = Assert.IsType<string>(testCase[1]);
        Type? viewModelType = testCase[2] as Type;
        return BuildCaseKey(pageType, ThemeIds.Parse(themeName), viewModelType);
      })
      .ToHashSet(StringComparer.Ordinal);

    HashSet<string> expectedCases = PageRegistry.All
      .SelectMany(page => VisualDiscoveryThemes
        .Where(page.ShouldTest)
        .Select(themeId => BuildCaseKey(page.PageType, themeId, page.ViewModelType)))
      .ToHashSet(StringComparer.Ordinal);

    Assert.NotEmpty(discoveredCases);
    Assert.Equal(expectedCases, discoveredCases);

    Assert.Contains(
      discoveredCases,
      static key => key.Contains("ControlAlignment", StringComparison.Ordinal) &&
                    key.Contains("|MacClassic|", StringComparison.Ordinal));
    Assert.Contains(
      discoveredCases,
      static key => key.Contains("ToggleButtonExperiments", StringComparison.Ordinal) &&
                    key.Contains("|LiquidGlass|", StringComparison.Ordinal));
  }

  private static string BuildCaseKey(Type pageType, ThemeId themeId, Type? viewModelType) =>
    $"{pageType.FullName}|{themeId}|{viewModelType?.FullName ?? "<null>"}";
}
