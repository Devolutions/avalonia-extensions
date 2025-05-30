namespace SampleApp.ViewModels;

using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

public partial class MainWindowViewModel : ObservableObject
{
  [ObservableProperty] private Theme[] availableThemes =
  [
    new LinuxYaruTheme(),
    new DevExpressTheme(),
    new MacOsTheme()
  ];

  [ObservableProperty] private Theme currentTheme;

  public MainWindowViewModel()
  {
    this.CurrentTheme = this.AvailableThemes.FirstOrDefault(t => Equals(t, App.CurrentTheme!))!;
  }
}