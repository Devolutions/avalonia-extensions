namespace SampleApp.PageCatalog;

using System.Collections.Generic;
using System.Text.Json.Serialization;

public sealed class PageCatalogFile
{
  [JsonPropertyName("sampleAppStartUpSettings")]
  public SampleAppStartupSettings StartupSettings { get; init; } = new();

  [JsonPropertyName("statusSymbols")]
  public Dictionary<string, string> StatusSymbols { get; init; } = [];

  [JsonPropertyName("topLevelOrder")]
  public List<string> TopLevelOrder { get; init; } = [];

  [JsonPropertyName("pages")]
  public Dictionary<string, List<PageCatalogFileEntry>> Pages { get; init; } = [];
}

public sealed class PageCatalogFileEntry
{
  [JsonPropertyName("uniqueTitle")]
  public string UniqueTitle { get; init; } = "";

  [JsonPropertyName("source")]
  public string? Source { get; init; }

  [JsonPropertyName("category")]
  public string Category { get; init; } = "";

  [JsonPropertyName("demo")]
  public string Demo { get; init; } = "";

  [JsonPropertyName("viewModel")]
  public string? ViewModel { get; init; }

  [JsonPropertyName("status")]
  public Dictionary<string, string>? Status { get; init; }

  [JsonPropertyName("excludeFromTests")]
  public Dictionary<string, bool>? ExcludeFromTests { get; init; }
}

public sealed class SampleAppStartupSettings
{
  [JsonPropertyName("theme")]
  public string Theme { get; init; } = "default";

  [JsonPropertyName("selectedPage")]
  public string? SelectedPage { get; init; }

  [JsonPropertyName("selectedTab")]
  public string? SelectedTab { get; init; }

  [JsonPropertyName("scale")]
  public string Scale { get; init; } = "default";

  [JsonIgnore]
  public string StartupPage => this.SelectedPage ?? this.SelectedTab ?? "Overview";
}
