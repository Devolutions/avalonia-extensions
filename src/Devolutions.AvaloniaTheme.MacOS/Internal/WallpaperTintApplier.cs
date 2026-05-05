namespace Devolutions.AvaloniaTheme.MacOS.Internal;

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml.Linq;
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

  [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
  private static extern uint objc_msgSend_uint(IntPtr receiver, IntPtr selector);

  // CoreFoundation
  [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
  private static extern void CFRelease(IntPtr cf);

  [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
  private static extern IntPtr CFNotificationCenterGetDistributedCenter();

  [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
  private static extern IntPtr CFStringCreateWithCString(
    IntPtr alloc, [MarshalAs(UnmanagedType.LPUTF8Str)] string cStr, uint encoding);

  [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
  private static extern IntPtr CFUUIDCreateString(IntPtr alloc, IntPtr uuid);

  [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
  private static extern IntPtr CFURLCreateWithFileSystemPath(IntPtr allocator, IntPtr filePath, int pathStyle, bool isDirectory);

  [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
  private static extern unsafe void CFNotificationCenterAddObserver(
    IntPtr center, IntPtr observer,
    delegate* unmanaged<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, void> callBack,
    IntPtr name, IntPtr obj, int suspensionBehavior);

  // Display identity
  [DllImport("/System/Library/Frameworks/ColorSync.framework/ColorSync")]
  private static extern IntPtr CGDisplayCreateUUIDFromDisplayID(uint display);

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

  [DllImport("/System/Library/Frameworks/ImageIO.framework/ImageIO")]
  private static extern nuint CGImageSourceGetCount(IntPtr isrc);

  [StructLayout(LayoutKind.Sequential)]
  private struct CGRect
  {
    public double X, Y, Width, Height;

    public CGRect(double x, double y, double w, double h)
    {
      X = x; Y = y; Width = w; Height = h;
    }
  }

  // Used to read NSColor component values (double return via FP register on ARM64 / x86-64)
  [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
  private static extern double objc_msgSend_double(IntPtr receiver, IntPtr selector);

  // Used to create NSString instances from UTF-8 C strings
  [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
  private static extern IntPtr objc_msgSend_ptr_utf8(IntPtr receiver, IntPtr selector,
    [MarshalAs(UnmanagedType.LPUTF8Str)] string arg1);

  // AVFoundation — synchronous first-frame extraction for video wallpapers.
  // CMTime is a 24-byte struct; on ARM64/x86-64 it is passed by invisible pointer per platform ABI.
  [StructLayout(LayoutKind.Sequential)]
  private struct CMTime
  {
    public long  Value;     // int64_t
    public int   Timescale; // int32_t
    public uint  Flags;     // uint32_t (kCMTimeFlags_Valid = 1)
    public long  Epoch;     // int64_t
  }

  [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
  private static extern IntPtr objc_msgSend_cgimage_at_cmtime(
    IntPtr receiver, IntPtr selector, CMTime time, IntPtr actualTime, IntPtr error);

  // ─── Hook management ─────────────────────────────────────────────────────────

  private static readonly string WallpaperStorePath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    "Library", "Application Support", "com.apple.wallpaper", "Store", "Index.plist");

  private static readonly string SpacesPlistPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    "Library", "Preferences", "com.apple.spaces.plist");

  private static readonly string AerialsPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    "Library", "Application Support", "com.apple.wallpaper", "aerials");

  private static bool hooked;
  private static bool enabled;
  private static Application? callbackApp;  // retained for the wallpaper-change callback

  // ─── Diagnostics ─────────────────────────────────────────────────────────

  internal static string LastDiagnosticLog { get; private set; } = "";
  private static System.Text.StringBuilder? _diagBuilder;
  private static void LogBegin() => _diagBuilder = new System.Text.StringBuilder();
  private static void Log(string line) => _diagBuilder?.AppendLine(line);
  private static void LogEnd() => LastDiagnosticLog = _diagBuilder?.ToString() ?? "";

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

    // Subscribe to the distributed wallpaper-change notification so sampling runs
    // whenever the user changes the desktop background while the app is running.
    callbackApp = app;
    try
    {
      IntPtr center = CFNotificationCenterGetDistributedCenter();
      if (center != IntPtr.Zero)
      {
        // kCFStringEncodingUTF8 = 0x08000100
        IntPtr nameStr = CFStringCreateWithCString(IntPtr.Zero, "com.apple.desktop.backgroundChanged", 0x08000100);
        if (nameStr != IntPtr.Zero)
        {
          unsafe
          {
            // kCFNotificationSuspensionBehaviorDeliverImmediately = 4
            CFNotificationCenterAddObserver(center, IntPtr.Zero, &WallpaperChangedCallback,
              nameStr, IntPtr.Zero, 4);
          }

          CFRelease(nameStr);
        }
      }
    }
    catch { /* notification registration is best-effort */ }
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
    ClearResourceOverrides(app);
  }

  /// <summary>Removes the two runtime resource overrides without touching the <c>enabled</c> flag.</summary>
  private static void ClearResourceOverrides(Application app)
  {
    app.Resources.Remove("TextBoxBackgroundBrush");
    app.Resources.Remove(WallpaperDominantBrushKey);
  }

  /// <summary>Unmanaged callback for <c>com.apple.desktop.backgroundChanged</c> distributed notification.</summary>
  [System.Runtime.InteropServices.UnmanagedCallersOnly]
  private static void WallpaperChangedCallback(IntPtr center, IntPtr observer, IntPtr name, IntPtr obj, IntPtr info)
  {
    if (callbackApp is { } app && enabled)
      Dispatcher.UIThread.Post(() => Apply(app), DispatcherPriority.Background);
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

    LogBegin();
    if (TryGetWallpaperAverageColor(out Color wallpaperColor))
    {
      Log($"Result: dominant={wallpaperColor}");
      LogEnd();
      app.Resources[WallpaperDominantBrushKey] = new SolidColorBrush(wallpaperColor);
      Color tinted = ComputeTintedDarkBackground(wallpaperColor);
      app.Resources["TextBoxBackgroundBrush"] = new SolidColorBrush(tinted);
    }
    else
    {
      Log("Result: sampling failed — overrides cleared");
      LogEnd();
      ClearResourceOverrides(app);
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

      string? urlPath = wallpaperUrl != IntPtr.Zero
        ? GetNsStringUtf8(objc_msgSend_ptr(wallpaperUrl, sel_registerName("path")))
        : null;
      Log($"URL: {urlPath ?? "(nil)"}");

      bool isPlaceholderUrl = urlPath is null or "/System/Library/CoreServices/DefaultDesktop.heic";
      if (isPlaceholderUrl)
      {
        Log("Path: placeholder/nil URL → WallpaperStore");
        if (TryGetColorFromWallpaperStore(sharedWorkspace, mainScreen, out color))
          return true;

        if (wallpaperUrl == IntPtr.Zero)
        {
          Log("WallpaperStore failed → TryGetColorFromDesktopOptions");
          return TryGetColorFromDesktopOptions(sharedWorkspace, mainScreen, out color);
        }
      }

      // 2. Route known video extensions to AVFoundation.
      string? ext = GetUrlFileExtension(wallpaperUrl);
      Log($"Extension: {ext ?? "(none)"}");
      if (ext is "mov" or "mp4" or "m4v" or "avi" or "m2v")
      {
        Log("Path: video → TryGetColorFromVideoViaAVFoundation");
        return TryGetColorFromVideoViaAVFoundation(wallpaperUrl, out color);
      }

      Log("Path: image → ImageIO");
      return TryGetColorFromImageViaImageIo(wallpaperUrl, out color);
    }
    catch (Exception ex)
    {
      Log($"Exception: {ex.Message}");
      return false;
    }
  }

  /// <summary>Returns the lowercase file extension of a file:// NSURL, or null on failure.</summary>
  private static string? GetUrlFileExtension(IntPtr url)
  {
    try
    {
      IntPtr nsPath = objc_msgSend_ptr(url, sel_registerName("path"));
      if (nsPath == IntPtr.Zero) return null;
      IntPtr extObj = objc_msgSend_ptr(nsPath, sel_registerName("pathExtension"));
      if (extObj == IntPtr.Zero) return null;
      IntPtr utf8 = objc_msgSend_ptr(extObj, sel_registerName("UTF8String"));
      return utf8 != IntPtr.Zero ? Marshal.PtrToStringUTF8(utf8)?.ToLowerInvariant() : null;
    }
    catch { return null; }
  }

  /// <summary>Reads the UTF-8 string value of an NSString, or returns null.</summary>
  private static string? GetNsStringUtf8(IntPtr nsString)
  {
    try
    {
      if (nsString == IntPtr.Zero) return null;
      IntPtr utf8 = objc_msgSend_ptr(nsString, sel_registerName("UTF8String"));
      return utf8 != IntPtr.Zero ? Marshal.PtrToStringUTF8(utf8) : null;
    }
    catch { return null; }
  }

  private static bool TryGetColorFromWallpaperStore(IntPtr workspace, IntPtr screen, out Color color)
  {
    color = default;

    if (!File.Exists(WallpaperStorePath) || !File.Exists(SpacesPlistPath))
    {
      Log("WallpaperStore: required plist not found");
      return false;
    }

    if (!TryGetActiveMainSpaceUuid(out string activeSpaceUuid))
      return false;
    if (!TryGetDisplayUuid(screen, out string displayUuid))
      return false;

    Log($"WallpaperStore active space: {activeSpaceUuid}");
    Log($"WallpaperStore display: {displayUuid}");

    string[] configurationPaths =
    [
      $"Spaces.{activeSpaceUuid}.Displays.{displayUuid}.Desktop.Content.Choices.0.Configuration",
      $"Spaces.{activeSpaceUuid}.Default.Desktop.Content.Choices.0.Configuration",
      $"Displays.{displayUuid}.Desktop.Content.Choices.0.Configuration",
      "SystemDefault.Desktop.Content.Choices.0.Configuration"
    ];

    foreach (string configurationPath in configurationPaths)
    {
      if (!TryExtractEmbeddedPlistAsXml(configurationPath, out XDocument configDoc))
        continue;

      Log($"WallpaperStore config path: {configurationPath}");
      if (TryGetColorFromWallpaperConfiguration(configDoc, workspace, screen, out color))
        return true;
    }

    Log("WallpaperStore: no usable configuration matched");
    return false;
  }

  private static bool TryGetColorFromWallpaperConfiguration(XDocument configDoc, IntPtr workspace, IntPtr screen, out Color color)
  {
    color = default;

    XElement? dict = configDoc.Root?.Element("dict");
    if (dict is null)
    {
      Log("WallpaperStore: decoded config missing root dict");
      return false;
    }

    string? type = GetPlistString(dict, "type");
    string? assetId = GetPlistString(dict, "assetID");
    Log($"WallpaperStore config type: {type ?? "(none)"}");
    if (!string.IsNullOrEmpty(assetId))
      Log($"WallpaperStore aerial asset: {assetId}");

    if (!string.IsNullOrEmpty(assetId))
    {
      string aerialThumbnailPath = Path.Combine(AerialsPath, "thumbnails", assetId + ".png");
      if (File.Exists(aerialThumbnailPath))
      {
        Log($"WallpaperStore aerial thumbnail: {aerialThumbnailPath}");
        return TryGetColorFromFilePath(aerialThumbnailPath, out color);
      }

      string videoPath = Path.Combine(AerialsPath, "videos", assetId + ".mov");
      if (File.Exists(videoPath))
      {
        Log($"WallpaperStore aerial video: {videoPath}");
        return TryGetColorFromFilePath(videoPath, out color);
      }

      Log("WallpaperStore aerial asset has no local thumbnail/video");
      return false;
    }

    if (type == "systemColor")
    {
      XElement? systemColorDict = GetPlistValue(dict, "systemColor");
      string? systemColorName = systemColorDict?.Elements("key").FirstOrDefault()?.Value;
      Log($"WallpaperStore system color: {systemColorName ?? "(unknown)"}");
      return TryGetColorFromDesktopOptions(workspace, screen, out color);
    }

    string? relativeUrl = GetPlistString(GetPlistValue(dict, "url"), "relative");
    if (string.IsNullOrEmpty(relativeUrl))
      return false;

    string filePath;
    try { filePath = Uri.UnescapeDataString(new Uri(relativeUrl).LocalPath); }
    catch (Exception ex)
    {
      Log($"WallpaperStore URL parse failed: {ex.Message}");
      return false;
    }

    if (type == "systemDesktopPicture" &&
        filePath.EndsWith(".madesktop", StringComparison.OrdinalIgnoreCase) &&
        TryGetMadeDesktopThumbnailPath(filePath, out string? thumbnailPath))
    {
      filePath = thumbnailPath!;
    }

    Log($"WallpaperStore file: {filePath}");
    return TryGetColorFromFilePath(filePath, out color);
  }

  private static bool TryGetColorFromFilePath(string filePath, out Color color)
  {
    color = default;
    if (!File.Exists(filePath))
    {
      Log("WallpaperStore file missing");
      return false;
    }

    IntPtr url = IntPtr.Zero;
    try
    {
      if (!TryCreateFileUrl(filePath, out url))
        return false;

      string extension = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant();
      Log($"WallpaperStore extension: {extension}");
      if (extension is "mov" or "mp4" or "m4v" or "avi" or "m2v")
      {
        Log("WallpaperStore sample path: video → AVFoundation");
        return TryGetColorFromVideoViaAVFoundation(url, out color);
      }

      Log("WallpaperStore sample path: image → ImageIO");
      return TryGetColorFromImageViaImageIo(url, out color);
    }
    finally
    {
      if (url != IntPtr.Zero)
        CFRelease(url);
    }
  }

  private static bool TryGetColorFromImageViaImageIo(IntPtr wallpaperUrl, out Color color)
  {
    color = default;
    IntPtr imageSource = CGImageSourceCreateWithURL(wallpaperUrl, IntPtr.Zero);
    Log($"ImageIO source: 0x{imageSource:X}");
    if (imageSource == IntPtr.Zero)
    {
      Log("ImageIO failed — no further fallback");
      return false;
    }

    try
    {
      nuint frameCount = CGImageSourceGetCount(imageSource);
      Log($"ImageIO frame count: {frameCount}");

      if (frameCount <= 1)
      {
        IntPtr cgImage = CGImageSourceCreateImageAtIndex(imageSource, 0, IntPtr.Zero);
        Log($"ImageIO cgImage[0]: 0x{cgImage:X}");
        if (cgImage != IntPtr.Zero)
        {
          try { return TryAverageCGImageColor(cgImage, out color); }
          finally { CFRelease(cgImage); }
        }
      }
      else
      {
        int totalR = 0, totalG = 0, totalB = 0, successCount = 0;
        for (nuint i = 0; i < frameCount; i++)
        {
          IntPtr cgFrame = CGImageSourceCreateImageAtIndex(imageSource, i, IntPtr.Zero);
          if (cgFrame == IntPtr.Zero) continue;
          try
          {
            if (TryAverageCGImageColor(cgFrame, out Color frameColor))
            {
              totalR += frameColor.R;
              totalG += frameColor.G;
              totalB += frameColor.B;
              successCount++;
            }
          }
          finally { CFRelease(cgFrame); }
        }

        Log($"ImageIO dynamic: {successCount}/{frameCount} frames sampled");
        if (successCount > 0)
        {
          color = Color.FromArgb(255,
            (byte)(totalR / successCount),
            (byte)(totalG / successCount),
            (byte)(totalB / successCount));
          return true;
        }
      }
    }
    finally { CFRelease(imageSource); }

    Log("ImageIO failed — no further fallback");
    return false;
  }

  private static bool TryCreateFileUrl(string filePath, out IntPtr fileUrl)
  {
    fileUrl = IntPtr.Zero;
    IntPtr cfPath = IntPtr.Zero;
    try
    {
      cfPath = CFStringCreateWithCString(IntPtr.Zero, filePath, 0x08000100);
      if (cfPath == IntPtr.Zero) return false;

      fileUrl = CFURLCreateWithFileSystemPath(IntPtr.Zero, cfPath, 0, Directory.Exists(filePath));
      return fileUrl != IntPtr.Zero;
    }
    finally
    {
      if (cfPath != IntPtr.Zero)
        CFRelease(cfPath);
    }
  }

  private static bool TryGetActiveMainSpaceUuid(out string uuid)
  {
    uuid = string.Empty;
    return TryRunProcess(
      "/usr/libexec/PlistBuddy",
      ["-c", "Print SpacesDisplayConfiguration:Management\\ Data:Monitors:0:Current\\ Space:uuid", SpacesPlistPath],
      out uuid,
      trimOutput: true);
  }

  private static bool TryGetDisplayUuid(IntPtr screen, out string displayUuid)
  {
    displayUuid = string.Empty;
    IntPtr uuidRef = IntPtr.Zero;
    IntPtr uuidStringRef = IntPtr.Zero;
    try
    {
      if (screen == IntPtr.Zero)
      {
        Log("WallpaperStore: screen was null");
        return false;
      }

      uint displayId = objc_msgSend_uint(screen, sel_registerName("CGDirectDisplayID"));
      Log($"WallpaperStore CGDirectDisplayID: {displayId}");
      if (displayId == 0)
      {
        Log("WallpaperStore: CGDirectDisplayID was 0");
        return false;
      }

      uuidRef = CGDisplayCreateUUIDFromDisplayID(displayId);
      if (uuidRef == IntPtr.Zero)
      {
        Log("WallpaperStore: CGDisplayCreateUUIDFromDisplayID returned null");
        return false;
      }

      uuidStringRef = CFUUIDCreateString(IntPtr.Zero, uuidRef);
      displayUuid = GetNsStringUtf8(uuidStringRef) ?? string.Empty;
      return !string.IsNullOrWhiteSpace(displayUuid);
    }
    finally
    {
      if (uuidStringRef != IntPtr.Zero)
        CFRelease(uuidStringRef);
      if (uuidRef != IntPtr.Zero)
        CFRelease(uuidRef);
    }
  }

  private static bool TryExtractEmbeddedPlistAsXml(string keyPath, out XDocument document)
  {
    document = default!;
    if (!TryRunProcess(
          "/usr/bin/plutil",
          ["-extract", keyPath, "raw", "-o", "-", WallpaperStorePath],
          out string base64,
          trimOutput: true))
      return false;

    byte[] bytes;
    try { bytes = Convert.FromBase64String(base64); }
    catch (Exception ex)
    {
      Log($"WallpaperStore base64 decode failed: {ex.Message}");
      return false;
    }

    if (!TryRunProcess(
          "/usr/bin/plutil",
          ["-convert", "xml1", "-o", "-", "-"],
          out string xml,
          bytes,
          trimOutput: false))
      return false;

    try
    {
      document = XDocument.Parse(xml);
      return true;
    }
    catch (Exception ex)
    {
      Log($"WallpaperStore XML parse failed: {ex.Message}");
      return false;
    }
  }

  private static bool TryRunProcess(string fileName, string[] arguments, out string output, byte[]? stdinBytes = null, bool trimOutput = false)
  {
    output = string.Empty;
    try
    {
      using Process process = new();
      process.StartInfo.FileName = fileName;
      process.StartInfo.UseShellExecute = false;
      process.StartInfo.CreateNoWindow = true;
      process.StartInfo.RedirectStandardOutput = true;
      process.StartInfo.RedirectStandardError = true;
      process.StartInfo.RedirectStandardInput = stdinBytes is not null;
      foreach (string argument in arguments)
        process.StartInfo.ArgumentList.Add(argument);

      process.Start();
      if (stdinBytes is not null)
      {
        process.StandardInput.BaseStream.Write(stdinBytes, 0, stdinBytes.Length);
        process.StandardInput.Close();
      }

      string stdout = process.StandardOutput.ReadToEnd();
      string stderr = process.StandardError.ReadToEnd();
      process.WaitForExit();
      if (process.ExitCode != 0)
      {
        Log($"{Path.GetFileName(fileName)} exit {process.ExitCode}: {stderr.Trim()}");
        return false;
      }

      output = trimOutput ? stdout.Trim() : stdout;
      return true;
    }
    catch (Exception ex)
    {
      Log($"{Path.GetFileName(fileName)} exception: {ex.Message}");
      return false;
    }
  }

  private static bool TryGetMadeDesktopThumbnailPath(string madeDesktopPath, out string? thumbnailPath)
  {
    thumbnailPath = null;
    try
    {
      XDocument doc = XDocument.Load(madeDesktopPath);
      XElement? dict = doc.Root?.Element("dict");
      thumbnailPath = GetPlistString(dict, "thumbnailPath");
      return !string.IsNullOrWhiteSpace(thumbnailPath);
    }
    catch (Exception ex)
    {
      Log($"WallpaperStore .madesktop parse failed: {ex.Message}");
      return false;
    }
  }

  private static XElement? GetPlistValue(XElement? dict, string keyName)
  {
    if (dict is null) return null;

    XElement? key = dict.Elements("key").FirstOrDefault(element => element.Value == keyName);
    return key?.ElementsAfterSelf().FirstOrDefault();
  }

  private static string? GetPlistString(XElement? dict, string keyName)
  {
    return GetPlistValue(dict, keyName)?.Value;
  }

  /// <summary>Scales <paramref name="cgImage"/> to 10×10 and returns the average colour.</summary>
  private static bool TryAverageCGImageColor(IntPtr cgImage, out Color color)
  {
    color = default;
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

  /// <summary>
  ///   Reads the desktop fill colour from <c>NSWorkspace.desktopImageOptionsForScreen:</c>.
  ///   Works for solid-colour desktops (nil URL) and macOS 26 where a non-nil URL is returned
  ///   even for solid colour backgrounds but the NSColor is still in the options dictionary.
  ///   The <c>NSWorkspaceDesktopImageFillColorKey</c> NSString is dereferenced from the AppKit
  ///   symbol table — using the C symbol name literally as a string would look up the wrong key.
  /// </summary>
  private static bool TryGetColorFromDesktopOptions(IntPtr workspace, IntPtr screen, out Color color)
  {
    color = default;
    try
    {
      IntPtr optionsDict = objc_msgSend_ptr_ptr(workspace,
        sel_registerName("desktopImageOptionsForScreen:"), screen);
      Log($"DesktopOptions dict: 0x{optionsDict:X}");
      if (optionsDict == IntPtr.Zero) return false;

      // Dereference the actual NSString constant from the AppKit symbol table.
      if (!NativeLibrary.TryGetExport(
            NativeLibrary.Load("/System/Library/Frameworks/AppKit.framework/AppKit"),
            "NSWorkspaceDesktopImageFillColorKey",
            out IntPtr symPtr))
      {
        Log("DesktopOptions: NSWorkspaceDesktopImageFillColorKey not found in AppKit");
        return false;
      }

      IntPtr key = Marshal.ReadIntPtr(symPtr);
      Log($"DesktopOptions key ptr: 0x{key:X}");
      if (key == IntPtr.Zero) return false;

      IntPtr fillColor = objc_msgSend_ptr_ptr(optionsDict,
        sel_registerName("objectForKey:"), key);
      Log($"DesktopOptions fillColor: 0x{fillColor:X}");
      if (fillColor == IntPtr.Zero) return false;

      // Convert to device RGB so component selectors return normalised [0,1] values.
      IntPtr nsColorSpaceClass = objc_getClass("NSColorSpace");
      if (nsColorSpaceClass == IntPtr.Zero) return false;
      IntPtr deviceRGB = objc_msgSend_ptr(nsColorSpaceClass,
        sel_registerName("deviceRGBColorSpace"));
      if (deviceRGB == IntPtr.Zero) return false;

      IntPtr rgbColor = objc_msgSend_ptr_ptr(fillColor,
        sel_registerName("colorUsingColorSpace:"), deviceRGB);
      Log($"DesktopOptions rgbColor: 0x{rgbColor:X}");
      if (rgbColor == IntPtr.Zero) return false;

      double r = objc_msgSend_double(rgbColor, sel_registerName("redComponent"));
      double g = objc_msgSend_double(rgbColor, sel_registerName("greenComponent"));
      double b = objc_msgSend_double(rgbColor, sel_registerName("blueComponent"));
      Log($"DesktopOptions raw RGB: r={r:F4} g={g:F4} b={b:F4}");

      // Sanity-check: component values must be in the valid normalised range.
      if (r is < 0 or > 1 || g is < 0 or > 1 || b is < 0 or > 1)
      {
        Log("DesktopOptions: range check failed — discarding");
        return false;
      }

      color = Color.FromArgb(255, ToByte(r), ToByte(g), ToByte(b));
      return true;
    }
    catch (Exception ex) { Log($"DesktopOptions exception: {ex.Message}"); return false; }
  }

  /// <summary>
  ///   Uses <c>AVAssetImageGenerator</c> to extract the first frame of a video wallpaper
  ///   and averages its pixels.  This is used instead of the deprecated
  ///   <c>QLThumbnailImageCreate</c> which returns a generic file-type icon rather than
  ///   actual video content.
  /// </summary>
  private static bool TryGetColorFromVideoViaAVFoundation(IntPtr wallpaperUrl, out Color color)
  {
    color = default;
    try
    {
      NativeLibrary.TryLoad("/System/Library/Frameworks/AVFoundation.framework/AVFoundation", out _);

      IntPtr avUrlAssetClass = objc_getClass("AVURLAsset");
      Log($"AV: AVURLAsset class 0x{avUrlAssetClass:X}");
      if (avUrlAssetClass == IntPtr.Zero) return false;

      // [AVURLAsset assetWithURL:wallpaperUrl]
      IntPtr asset = objc_msgSend_ptr_ptr(avUrlAssetClass,
        sel_registerName("assetWithURL:"), wallpaperUrl);
      Log($"AV: asset 0x{asset:X}");
      if (asset == IntPtr.Zero) return false;

      IntPtr generatorClass = objc_getClass("AVAssetImageGenerator");
      Log($"AV: generator class 0x{generatorClass:X}");
      if (generatorClass == IntPtr.Zero) return false;

      // [AVAssetImageGenerator assetImageGeneratorWithAsset:asset]
      IntPtr generator = objc_msgSend_ptr_ptr(generatorClass,
        sel_registerName("assetImageGeneratorWithAsset:"), asset);
      Log($"AV: generator instance 0x{generator:X}");
      if (generator == IntPtr.Zero) return false;

      // Extract the frame at time 0 synchronously.
      // kCMTimeFlags_Valid = 1; Epoch = 0 (normal timeline)
      CMTime zeroTime = new CMTime { Value = 0, Timescale = 1, Flags = 1, Epoch = 0 };
      IntPtr cgImage = objc_msgSend_cgimage_at_cmtime(generator,
        sel_registerName("copyCGImageAtTime:actualTime:error:"),
        zeroTime, IntPtr.Zero, IntPtr.Zero);
      Log($"AV: cgImage at t=0: 0x{cgImage:X}");
      if (cgImage == IntPtr.Zero) return false;

      try { return TryAverageCGImageColor(cgImage, out color); }
      finally { CFRelease(cgImage); }
    }
    catch (Exception ex) { Log($"AV exception: {ex.Message}"); return false; }
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
