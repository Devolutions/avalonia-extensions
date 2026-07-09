namespace SampleApp.ControlCatalog;

using System;
using System.Collections.Generic;
using System.Linq;

public enum ControlThemeId
{
  MacClassic,
  LiquidGlass,
  DevExpress,
  Linux,
  Fluent,
  Simple,
  WinUi,
}

public static class ControlThemeIds
{
  private static readonly Dictionary<string, ControlThemeId> NameToId = new(StringComparer.OrdinalIgnoreCase)
  {
    ["MacOS"] = ControlThemeId.MacClassic,
    [App.MacClassicThemeName] = ControlThemeId.MacClassic,
    [App.LiquidGlassThemeName] = ControlThemeId.LiquidGlass,
    ["DevExpress"] = ControlThemeId.DevExpress,
    ["Linux"] = ControlThemeId.Linux,
    ["Fluent"] = ControlThemeId.Fluent,
    ["Simple"] = ControlThemeId.Simple,
    [App.WinUiThemeName] = ControlThemeId.WinUi,
    [App.WinUiClassicThemeName] = ControlThemeId.WinUi,
    [App.WinUiMicaThemeName] = ControlThemeId.WinUi,
  };

  public static IReadOnlyList<ControlThemeId> All { get; } =
  [
    ControlThemeId.MacClassic,
    ControlThemeId.LiquidGlass,
    ControlThemeId.DevExpress,
    ControlThemeId.Linux,
    ControlThemeId.Fluent,
    ControlThemeId.Simple,
    ControlThemeId.WinUi,
  ];

  public static string ToThemeName(this ControlThemeId themeId) =>
    themeId switch
    {
      ControlThemeId.MacClassic => App.MacClassicThemeName,
      ControlThemeId.LiquidGlass => App.LiquidGlassThemeName,
      ControlThemeId.DevExpress => "DevExpress",
      ControlThemeId.Linux => "Linux",
      ControlThemeId.Fluent => "Fluent",
      ControlThemeId.Simple => "Simple",
      ControlThemeId.WinUi => App.WinUiThemeName,
      _ => throw new ArgumentOutOfRangeException(nameof(themeId), themeId, "Unknown control theme id."),
    };

  public static bool TryParse(string? themeName, out ControlThemeId themeId)
  {
    if (string.IsNullOrWhiteSpace(themeName))
    {
      themeId = default;
      return false;
    }

    return NameToId.TryGetValue(themeName.Trim(), out themeId);
  }

  public static ControlThemeId Parse(string themeName)
  {
    if (TryParse(themeName, out ControlThemeId themeId))
    {
      return themeId;
    }

    throw new ArgumentException($"Unknown theme '{themeName}'.", nameof(themeName));
  }

  public static string ToCsv(IEnumerable<ControlThemeId> themeIds) =>
    string.Join(", ", themeIds.Select(ToThemeName));
}
