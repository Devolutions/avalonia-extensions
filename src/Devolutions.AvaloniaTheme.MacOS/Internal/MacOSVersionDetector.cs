namespace Devolutions.AvaloniaTheme.MacOS.Internal;

/// <summary>
///   Detects macOS version to determine if Liquid Glass visual effects should be enabled.
/// </summary>
/// <remarks>
///   Liquid Glass is a visual design language introduced in macOS 26 (Tahoe).
///   This class provides version detection with caching and test override support.
/// </remarks>
public static class MacOSVersionDetector
{
  private static readonly Lazy<bool> _isLiquidGlassSupported = new Lazy<bool>(
    () => OperatingSystem.IsMacOS() && OperatingSystem.IsMacOSVersionAtLeast(26),
    System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

  private static readonly object _overrideLock = new object();
  internal static bool? _testOverride;

  /// <summary>
  ///   Determines if the current OS supports Liquid Glass visual effects.
  /// </summary>
  /// <returns>
  ///   <c>true</c> if running on macOS 26 or later; <c>false</c> otherwise.
  /// </returns>
  /// <remarks>
  ///   <para>
  ///     Version mapping:
  ///     - macOS 26.x (Tahoe) = Liquid Glass supported
  ///     - macOS 15.x (Sequoia) = Classic theme only
  ///     - macOS 14.x (Sonoma) = Classic theme only
  ///   </para>
  ///   <para>
  ///     The result is cached for performance since OS version cannot change during process lifetime.
  ///   </para>
  /// </remarks>
  public static bool IsLiquidGlassSupported()
  {
    // Test override takes precedence (for development/testing)
    lock (_overrideLock)
    {
      if (_testOverride.HasValue)
      {
        return _testOverride.Value;
      }
    }

    // Return thread-safe cached result
    return _isLiquidGlassSupported.Value;
  }

  /// <summary>
  ///   Sets a test override to force enable/disable Liquid Glass support.
  /// </summary>
  /// <param name="useLiquidGlass">
  ///   <c>true</c> to force enable Liquid Glass;
  ///   <c>false</c> to force disable;
  ///   <c>null</c> to clear override and use actual OS version detection.
  /// </param>
  /// <remarks>
  ///   Intended for testing and development purposes only.
  /// </remarks>
  public static void SetTestOverride(bool? useLiquidGlass)
  {
    lock (_overrideLock)
    {
      _testOverride = useLiquidGlass;
    }
  }

  /// <summary>
  ///   Resets all cached state (for testing purposes).
  /// </summary>
  internal static void ResetCache()
  {
    lock (_overrideLock)
    {
      _testOverride = null;
    }
    // Note: Cannot reset Lazy<T> once initialized, but this is acceptable
    // since OS version cannot change during process lifetime
  }
}