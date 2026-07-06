namespace SampleApp.ControlCatalog;

using System.Collections.Generic;
using System.Text.Json.Serialization;

public sealed class ControlCatalogFile
{
  [JsonPropertyName("statusSymbols")]
  public Dictionary<string, string> StatusSymbols { get; init; } = [];

  [JsonPropertyName("controls")]
  public List<ControlCatalogFileEntry> Controls { get; init; } = [];
}

public sealed class ControlCatalogFileEntry
{
  [JsonPropertyName("name")]
  public string Name { get; init; } = "";

  [JsonPropertyName("type")]
  public string Type { get; init; } = "";

  [JsonPropertyName("source")]
  public string Source { get; init; } = "";

  [JsonPropertyName("category")]
  public string Category { get; init; } = "";

  [JsonPropertyName("demo")]
  public string Demo { get; init; } = "";

  [JsonPropertyName("viewModel")]
  public string? ViewModel { get; init; }

  [JsonPropertyName("status")]
  public Dictionary<string, string> Status { get; init; } = [];

  [JsonPropertyName("excludeFromTests")]
  public Dictionary<string, bool>? ExcludeFromTests { get; init; }
}
