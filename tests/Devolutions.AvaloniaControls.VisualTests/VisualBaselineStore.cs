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
    => GetTargetOS(themeName) == currentOS
      ? GetCanonicalPath(themeName, fileName)
      : GetLocalPath(themeName, fileName);

  public BaselineComparison Compare(string screenshotPath, string themeName, string fileName, string diffPath)
  {
    string baselinePath = GetComparisonPath(themeName, fileName);
    bool missingBaseline = !File.Exists(baselinePath);
    bool matches = !missingBaseline && ImageComparer.CompareImages(baselinePath, screenshotPath, diffPath);
    return new BaselineComparison(baselinePath, missingBaseline, matches);
  }

  public void InitializeLocal(string screenshotPath, string themeName, string fileName)
  {
    if (GetTargetOS(themeName) != currentOS)
    {
      Copy(screenshotPath, GetLocalPath(themeName, fileName));
    }
  }

  public void Update(string screenshotPath, string themeName, string fileName)
  {
    Copy(screenshotPath, GetComparisonPath(themeName, fileName));
  }

  private static void Copy(string sourcePath, string destinationPath)
  {
    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
    File.Copy(sourcePath, destinationPath, overwrite: true);
  }
}

internal sealed record BaselineComparison(string Path, bool MissingBaseline, bool Matches);
