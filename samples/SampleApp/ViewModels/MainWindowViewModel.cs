namespace SampleApp.ViewModels;

using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

public record WallpaperItem(string Name, WallpaperOption Option)
{
  public override string ToString() => this.Name;
}

public enum WallpaperOption
{
  None,
  Psychodelic,
  CustomLight,
  CustomDark
}

public record ScaleOption(string Name, double Scale)
{
  public override string ToString() => this.Name;
}

public partial class MainWindowViewModel : ObservableObject
{
  [ObservableProperty]
  private Theme currentTheme;

  [ObservableProperty]
  private ScaleOption selectedScale;

  [ObservableProperty]
  private WallpaperItem selectedWallpaper;

  [ObservableProperty]
  private double systemScale;

  public MainWindowViewModel()
  {
    this.SelectedWallpaper = this.AvailableWallpapers[0];
    this.CurrentTheme = this.AvailableThemes.FirstOrDefault(t => Equals(t, App.CurrentTheme!))!;
    this.SelectedScale = this.AvailableScales[0]; // 0 = System Default, 9 = 300%
  }

  public WallpaperItem[] AvailableWallpapers { get; } =
  [
    new("None", WallpaperOption.None),
    new("Psychodelic", WallpaperOption.Psychodelic),
    new("Custom Light", WallpaperOption.CustomLight),
    new("Custom Dark", WallpaperOption.CustomDark)
  ];

  public ScaleOption[] AvailableScales { get; } =
  [
    new("Default", 0),
    new("100%", 1.0),
    new("125%", 1.25),
    new("150%", 1.5),
    new("175%", 1.75),
    new("200%", 2.0),
    new("225%", 2.25),
    new("250%", 2.5),
    new("275%", 2.75),
    new("300%", 3.0),
    new("400%", 4.0)
  ];

  public Theme[] AvailableThemes { get; } =
  [
    new LinuxYaruTheme(),
    new DevExpressTheme(),
    new MacOsTheme(),
    new MacOsClassicTheme(),
    new MacOsLiquidGlassTheme(),
    new FluentTheme(),
    new SimpleTheme()
  ];
}