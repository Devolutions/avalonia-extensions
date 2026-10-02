namespace Devolutions.AvaloniaControls.VisualTests;

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
  public void UpdateWritesAllLocalImagesButOnlyNativeCanonicalImages(string currentOS)
  {
    var store = new VisualBaselineStore(directory, currentOS);
    string source = WriteFile("actual.png", "new screenshot");

    foreach (string theme in new[] { "DevExpress", "WinUiClassic", "WinUiMica", "MacClassic", "LiquidGlass", "Linux" })
    {
      string canonical = store.GetCanonicalPath(theme, "page.png");
      Directory.CreateDirectory(Path.GetDirectoryName(canonical)!);
      File.WriteAllText(canonical, "existing canonical");

      store.Update(source, theme, "page.png");

      Assert.Equal("new screenshot", File.ReadAllText(store.GetLocalPath(theme, "page.png")));
      Assert.Equal(
        VisualBaselineStore.GetTargetOS(theme) == currentOS ? "new screenshot" : "existing canonical",
        File.ReadAllText(canonical));
      Assert.Equal(store.GetLocalPath(theme, "page.png"), store.GetComparisonPath(theme, "page.png"));
    }
  }

  [Theory]
  [InlineData("Windows")]
  [InlineData("macOS")]
  [InlineData("Linux")]
  public void MissingLocalImageFallsBackToThemeTargetPlatform(string currentOS)
  {
    var store = new VisualBaselineStore(directory, currentOS);

    Assert.Equal(
      Path.Combine(directory, "Baseline", "Windows", "DevExpress", "page.png"),
      store.GetComparisonPath("DevExpress", "page.png"));
    Assert.Equal(
      Path.Combine(directory, "Baseline", "macOS", "LiquidGlass", "page.png"),
      store.GetComparisonPath("LiquidGlass", "page.png"));
    Assert.Equal(
      Path.Combine(directory, "Baseline", "Linux", "Linux", "page.png"),
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
  public void NativeUpdateCreatesBothBaselineTreesForLightAndDark(string theme)
  {
    var store = new VisualBaselineStore(directory, VisualBaselineStore.GetTargetOS(theme));
    string source = WriteFile("actual.png", "screenshot");

    foreach (string fileName in new[] { "page.png", "page_dark.png" })
    {
      store.Update(source, theme, fileName);

      Assert.Equal("screenshot", File.ReadAllText(store.GetLocalPath(theme, fileName)));
      Assert.Equal("screenshot", File.ReadAllText(store.GetCanonicalPath(theme, fileName)));
    }
  }

  [Fact]
  public void LocalBaselineFromAnotherOSIsNotUsed()
  {
    var windowsStore = new VisualBaselineStore(directory, "Windows");
    var macStore = new VisualBaselineStore(directory, "macOS");
    string source = WriteFile("actual.png", "screenshot");
    windowsStore.Update(source, "DevExpress", "page.png");

    Assert.Equal(macStore.GetCanonicalPath("DevExpress", "page.png"), macStore.GetComparisonPath("DevExpress", "page.png"));
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
