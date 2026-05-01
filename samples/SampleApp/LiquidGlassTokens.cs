namespace SampleApp;

using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;

public static class LiquidGlassTokenKeys
{
  public const string TextBoxBackgroundBrush = "LG.TextBox.BackgroundBrush";
}

public static class LiquidGlassPaletteApplier
{
  private static bool hooked;

  private static bool IsThemeVariantProperty(AvaloniaProperty property) =>
    property.Name is "RequestedThemeVariant" or "ActualThemeVariant";

  public static void ApplyTextBoxBackgroundFromSystem(Application app)
  {
    if (app is null) throw new ArgumentNullException(nameof(app));

    ThemeVariant? effectiveThemeVariant = app.ActualThemeVariant;
    if (app.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktopLifetime &&
        desktopLifetime.MainWindow is { } mainWindow)
    {
      effectiveThemeVariant = mainWindow.ActualThemeVariant ?? effectiveThemeVariant;
    }

    bool? preferDark = effectiveThemeVariant == ThemeVariant.Dark
      ? true
      : effectiveThemeVariant == ThemeVariant.Light
        ? false
        : null;

    Color color;
    if (OperatingSystem.IsMacOS())
    {
      if (!AppKitSemanticColors.TryGetWallpaperTintedColor(preferDark, out color) &&
          !AppKitSemanticColors.TryGetControlBackgroundColor(preferDark, out color))
      {
        color = preferDark == true ? Color.Parse("#FF1E1E1E") : Color.Parse("#FFE8E8E8");
      }
    }
    else
    {
      color = Color.Parse("#FF2A2A2A");
    }

    app.Resources[LiquidGlassTokenKeys.TextBoxBackgroundBrush] = new SolidColorBrush(color);
  }

  public static void HookAndApply(Application app)
  {
    if (app is null) throw new ArgumentNullException(nameof(app));

    ApplyTextBoxBackgroundFromSystem(app);

    if (hooked)
    {
      return;
    }

    hooked = true;
    app.PlatformSettings?.ColorValuesChanged += (_, _) => ApplyTextBoxBackgroundFromSystem(app);
    app.PropertyChanged += (_, e) =>
    {
      if (IsThemeVariantProperty(e.Property))
      {
        Dispatcher.UIThread.Post(
          () => ApplyTextBoxBackgroundFromSystem(app),
          DispatcherPriority.Background);
      }
    };
  }
}

internal static class AppKitSemanticColors
{
  [DllImport("/usr/lib/libobjc.A.dylib")]
  private static extern IntPtr objc_getClass(string name);

  [DllImport("/usr/lib/libobjc.A.dylib")]
  private static extern IntPtr sel_registerName(string name);

  [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
  private static extern IntPtr objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector);

  [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
  private static extern IntPtr objc_msgSend_IntPtr_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1);

  [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
  private static extern IntPtr objc_msgSend_IntPtr_string(IntPtr receiver, IntPtr selector, string arg1);

  [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
  private static extern void objc_msgSend_void_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1);

  [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
  private static extern double objc_msgSend_double(IntPtr receiver, IntPtr selector);

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
    public CGRect(double x, double y, double width, double height)
    { X = x; Y = y; Width = width; Height = height; }
  }

  public static bool TryGetWallpaperTintedColor(bool? preferDark, out Color color)
  {
    color = default;
    try
    {
      // Get wallpaper URL via NSWorkspace
      IntPtr nsWorkspaceClass = objc_getClass("NSWorkspace");
      if (nsWorkspaceClass == IntPtr.Zero) return false;
      IntPtr sharedWorkspace = objc_msgSend_IntPtr(nsWorkspaceClass, sel_registerName("sharedWorkspace"));
      if (sharedWorkspace == IntPtr.Zero) return false;

      IntPtr nsScreenClass = objc_getClass("NSScreen");
      if (nsScreenClass == IntPtr.Zero) return false;
      IntPtr mainScreen = objc_msgSend_IntPtr(nsScreenClass, sel_registerName("mainScreen"));
      if (mainScreen == IntPtr.Zero) return false;

      // NSURL is toll-free bridged to CFURL — pass directly to CGImageSourceCreateWithURL
      IntPtr wallpaperUrl = objc_msgSend_IntPtr_IntPtr(sharedWorkspace,
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
          // Draw the entire wallpaper into a 10×10 bitmap — effectively averages all pixels
          const int size = 10;
          IntPtr colorSpace = CGColorSpaceCreateDeviceRGB();
          if (colorSpace == IntPtr.Zero) return false;
          // kCGImageAlphaPremultipliedLast = 1  →  RGBA byte order
          IntPtr ctx = CGBitmapContextCreate(IntPtr.Zero, size, size, 8, (nuint)(size * 4), colorSpace, 1);
          CGColorSpaceRelease(colorSpace);
          if (ctx == IntPtr.Zero) return false;
          try
          {
            CGContextDrawImage(ctx, new CGRect(0, 0, size, size), cgImage);
            IntPtr data = CGBitmapContextGetData(ctx);
            if (data == IntPtr.Zero) return false;

            long totalR = 0, totalG = 0, totalB = 0, totalA = 0;
            int count = size * size;
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

            color = ComputeTintedBackgroundColor(avgR, avgG, avgB, preferDark);
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

  private static Color ComputeTintedBackgroundColor(double wallR, double wallG, double wallB, bool? preferDark)
  {
    bool isDark = preferDark ?? false;
    double baseL = isDark ? 0.118 : 0.922;

    // Extract hue and saturation from the wallpaper's average color
    RgbToHsl(wallR, wallG, wallB, out double h, out double s, out _);

    // Apply a subtle tint: only when the wallpaper has enough saturation
    // Cap tinted saturation at ~12% to stay close to macOS Desktop Tinting behaviour
    double tintS = s > 0.05 ? Math.Min(s * 0.5, 0.12) : 0.0;

    HslToRgb(h, tintS, baseL, out double r, out double g, out double b);
    return Color.FromArgb(255, ToByte(r), ToByte(g), ToByte(b));
  }

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

  public static bool TryGetControlBackgroundColor(bool? preferDark, out Color color)
  {
    color = default;

    try
    {
      IntPtr previousAppearance = IntPtr.Zero;
      bool appearanceSet = false;

      if (preferDark.HasValue)
      {
        IntPtr nsAppearanceClass = objc_getClass("NSAppearance");
        if (nsAppearanceClass != IntPtr.Zero)
        {
          IntPtr selCurrentAppearance = sel_registerName("currentAppearance");
          IntPtr selSetCurrentAppearance = sel_registerName("setCurrentAppearance:");
          IntPtr selAppearanceNamed = sel_registerName("appearanceNamed:");

          previousAppearance = objc_msgSend_IntPtr(nsAppearanceClass, selCurrentAppearance);

          string appearanceName = preferDark.Value ? "NSAppearanceNameDarkAqua" : "NSAppearanceNameAqua";
          IntPtr nsAppearanceName = CreateNSString(appearanceName);
          if (nsAppearanceName != IntPtr.Zero)
          {
            IntPtr desiredAppearance = objc_msgSend_IntPtr_IntPtr(nsAppearanceClass, selAppearanceNamed, nsAppearanceName);
            if (desiredAppearance != IntPtr.Zero)
            {
              objc_msgSend_void_IntPtr(nsAppearanceClass, selSetCurrentAppearance, desiredAppearance);
              appearanceSet = true;
            }
          }
        }
      }

      try
      {
      IntPtr nsColorClass = objc_getClass("NSColor");
      if (nsColorClass == IntPtr.Zero)
      {
        return false;
      }

      IntPtr nsColor = objc_msgSend_IntPtr(nsColorClass, sel_registerName("controlBackgroundColor"));
      if (nsColor == IntPtr.Zero)
      {
        return false;
      }

      IntPtr nsColorSpaceClass = objc_getClass("NSColorSpace");
      if (nsColorSpaceClass == IntPtr.Zero)
      {
        return false;
      }

      IntPtr deviceRgbSpace = objc_msgSend_IntPtr(nsColorSpaceClass, sel_registerName("deviceRGBColorSpace"));
      if (deviceRgbSpace == IntPtr.Zero)
      {
        return false;
      }

      IntPtr rgbColor = objc_msgSend_IntPtr_IntPtr(nsColor, sel_registerName("colorUsingColorSpace:"), deviceRgbSpace);
      if (rgbColor == IntPtr.Zero)
      {
        return false;
      }

      double red = objc_msgSend_double(rgbColor, sel_registerName("redComponent"));
      double green = objc_msgSend_double(rgbColor, sel_registerName("greenComponent"));
      double blue = objc_msgSend_double(rgbColor, sel_registerName("blueComponent"));
      double alpha = objc_msgSend_double(rgbColor, sel_registerName("alphaComponent"));

      color = Color.FromArgb(ToByte(alpha), ToByte(red), ToByte(green), ToByte(blue));
      return true;
      }
      finally
      {
        if (appearanceSet)
        {
          IntPtr nsAppearanceClass = objc_getClass("NSAppearance");
          if (nsAppearanceClass != IntPtr.Zero)
          {
            objc_msgSend_void_IntPtr(nsAppearanceClass, sel_registerName("setCurrentAppearance:"), previousAppearance);
          }
        }
      }
    }
    catch
    {
      return false;
    }
  }

  private static byte ToByte(double value)
  {
    double normalized = Math.Clamp(value, 0, 1);
    return (byte)Math.Round(normalized * 255.0);
  }

  private static IntPtr CreateNSString(string value)
  {
    IntPtr nsStringClass = objc_getClass("NSString");
    if (nsStringClass == IntPtr.Zero)
    {
      return IntPtr.Zero;
    }

    return objc_msgSend_IntPtr_string(nsStringClass, sel_registerName("stringWithUTF8String:"), value);
  }
}