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
using SampleApp.ControlCatalog;

[Collection("VisualTests")]
public class VisualRegressionTests
{
  private const string TestResultsDirectory = "../../../Screenshots/Test";
  private static readonly string BaselinesDirectory = $"../../../Screenshots/Baseline/{GetCurrentOS()}";
  private static readonly string TestDiffsDirectory = $"../../../Screenshots/Test-Diffs/{DateTime.Now:yyyy-MM-dd__HH-mm}";
  private static readonly ControlThemeId[] SupportedThemes =
  [
    ControlThemeId.MacClassic,
    ControlThemeId.LiquidGlass,
    ControlThemeId.Linux,
    ControlThemeId.DevExpress,
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

  public static IEnumerable<object?[]> GetDemoPages()
  {
    ControlRegistry.EnsureValid();

    foreach (ControlCatalogEntry control in ControlRegistry.All.OrderBy(control => control.PageType.Name))
    {
      if (!control.PageType.Name.EndsWith("Demo", StringComparison.Ordinal))
      {
        continue;
      }

      foreach (ControlThemeId themeId in SupportedThemes)
      {
        if (!control.ShouldTest(themeId))
        {
          continue;
        }

        yield return [control.PageType, themeId.ToThemeName(), control.ViewModelType];
      }
    }
  }

  // TestPage() is called automatically by the xUnit Test Runner for each entry 
  //   returned by GetDemoPages() when you run the tests.
  [AvaloniaTheory]
  [MemberData(nameof(GetDemoPages))]
  public void TestPage(Type pageType, string themeName, Type? viewModelType)
  {
    string pageName = pageType.Name;
    // Ensure directories exist
    Directory.CreateDirectory(TestResultsDirectory);

    // 1. Set Theme FIRST (Before creating any UI controls)
    // Force theme reload to ensure fresh styles for every test
    App.CurrentTheme = null;

    Theme theme = themeName switch
    {
      "MacClassic" => new MacOsClassicTheme(),
      "LiquidGlass" => new MacOsLiquidGlassTheme(),
      "Linux" => new LinuxYaruTheme(),
      "DevExpress" => new DevExpressTheme(),
      _ => throw new ArgumentException($"Unknown theme: {themeName}")
    };
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
      Width = 1200,
      Height = 920,
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

      // 4. Test Light Mode
      CaptureAndCompare(window, pageName, themeName, "", ThemeVariant.Light);

      // 5. Test Dark Mode
      CaptureAndCompare(window, pageName, themeName, "_dark", ThemeVariant.Dark);
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
    if (Application.Current != null)
    {
      Application.Current.RequestedThemeVariant = variant;
    }

    // Wait for layout and theme application
    Dispatcher.UIThread.RunJobs();

    using WriteableBitmap bitmap = CaptureStableFrame(window, variant);

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
        Assert.Fail($"Visual regression detected for [{themeName}] {pageName} - {variant}. Diff saved to {Path.GetDirectoryName(diffPath)}");
      }
    }
    else
    {
      Assert.Fail($"No baseline found for [{themeName}] {pageName} - {variant}. Saved screenshot to {testPath}");
    }
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