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
  public void ControlCatalog_KeepsExperimentsOutOfControlsCatalog()
  {
    Assert.DoesNotContain(
      ControlRegistry.All,
      control => control.Key.Contains("Experiment", StringComparison.OrdinalIgnoreCase));
  }

  [Fact]
  public void ControlCatalog_UsesStatusSymbolsToDriveTesting()
  {
    ControlCatalogEntry toggleButton = Assert.Single(
      ControlRegistry.All,
      control => control.Key == nameof(SampleApp.DemoPages.ToggleButtonDemo));

    Assert.Equal("Not Supported", ControlRegistry.GetStatusDescription(toggleButton.GetStatusSymbol(ControlThemeId.MacClassic)));
    Assert.Equal("Supported", ControlRegistry.GetStatusDescription(toggleButton.GetStatusSymbol(ControlThemeId.WinUi)));
    Assert.False(toggleButton.ShouldTest(ControlThemeId.MacClassic));
    Assert.True(toggleButton.ShouldTest(ControlThemeId.WinUi));
  }

  [Fact]
  public void ControlCatalog_ExcludeFromTestsOnlyRemovesCoverage()
  {
    ControlCatalogEntry? treeDataGridInfo = ControlRegistry.All
      .SingleOrDefault(control => control.Key == "TreeDataGridInfo");

    if (treeDataGridInfo == null)
    {
      return;
    }

    Assert.Equal("Supported", ControlRegistry.GetStatusDescription(treeDataGridInfo.GetStatusSymbol(ControlThemeId.MacClassic)));
    Assert.False(treeDataGridInfo.ShouldTest(ControlThemeId.MacClassic));
  }

  [Fact]
  public void ControlCatalogValidation_RequiresAllThemeStatuses()
  {
    var entry = new ControlCatalogEntry(
      key: "MissingThemesDemo",
      controlTypeName: "MissingThemesControl",
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
      controlTypeName: "UnknownSymbolControl",
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
      controlTypeName: "InvalidPageControl",
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
