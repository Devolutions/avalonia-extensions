namespace SampleApp.PageCatalog;

using System;
using System.Collections.Generic;
using System.Linq;

public enum ThemeId
{
  MacClassic,
  LiquidGlass,
  DevExpress,
  Linux,
  Fluent,
  Simple,
  WinUi,
}

public static class ThemeIds
{
  private static readonly Dictionary<string, ThemeId> NameToId = new(StringComparer.OrdinalIgnoreCase)
  {
    ["MacOS"] = ThemeId.MacClassic,
    [App.MacClassicThemeName] = ThemeId.MacClassic,
    [App.LiquidGlassThemeName] = ThemeId.LiquidGlass,
    ["DevExpress"] = ThemeId.DevExpress,
    ["Linux"] = ThemeId.Linux,
    ["Fluent"] = ThemeId.Fluent,
    ["Simple"] = ThemeId.Simple,
    [App.WinUiThemeName] = ThemeId.WinUi,
    [App.WinUiClassicThemeName] = ThemeId.WinUi,
    [App.WinUiMicaThemeName] = ThemeId.WinUi,
  };

  public static IReadOnlyList<ThemeId> All { get; } =
  [
    ThemeId.MacClassic,
    ThemeId.LiquidGlass,
    ThemeId.DevExpress,
    ThemeId.Linux,
    ThemeId.Fluent,
    ThemeId.Simple,
    ThemeId.WinUi,
  ];

  public static string ToThemeName(this ThemeId themeId) =>
    themeId switch
    {
      ThemeId.MacClassic => App.MacClassicThemeName,
      ThemeId.LiquidGlass => App.LiquidGlassThemeName,
      ThemeId.DevExpress => "DevExpress",
      ThemeId.Linux => "Linux",
      ThemeId.Fluent => "Fluent",
      ThemeId.Simple => "Simple",
      ThemeId.WinUi => App.WinUiThemeName,
      _ => throw new ArgumentOutOfRangeException(nameof(themeId), themeId, "Unknown control theme id."),
    };

  public static bool TryParse(string? themeName, out ThemeId themeId)
  {
    if (string.IsNullOrWhiteSpace(themeName))
    {
      themeId = default;
      return false;
    }

    return NameToId.TryGetValue(themeName.Trim(), out themeId);
  }

  public static ThemeId Parse(string themeName)
  {
    if (TryParse(themeName, out ThemeId themeId))
    {
      return themeId;
    }

    throw new ArgumentException($"Unknown theme '{themeName}'.", nameof(themeName));
  }

  public static string ToCsv(IEnumerable<ThemeId> themeIds) =>
    string.Join(", ", themeIds.Select(ToThemeName));
}
