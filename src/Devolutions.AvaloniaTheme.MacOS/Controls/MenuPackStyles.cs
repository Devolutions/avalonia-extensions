namespace Devolutions.AvaloniaTheme.MacOS.Controls;

using System;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Internal;

public static class MacMenuPackStyles
{
  public static void ApplyTo(Styles target)
  {
    Uri baseThemeResourcesUri = new("avares://Devolutions.AvaloniaTheme.MacOS/Accents/ThemeResources.axaml");
    ResourceInclude baseThemeResourcesInclude = new(baseThemeResourcesUri) { Source = baseThemeResourcesUri };
    target.Resources.MergedDictionaries.Add(baseThemeResourcesInclude);

    Uri menuPackUri = new("avares://Devolutions.AvaloniaTheme.MacOS/Controls/MenuPack.styles.axaml");
    target.Add(new StyleInclude(menuPackUri) { Source = menuPackUri });

    if (MacOSVersionDetector.IsLiquidGlassSupported())
    {
      Uri liquidGlassThemeUri = new("avares://Devolutions.AvaloniaTheme.MacOS/Accents/ThemeResources_LiquidGlass.axaml");
      ResourceInclude liquidGlassThemeInclude = new(liquidGlassThemeUri) { Source = liquidGlassThemeUri };
      target.Resources.MergedDictionaries.Add(liquidGlassThemeInclude);

      Uri liquidGlassMenuUri = new("avares://Devolutions.AvaloniaTheme.MacOS/Accents/MenuResources_LiquidGlass.axaml");
      ResourceInclude liquidGlassMenuInclude = new(liquidGlassMenuUri) { Source = liquidGlassMenuUri };
      target.Resources.MergedDictionaries.Add(liquidGlassMenuInclude);
    }

    var menuAliases = MenuResourceAliasBuilder.Build(target);
    target.Resources.MergedDictionaries.Add(menuAliases);
    MenuResourceAliasBuilder.Rebuild(target, menuAliases);
  }
}
