namespace Devolutions.AvaloniaControls.VisualTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
  public void PageCatalog_UsesCatalogOrderAndProvidesEntries()
  {
    Assert.NotEmpty(PageRegistry.All);
    Assert.NotEmpty(PageRegistry.ControlDemos);
    Assert.All(PageRegistry.All, page => Assert.False(string.IsNullOrWhiteSpace(page.Title)));
    Assert.All(PageRegistry.All, page => Assert.False(string.IsNullOrWhiteSpace(page.Section)));
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

    Assert.False(string.IsNullOrWhiteSpace(PageRegistry.GetStatusDescription(entry.GetStatusSymbol(ThemeId.MacClassic))));
    Assert.False(entry.ShouldTest(ThemeId.MacClassic));
    Assert.True(entry.ShouldTest(ThemeId.DevExpress));
  }

  [Fact]
  public void PageCatalog_IsNotSupportedSymbol_UsesSymbolSemantics()
  {
    Assert.True(PageRegistry.IsNotSupportedSymbol(string.Empty));
    Assert.True(PageRegistry.IsNotSupportedSymbol("❌"));
    Assert.False(PageRegistry.IsNotSupportedSymbol("✅"));
    Assert.False(PageRegistry.IsNotSupportedSymbol("🚧"));
  }

  [Fact]
  public void PageCatalog_CreateControls_HandlesCaseMismatchedSectionKeys()
  {
    PageCatalogFile catalog = new()
    {
      TopLevelOrder = ["control demos"],
      Pages = new Dictionary<string, List<PageCatalogFileEntry>>
      {
        ["Control Demos"] =
        [
          new PageCatalogFileEntry
          {
            UniqueTitle = "Case Mismatch Demo",
            Source = nameof(ControlSource.AvaloniaPro),
            Category = "Input",
            Demo = "DoesNotExistDemo",
          },
        ],
      },
    };

    MethodInfo method = typeof(PageRegistry).GetMethod("CreateControls", BindingFlags.NonPublic | BindingFlags.Static)!;
    IReadOnlyList<PageCatalogEntry> entries = (IReadOnlyList<PageCatalogEntry>)method.Invoke(null, [catalog])!;

    PageCatalogEntry entry = Assert.Single(entries);
    Assert.Equal("Case Mismatch Demo", entry.Title);
    Assert.Equal("control demos", entry.Section);
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
