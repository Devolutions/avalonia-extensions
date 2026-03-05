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

    Color color = OperatingSystem.IsMacOS() && AppKitSemanticColors.TryGetControlBackgroundColor(preferDark, out Color systemColor)
      ? systemColor
      : Color.Parse("#FF2A2A2A");

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