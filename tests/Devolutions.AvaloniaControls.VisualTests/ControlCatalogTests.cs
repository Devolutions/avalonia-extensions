namespace Devolutions.AvaloniaControls.VisualTests;

using System;
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
}
