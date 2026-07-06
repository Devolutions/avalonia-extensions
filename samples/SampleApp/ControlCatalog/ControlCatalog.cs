namespace SampleApp.ControlCatalog;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Avalonia.Controls;

public static class ControlRegistry
{
  private const string NotSupportedDescription = "Not Supported";
  private const string CatalogResourceName = "SampleApp.ControlCatalog.control-catalog.jsonc";
  private static readonly ControlCatalogFile CatalogFile = LoadCatalogFile();
  private static readonly IReadOnlyList<ControlCatalogEntry> Controls = CreateControls(CatalogFile);
  private static readonly IReadOnlyList<string> ValidationErrors = ControlCatalogEntry.Validate(Controls);

  public static IReadOnlyList<ControlCatalogEntry> All => Controls;

  public static IReadOnlyDictionary<string, string> StatusDescriptions => CatalogFile.StatusSymbols;

  public static void EnsureValid()
  {
    if (ValidationErrors.Count == 0)
    {
      return;
    }

    string joinedErrors = string.Join(Environment.NewLine, ValidationErrors);
    throw new InvalidOperationException($"The controls catalog is invalid:{Environment.NewLine}{joinedErrors}");
  }

  public static string GetStatusDescription(string symbol) =>
    StatusDescriptions.TryGetValue(symbol, out string? description)
      ? description
      : "Unknown status";

  public static bool IsNotSupportedSymbol(string symbol) =>
    string.Equals(GetStatusDescription(symbol), NotSupportedDescription, StringComparison.OrdinalIgnoreCase);

  private static ControlCatalogFile LoadCatalogFile()
  {
    Assembly assembly = typeof(ControlRegistry).Assembly;
    using Stream? stream = assembly.GetManifestResourceStream(CatalogResourceName);
    if (stream == null)
    {
      throw new InvalidOperationException($"Could not find embedded control catalog resource '{CatalogResourceName}'.");
    }

    using var reader = new StreamReader(stream);
    string json = reader.ReadToEnd();
    ControlCatalogFile? catalogFile = JsonSerializer.Deserialize<ControlCatalogFile>(
      json,
      new JsonSerializerOptions
      {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
      });

    return catalogFile ?? throw new InvalidOperationException("Failed to deserialize the control catalog file.");
  }

  private static IReadOnlyList<ControlCatalogEntry> CreateControls(ControlCatalogFile catalogFile) =>
    catalogFile.Controls.Select(CreateControl).ToArray();

  private static ControlCatalogEntry CreateControl(ControlCatalogFileEntry entry)
  {
    string demoTypeName = NormalizeTypeName(entry.Demo);
    Type pageType = ResolvePageType(entry.Source, demoTypeName);
    Type? viewModelType = ResolveViewModelType(entry.ViewModel);

    return new ControlCatalogEntry(
      key: demoTypeName,
      controlTypeName: entry.Type,
      title: entry.Name,
      pageType: pageType,
      source: ParseSource(entry.Source),
      categoryPath: ParseCategoryPath(entry.Category),
      statusByTheme: ParseStatuses(entry.Status),
      excludeFromTests: ParseExcludedThemes(entry.ExcludeFromTests),
      viewModelType: viewModelType);
  }

  private static Type ResolvePageType(string source, string demoTypeName)
  {
    Type? pageType = Type.GetType($"SampleApp.DemoPages.{demoTypeName}, SampleApp", throwOnError: false);
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

  private static Type? ResolveViewModelType(string? viewModelName)
  {
    if (string.IsNullOrWhiteSpace(viewModelName))
    {
      return null;
    }

    string normalizedName = NormalizeTypeName(viewModelName);
    return Type.GetType($"SampleApp.ViewModels.{normalizedName}, SampleApp", throwOnError: true);
  }

  private static string NormalizeTypeName(string typeOrFileName)
  {
    string trimmed = typeOrFileName.Trim();
    return Path.GetFileNameWithoutExtension(trimmed);
  }

  private static ControlSource ParseSource(string source) =>
    Enum.TryParse(source, ignoreCase: true, out ControlSource parsedSource)
      ? parsedSource
      : throw new InvalidOperationException($"Unknown control source '{source}'.");

  private static IReadOnlyList<string> ParseCategoryPath(string category)
  {
    string[] parts = category.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    return parts.Length == 0 ? [] : parts;
  }

  private static IReadOnlyDictionary<ControlThemeId, string> ParseStatuses(IReadOnlyDictionary<string, string> statuses)
  {
    Dictionary<ControlThemeId, string> parsedStatuses = ControlThemeIds.All.ToDictionary(
      static themeId => themeId,
      _ => GetRequiredStatusSymbol(NotSupportedDescription));

    foreach ((string themeName, string symbol) in statuses)
    {
      parsedStatuses[ControlThemeIds.Parse(themeName)] = symbol;
    }

    return parsedStatuses;
  }

  private static string GetRequiredStatusSymbol(string description)
  {
    foreach ((string symbol, string meaning) in StatusDescriptions)
    {
      if (string.Equals(meaning, description, StringComparison.OrdinalIgnoreCase))
      {
        return symbol;
      }
    }

    throw new InvalidOperationException($"The control catalog status legend must define a symbol for '{description}'.");
  }

  private static IReadOnlyCollection<ControlThemeId> ParseExcludedThemes(IReadOnlyDictionary<string, bool>? excludeFromTests)
  {
    if (excludeFromTests == null)
    {
      return [];
    }

    return excludeFromTests
      .Where(static pair => pair.Value)
      .Select(pair => ControlThemeIds.Parse(pair.Key))
      .ToArray();
  }
}
