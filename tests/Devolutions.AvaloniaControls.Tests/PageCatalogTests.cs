namespace Devolutions.AvaloniaControls.Tests;

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
  public void PageCatalog_StartupSettings_DefaultToOverviewWhenSelectedPageMissing()
  {
    SampleAppStartupSettings settings = new()
    {
      Theme = "default",
      Scale = "default",
    };

    Assert.Equal("Overview", settings.StartupPage);
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
  public void PageCatalog_InProgressSymbol_ExcludesFromTestsButNotFromApplicability()
  {
    Assert.False(PageRegistry.IsNotSupportedSymbol("🚧"));
    Assert.True(PageRegistry.IsInProgressSymbol("🚧"));
    Assert.False(PageRegistry.IsInProgressSymbol("⚠️"));

    Dictionary<ThemeId, string> statuses = CreateValidStatuses();
    statuses[ThemeId.MacClassic] = "🚧";
    statuses[ThemeId.LiquidGlass] = "⚠️";

    var entry = new PageCatalogEntry(
      key: "InProgressDemo",
      section: "Control Demos",
      title: "In Progress",
      pageType: typeof(UserControl),
      source: ControlSource.Avalonia,
      categoryPath: ["Input"],
      statusByTheme: statuses);

    // 🚧 (in progress, unverified) is excluded from tests...
    Assert.False(entry.ShouldTest(ThemeId.MacClassic));
    // ...while ⚠️ (imperfect but included) still guards existing coverage.
    Assert.True(entry.ShouldTest(ThemeId.LiquidGlass));
    // ...and 🚧 still counts as "applicable" (unlike ❌/""), since work has genuinely started.
    Assert.Contains(ThemeId.MacClassic.ToThemeName(), entry.ApplicableToCsv.Split(", "));
  }

  [Theory]
  [InlineData("✅", "✅", true)]
  [InlineData("⚠️", "⚠️", true)]
  [InlineData("🚧", "🚧", false)]
  [InlineData("❌", "❌", false)]
  public void PageCatalog_SameAsSymbol_InheritsReferenceThemeStatus(string micaStatus, string expectedClassicStatus, bool expectedShouldTest)
  {
    Assert.True(PageRegistry.IsSameAsReferenceSymbol("↔️"));
    Assert.Equal(ThemeId.WinUiMica, ThemeId.WinUiClassic.GetSameAsReference());

    Dictionary<ThemeId, string> statuses = CreateValidStatuses();
    statuses[ThemeId.WinUiClassic] = "↔️";
    statuses[ThemeId.WinUiMica] = micaStatus;
    PageCatalogEntry entry = CreateEntry("SameAsDemo", statuses);

    Assert.Equal("↔️", entry.GetStatusSymbol(ThemeId.WinUiClassic));
    Assert.Equal(expectedClassicStatus, entry.GetEffectiveStatusSymbol(ThemeId.WinUiClassic));
    Assert.True(entry.IsSameAsReference(ThemeId.WinUiClassic));
    Assert.False(entry.IsSameAsReference(ThemeId.WinUiMica));
    Assert.Equal(expectedShouldTest, entry.ShouldTest(ThemeId.WinUiClassic));
    Assert.Equal(entry.ShouldTest(ThemeId.WinUiMica), entry.ShouldTest(ThemeId.WinUiClassic));
  }

  [Fact]
  public void PageCatalog_SameAsSymbol_RespectsOwnExcludeFromTests()
  {
    Dictionary<ThemeId, string> statuses = CreateValidStatuses();
    statuses[ThemeId.WinUiClassic] = "↔️";

    var entry = new PageCatalogEntry(
      key: "SameAsExcludedDemo",
      section: "Control Demos",
      title: "Same As Excluded",
      pageType: typeof(UserControl),
      source: ControlSource.Avalonia,
      categoryPath: ["Input"],
      statusByTheme: statuses,
      excludeFromTests: [ThemeId.WinUiClassic]);

    Assert.False(entry.ShouldTest(ThemeId.WinUiClassic));
    Assert.True(entry.ShouldTest(ThemeId.WinUiMica));
  }

  [Fact]
  public void PageCatalogValidation_RejectsSameAsSymbolWithoutReferenceTheme()
  {
    Dictionary<ThemeId, string> statuses = CreateValidStatuses();
    statuses[ThemeId.WinUiClassic] = "↔️";
    statuses[ThemeId.WinUiMica] = "↔️";
    statuses[ThemeId.MacClassic] = "↔️";

    IReadOnlyList<string> errors = PageCatalogEntry.Validate([CreateEntry("SameAsInvalidDemo", statuses)]);

    Assert.Contains(errors, error => error.Contains($"theme '{ThemeId.WinUiMica}', which has no reference theme", StringComparison.Ordinal));
    Assert.Contains(errors, error => error.Contains($"theme '{ThemeId.MacClassic}', which has no reference theme", StringComparison.Ordinal));
    Assert.DoesNotContain(errors, error => error.Contains($"theme '{ThemeId.WinUiClassic}'", StringComparison.Ordinal));
  }

  [Fact]
  public void PageCatalog_WinUiVariantsAreSeparateColumns()
  {
    Assert.Equal(ThemeId.WinUiClassic, ThemeIds.Parse("WinUIClassic"));
    Assert.Equal(ThemeId.WinUiMica, ThemeIds.Parse("WinUIMica"));
    // "WinUI" is the automatic theme's family name (XAML gating), not a catalog column.
    Assert.False(ThemeIds.TryParse("WinUI", out _));
  }

  [Fact]
  public void PageCatalog_CreatePages_HandlesCaseMismatchedSectionKeys()
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

    MethodInfo method = typeof(PageRegistry).GetMethod("CreatePages", BindingFlags.NonPublic | BindingFlags.Static)!;
    IReadOnlyList<PageCatalogEntry> entries = (IReadOnlyList<PageCatalogEntry>)method.Invoke(null, [catalog])!;

    PageCatalogEntry entry = Assert.Single(entries);
    Assert.Equal("Case Mismatch Demo", entry.Title);
    Assert.Equal("control demos", entry.Section);
  }

  [Fact]
  public void PageCatalog_CreatePages_AllowsOmittedCategory()
  {
    PageCatalogFile catalog = new()
    {
      TopLevelOrder = ["Control Demos"],
      Pages = new Dictionary<string, List<PageCatalogFileEntry>>
      {
        ["Control Demos"] =
        [
          new PageCatalogFileEntry
          {
            UniqueTitle = "No Category Demo",
            Source = nameof(ControlSource.AvaloniaPro),
            Demo = "DoesNotExistDemo",
          },
        ],
      },
    };

    MethodInfo method = typeof(PageRegistry).GetMethod("CreatePages", BindingFlags.NonPublic | BindingFlags.Static)!;
    IReadOnlyList<PageCatalogEntry> entries = (IReadOnlyList<PageCatalogEntry>)method.Invoke(null, [catalog])!;
    PageCatalogEntry entry = Assert.Single(entries);
    Assert.Empty(entry.CategoryPath);
  }

  [Fact]
  public void PageCatalogValidation_AllowsEmptyCategoryPath()
  {
    var entry = new PageCatalogEntry(
      key: "NoCategoryDemo",
      section: "Control Demos",
      title: "No Category",
      pageType: typeof(UserControl),
      source: ControlSource.Avalonia,
      categoryPath: [],
      statusByTheme: CreateValidStatuses());

    IReadOnlyList<string> errors = PageCatalogEntry.Validate([entry]);
    Assert.DoesNotContain(errors, error => error.Contains("category", StringComparison.OrdinalIgnoreCase));
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

  private static PageCatalogEntry CreateEntry(string key, IReadOnlyDictionary<ThemeId, string> statuses) =>
    new(
      key: key,
      section: "Control Demos",
      title: key,
      pageType: typeof(UserControl),
      source: ControlSource.Avalonia,
      categoryPath: ["Input"],
      statusByTheme: statuses);

  private static Dictionary<ThemeId, string> CreateValidStatuses() =>
    ThemeIds.All.ToDictionary(static theme => theme, _ => "✅");
}
