namespace Devolutions.AvaloniaControls.VisualTests;

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using SampleApp.PageCatalog;

public class PageCatalogTests
{
  [Fact]
  public void PageCatalog_IsValid()
  {
    PageRegistry.EnsureValid();
  }

  [Fact]
  public void PageCatalog_ExposesStartupSettings()
  {
    SampleAppStartupSettings startupSettings = PageRegistry.StartupSettings;

    Assert.False(string.IsNullOrWhiteSpace(startupSettings.Theme));
    Assert.False(string.IsNullOrWhiteSpace(startupSettings.StartupPage));
    Assert.False(string.IsNullOrWhiteSpace(startupSettings.Scale));
  }

  [Fact]
  public void PageCatalog_ControlDemosSectionExcludesExperiments()
  {
    Assert.DoesNotContain(
      PageRegistry.ControlDemos,
      control => control.Key.Contains("Experiment", StringComparison.OrdinalIgnoreCase));
  }

  [Fact]
  public void PageCatalog_DefaultsOmittedStatusesToEmptySymbol()
  {
    PageCatalogEntry overview = Assert.Single(PageRegistry.All, control => control.Key == "Overview");
    Assert.Null(overview.ViewModelType);

    foreach (ThemeId themeId in ThemeIds.All)
    {
      Assert.Equal(string.Empty, overview.GetStatusSymbol(themeId));
      Assert.False(overview.ShouldTest(themeId));
    }
  }

  [Fact]
  public void PageCatalog_UsesStatusSymbolsToDriveTesting()
  {
    PageCatalogEntry toggleButton = Assert.Single(
      PageRegistry.All,
      control => control.Key == "ToggleButton");

    Assert.Equal("Not Supported", PageRegistry.GetStatusDescription(toggleButton.GetStatusSymbol(ThemeId.MacClassic)));
    Assert.Equal("Supported", PageRegistry.GetStatusDescription(toggleButton.GetStatusSymbol(ThemeId.WinUi)));
    Assert.False(toggleButton.ShouldTest(ThemeId.MacClassic));
    Assert.True(toggleButton.ShouldTest(ThemeId.WinUi));
  }

  [Fact]
  public void PageCatalog_ExcludeFromTestsOnlyRemovesCoverage()
  {
    Dictionary<ThemeId, string> statuses = CreateValidStatuses();
    statuses[ThemeId.MacClassic] = "✅";

    var entry = new PageCatalogEntry(
      key: "ExcludeFromTestsDemo",
      section: "Control Demos",
      title: "Exclude From Tests",
      pageType: typeof(UserControl),
      source: ControlSource.Avalonia,
      categoryPath: ["Input"],
      statusByTheme: statuses,
      excludeFromTests: [ThemeId.MacClassic]);

    Assert.Equal("Supported", PageRegistry.GetStatusDescription(entry.GetStatusSymbol(ThemeId.MacClassic)));
    Assert.False(entry.ShouldTest(ThemeId.MacClassic));
    Assert.True(entry.ShouldTest(ThemeId.DevExpress));
  }

  [Fact]
  public void PageCatalogValidation_RequiresAllThemeStatuses()
  {
    var entry = new PageCatalogEntry(
      key: "MissingThemesDemo",
      section: "Control Demos",
      title: "Missing Themes",
      pageType: typeof(UserControl),
      source: ControlSource.Avalonia,
      categoryPath: ["Input"],
      statusByTheme: new Dictionary<ThemeId, string>
      {
        [ThemeId.MacClassic] = "✅",
      });

    IReadOnlyList<string> errors = PageCatalogEntry.Validate([entry]);
    Assert.Contains(errors, error => error.Contains("missing a status symbol for theme", StringComparison.OrdinalIgnoreCase));
  }

  [Fact]
  public void PageCatalogValidation_RejectsUnknownStatusSymbols()
  {
    Dictionary<ThemeId, string> statuses = CreateValidStatuses();
    statuses[ThemeId.MacClassic] = "😱";

    var entry = new PageCatalogEntry(
      key: "UnknownSymbolDemo",
      section: "Control Demos",
      title: "Unknown Symbol",
      pageType: typeof(UserControl),
      source: ControlSource.Avalonia,
      categoryPath: ["Input"],
      statusByTheme: statuses);

    IReadOnlyList<string> errors = PageCatalogEntry.Validate([entry]);
    Assert.Contains(errors, error => error.Contains("unknown status symbol", StringComparison.OrdinalIgnoreCase));
  }

  [Fact]
  public void PageCatalogValidation_RejectsInvalidPageType()
  {
    var entry = new PageCatalogEntry(
      key: "InvalidPageDemo",
      section: "Control Demos",
      title: "Invalid Page",
      pageType: typeof(string),
      source: ControlSource.Avalonia,
      categoryPath: ["Input"],
      statusByTheme: CreateValidStatuses());

    IReadOnlyList<string> errors = PageCatalogEntry.Validate([entry]);
    Assert.Contains(errors, error => error.Contains("must derive from Control", StringComparison.OrdinalIgnoreCase));
  }

  private static Dictionary<ThemeId, string> CreateValidStatuses() =>
    ThemeIds.All.ToDictionary(static theme => theme, _ => "✅");
}
