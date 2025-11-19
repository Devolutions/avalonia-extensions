namespace SampleApp;

using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using ViewModels;

public partial class MainWindow : Window
{
  private readonly IBrush tieDyeBrush;
  private MainWindowViewModel? currentViewModel;
  private bool suppressThemeChangeEvents;

  public MainWindow()
  {
    this.InitializeComponent();
    this.tieDyeBrush = this.GenerateTieDyeBrush();
    
    // Update preview background once the window is fully loaded
    this.Loaded += (s, e) => this.UpdatePreviewBackground();
    
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
    if (this.currentViewModel == null) return;
    Panel? panel = this.FindControl<Panel>("PreviewWallpaper");
    if (panel == null) return;

    panel.Background = this.currentViewModel.ShowWallpaper ? this.tieDyeBrush : Brushes.Transparent;
  }

  // When switching Themes, the ViewModel survives across window recreation, but the window itself is brand new.
  // That's why OnDataContextChanged fires - the new window is receiving the ViewModel for the first time,
  // even though the ViewModel already existed (and remembers things like wallpaper setting).
  protected override void OnDataContextChanged(EventArgs e)
  {
    base.OnDataContextChanged(e);

    MainWindowViewModel? vm = this.DataContext as MainWindowViewModel;
    
    // Only unsubscribe if we're switching to a different ViewModel
    if (this.currentViewModel != null && this.currentViewModel != vm)
    {
      this.currentViewModel.PropertyChanged -= this.OnViewModelPropertyChanged;
    }

    // Only subscribe if this is a new ViewModel
    if (vm != null && this.currentViewModel != vm)
    {
      vm.PropertyChanged += this.OnViewModelPropertyChanged;
    }
    
    this.currentViewModel = vm;
  }

  protected override void OnClosed(EventArgs e)
  {
    // Unsubscribe from ViewModel events to prevent memory leaks
    if (this.currentViewModel != null)
    {
      this.currentViewModel.PropertyChanged -= this.OnViewModelPropertyChanged;
      this.currentViewModel = null;
    }

    base.OnClosed(e);
  }

  private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
  {
    if (e.PropertyName == nameof(MainWindowViewModel.ShowWallpaper))
    {
      this.UpdatePreviewBackground();
    }
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