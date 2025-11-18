namespace SampleApp.ViewModels;

using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

public partial class MainWindowViewModel : ObservableObject
{
  [ObservableProperty]
  private Theme[] availableThemes =
  [
    new LinuxYaruTheme(),
    new DevExpressTheme(),
    new MacOsTheme(),
    new MacOsClassicTheme(),
    new MacOsLiquidGlassTheme(),
    new FluentTheme(),
    new SimpleTheme()
  ];

  [ObservableProperty]
  private Theme currentTheme;

  [ObservableProperty]
  private bool showWallpaper = true;

  public MainWindowViewModel()
  {
    this.CurrentTheme = this.AvailableThemes.FirstOrDefault(t => Equals(t, App.CurrentTheme!))!;
  }
}