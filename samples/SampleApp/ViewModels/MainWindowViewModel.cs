namespace SampleApp.ViewModels;

using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

public partial class MainWindowViewModel : ObservableObject
{
  [ObservableProperty]
  private Theme currentTheme;

  [ObservableProperty]
  private bool showWallpaper = false;

  public MainWindowViewModel()
  {
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