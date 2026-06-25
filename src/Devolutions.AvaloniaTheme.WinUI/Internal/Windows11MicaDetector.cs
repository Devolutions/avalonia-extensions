namespace Devolutions.AvaloniaTheme.WinUI.Internal;

using System;
using System.Threading;

/// <summary>
///   Detects whether the Windows 11 Mica visual treatment should be applied.
/// </summary>
/// <remarks>
///   Mica is the translucent, wallpaper-tinted backdrop introduced with Windows 11 (build 22000).
///   This class provides version detection with caching and test override support so the Mica
///   surface treatment can be previewed on non-Windows hosts (e.g. when developing on macOS).
///
///   Only the <em>resource</em> side of Mica (translucent surface brushes) is governed here.
///   The actual native compositor backdrop (TransparencyLevelHint / DWM) is an application-layer
///   concern and only has a visible effect on real Windows 11.
/// </remarks>
public static class Windows11MicaDetector
{
    private static readonly Lazy<bool> IsMicaSupportedLazy = new(
        () => OperatingSystem.IsWindows() && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000),
        LazyThreadSafetyMode.ExecutionAndPublication);

    private static readonly object OverrideLock = new();
    internal static bool? TestOverride;

    /// <summary>
    ///   Determines whether the current environment should use the Windows 11 Mica surface treatment.
    /// </summary>
    /// <returns>
    ///   <c>true</c> when running on Windows 11 (build 22000) or later, or when a test override forces it on;
    ///   <c>false</c> otherwise.
    /// </returns>
    /// <remarks>
    ///   The result is cached for performance since the OS version cannot change during process lifetime.
    /// </remarks>
    public static bool IsMicaSupported()
    {
        // Test override takes precedence (for development/testing).
        lock (OverrideLock)
        {
            if (TestOverride.HasValue)
            {
                return TestOverride.Value;
            }
        }

        return IsMicaSupportedLazy.Value;
    }

    /// <summary>
    ///   Sets a test override to force enable/disable the Mica surface treatment.
    /// </summary>
    /// <param name="useMica">
    ///   <c>true</c> to force enable Mica;
    ///   <c>false</c> to force disable;
    ///   <c>null</c> to clear the override and use actual OS version detection.
    /// </param>
    /// <remarks>
    ///   Intended for testing and development purposes only.
    /// </remarks>
    public static void SetTestOverride(bool? useMica)
    {
        lock (OverrideLock)
        {
            TestOverride = useMica;
        }
    }

    /// <summary>
    ///   Resets all cached state (for testing purposes).
    /// </summary>
    internal static void ResetCache()
    {
        lock (OverrideLock)
        {
            TestOverride = null;
        }
        // Note: Cannot reset Lazy<T> once initialized, but this is acceptable
        // since the OS version cannot change during process lifetime.
    }
}
