namespace Devolutions.AvaloniaControls.VisualTests;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using SampleApp;
using SampleApp.PageCatalog;

[Collection("VisualTests")]
public class VisualRegressionTests
{
  private const double DefaultCaptureWidth = 1200;
  private const double MaxCaptureHeight = 3000;
  private const double CaptureHeightSlack = 1;
  private const double ResizeEpsilon = 0.5;
  private const string TestResultsDirectory = "../../../Screenshots/Test";
  private static readonly string BaselinesDirectory = $"../../../Screenshots/Baseline/{GetCurrentOS()}";
  private static readonly string TestDiffsDirectory = $"../../../Screenshots/Test-Diffs/{DateTime.Now:yyyy-MM-dd__HH-mm}";
  private static readonly ThemeId[] SupportedThemes =
  [
    ThemeId.MacClassic,
    ThemeId.LiquidGlass,
    ThemeId.Linux,
    ThemeId.DevExpress,
    ThemeId.WinUiClassic,
    ThemeId.WinUiMica,
  ];
  private static readonly TimeSpan CaptureStabilizationTimeout = TimeSpan.FromMilliseconds(250);
  private static readonly TimeSpan CaptureStabilizationInterval = TimeSpan.FromMilliseconds(16);

  private static string GetCurrentOS()
  {
    if (OperatingSystem.IsWindows())
    {
      return "Windows";
    }

    if (OperatingSystem.IsMacOS())
    {
      return "macOS";
    }

    if (OperatingSystem.IsLinux())
    {
      return "Linux";
    }

    return "Unknown";
  }

  public static IEnumerable<object?[]> GetTestPages()
  {
    PageRegistry.EnsureValid();

    foreach (PageCatalogEntry page in PageRegistry.All.OrderBy(page => page.PageType.Name))
    {
      foreach (ThemeId themeId in SupportedThemes)
      {
        if (!page.ShouldTest(themeId))
        {
          continue;
        }

        // For ↔️ ("same as reference") pages, pass the reference theme so TestPage asserts
        // pixel-identical rendering against it instead of comparing to stored baselines.
        string? sameAsThemeName = page.IsSameAsReference(themeId)
          ? themeId.GetSameAsReference()!.Value.ToThemeName()
          : null;
        yield return [page.PageType, themeId.ToThemeName(), page.ViewModelType, sameAsThemeName];
      }
    }
  }

  // TestPage() is called automatically by the xUnit Test Runner for each entry
  //   returned by GetTestPages() when you run the tests.
  [AvaloniaTheory]
  [MemberData(nameof(GetTestPages))]
  public void TestPage(Type pageType, string themeName, Type? viewModelType, string? sameAsThemeName)
  {
    string pageName = pageType.Name;
    // Ensure directories exist
    Directory.CreateDirectory(TestResultsDirectory);

    if (sameAsThemeName != null)
    {
      AssertRendersSameAs(pageType, viewModelType, pageName, themeName, sameAsThemeName);
      return;
    }

    RunThemeCapture(pageType, viewModelType, CreateTheme(themeName), window =>
    {
      CaptureAndCompare(window, pageName, themeName, "", ThemeVariant.Light);
      CaptureAndCompare(window, pageName, themeName, "_dark", ThemeVariant.Dark);
    });
  }

  private static Theme CreateTheme(string themeName) => themeName switch
  {
    "MacClassic" => new MacOsClassicTheme(),
    "LiquidGlass" => new MacOsLiquidGlassTheme(),
    "Linux" => new LinuxYaruTheme(),
    "DevExpress" => new DevExpressTheme(),
    "WinUiClassic" => new WinUiClassicTheme(),
    "WinUiMica" => new WinUiMicaTheme(),
    _ => throw new ArgumentException($"Unknown theme: {themeName}")
  };

  /// <summary>
  /// For ↔️ pages: renders the page in both the reference theme and <paramref name="themeName"/>
  /// (Light and Dark) and asserts the output is pixel-identical. No baselines are read or written
  /// for <paramref name="themeName"/>; the reference theme's own test case guards its baselines.
  /// </summary>
  [System.Diagnostics.StackTraceHidden]
  private static void AssertRendersSameAs(
    Type pageType,
    Type? viewModelType,
    string pageName,
    string themeName,
    string referenceThemeName)
  {
    List<string> mismatches = FindRenderMismatches(pageType, viewModelType, pageName, themeName, referenceThemeName, themeName);

    if (mismatches.Count > 0)
    {
      Assert.Fail(
        $"[{themeName}] {pageName} is marked ↔️ (same as {referenceThemeName}) in page-catalog.jsonc, but renders differently: " +
        $"{string.Join(", ", mismatches)}. If the difference is intended, give {themeName} its own status symbol and baselines.");
    }
  }

  // No catalog page uses ↔️ yet, so TestPage() doesn't reach AssertRendersSameAs(). These cases
  // exercise the same comparison directly, independent of catalog statuses: identical input must
  // match (no false positives from nondeterministic rendering), and visibly different themes must
  // be reported as a mismatch in both Light and Dark.
  [AvaloniaTheory]
  [InlineData("WinUiMica", "WinUiMica", false)]
  [InlineData("MacClassic", "DevExpress", true)]
  public void RenderEqualityCheck_DetectsMatchesAndMismatches(string themeName, string referenceThemeName, bool expectMismatch)
  {
    Type pageType = typeof(SampleApp.DemoPages.ButtonDemo);
    List<string> mismatches = FindRenderMismatches(
      pageType, null, pageType.Name, themeName, referenceThemeName, $"_RenderEqualityCheck/{themeName}-vs-{referenceThemeName}");

    if (expectMismatch)
    {
      Assert.Equal(2, mismatches.Count);
      Assert.Contains(mismatches, m => m.StartsWith(nameof(ThemeVariant.Light), StringComparison.Ordinal));
      Assert.Contains(mismatches, m => m.StartsWith(nameof(ThemeVariant.Dark), StringComparison.Ordinal));
    }
    else
    {
      Assert.Empty(mismatches);
    }
  }

  /// <summary>
  /// Renders the page in <paramref name="referenceThemeName"/> and <paramref name="themeName"/>
  /// (Light and Dark) and returns one entry per variant whose output isn't pixel-identical.
  /// Screenshots and diffs go under <paramref name="outputFolder"/>; no baselines are involved.
  /// </summary>
  [System.Diagnostics.StackTraceHidden]
  private static List<string> FindRenderMismatches(
    Type pageType,
    Type? viewModelType,
    string pageName,
    string themeName,
    string referenceThemeName,
    string outputFolder)
  {
    (string Suffix, ThemeVariant Variant)[] variants = [("", ThemeVariant.Light), ("_dark", ThemeVariant.Dark)];
    string testDirectory = Path.Combine(TestResultsDirectory, outputFolder);
    Directory.CreateDirectory(testDirectory);

    string ReferencePath(string suffix) => Path.Combine(testDirectory, $"{pageName}{suffix}__{referenceThemeName}-reference.png");
    string TestPath(string suffix) => Path.Combine(testDirectory, $"{pageName}{suffix}.png");

    RunThemeCapture(pageType, viewModelType, CreateTheme(referenceThemeName), window =>
    {
      foreach ((string suffix, ThemeVariant variant) in variants)
      {
        using WriteableBitmap bitmap = CaptureVariant(window, variant, out _);
        bitmap.Save(ReferencePath(suffix));
      }
    });

    var mismatches = new List<string>();
    RunThemeCapture(pageType, viewModelType, CreateTheme(themeName), window =>
    {
      foreach ((string suffix, ThemeVariant variant) in variants)
      {
        using WriteableBitmap bitmap = CaptureVariant(window, variant, out _);
        bitmap.Save(TestPath(suffix));

        string diffPath = Path.Combine(TestDiffsDirectory, outputFolder, $"{pageName}{suffix}_diff.png");
        if (!ImageComparer.CompareImages(ReferencePath(suffix), TestPath(suffix), diffPath))
        {
          mismatches.Add($"{variant} (diff saved to {Path.GetDirectoryName(diffPath)})");
        }
      }
    });

    return mismatches;
  }

  [System.Diagnostics.StackTraceHidden]
  private static void RunThemeCapture(Type pageType, Type? viewModelType, Theme theme, Action<Window> captureVariants)
  {
    // 1. Set Theme FIRST (Before creating any UI controls)
    // Force theme reload to ensure fresh styles for every test
    App.CurrentTheme = null;
    App.SetTheme(theme);

    // 2. Instantiate the page
    var content = (Control)Activator.CreateInstance(pageType)!;

    // Assign DataContext for pages that require it
    if (viewModelType != null)
    {
      content.DataContext = Activator.CreateInstance(viewModelType);
    }

    // 3. Create a window to host it
    var window = new Window
    {
      Width = DefaultCaptureWidth,
      Height = MaxCaptureHeight,
      Content = content
    };

    try
    {
      window.Show();

      // Wait for window to be ready
      Dispatcher.UIThread.RunJobs();

      // Simulate Tab key to focus the first control
      window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
      Dispatcher.UIThread.RunJobs();

      // 4. Capture Light / Dark variants
      captureVariants(window);
    }
    finally
    {
      // Always close and detach content to avoid leaking window state across tests.
      window.Close();
      window.Content = null;
      Dispatcher.UIThread.RunJobs();
    }
  }

  [System.Diagnostics.StackTraceHidden]
  private static void CaptureAndCompare(Window window, string pageName, string themeName, string suffix, ThemeVariant variant)
  {
    using WriteableBitmap bitmap = CaptureVariant(window, variant, out double? cappedDesiredHeight);

    // Save and Compare
    var fileName = $"{pageName}{suffix}.png";
    string baselinePath = Path.Combine(BaselinesDirectory, themeName, fileName);
    string testPath = Path.Combine(TestResultsDirectory, themeName, fileName);
    string diffPath = Path.Combine(TestDiffsDirectory, themeName, $"{pageName}{suffix}_diff.png");

    // Ensure subdirectories exist
    Directory.CreateDirectory(Path.GetDirectoryName(baselinePath)!);
    Directory.CreateDirectory(Path.GetDirectoryName(testPath)!);

    bitmap.Save(testPath);

    if (Environment.GetEnvironmentVariable("UPDATE_BASELINES") == "true")
    {
      File.Copy(testPath, baselinePath, true);
    }

    if (File.Exists(baselinePath))
    {
      bool passed = ImageComparer.CompareImages(baselinePath, testPath, diffPath);
      if (!passed)
      {
        string cappedHeightSuffix = cappedDesiredHeight.HasValue ? $" DesiredH={cappedDesiredHeight.Value}." : string.Empty;
        Assert.Fail($"Visual regression detected for [{themeName}] {pageName} - {variant}.{cappedHeightSuffix} Diff saved to {Path.GetDirectoryName(diffPath)}");
      }
    }
    else
    {
      string cappedHeightSuffix = cappedDesiredHeight.HasValue ? $" DesiredH={cappedDesiredHeight.Value}." : string.Empty;
      Assert.Fail($"No baseline found for [{themeName}] {pageName} - {variant}.{cappedHeightSuffix} Saved screenshot to {testPath}");
    }
  }

  private static WriteableBitmap CaptureVariant(Window window, ThemeVariant variant, out double? cappedDesiredHeight)
  {
    if (Application.Current != null)
    {
      Application.Current.RequestedThemeVariant = variant;
    }

    // Wait for layout and theme application
    Dispatcher.UIThread.RunJobs();
    cappedDesiredHeight = ResizeWindowForContentHeight(window);

    return CaptureStableFrame(window, variant);
  }

  private static double? ResizeWindowForContentHeight(Window window)
  {
    if (window.Content is not Control content)
    {
      throw new InvalidOperationException("Visual regression host window content must be a Control.");
    }

    content.Measure(new Size(DefaultCaptureWidth, double.PositiveInfinity));
    double desiredHeight = content.DesiredSize.Height;
    if (desiredHeight <= 0 || double.IsNaN(desiredHeight) || double.IsInfinity(desiredHeight))
    {
      throw new InvalidOperationException($"Unable to determine content height for {content.GetType().Name}.");
    }

    double unclampedHeight = Math.Ceiling(desiredHeight + CaptureHeightSlack);
    double targetHeight = Math.Min(unclampedHeight, MaxCaptureHeight);
    double? cappedDesiredHeight = unclampedHeight > MaxCaptureHeight ? unclampedHeight : null;

    if (Math.Abs(window.Height - targetHeight) <= ResizeEpsilon)
    {
      return cappedDesiredHeight;
    }

    window.Height = targetHeight;
    Dispatcher.UIThread.RunJobs();
    return cappedDesiredHeight;
  }

  private static WriteableBitmap CaptureStableFrame(Window window, ThemeVariant variant)
  {
    WriteableBitmap? previousFrame = window.CaptureRenderedFrame();
    if (previousFrame == null)
    {
      throw new Exception($"Failed to capture frame for {variant}");
    }

    byte[] previousPixels = CopyFramePixels(previousFrame);
    var stabilizationTimer = Stopwatch.StartNew();

    while (stabilizationTimer.Elapsed < CaptureStabilizationTimeout)
    {
      Thread.Sleep(CaptureStabilizationInterval);
      Dispatcher.UIThread.RunJobs();

      WriteableBitmap? currentFrame = window.CaptureRenderedFrame();
      if (currentFrame == null)
      {
        previousFrame.Dispose();
        throw new Exception($"Failed to capture frame for {variant}");
      }

      byte[] currentPixels = CopyFramePixels(currentFrame);
      if (previousPixels.AsSpan().SequenceEqual(currentPixels))
      {
        previousFrame.Dispose();
        return currentFrame;
      }

      previousFrame.Dispose();
      previousFrame = currentFrame;
      previousPixels = currentPixels;
    }

    return previousFrame;
  }

  private static byte[] CopyFramePixels(WriteableBitmap frame)
  {
    using ILockedFramebuffer lockedFramebuffer = frame.Lock();
    int byteCount = lockedFramebuffer.RowBytes * lockedFramebuffer.Size.Height;
    var buffer = new byte[byteCount];
    Marshal.Copy(lockedFramebuffer.Address, buffer, 0, byteCount);
    return buffer;
  }
}

internal static class TestInitializer
{
  [ModuleInitializer]
  internal static void Run()
  {
    EnsureAvaloniaLicenseKeyIsLoaded();

    if (Environment.GetEnvironmentVariable("UPDATE_BASELINES") == "true")
    {
      // xUnit v3 spawns the test process multiple times (discovery + execution).
      // Use a sentinel file to show the warning banner only once per 30-second window.
      string sentinelFile = Path.Combine(Path.GetTempPath(), "avalonia-update-baselines-warning.tmp");
      bool shouldShowBanner = ShouldShowUpdateBaselinesBanner(sentinelFile);

      if (shouldShowBanner)
      {
        TryMarkUpdateBaselinesBannerShown(sentinelFile);
        TextWriter stderr = Console.Error;
        stderr.WriteLine("\n\n" + new string('_', 80));
        stderr.WriteLine("\u001b[33m\u001b[1mWARNING: UPDATE_BASELINES environment variable is set to 'true'!\u001b[0m");
        stderr.WriteLine("Visual regression baselines will be updated.");
        stderr.WriteLine("If this was not intentional:");
        stderr.WriteLine("");
        stderr.WriteLine(" 🚨 \u001b[1mYou may abort with Ctrl+C.\u001b[0m  🚨 ");
        stderr.WriteLine("");
        stderr.WriteLine("Remove the variable from the current shell with:");
        stderr.WriteLine("`export UPDATE_BASELINES=false`");
        stderr.WriteLine(new string('_', 80) + "\n");
      }
    }
  }

  private static bool ShouldShowUpdateBaselinesBanner(string sentinelFile)
  {
    try
    {
      if (File.Exists(sentinelFile))
      {
        var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(sentinelFile);
        return age.TotalSeconds > 30;
      }

      return true;
    }
    catch (IOException)
    {
      return true;
    }
    catch (UnauthorizedAccessException)
    {
      return true;
    }
    catch (NotSupportedException)
    {
      return true;
    }
  }

  private static void TryMarkUpdateBaselinesBannerShown(string sentinelFile)
  {
    try
    {
      File.WriteAllText(sentinelFile, "");
    }
    catch (IOException)
    {
    }
    catch (UnauthorizedAccessException)
    {
    }
    catch (NotSupportedException)
    {
    }
  }

  private static void EnsureAvaloniaLicenseKeyIsLoaded()
  {
    if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AVALONIA_LICENSE_KEY")))
    {
      return;
    }

    var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
    while (directory != null)
    {
      string dotEnvPath = Path.Combine(directory.FullName, ".env");
      if (File.Exists(dotEnvPath))
      {
        string? licenseLine = File.ReadLines(dotEnvPath)
          .Select(line => line.Trim())
          .FirstOrDefault(line => line.StartsWith("AVALONIA_LICENSE_KEY=", StringComparison.Ordinal));

        if (!string.IsNullOrWhiteSpace(licenseLine))
        {
          string licenseKey = licenseLine["AVALONIA_LICENSE_KEY=".Length..].Trim();
          if (!string.IsNullOrWhiteSpace(licenseKey))
          {
            Environment.SetEnvironmentVariable("AVALONIA_LICENSE_KEY", licenseKey);
          }
        }

        return;
      }

      directory = directory.Parent;
    }
  }
}