namespace Devolutions.AvaloniaControls.VisualTests;

using SampleApp.PageCatalog;

internal sealed class VisualBaselineStore(string screenshotsDirectory, string currentOS)
{
  public static string GetTargetOS(string themeName) =>
    ThemeIds.Parse(themeName) switch
    {
      ThemeId.MacClassic or ThemeId.LiquidGlass => "macOS",
      ThemeId.DevExpress or ThemeId.WinUiClassic or ThemeId.WinUiMica => "Windows",
      ThemeId.Linux => "Linux",
      _ => throw new ArgumentException($"No canonical platform is defined for theme '{themeName}'.", nameof(themeName)),
    };

  public string GetLocalPath(string themeName, string fileName) =>
    Path.Combine(screenshotsDirectory, "LocalBaselines", themeName, fileName);

  public string GetCanonicalPath(string themeName, string fileName) =>
    Path.Combine(screenshotsDirectory, "Baseline", GetTargetOS(themeName), themeName, fileName);

  public string GetComparisonPath(string themeName, string fileName)
  {
    string localPath = GetLocalPath(themeName, fileName);
    return File.Exists(localPath)
      ? localPath
      : Path.Combine(screenshotsDirectory, "Baseline", currentOS, themeName, fileName);
  }

  public BaselineComparison Compare(string screenshotPath, string themeName, string fileName, string diffPath)
  {
    string baselinePath = GetComparisonPath(themeName, fileName);
    bool missingLocal = !File.Exists(GetLocalPath(themeName, fileName));
    bool missingBaseline = !File.Exists(baselinePath);
    bool matches = !missingBaseline && ImageComparer.CompareImages(baselinePath, screenshotPath, diffPath);
    return new BaselineComparison(baselinePath, missingLocal, missingBaseline, matches);
  }

  public void Update(string screenshotPath, string themeName, string fileName)
  {
    string targetOS = GetTargetOS(themeName);
    Copy(screenshotPath, GetLocalPath(themeName, fileName));

    // Until CI owns canonical generation, native-platform local updates publish both sets.
    if (currentOS == targetOS)
    {
      Copy(screenshotPath, GetCanonicalPath(themeName, fileName));
    }
  }

  private static void Copy(string sourcePath, string destinationPath)
  {
    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
    File.Copy(sourcePath, destinationPath, overwrite: true);
  }
}

internal sealed record BaselineComparison(string Path, bool MissingLocal, bool MissingBaseline, bool Matches);
