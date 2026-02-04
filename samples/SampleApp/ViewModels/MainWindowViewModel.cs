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

public partial class MainWindowViewModel : ObservableObject
{
  [ObservableProperty]
  private Theme currentTheme;

  [ObservableProperty]
  private WallpaperItem selectedWallpaper;

  public WallpaperItem[] AvailableWallpapers { get; } =
  [
    new("None", WallpaperOption.None),
    new("Psychodelic", WallpaperOption.Psychodelic),
    new("Custom Light", WallpaperOption.CustomLight),
    new("Custom Dark", WallpaperOption.CustomDark)
  ];

  public MainWindowViewModel()
  {
    this.SelectedWallpaper = this.AvailableWallpapers[0];
    this.CurrentTheme = this.AvailableThemes.FirstOrDefault(t => Equals(t, App.CurrentTheme!))!;
  }

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