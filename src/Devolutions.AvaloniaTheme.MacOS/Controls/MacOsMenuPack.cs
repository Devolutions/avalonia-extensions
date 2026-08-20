namespace Devolutions.AvaloniaTheme.MacOS.Controls;

using System;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Internal;

/// <summary>
///   Externally consumable MacOS menu styling (<c>ContextMenu</c>, <c>Menu</c>,
///   <c>MenuFlyoutPresenter</c>, <c>MenuItem</c> and menu helper styles) without
///   importing the full <c>DevolutionsMacOsTheme</c>.
/// </summary>
/// <remarks>
///   <para>
///     The pack detects the running macOS version itself, so menus follow the classic /
///     LiquidGlass appearance independently of the host theme.
///   </para>
///   <para>
///     Only the prefixed <c>MacOsMenu*</c> tokens are published: the MacOS theme dictionaries
///     are deliberately not merged, so non-menu controls keep resolving their host theme's
///     resources.
///   </para>
/// </remarks>
public class MacOsMenuPack : Styles
{
  public MacOsMenuPack()
  {
    // Carries the menu control themes plus the classic MacOsMenu* token defaults.
    Uri menuPackUri = new("avares://Devolutions.AvaloniaTheme.MacOS/Controls/MenuPack.styles.axaml");
    this.Add(new StyleInclude(menuPackUri) { Source = menuPackUri });

    if (MacOSVersionDetector.IsLiquidGlassSupported())
    {
      Uri liquidGlassMenuUri = new("avares://Devolutions.AvaloniaTheme.MacOS/Accents/MenuResources_LiquidGlass.axaml");
      this.Resources.MergedDictionaries.Add(new ResourceInclude(liquidGlassMenuUri) { Source = liquidGlassMenuUri });
    }
  }
}
