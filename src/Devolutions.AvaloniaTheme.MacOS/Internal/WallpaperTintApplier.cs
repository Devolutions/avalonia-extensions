namespace Devolutions.AvaloniaTheme.MacOS.Internal;

using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;

/// <summary>
///   Applies a wallpaper-tinted background colour to <c>TextBoxBackgroundBrush</c> at runtime
///   when running under macOS LiquidGlass (macOS 26+).
///   The resolved colour is also stored under <c>LG.Wallpaper.DominantBrush</c> for diagnostic use.
/// </summary>
/// <remarks>
///   <para>
///     Formula (dark mode, calibrated from native macOS 26 samples):
///     <code>
///       L      = 0.118 + wallpaperL × 0.08   (lightness bleed-through from wallpaper)
///       S      = min(wallpaperS × 0.20, 0.094) (hue strength, capped)
///       result = HSL(wallpaperH, S, L)
///     </code>
///     Light mode returns white (#FFFFFF) — native macOS always uses white for TextBox
///     backgrounds in light mode regardless of wallpaper.
///   </para>
///   <para>
///     The applier uses CoreGraphics via P/Invoke to sample the wallpaper image.
///     When sampling fails (plain colour desktop, dynamic/video wallpaper, etc.) the method
///     returns <c>false</c> and callers fall back to the static theme resource.
///   </para>
/// </remarks>
internal static class WallpaperTintApplier
{
  // ─── Public token key surfaced for diagnostic consumers ─────────────────────
  /// <summary>Resource key where the raw dominant wallpaper colour is stored.</summary>
  internal const string WallpaperDominantBrushKey = "LG.Wallpaper.DominantBrush";

  // ─── P/Invoke ────────────────────────────────────────────────────────────────

  [DllImport("/usr/lib/libobjc.A.dylib")]
  private static extern IntPtr objc_getClass(string name);

  [DllImport("/usr/lib/libobjc.A.dylib")]
  private static extern IntPtr sel_registerName(string name);

  [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
  private static extern IntPtr objc_msgSend_ptr(IntPtr receiver, IntPtr selector);

  [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
  private static extern IntPtr objc_msgSend_ptr_ptr(IntPtr receiver, IntPtr selector, IntPtr arg1);

  // CoreFoundation
  [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
  private static extern void CFRelease(IntPtr cf);

  // CoreGraphics
  [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
  private static extern IntPtr CGColorSpaceCreateDeviceRGB();

  [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
  private static extern void CGColorSpaceRelease(IntPtr colorSpace);

  [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
  private static extern IntPtr CGBitmapContextCreate(IntPtr data, nuint width, nuint height, nuint bitsPerComponent, nuint bytesPerRow, IntPtr space, uint bitmapInfo);

  [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
  private static extern void CGContextDrawImage(IntPtr context, CGRect rect, IntPtr image);

  [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
  private static extern IntPtr CGBitmapContextGetData(IntPtr context);

  [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
  private static extern void CGContextRelease(IntPtr context);

  // ImageIO
  [DllImport("/System/Library/Frameworks/ImageIO.framework/ImageIO")]
  private static extern IntPtr CGImageSourceCreateWithURL(IntPtr url, IntPtr options);

  [DllImport("/System/Library/Frameworks/ImageIO.framework/ImageIO")]
  private static extern IntPtr CGImageSourceCreateImageAtIndex(IntPtr isrc, nuint index, IntPtr options);

  [StructLayout(LayoutKind.Sequential)]
  private struct CGRect
  {
    public double X, Y, Width, Height;

    public CGRect(double x, double y, double w, double h)
    {
      X = x; Y = y; Width = w; Height = h;
    }
  }

  // ─── Hook management ─────────────────────────────────────────────────────────

  private static bool hooked;
  private static bool enabled;

  private static bool IsThemeVariantProperty(AvaloniaProperty property) =>
    property.Name is "RequestedThemeVariant" or "ActualThemeVariant";

  /// <summary>
  ///   Applies the wallpaper tint immediately and wires up event hooks so the colour
  ///   updates whenever the OS dark/light mode changes or the application theme variant
  ///   changes. Safe to call multiple times; hooks are installed only once.
  /// </summary>
  public static void HookAndApply(Application app)
  {
    if (app is null) throw new ArgumentNullException(nameof(app));

    enabled = true;
    Apply(app);

    if (hooked) return;
    hooked = true;

    if (app.PlatformSettings is { } platformSettings)
    {
      platformSettings.ColorValuesChanged += (_, _) => Apply(app);
    }

    app.PropertyChanged += (_, e) =>
    {
      if (IsThemeVariantProperty(e.Property))
      {
        Dispatcher.UIThread.Post(() => Apply(app), DispatcherPriority.Background);
      }
    };
  }

  /// <summary>
  ///   Removes all runtime wallpaper-tint resource overrides so static theme resources take over,
  ///   and disables the hook so dark/light-mode changes no longer reapply the tint.
  ///   Re-enabled automatically the next time <see cref="HookAndApply"/> is called.
  /// </summary>
  public static void Clear(Application app)
  {
    if (app is null) throw new ArgumentNullException(nameof(app));

    enabled = false;
    app.Resources.Remove("TextBoxBackgroundBrush");
    app.Resources.Remove(WallpaperDominantBrushKey);
  }

  /// <summary>
  ///   Computes the wallpaper-tinted TextBox background colour and writes it to
  ///   <c>Application.Resources</c>.  Falls back silently when wallpaper sampling is
  ///   unavailable (plain colour, dynamic, or video wallpapers).
  /// </summary>
  public static void Apply(Application app)
  {
    if (app is null) throw new ArgumentNullException(nameof(app));
    if (!enabled) return;

    ThemeVariant? effectiveVariant = app.ActualThemeVariant;
    if (app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime &&
        desktopLifetime.MainWindow is { } mainWindow)
    {
      effectiveVariant = mainWindow.ActualThemeVariant ?? effectiveVariant;
    }

    bool isDark = effectiveVariant == ThemeVariant.Dark;

    // Light mode: native macOS always uses plain white — no wallpaper tint.
    if (!isDark)
    {
      app.Resources["TextBoxBackgroundBrush"] = new SolidColorBrush(Colors.White);
      app.Resources.Remove(WallpaperDominantBrushKey);
      return;
    }

    if (TryGetWallpaperAverageColor(out Color wallpaperColor))
    {
      app.Resources[WallpaperDominantBrushKey] = new SolidColorBrush(wallpaperColor);
      Color tinted = ComputeTintedDarkBackground(wallpaperColor);
      app.Resources["TextBoxBackgroundBrush"] = new SolidColorBrush(tinted);
    }
    else
    {
      // Sampling failed — remove both overrides so static AXAML resources take over.
      Clear(app);
    }
  }

  // ─── Wallpaper sampling ──────────────────────────────────────────────────────

  private static bool TryGetWallpaperAverageColor(out Color color)
  {
    color = default;
    try
    {
      IntPtr nsWorkspaceClass = objc_getClass("NSWorkspace");
      if (nsWorkspaceClass == IntPtr.Zero) return false;
      IntPtr sharedWorkspace = objc_msgSend_ptr(nsWorkspaceClass, sel_registerName("sharedWorkspace"));
      if (sharedWorkspace == IntPtr.Zero) return false;

      IntPtr nsScreenClass = objc_getClass("NSScreen");
      if (nsScreenClass == IntPtr.Zero) return false;
      IntPtr mainScreen = objc_msgSend_ptr(nsScreenClass, sel_registerName("mainScreen"));
      if (mainScreen == IntPtr.Zero) return false;

      // NSURL is toll-free bridged to CFURL — pass directly to CGImageSourceCreateWithURL
      IntPtr wallpaperUrl = objc_msgSend_ptr_ptr(sharedWorkspace,
        sel_registerName("desktopImageURLForScreen:"), mainScreen);
      if (wallpaperUrl == IntPtr.Zero) return false;

      IntPtr imageSource = CGImageSourceCreateWithURL(wallpaperUrl, IntPtr.Zero);
      if (imageSource == IntPtr.Zero) return false;
      try
      {
        IntPtr cgImage = CGImageSourceCreateImageAtIndex(imageSource, 0, IntPtr.Zero);
        if (cgImage == IntPtr.Zero) return false;
        try
        {
          // Scale the entire wallpaper down to 10×10 — effectively averages all pixels
          const int size = 10;
          IntPtr colorSpace = CGColorSpaceCreateDeviceRGB();
          if (colorSpace == IntPtr.Zero) return false;

          // kCGImageAlphaPremultipliedLast = 1 → RGBA byte order
          IntPtr ctx = CGBitmapContextCreate(IntPtr.Zero, size, size, 8, (nuint)(size * 4), colorSpace, 1);
          CGColorSpaceRelease(colorSpace);
          if (ctx == IntPtr.Zero) return false;
          try
          {
            CGContextDrawImage(ctx, new CGRect(0, 0, size, size), cgImage);
            IntPtr data = CGBitmapContextGetData(ctx);
            if (data == IntPtr.Zero) return false;

            long totalR = 0, totalG = 0, totalB = 0, totalA = 0;
            const int count = size * size;
            for (int i = 0; i < count; i++)
            {
              int offset = i * 4;
              totalR += Marshal.ReadByte(data, offset);
              totalG += Marshal.ReadByte(data, offset + 1);
              totalB += Marshal.ReadByte(data, offset + 2);
              totalA += Marshal.ReadByte(data, offset + 3);
            }

            double avgA = totalA / (double)count / 255.0;
            double avgR = avgA > 0 ? totalR / (double)count / 255.0 / avgA : 0;
            double avgG = avgA > 0 ? totalG / (double)count / 255.0 / avgA : 0;
            double avgB = avgA > 0 ? totalB / (double)count / 255.0 / avgA : 0;

            color = Color.FromArgb(255, ToByte(avgR), ToByte(avgG), ToByte(avgB));
            return true;
          }
          finally { CGContextRelease(ctx); }
        }
        finally { CFRelease(cgImage); }
      }
      finally { CFRelease(imageSource); }
    }
    catch
    {
      return false;
    }
  }

  // ─── Tint formula ────────────────────────────────────────────────────────────

  // Calibrated from 4 native macOS 26 dark-mode TextBox samples:
  //   kL  ≈ 0.08   (lightness bleed-through from wallpaper luminance)
  //   kS  ≈ 0.20   (saturation fraction carried over from wallpaper hue)
  //   cap ≈ 0.094  (maximum allowed chroma)

  private static Color ComputeTintedDarkBackground(Color wallpaper)
  {
    double wallR = wallpaper.R / 255.0;
    double wallG = wallpaper.G / 255.0;
    double wallB = wallpaper.B / 255.0;

    RgbToHsl(wallR, wallG, wallB, out double h, out double s, out double wallL);

    double targetL = 0.118 + wallL * 0.08;
    double targetS = s > 0.05 ? Math.Min(s * 0.20, 0.094) : 0.0;

    HslToRgb(h, targetS, targetL, out double r, out double g, out double b);
    return Color.FromArgb(255, ToByte(r), ToByte(g), ToByte(b));
  }

  // ─── HSL helpers ─────────────────────────────────────────────────────────────

  private static void RgbToHsl(double r, double g, double b, out double h, out double s, out double l)
  {
    double max = Math.Max(r, Math.Max(g, b));
    double min = Math.Min(r, Math.Min(g, b));
    l = (max + min) / 2.0;
    double d = max - min;
    if (d < 1e-10) { h = 0; s = 0; return; }

    s = l > 0.5 ? d / (2.0 - max - min) : d / (max + min);
    if (max == r)      h = ((g - b) / d + (g < b ? 6.0 : 0.0)) / 6.0;
    else if (max == g) h = ((b - r) / d + 2.0) / 6.0;
    else               h = ((r - g) / d + 4.0) / 6.0;
  }

  private static void HslToRgb(double h, double s, double l, out double r, out double g, out double b)
  {
    if (s < 1e-10) { r = g = b = l; return; }
    double q = l < 0.5 ? l * (1.0 + s) : l + s - l * s;
    double p = 2.0 * l - q;
    r = HueToRgb(p, q, h + 1.0 / 3.0);
    g = HueToRgb(p, q, h);
    b = HueToRgb(p, q, h - 1.0 / 3.0);
  }

  private static double HueToRgb(double p, double q, double t)
  {
    if (t < 0) t += 1.0;
    if (t > 1) t -= 1.0;
    if (t < 1.0 / 6.0) return p + (q - p) * 6.0 * t;
    if (t < 0.5)        return q;
    if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6.0;
    return p;
  }

  private static byte ToByte(double value)
  {
    return (byte)Math.Round(Math.Clamp(value, 0.0, 1.0) * 255.0);
  }
}
