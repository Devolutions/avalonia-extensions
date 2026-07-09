namespace Devolutions.AvaloniaControls.VisualTests;

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using SampleApp.ControlCatalog;

public class ControlCatalogTests
{
  [Fact]
  public void ControlCatalog_IsValid()
  {
    ControlRegistry.EnsureValid();
  }

  [Fact]
  public void ControlCatalog_ExposesStartupSettings()
  {
    SampleAppStartupSettings startupSettings = ControlRegistry.StartupSettings;

    Assert.False(string.IsNullOrWhiteSpace(startupSettings.Theme));
    Assert.False(string.IsNullOrWhiteSpace(startupSettings.StartupPage));
    Assert.False(string.IsNullOrWhiteSpace(startupSettings.Scale));
  }

  [Fact]
  public void ControlCatalog_ControlDemosSectionExcludesExperiments()
  {
    Assert.DoesNotContain(
      ControlRegistry.ControlDemos,
      control => control.Key.Contains("Experiment", StringComparison.OrdinalIgnoreCase));
  }

  [Fact]
  public void ControlCatalog_DefaultsOmittedStatusesToEmptySymbol()
  {
    ControlCatalogEntry overview = Assert.Single(ControlRegistry.All, control => control.Key == "Overview");
    Assert.Null(overview.ViewModelType);

    foreach (ControlThemeId themeId in ControlThemeIds.All)
    {
      Assert.Equal(string.Empty, overview.GetStatusSymbol(themeId));
      Assert.False(overview.ShouldTest(themeId));
    }
  }

  [Fact]
  public void ControlCatalog_UsesStatusSymbolsToDriveTesting()
  {
    ControlCatalogEntry toggleButton = Assert.Single(
      ControlRegistry.All,
      control => control.Key == "ToggleButton");

    Assert.Equal("Not Supported", ControlRegistry.GetStatusDescription(toggleButton.GetStatusSymbol(ControlThemeId.MacClassic)));
    Assert.Equal("Supported", ControlRegistry.GetStatusDescription(toggleButton.GetStatusSymbol(ControlThemeId.WinUi)));
    Assert.False(toggleButton.ShouldTest(ControlThemeId.MacClassic));
    Assert.True(toggleButton.ShouldTest(ControlThemeId.WinUi));
  }

  [Fact]
  public void ControlCatalog_ExcludeFromTestsOnlyRemovesCoverage()
  {
    Dictionary<ControlThemeId, string> statuses = CreateValidStatuses();
    statuses[ControlThemeId.MacClassic] = "✅";

    var entry = new ControlCatalogEntry(
      key: "ExcludeFromTestsDemo",
      section: "Control Demos",
      title: "Exclude From Tests",
      pageType: typeof(UserControl),
      source: ControlSource.Avalonia,
      categoryPath: ["Input"],
      statusByTheme: statuses,
      excludeFromTests: [ControlThemeId.MacClassic]);

    Assert.Equal("Supported", ControlRegistry.GetStatusDescription(entry.GetStatusSymbol(ControlThemeId.MacClassic)));
    Assert.False(entry.ShouldTest(ControlThemeId.MacClassic));
    Assert.True(entry.ShouldTest(ControlThemeId.DevExpress));
  }

  [Fact]
  public void ControlCatalogValidation_RequiresAllThemeStatuses()
  {
    var entry = new ControlCatalogEntry(
      key: "MissingThemesDemo",
      section: "Control Demos",
      title: "Missing Themes",
      pageType: typeof(UserControl),
      source: ControlSource.Avalonia,
      categoryPath: ["Input"],
      statusByTheme: new Dictionary<ControlThemeId, string>
      {
        [ControlThemeId.MacClassic] = "✅",
      });

    IReadOnlyList<string> errors = ControlCatalogEntry.Validate([entry]);
    Assert.Contains(errors, error => error.Contains("missing a status symbol for theme", StringComparison.OrdinalIgnoreCase));
  }

  [Fact]
  public void ControlCatalogValidation_RejectsUnknownStatusSymbols()
  {
    Dictionary<ControlThemeId, string> statuses = CreateValidStatuses();
    statuses[ControlThemeId.MacClassic] = "😱";

    var entry = new ControlCatalogEntry(
      key: "UnknownSymbolDemo",
      section: "Control Demos",
      title: "Unknown Symbol",
      pageType: typeof(UserControl),
      source: ControlSource.Avalonia,
      categoryPath: ["Input"],
      statusByTheme: statuses);

    IReadOnlyList<string> errors = ControlCatalogEntry.Validate([entry]);
    Assert.Contains(errors, error => error.Contains("unknown status symbol", StringComparison.OrdinalIgnoreCase));
  }

  [Fact]
  public void ControlCatalogValidation_RejectsInvalidPageType()
  {
    var entry = new ControlCatalogEntry(
      key: "InvalidPageDemo",
      section: "Control Demos",
      title: "Invalid Page",
      pageType: typeof(string),
      source: ControlSource.Avalonia,
      categoryPath: ["Input"],
      statusByTheme: CreateValidStatuses());

    IReadOnlyList<string> errors = ControlCatalogEntry.Validate([entry]);
    Assert.Contains(errors, error => error.Contains("must derive from Control", StringComparison.OrdinalIgnoreCase));
  }

  private static Dictionary<ControlThemeId, string> CreateValidStatuses() =>
    ControlThemeIds.All.ToDictionary(static theme => theme, _ => "✅");
}
