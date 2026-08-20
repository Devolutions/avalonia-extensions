namespace Devolutions.AvaloniaTheme.MacOS.Controls;

using Avalonia.Styling;

/// <summary>
///   Convenience helper for applying the MacOS menu pack from code.
/// </summary>
public static class MacMenuPackStyles
{
  /// <summary>
  ///   Adds the MacOS menu pack to the supplied styles collection.
  /// </summary>
  /// <remarks>
  ///   Equivalent to including
  ///   <c>avares://Devolutions.AvaloniaTheme.MacOS/Controls/MenuPack.styles.axaml</c> from XAML.
  /// </remarks>
  public static void ApplyTo(Styles target) => target.Add(new MacOsMenuPack());
}
