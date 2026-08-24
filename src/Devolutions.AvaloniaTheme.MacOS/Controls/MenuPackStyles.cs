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
  ///   Equivalent to adding <see cref="MacOsMenuPack" /> in XAML (<c>&lt;macos:MacOsMenuPack /&gt;</c>).
  ///   Note this is not the same as including
  ///   <c>avares://Devolutions.AvaloniaTheme.MacOS/Controls/MenuPack.styles.axaml</c> directly:
  ///   that file carries only the classic menu defaults, whereas <see cref="MacOsMenuPack" />
  ///   additionally selects the resources matching the running macOS version.
  /// </remarks>
  public static void ApplyTo(Styles target) => target.Add(new MacOsMenuPack());
}
