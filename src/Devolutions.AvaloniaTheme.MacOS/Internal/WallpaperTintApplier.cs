namespace Devolutions.AvaloniaTheme.MacOS.Internal;

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
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
///     Solid-colour wallpapers are resolved via a sampled colour palette and NSColor class
///     selectors.  Video wallpapers are sampled via AVFoundation.  Image and system desktop
///     picture wallpapers are sampled via ImageIO.  When the wallpaper configuration cannot
///     be resolved (e.g. newly-applied animated wallpapers with no stored colour blob),
///     sampling returns <c>false</c> and callers fall back to the static theme resource.
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

  // Used for class-method existence check via +[NSColor respondsToSelector:].
  // BOOL is signed char on macOS; UnmanagedType.I1 reads the low byte correctly on ARM64/x86-64.
  [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
  [return: MarshalAs(UnmanagedType.I1)]
  private static extern bool objc_msgSend_bool_ptr(IntPtr receiver, IntPtr selector, IntPtr arg1);

  // CoreFoundation
  [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
  private static extern void CFRelease(IntPtr cf);

  [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
  private static extern IntPtr CFStringCreateWithCString(
    IntPtr alloc, [MarshalAs(UnmanagedType.LPUTF8Str)] string cStr, uint encoding);

  [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
  private static extern IntPtr CFUUIDCreateString(IntPtr alloc, IntPtr uuid);

  [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
  private static extern IntPtr CFURLCreateWithFileSystemPath(IntPtr allocator, IntPtr filePath, int pathStyle, bool isDirectory);

  // Display identity
  [DllImport("/System/Library/Frameworks/ColorSync.framework/ColorSync")]
  private static extern IntPtr CGDisplayCreateUUIDFromDisplayID(uint display);

  // CoreGraphics
  [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
  private static extern IntPtr CGColorSpaceCreateDeviceRGB();

  [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
  private static extern void CGColorSpaceRelease(IntPtr colorSpace);

  [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
  private static extern IntPtr CGBitmapContextCreate(IntPtr data, nuint width, nuint height, nuint bitsPerComponent, nuint bytesPerRow,
    IntPtr space, uint bitmapInfo);

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
      this.X = x;
      this.Y = y;
      this.Width = w;
      this.Height = h;
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
    public long Value; // int64_t
    public int Timescale; // int32_t
    public uint Flags; // uint32_t (kCMTimeFlags_Valid = 1)
    public long Epoch; // int64_t
  }

  [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
  private static extern IntPtr objc_msgSend_cgimage_at_cmtime(
    IntPtr receiver, IntPtr selector, CMTime time, IntPtr actualTime, IntPtr error);

  // ─── Hook management ─────────────────────────────────────────────────────────

  private static readonly string WallpaperStorePath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    "Library",
    "Application Support",
    "com.apple.wallpaper",
    "Store",
    "Index.plist");

  private static readonly string AerialsPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    "Library",
    "Application Support",
    "com.apple.wallpaper",
    "aerials");

  private readonly record struct WallpaperConfigurationCandidate(
    string ConfigurationPath,
    DateTime LastUse,
    DateTime LastSet,
    int Rank);

  private static readonly Dictionary<string, Color> NamedWallpaperColors = new(StringComparer.OrdinalIgnoreCase)
  {
     ["blueViolet"] = Color.FromArgb(255, 104, 103, 170),
     ["cyan"] = Color.FromArgb(255, 81, 172, 204),
     ["dustyRose"] = Color.FromArgb(255, 209, 116, 123),
     ["electricBlue"] = Color.FromArgb(255, 69, 83, 206),
     ["gold2"] = Color.FromArgb(255, 247, 220, 204),
     ["gold"] = Color.FromArgb(255, 240, 223, 203),
     ["ocher"] = Color.FromArgb(255, 205, 168, 99),
     ["plum"] = Color.FromArgb(255, 190, 84, 145),
     ["redOrange"] = Color.FromArgb(255, 216, 77, 51),
     ["roseGold"] = Color.FromArgb(255, 240, 212, 207),
     ["silver"] = Color.FromArgb(255, 227, 228, 229),
     ["softPink"] = Color.FromArgb(255, 247, 222, 229),
     ["spaceGrayPro"] = Color.FromArgb(255, 122, 123, 128),
     ["spaceGray"] = Color.FromArgb(255, 190, 191, 196),
     ["stone"] = Color.FromArgb(255, 84, 85, 84),
     ["teal"] = Color.FromArgb(255, 52, 119, 115),
     ["turquoiseGreen"] = Color.FromArgb(255, 128, 194, 164),
     ["yellow"] = Color.FromArgb(255, 243, 187, 68)
  };

  private static bool hooked;
  private static bool enabled;

  private static bool IsThemeVariantProperty(AvaloniaProperty property) =>
    property.Name is "RequestedThemeVariant" or "ActualThemeVariant";

  /// <summary>
  ///   Applies the wallpaper tint immediately and wires up event hooks so the colour
  ///   updates whenever the OS dark/light mode or application theme variant changes.
  ///   Safe to call multiple times; hooks are installed only once.
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
    ClearResourceOverrides(app);
  }

  /// <summary>Removes the two runtime resource overrides without touching the <c>enabled</c> flag.</summary>
  private static void ClearResourceOverrides(Application app)
  {
    app.Resources.Remove("TextBoxBackgroundBrush");
    app.Resources.Remove(WallpaperDominantBrushKey);
  }

  /// <summary>
  ///   Computes the wallpaper-tinted TextBox background colour and writes it to
  ///   <c>Application.Resources</c>.  Falls back to the static theme resource when the
  ///   wallpaper configuration cannot be resolved.
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
        sel_registerName("desktopImageURLForScreen:"),
        mainScreen);

      string? urlPath = wallpaperUrl != IntPtr.Zero
        ? GetNsStringUtf8(objc_msgSend_ptr(wallpaperUrl, sel_registerName("path")))
        : null;
      // DefaultDesktop.heic is the placeholder returned by animated/system wallpapers that have
      // no single backing file.  A nil URL means no wallpaper is set at all.
      // Both cases require the WallpaperStore path to resolve the actual colour.
      bool isPlaceholderUrl = urlPath is null or "/System/Library/CoreServices/DefaultDesktop.heic";
      if (isPlaceholderUrl)
      {
        if (TryGetColorFromWallpaperStore(sharedWorkspace, mainScreen, out color))
        {
          return true;
        }

        // Store also failed.  For a nil URL there is no image to fall back on, so try
        // desktopImageOptionsForScreen: which carries the fill colour for solid-colour desktops.
        if (wallpaperUrl == IntPtr.Zero)
        {
          return TryGetColorFromDesktopOptions(sharedWorkspace, mainScreen, out color);
        }
      }

      // Route known video extensions to AVFoundation.
      string? ext = GetUrlFileExtension(wallpaperUrl);
      if (ext is "mov" or "mp4" or "m4v" or "avi" or "m2v")
      {
        return TryGetColorFromVideoViaAVFoundation(wallpaperUrl, out color);
      }

      return TryGetColorFromImageViaImageIo(wallpaperUrl, out color);
    }
    catch
    {
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
    catch
    {
      return null;
    }
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
    catch
    {
      return null;
    }
  }

  private static bool TryGetColorFromWallpaperStore(IntPtr workspace, IntPtr screen, out Color color)
  {
    color = default;

    if (!File.Exists(WallpaperStorePath))
    {
      return false;
    }

    if (!TryGetDisplayUuid(screen, out string displayUuid))
    {
      return false;
    }

    if (!TryGetWallpaperConfigurationCandidates(displayUuid, out WallpaperConfigurationCandidate[] candidates))
    {
      candidates =
      [
        new WallpaperConfigurationCandidate($"Displays.{displayUuid}.Desktop.Content.Choices.0.Configuration",
          DateTime.MinValue,
          DateTime.MinValue,
          2),
        new WallpaperConfigurationCandidate("SystemDefault.Desktop.Content.Choices.0.Configuration", DateTime.MinValue, DateTime.MinValue, 3)
      ];
    }

    foreach (WallpaperConfigurationCandidate candidate in candidates)
    {
      if (!TryExtractEmbeddedPlistAsXml(candidate.ConfigurationPath, out XDocument configDoc))
      {
        // If a well-timestamped candidate has no configuration blob (e.g. Tahoe Day / newly-applied
        // animated wallpapers whose config hasn't been written yet), stop the waterfall rather than
        // falling through to a stale systemColor entry from a different space.
        if (candidate.LastUse > DateTime.MinValue)
        {
          break;
        }

        continue;
      }

      if (TryGetColorFromWallpaperConfiguration(configDoc, workspace, screen, out color))
      {
        return true;
      }
    }

    return false;
  }

  private static bool TryGetWallpaperConfigurationCandidates(string displayUuid, out WallpaperConfigurationCandidate[] candidates)
  {
    candidates = [];

    if (!TryRunProcess(
          "/usr/bin/plutil",
          ["-convert", "xml1", "-o", "-", WallpaperStorePath],
          out string wallpaperStoreXml,
          trimOutput: false))
    {
      return false;
    }

    XDocument wallpaperStoreDoc;
    try
    {
      wallpaperStoreDoc = XDocument.Parse(wallpaperStoreXml);
    }
    catch
    {
      return false;
    }

    XElement? rootDict = wallpaperStoreDoc.Root?.Element("dict");
    if (rootDict is null)
    {
      return false;
    }

    List<WallpaperConfigurationCandidate> results = [];
    HashSet<string> seenPaths = new(StringComparer.Ordinal);

    XElement? spacesDict = GetPlistValue(rootDict, "Spaces");
    if (spacesDict is not null)
    {
      foreach ((string spaceKey, XElement spaceValue) in EnumeratePlistDictionary(spacesDict))
      {
        if (string.IsNullOrEmpty(spaceKey) || spaceValue.Name != "dict")
        {
          continue;
        }

        XElement? displayOwners = GetPlistValue(spaceValue, "Displays");
        XElement? displayOwner = GetPlistValue(displayOwners, displayUuid);
        AddWallpaperConfigurationCandidate(results,
          seenPaths,
          displayOwner,
          $"Spaces.{spaceKey}.Displays.{displayUuid}.Desktop.Content.Choices.0.Configuration",
          0);

        XElement? defaultOwner = GetPlistValue(spaceValue, "Default");
        AddWallpaperConfigurationCandidate(results,
          seenPaths,
          defaultOwner,
          $"Spaces.{spaceKey}.Default.Desktop.Content.Choices.0.Configuration",
          1);
      }
    }

    XElement? displaysDict = GetPlistValue(rootDict, "Displays");
    XElement? rootDisplayOwner = GetPlistValue(displaysDict, displayUuid);
    AddWallpaperConfigurationCandidate(results,
      seenPaths,
      rootDisplayOwner,
      $"Displays.{displayUuid}.Desktop.Content.Choices.0.Configuration",
      2);

    XElement? systemDefaultOwner = GetPlistValue(rootDict, "SystemDefault");
    AddWallpaperConfigurationCandidate(results,
      seenPaths,
      systemDefaultOwner,
      "SystemDefault.Desktop.Content.Choices.0.Configuration",
      3);

    // Candidates are sorted newest-first so the entry that reflects the current wallpaper
    // is tried before any stale entries left behind from previous spaces or displays.
    candidates = results
      .OrderByDescending(candidate => candidate.LastUse)
      .ThenByDescending(candidate => candidate.LastSet)
      .ThenBy(candidate => candidate.Rank)
      .ToArray();

    return candidates.Length > 0;
  }

  private static void AddWallpaperConfigurationCandidate(
    List<WallpaperConfigurationCandidate> results,
    HashSet<string> seenPaths,
    XElement? owner,
    string configurationPath,
    int rank)
  {
    XElement? desktop = GetPlistValue(owner, "Desktop");
    if (desktop is null || !seenPaths.Add(configurationPath))
    {
      return;
    }

    results.Add(new WallpaperConfigurationCandidate(
      configurationPath,
      GetPlistDateTime(desktop, "LastUse"),
      GetPlistDateTime(desktop, "LastSet"),
      rank));
  }

  private static IEnumerable<(string Key, XElement Value)> EnumeratePlistDictionary(XElement? dict)
  {
    if (dict is null)
    {
      yield break;
    }

    XElement[] children = dict.Elements().ToArray();
    for (var i = 0; i < children.Length - 1; i++)
    {
      XElement key = children[i];
      if (key.Name != "key")
      {
        continue;
      }

      yield return (key.Value, children[i + 1]);
      i++;
    }
  }

  private static DateTime GetPlistDateTime(XElement? dict, string keyName)
  {
    string? value = GetPlistValue(dict, keyName)?.Value;
    return DateTime.TryParse(value, out DateTime parsed) ? parsed : DateTime.MinValue;
  }

  private static bool TryGetColorFromWallpaperConfiguration(XDocument configDoc, IntPtr workspace, IntPtr screen, out Color color)
  {
    color = default;

    XElement? dict = configDoc.Root?.Element("dict");
    if (dict is null)
    {
      return false;
    }

    string? type = GetPlistString(dict, "type");
    string? assetId = GetPlistString(dict, "assetID");

    if (!string.IsNullOrEmpty(assetId))
    {
      string aerialThumbnailPath = Path.Combine(AerialsPath, "thumbnails", assetId + ".png");
      if (File.Exists(aerialThumbnailPath))
      {
        return TryGetColorFromFilePath(aerialThumbnailPath, out color);
      }

      string videoPath = Path.Combine(AerialsPath, "videos", assetId + ".mov");
      if (File.Exists(videoPath))
      {
        return TryGetColorFromFilePath(videoPath, out color);
      }

      return false;
    }

    if (type == "systemColor")
    {
      XElement? systemColorDict = GetPlistValue(dict, "systemColor");
      string? systemColorName = systemColorDict?.Elements("key").FirstOrDefault()?.Value;

      if (!string.IsNullOrEmpty(systemColorName) &&
          TryGetColorFromNamedWallpaperColor(systemColorName, out color))
      {
        return true;
      }

      return TryGetColorFromDesktopOptions(workspace, screen, out color);
    }

    string? relativeUrl = GetPlistString(GetPlistValue(dict, "url"), "relative");
    if (string.IsNullOrEmpty(relativeUrl))
    {
      return false;
    }

    string filePath;
    try
    {
      filePath = Uri.UnescapeDataString(new Uri(relativeUrl).LocalPath);
    }
    catch
    {
      return false;
    }

    if (type == "systemDesktopPicture" &&
        filePath.EndsWith(".madesktop", StringComparison.OrdinalIgnoreCase) &&
        TryGetMadeDesktopThumbnailPath(filePath, out string? thumbnailPath))
    {
      filePath = thumbnailPath!;
    }

    return TryGetColorFromFilePath(filePath, out color);
  }

  private static bool TryGetColorFromFilePath(string filePath, out Color color)
  {
    color = default;
    if (!File.Exists(filePath))
    {
      return false;
    }

    IntPtr url = IntPtr.Zero;
    try
    {
      if (!TryCreateFileUrl(filePath, out url))
      {
        return false;
      }

      string extension = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant();
      if (extension is "mov" or "mp4" or "m4v" or "avi" or "m2v")
      {
        return TryGetColorFromVideoViaAVFoundation(url, out color);
      }

      return TryGetColorFromImageViaImageIo(url, out color);
    }
    finally
    {
      if (url != IntPtr.Zero)
      {
        CFRelease(url);
      }
    }
  }

  private static bool TryGetColorFromImageViaImageIo(IntPtr wallpaperUrl, out Color color)
  {
    color = default;
    IntPtr imageSource = CGImageSourceCreateWithURL(wallpaperUrl, IntPtr.Zero);
    if (imageSource == IntPtr.Zero)
    {
      return false;
    }

    try
    {
      nuint frameCount = CGImageSourceGetCount(imageSource);

      if (frameCount <= 1)
      {
        IntPtr cgImage = CGImageSourceCreateImageAtIndex(imageSource, 0, IntPtr.Zero);
        if (cgImage != IntPtr.Zero)
        {
          try
          {
            return TryAverageCGImageColor(cgImage, out color);
          }
          finally
          {
            CFRelease(cgImage);
          }
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
          finally
          {
            CFRelease(cgFrame);
          }
        }

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
    finally
    {
      CFRelease(imageSource);
    }

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
      {
        CFRelease(cfPath);
      }
    }
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
        return false;
      }

      uint displayId = objc_msgSend_uint(screen, sel_registerName("CGDirectDisplayID"));
      if (displayId == 0)
      {
        return false;
      }

      uuidRef = CGDisplayCreateUUIDFromDisplayID(displayId);
      if (uuidRef == IntPtr.Zero)
      {
        return false;
      }

      uuidStringRef = CFUUIDCreateString(IntPtr.Zero, uuidRef);
      displayUuid = GetNsStringUtf8(uuidStringRef) ?? string.Empty;
      return !string.IsNullOrWhiteSpace(displayUuid);
    }
    finally
    {
      if (uuidStringRef != IntPtr.Zero)
      {
        CFRelease(uuidStringRef);
      }

      if (uuidRef != IntPtr.Zero)
      {
        CFRelease(uuidRef);
      }
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
    {
      return false;
    }

    if (string.IsNullOrWhiteSpace(base64))
    {
      return false;
    }

    byte[] bytes;
    try
    {
      bytes = Convert.FromBase64String(base64);
    }
    catch
    {
      return false;
    }

    if (!TryRunProcess(
          "/usr/bin/plutil",
          ["-convert", "xml1", "-o", "-", "-"],
          out string xml,
          bytes,
          false))
    {
      return false;
    }

    try
    {
      document = XDocument.Parse(xml);
      return true;
    }
    catch
    {
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
      {
        process.StartInfo.ArgumentList.Add(argument);
      }

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
        return false;
      }

      output = trimOutput ? stdout.Trim() : stdout;
      return true;
    }
    catch
    {
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
    catch
    {
      return false;
    }
  }

  private static XElement? GetPlistValue(XElement? dict, string keyName)
  {
    if (dict is null) return null;

    XElement? key = dict.Elements("key").FirstOrDefault(element => element.Value == keyName);
    return key?.ElementsAfterSelf().FirstOrDefault();
  }

  private static string? GetPlistString(XElement? dict, string keyName) =>
    GetPlistValue(dict, keyName)?.Value;

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
      for (var i = 0; i < count; i++)
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
    finally
    {
      CGContextRelease(ctx);
    }
  }

  private static bool TryGetColorFromNamedWallpaperColor(string systemColorName, out Color color)
  {
    color = default;

    if (NamedWallpaperColors.TryGetValue(systemColorName, out color))
    {
      return true;
    }

    return TryGetColorFromNsColorSelector(BuildNsColorSelector(systemColorName), out color);
  }

  private static string BuildNsColorSelector(string systemColorName)
  {
    ReadOnlySpan<char> span = systemColorName.AsSpan().Trim();
    if (span.IsEmpty)
    {
      return "Color";
    }

    StringBuilder builder = new(span.Length + 5);
    var upperNext = false;
    var wroteAny = false;

    foreach (char ch in span)
    {
      if (char.IsLetterOrDigit(ch))
      {
        builder.Append(!wroteAny
          ? char.ToLowerInvariant(ch)
          : upperNext
            ? char.ToUpperInvariant(ch)
            : ch);
        wroteAny = true;
        upperNext = false;
      }
      else if (wroteAny)
      {
        upperNext = true;
      }
    }

    builder.Append("Color");
    return builder.ToString();
  }

  /// <summary>
  ///   Calls <c>+[NSColor &lt;selector&gt;]</c> only if the class responds to that selector and
  ///   converts the result to a plain sRGB <see cref="Color"/>.
  /// </summary>
  private static bool TryGetColorFromNsColorSelector(string selector, out Color color)
  {
    color = default;
    try
    {
      IntPtr nsColorClass = objc_getClass("NSColor");
      if (nsColorClass == IntPtr.Zero) return false;

      IntPtr sel = sel_registerName(selector);
      // Check before calling — unrecognized selectors raise NSInvalidArgumentException.
      if (!objc_msgSend_bool_ptr(nsColorClass, sel_registerName("respondsToSelector:"), sel))
      {
        return false;
      }

      IntPtr nsColor = objc_msgSend_ptr(nsColorClass, sel);
      if (nsColor == IntPtr.Zero) return false;

      // Convert to device RGB so component selectors work reliably.
      IntPtr nsColorSpaceClass = objc_getClass("NSColorSpace");
      if (nsColorSpaceClass == IntPtr.Zero) return false;
      IntPtr deviceRGB = objc_msgSend_ptr(nsColorSpaceClass, sel_registerName("deviceRGBColorSpace"));
      if (deviceRGB == IntPtr.Zero) return false;

      IntPtr rgbColor = objc_msgSend_ptr_ptr(nsColor, sel_registerName("colorUsingColorSpace:"), deviceRGB);
      if (rgbColor == IntPtr.Zero) return false;

      double r = objc_msgSend_double(rgbColor, sel_registerName("redComponent"));
      double g = objc_msgSend_double(rgbColor, sel_registerName("greenComponent"));
      double b = objc_msgSend_double(rgbColor, sel_registerName("blueComponent"));

      if (r is < 0 or > 1 || g is < 0 or > 1 || b is < 0 or > 1)
      {
        return false;
      }

      color = Color.FromArgb(255, ToByte(r), ToByte(g), ToByte(b));
      return true;
    }
    catch
    {
      return false;
    }
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
        sel_registerName("desktopImageOptionsForScreen:"),
        screen);
      if (optionsDict == IntPtr.Zero) return false;

      // Dereference the actual NSString constant from the AppKit symbol table.
      if (!NativeLibrary.TryGetExport(
            NativeLibrary.Load("/System/Library/Frameworks/AppKit.framework/AppKit"),
            "NSWorkspaceDesktopImageFillColorKey",
            out IntPtr symPtr))
      {
        return false;
      }

      IntPtr key = Marshal.ReadIntPtr(symPtr);
      if (key == IntPtr.Zero) return false;

      IntPtr fillColor = objc_msgSend_ptr_ptr(optionsDict,
        sel_registerName("objectForKey:"),
        key);
      if (fillColor == IntPtr.Zero) return false;

      // Convert to device RGB so component selectors return normalised [0,1] values.
      IntPtr nsColorSpaceClass = objc_getClass("NSColorSpace");
      if (nsColorSpaceClass == IntPtr.Zero) return false;
      IntPtr deviceRGB = objc_msgSend_ptr(nsColorSpaceClass,
        sel_registerName("deviceRGBColorSpace"));
      if (deviceRGB == IntPtr.Zero) return false;

      IntPtr rgbColor = objc_msgSend_ptr_ptr(fillColor,
        sel_registerName("colorUsingColorSpace:"),
        deviceRGB);
      if (rgbColor == IntPtr.Zero) return false;

      double r = objc_msgSend_double(rgbColor, sel_registerName("redComponent"));
      double g = objc_msgSend_double(rgbColor, sel_registerName("greenComponent"));
      double b = objc_msgSend_double(rgbColor, sel_registerName("blueComponent"));

      // Sanity-check: component values must be in the valid normalised range.
      if (r is < 0 or > 1 || g is < 0 or > 1 || b is < 0 or > 1)
      {
        return false;
      }

      color = Color.FromArgb(255, ToByte(r), ToByte(g), ToByte(b));
      return true;
    }
    catch
    {
      return false;
    }
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
      if (avUrlAssetClass == IntPtr.Zero) return false;

      // [AVURLAsset assetWithURL:wallpaperUrl]
      IntPtr asset = objc_msgSend_ptr_ptr(avUrlAssetClass,
        sel_registerName("assetWithURL:"),
        wallpaperUrl);
      if (asset == IntPtr.Zero) return false;

      IntPtr generatorClass = objc_getClass("AVAssetImageGenerator");
      if (generatorClass == IntPtr.Zero) return false;

      // [AVAssetImageGenerator assetImageGeneratorWithAsset:asset]
      IntPtr generator = objc_msgSend_ptr_ptr(generatorClass,
        sel_registerName("assetImageGeneratorWithAsset:"),
        asset);
      if (generator == IntPtr.Zero) return false;

      // Extract the frame at time 0 synchronously.
      // kCMTimeFlags_Valid = 1; Epoch = 0 (normal timeline)
      var zeroTime = new CMTime { Value = 0, Timescale = 1, Flags = 1, Epoch = 0 };
      IntPtr cgImage = objc_msgSend_cgimage_at_cmtime(generator,
        sel_registerName("copyCGImageAtTime:actualTime:error:"),
        zeroTime,
        IntPtr.Zero,
        IntPtr.Zero);
      if (cgImage == IntPtr.Zero) return false;

      try
      {
        return TryAverageCGImageColor(cgImage, out color);
      }
      finally
      {
        CFRelease(cgImage);
      }
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
    if (d < 1e-10)
    {
      h = 0;
      s = 0;
      return;
    }

    s = l > 0.5 ? d / (2.0 - max - min) : d / (max + min);
    if (max == r)
    {
      h = ((g - b) / d + (g < b ? 6.0 : 0.0)) / 6.0;
    }
    else if (max == g)
    {
      h = ((b - r) / d + 2.0) / 6.0;
    }
    else
    {
      h = ((r - g) / d + 4.0) / 6.0;
    }
  }

  private static void HslToRgb(double h, double s, double l, out double r, out double g, out double b)
  {
    if (s < 1e-10)
    {
      r = g = b = l;
      return;
    }

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
    if (t < 0.5) return q;
    if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6.0;
    return p;
  }

  private static byte ToByte(double value) =>
    (byte)Math.Round(Math.Clamp(value, 0.0, 1.0) * 255.0);
}