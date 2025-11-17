namespace SampleApp;

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using ViewModels;

public partial class MainWindow : Window
{
  private bool suppressThemeChangeEvents;

  public MainWindow()
  {
    this.InitializeComponent();
    this.UpdatePreviewBackground();
#if DEBUG
    bool useAccelerate = Environment.GetEnvironmentVariable("USE_AVALONIA_ACCELERATE_TOOLS")?.ToLowerInvariant() == "true";

    if (useAccelerate)
    {
      // Enable Accelerate dev tools (AvaloniaUI.DiagnosticsSupport) - requiring a licence to use
      (Application.Current as App)?.AttachDevToolsOnce();
      // Enable original free dev tools (Avalonia.Diagnostics) as an additional option available on F10
      this.AttachDevTools(new KeyGesture(Key.F10));
    }
    else
    {
      // Enable original free dev tools (Avalonia.Diagnostics)
      this.AttachDevTools();
    }
#endif
  }

  public void SuppressThemeChangeEvents(bool suppress)
  {
    this.suppressThemeChangeEvents = suppress;
  }

  private void Themes_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
  {
    if (this.suppressThemeChangeEvents) return;

    SelectingItemsControl? cb = sender as SelectingItemsControl;
    if (cb?.SelectedItem is Theme newTheme) App.SetTheme(newTheme);
  }

  private void UpdatePreviewBackground()
  {
    MainWindowViewModel? vm = this.DataContext as MainWindowViewModel;
    if (vm == null) return;
    Panel? panel = this.FindControl<Panel>("PreviewWallpaper");
    if (panel == null) return;

    panel.Background = vm.ShowWallpaper ? this.GenerateTieDyeBrush() : Brushes.Transparent;
  }

  protected override void OnDataContextChanged(EventArgs e)
  {
    base.OnDataContextChanged(e);
    MainWindowViewModel? vm = this.DataContext as MainWindowViewModel;
    if (vm == null) return;

    this.UpdatePreviewBackground();
    vm.PropertyChanged += (s, args) =>
    {
      if (args.PropertyName == nameof(vm.ShowWallpaper))
      {
        this.UpdatePreviewBackground();
      }
    };
  }

  private IBrush GenerateTieDyeBrush() =>
    // Simple vibrant radial gradient for demo purposes
    new RadialGradientBrush
    {
      GradientOrigin = new RelativePoint(0.4, 0.4, RelativeUnit.Relative),
      Center = new RelativePoint(0.3, 0.3, RelativeUnit.Relative),
      RadiusX = new RelativeScalar(0.8, RelativeUnit.Relative),
      RadiusY = new RelativeScalar(0.8, RelativeUnit.Relative),
      GradientStops = new GradientStops
      {
        new GradientStop(Color.Parse("#FF00FF"), 0),
        new GradientStop(Color.Parse("#00FFFF"), 0.3),
        new GradientStop(Color.Parse("#FFFF00"), 0.6),
        new GradientStop(Color.Parse("#FF0000"), 1)
      }
    };
}