namespace Devolutions.AvaloniaControls.VisualTests;

using SkiaSharp;

public sealed class VisualBaselineStoreTests : IDisposable
{
  private readonly string directory = Path.Combine(Path.GetTempPath(), $"visual-baselines-{Guid.NewGuid():N}");

  [Theory]
  [InlineData("DevExpress", "Windows")]
  [InlineData("WinUiClassic", "Windows")]
  [InlineData("WinUiMica", "Windows")]
  [InlineData("MacClassic", "macOS")]
  [InlineData("LiquidGlass", "macOS")]
  [InlineData("Linux", "Linux")]
  public void TargetPlatformMatchesTheme(string theme, string targetOS) =>
    Assert.Equal(targetOS, VisualBaselineStore.GetTargetOS(theme));

  [Theory]
  [InlineData("Unknown")]
  [InlineData("Fluent")]
  [InlineData("Simple")]
  public void UndefinedCanonicalPlatformFailsExplicitly(string theme) =>
    Assert.Throws<ArgumentException>(() => VisualBaselineStore.GetTargetOS(theme));

  [Theory]
  [InlineData("Windows")]
  [InlineData("macOS")]
  [InlineData("Linux")]
  public void UpdateWritesNativeCanonicalAndNonNativePersonalImages(string currentOS)
  {
    var store = new VisualBaselineStore(directory, currentOS);
    string source = WriteFile("actual.png", "new screenshot");

    foreach (string theme in new[] { "DevExpress", "WinUiClassic", "WinUiMica", "MacClassic", "LiquidGlass", "Linux" })
    {
      string canonical = store.GetCanonicalPath(theme, "page.png");
      Directory.CreateDirectory(Path.GetDirectoryName(canonical)!);
      File.WriteAllText(canonical, "existing canonical");

      store.Update(source, theme, "page.png");

      bool native = VisualBaselineStore.GetTargetOS(theme) == currentOS;
      Assert.Equal(!native, File.Exists(store.GetLocalPath(theme, "page.png")));
      Assert.Equal(
        VisualBaselineStore.GetTargetOS(theme) == currentOS ? "new screenshot" : "existing canonical",
        File.ReadAllText(canonical));
      Assert.Equal(native ? canonical : store.GetLocalPath(theme, "page.png"), store.GetComparisonPath(theme, "page.png"));
    }
  }

  [Theory]
  [InlineData("Windows")]
  [InlineData("macOS")]
  [InlineData("Linux")]
  public void ComparisonSourceDependsOnThemeTargetPlatform(string currentOS)
  {
    var store = new VisualBaselineStore(directory, currentOS);

    Assert.Equal(
      currentOS == "Windows" ? store.GetCanonicalPath("DevExpress", "page.png") : store.GetLocalPath("DevExpress", "page.png"),
      store.GetComparisonPath("DevExpress", "page.png"));
    Assert.Equal(
      currentOS == "macOS" ? store.GetCanonicalPath("LiquidGlass", "page.png") : store.GetLocalPath("LiquidGlass", "page.png"),
      store.GetComparisonPath("LiquidGlass", "page.png"));
    Assert.Equal(
      currentOS == "Linux" ? store.GetCanonicalPath("Linux", "page.png") : store.GetLocalPath("Linux", "page.png"),
      store.GetComparisonPath("Linux", "page.png"));
  }

  [Fact]
  public void ForeignUpdateDoesNotCreateCanonicalFiles()
  {
    var store = new VisualBaselineStore(directory, "macOS");
    string source = WriteFile("actual.png", "screenshot");

    store.Update(source, "WinUiMica", "page.png");

    Assert.True(File.Exists(store.GetLocalPath("WinUiMica", "page.png")));
    Assert.False(File.Exists(store.GetCanonicalPath("WinUiMica", "page.png")));
  }

  [Theory]
  [InlineData("DevExpress")]
  [InlineData("WinUiClassic")]
  [InlineData("WinUiMica")]
  [InlineData("MacClassic")]
  [InlineData("LiquidGlass")]
  [InlineData("Linux")]
  public void NativeUpdateCreatesOnlyCanonicalImagesForLightAndDark(string theme)
  {
    var store = new VisualBaselineStore(directory, VisualBaselineStore.GetTargetOS(theme));
    string source = WriteFile("actual.png", "screenshot");

    foreach (string fileName in new[] { "page.png", "page_dark.png" })
    {
      store.Update(source, theme, fileName);

      Assert.False(File.Exists(store.GetLocalPath(theme, fileName)));
      Assert.Equal("screenshot", File.ReadAllText(store.GetCanonicalPath(theme, fileName)));
    }
  }

  [Fact]
  public void LocalPathHasNoPlatformSubdirectory()
  {
    var store = new VisualBaselineStore(directory, "macOS");
    Assert.Equal(Path.Combine(directory, "LocalBaselines", "DevExpress", "page.png"), store.GetLocalPath("DevExpress", "page.png"));
  }

  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public void NativeThemeUsesTrackedImageWithoutRequiringPersonalImage(bool matches)
  {
    var store = new VisualBaselineStore(directory, "macOS");
    string actual = WriteImage("actual.png", SKColors.Blue);
    string tracked = WriteImage(Path.Combine("Baseline", "macOS", "MacClassic", "page.png"), matches ? SKColors.Blue : SKColors.Red);
    string diff = Path.Combine(directory, "diff.png");

    BaselineComparison comparison = store.Compare(actual, "MacClassic", "page.png", diff);

    Assert.Equal(tracked, comparison.Path);
    Assert.False(comparison.MissingBaseline);
    Assert.Equal(matches, comparison.Matches);
    Assert.Equal(!matches, File.Exists(diff));
  }

  [Theory]
  [InlineData("macOS", "WinUiMica")]
  [InlineData("Linux", "WinUiClassic")]
  [InlineData("Windows", "MacClassic")]
  public void ForeignCanonicalImageIsNeverCompared(string currentOS, string theme)
  {
    var store = new VisualBaselineStore(directory, currentOS);
    string actual = WriteImage("actual.png", SKColors.Blue);
    WriteImage(Path.GetRelativePath(directory, store.GetCanonicalPath(theme, "page.png")), SKColors.Red);
    string diff = Path.Combine(directory, "diff.png");

    BaselineComparison comparison = store.Compare(actual, theme, "page.png", diff);

    Assert.True(comparison.MissingBaseline);
    Assert.False(comparison.Matches);
    Assert.False(File.Exists(diff));
  }

  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public void NativeThemeIgnoresStalePersonalImage(bool matches)
  {
    var store = new VisualBaselineStore(directory, "macOS");
    string actual = WriteImage("actual.png", SKColors.Blue);
    string tracked = WriteImage(Path.Combine("Baseline", "macOS", "MacClassic", "page.png"), matches ? SKColors.Blue : SKColors.Red);
    WriteImage(Path.Combine("LocalBaselines", "MacClassic", "page.png"), SKColors.Blue);

    BaselineComparison comparison = store.Compare(actual, "MacClassic", "page.png", Path.Combine(directory, "diff.png"));

    Assert.Equal(tracked, comparison.Path);
    Assert.False(comparison.MissingBaseline);
    Assert.Equal(matches, comparison.Matches);
  }

  [Theory]
  [InlineData("Windows")]
  [InlineData("macOS")]
  [InlineData("Linux")]
  public void InitializationOverwritesNonNativeImagesWithoutChangingTrackedFiles(string currentOS)
  {
    var store = new VisualBaselineStore(directory, currentOS);
    string actual = WriteFile("actual.png", "initial screenshot");

    foreach (string theme in new[] { "DevExpress", "WinUiClassic", "WinUiMica", "MacClassic", "LiquidGlass", "Linux" })
    {
      string canonical = store.GetCanonicalPath(theme, "page.png");
      Directory.CreateDirectory(Path.GetDirectoryName(canonical)!);
      File.WriteAllText(canonical, "tracked");
      store.InitializeLocal(actual, theme, "page.png");
      bool native = VisualBaselineStore.GetTargetOS(theme) == currentOS;
      Assert.Equal(!native, File.Exists(store.GetLocalPath(theme, "page.png")));
      Assert.Equal("tracked", File.ReadAllText(canonical));

      File.WriteAllText(actual, "changed screenshot");
      store.InitializeLocal(actual, theme, "page.png");
      if (!native)
      {
        Assert.Equal("changed screenshot", File.ReadAllText(store.GetLocalPath(theme, "page.png")));
      }
      File.WriteAllText(actual, "initial screenshot");
    }
  }

  [Fact]
  public void InitializationDoesNotFillMissingNativeCanonicalImage()
  {
    var store = new VisualBaselineStore(directory, "macOS");
    string actual = WriteImage("actual.png", SKColors.Blue);
    store.InitializeLocal(actual, "MacClassic", "page.png");

    Assert.False(File.Exists(store.GetLocalPath("MacClassic", "page.png")));
    Assert.True(store.Compare(actual, "MacClassic", "page.png", Path.Combine(directory, "diff.png")).MissingBaseline);
  }

  private string WriteImage(string relativePath, SKColor color)
  {
    string path = Path.Combine(directory, relativePath);
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    using var bitmap = new SKBitmap(10, 10);
    using var canvas = new SKCanvas(bitmap);
    canvas.Clear(color);
    using var image = SKImage.FromBitmap(bitmap);
    using var data = image.Encode(SKEncodedImageFormat.Png, 100);
    using var stream = File.Create(path);
    data.SaveTo(stream);
    return path;
  }

  private string WriteFile(string name, string contents)
  {
    Directory.CreateDirectory(directory);
    string path = Path.Combine(directory, name);
    File.WriteAllText(path, contents);
    return path;
  }

  public void Dispose()
  {
    if (Directory.Exists(directory))
    {
      Directory.Delete(directory, recursive: true);
    }
  }
}
