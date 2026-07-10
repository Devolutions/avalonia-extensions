namespace SampleApp.PageCatalog;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Avalonia.Controls;

public static class PageRegistry
{
  public const string ControlDemosSection = "Control Demos";
  private const string NotSupportedDescription = "Not Supported";
  private const string CatalogResourceName = "SampleApp.PageCatalog.page-catalog.jsonc";
  private static readonly PageCatalogFile CatalogFile = LoadCatalogFile();
  private static readonly IReadOnlyList<PageCatalogEntry> Controls = CreateControls(CatalogFile);
  private static readonly IReadOnlyList<string> ValidationErrors = PageCatalogEntry.Validate(Controls);
  private static readonly IReadOnlyDictionary<string, IReadOnlyList<PageCatalogEntry>> ControlsBySection =
    Controls
      .GroupBy(static entry => entry.Section, StringComparer.OrdinalIgnoreCase)
      .ToDictionary(
        static group => group.Key,
        static group => (IReadOnlyList<PageCatalogEntry>)group.ToArray(),
        StringComparer.OrdinalIgnoreCase);

  public static IReadOnlyList<PageCatalogEntry> All => Controls;

  public static IReadOnlyList<PageCatalogEntry> ControlDemos =>
    ControlsBySection.TryGetValue(ControlDemosSection, out IReadOnlyList<PageCatalogEntry>? entries)
      ? entries
      : [];

  public static SampleAppStartupSettings StartupSettings => CatalogFile.StartupSettings;

  public static IReadOnlyDictionary<string, string> StatusDescriptions => CatalogFile.StatusSymbols;

  public static void EnsureValid()
  {
    if (ValidationErrors.Count == 0)
    {
      return;
    }

    string joinedErrors = string.Join(Environment.NewLine, ValidationErrors);
    throw new InvalidOperationException($"The page catalog is invalid:{Environment.NewLine}{joinedErrors}");
  }

  public static string GetStatusDescription(string symbol) =>
    StatusDescriptions.TryGetValue(symbol, out string? description)
      ? description
      : "Unknown status";

  public static bool IsNotSupportedSymbol(string symbol) =>
    string.IsNullOrEmpty(symbol) ||
    string.Equals(GetStatusDescription(symbol), NotSupportedDescription, StringComparison.OrdinalIgnoreCase);

  private static PageCatalogFile LoadCatalogFile()
  {
    Assembly assembly = typeof(PageRegistry).Assembly;
    using Stream? stream = assembly.GetManifestResourceStream(CatalogResourceName);
    if (stream == null)
    {
      throw new InvalidOperationException($"Could not find embedded page catalog resource '{CatalogResourceName}'.");
    }

    using var reader = new StreamReader(stream);
    string json = reader.ReadToEnd();
    PageCatalogFile? catalogFile = JsonSerializer.Deserialize<PageCatalogFile>(
      json,
      new JsonSerializerOptions
      {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
      });

    return catalogFile ?? throw new InvalidOperationException("Failed to deserialize the page catalog file.");
  }

  private static IReadOnlyList<PageCatalogEntry> CreateControls(PageCatalogFile catalogFile)
  {
    IReadOnlyList<string> sectionOrder = GetSectionOrder(catalogFile);
    var controls = new List<PageCatalogEntry>();

    foreach (string section in sectionOrder)
    {
      if (!catalogFile.Pages.TryGetValue(section, out List<PageCatalogFileEntry>? entries))
      {
        continue;
      }

      controls.AddRange(entries.Select(entry => CreateControl(section, entry)));
    }

    return controls;
  }

  private static IReadOnlyList<string> GetSectionOrder(PageCatalogFile catalogFile)
  {
    var ordered = new List<string>();
    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (string section in catalogFile.TopLevelOrder)
    {
      if (!seen.Add(section))
      {
        throw new InvalidOperationException($"Duplicate section name '{section}' in topLevelOrder.");
      }

      ordered.Add(section);
    }

    foreach (string section in catalogFile.Pages.Keys)
    {
      if (seen.Add(section))
      {
        ordered.Add(section);
      }
    }

    return ordered;
  }

  private static PageCatalogEntry CreateControl(string section, PageCatalogFileEntry entry)
  {
    string demoTypeName = NormalizeTypeName(entry.Demo);
    Type pageType = ResolvePageType(entry.Source, demoTypeName);
    Type? viewModelType = ResolveViewModelType(entry.Source, entry.ViewModel);

    return new PageCatalogEntry(
      key: entry.UniqueTitle,
      section: section,
      title: entry.UniqueTitle,
      pageType: pageType,
      source: ParseSource(entry.Source),
      categoryPath: ParseCategoryPath(entry.Category),
      statusByTheme: ParseStatuses(entry.Status),
      excludeFromTests: ParseExcludedThemes(entry.ExcludeFromTests),
      viewModelType: viewModelType);
  }

  private static Type ResolvePageType(string? source, string demoTypeName)
  {
    Type? pageType = Type.GetType($"SampleApp.DemoPages.{demoTypeName}, SampleApp", throwOnError: false)
                     ?? Type.GetType($"SampleApp.Experiments.{demoTypeName}, SampleApp", throwOnError: false);
    if (pageType != null)
    {
      return pageType;
    }

    if (string.Equals(source, nameof(ControlSource.AvaloniaPro), StringComparison.OrdinalIgnoreCase))
    {
      return typeof(TextBlock);
    }

    throw new InvalidOperationException($"Could not resolve demo page type '{demoTypeName}'.");
  }

  private static Type? ResolveViewModelType(string? source, string? viewModelName)
  {
    if (string.IsNullOrWhiteSpace(viewModelName))
    {
      return null;
    }

    string normalizedName = NormalizeTypeName(viewModelName);
    Type? viewModelType = Type.GetType($"SampleApp.ViewModels.{normalizedName}, SampleApp", throwOnError: false);
    if (viewModelType != null)
    {
      return viewModelType;
    }

    if (string.Equals(source, nameof(ControlSource.AvaloniaPro), StringComparison.OrdinalIgnoreCase))
    {
      return null;
    }

    throw new InvalidOperationException($"Could not resolve view model type '{normalizedName}'.");
  }

  private static string NormalizeTypeName(string typeOrFileName)
  {
    string trimmed = typeOrFileName.Trim();
    return Path.GetFileNameWithoutExtension(trimmed);
  }

  private static ControlSource? ParseSource(string? source)
  {
    if (string.IsNullOrWhiteSpace(source))
    {
      return null;
    }

    if (Enum.TryParse(source, ignoreCase: true, out ControlSource parsedSource))
    {
      return parsedSource;
    }

    if (string.Equals(source, "SampleApp", StringComparison.OrdinalIgnoreCase))
    {
      return null;
    }

    throw new InvalidOperationException($"Unknown control source '{source}'.");
  }

  private static IReadOnlyList<string> ParseCategoryPath(string category)
  {
    string[] parts = category.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    return parts.Length == 0 ? [] : parts;
  }

  private static IReadOnlyDictionary<ThemeId, string> ParseStatuses(IReadOnlyDictionary<string, string>? statuses)
  {
    var parsedStatuses = new Dictionary<ThemeId, string>();

    if (statuses != null)
    {
      foreach ((string themeName, string symbol) in statuses)
      {
        parsedStatuses[ThemeIds.Parse(themeName)] = symbol;
      }
    }

    foreach (ThemeId themeId in ThemeIds.All)
    {
      parsedStatuses.TryAdd(themeId, string.Empty);
    }

    return parsedStatuses;
  }

  private static IReadOnlyCollection<ThemeId> ParseExcludedThemes(IReadOnlyDictionary<string, bool>? excludeFromTests)
  {
    if (excludeFromTests == null)
    {
      return [];
    }

    return excludeFromTests
      .Where(static pair => pair.Value)
      .Select(pair => ThemeIds.Parse(pair.Key))
      .ToArray();
  }
}
