namespace Devolutions.AvaloniaTheme.MacOS.Controls;

using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Internal;

/// <summary>
///   Externally consumable MacOS menu styling (<c>ContextMenu</c>, <c>Menu</c>,
///   <c>MenuFlyoutPresenter</c>, <c>MenuItem</c> and menu helper styles) without
///   importing the full <c>DevolutionsMacOsTheme</c>.
/// </summary>
/// <remarks>
///   The pack detects the running macOS version itself, so menus follow the classic /
///   LiquidGlass appearance independently of the host theme.
/// </remarks>
public class MacOsMenuPack : Styles
{
  public MacOsMenuPack()
  {
    // Nested so the menu defaults it carries stay below the resources merged here,
    // which lets the aliases resolved for the active sub-theme take precedence.
    Uri menuPackUri = new("avares://Devolutions.AvaloniaTheme.MacOS/Controls/MenuPack.styles.axaml");
    this.Add(new StyleInclude(menuPackUri) { Source = menuPackUri });

    // The menu tokens are derived from these theme resources, so the pack has to carry
    // them even when no MacOS theme is present in the host application.
    Uri baseUri = new("avares://Devolutions.AvaloniaTheme.MacOS/Accents/ThemeResources.axaml");
    this.Resources.MergedDictionaries.Add(new ResourceInclude(baseUri) { Source = baseUri });

    if (MacOSVersionDetector.IsLiquidGlassSupported())
    {
      Uri liquidGlassUri = new("avares://Devolutions.AvaloniaTheme.MacOS/Accents/ThemeResources_LiquidGlass.axaml");
      this.Resources.MergedDictionaries.Add(new ResourceInclude(liquidGlassUri) { Source = liquidGlassUri });
    }

    ResourceDictionary menuAliases = new();
    this.Resources.MergedDictionaries.Add(menuAliases);
    MenuResourceAliasBuilder.Rebuild(this, menuAliases);
  }
}
