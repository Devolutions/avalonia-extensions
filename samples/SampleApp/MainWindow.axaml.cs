namespace SampleApp;

using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Controls;
using DemoPages;
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

    // Once the window is fully loaded, update background, detect scale, and size containers
    this.Loaded += (s, e) =>
    {
      this.UpdatePreviewBackground();
      this.DetectSystemScale();
      this.InitializeContainerSizes();
    };

#if ENABLE_ACCELERATE
    this.AddTreeDataGridTab();
#endif

#if DEBUG
    bool useAccelerateDevTools = Environment.GetEnvironmentVariable("USE_AVALONIA_ACCELERATE_TOOLS")?.ToLowerInvariant() == "true";

    if (useAccelerateDevTools)
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

  private void Scale_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
  {
    SelectingItemsControl? cb = sender as SelectingItemsControl;
    if (cb?.SelectedItem is not ScaleOption scaleOption) return;

    Border? container = this.FindControl<Border>("ScaledContainer");
    Canvas? wrapper = this.FindControl<Canvas>("ScaledWrapper");
    if (container == null || wrapper == null) return;
    
    // Use window's client area dimensions as base size
    double baseWidth = this.ClientSize.Width;
    double baseHeight = this.ClientSize.Height;

    if (scaleOption.Scale == 0) // System Default
    {
      container.RenderTransform = null;
      container.Width = baseWidth;
      container.Height = baseHeight;
      wrapper.Width = baseWidth;
      wrapper.Height = baseHeight;
    }
    else
    {
      // Calculate scale relative to system scale
      // e.g., if system is 200% and user selects 100%, scale = 100/200 = 0.5
      double relativeScale = scaleOption.Scale / this.RenderScaling;
      
      // Apply transform to container
      container.RenderTransform = new ScaleTransform(relativeScale, relativeScale);
      container.RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Relative);
      container.Width = baseWidth;
      container.Height = baseHeight;
      
      // Size the wrapper Canvas so ScrollViewer knows the scaled bounds
      wrapper.Width = baseWidth * relativeScale;
      wrapper.Height = baseHeight * relativeScale;
    }
  }

  private void InitializeContainerSizes()
  {
    Border? container = this.FindControl<Border>("ScaledContainer");
    Canvas? wrapper = this.FindControl<Canvas>("ScaledWrapper");
    if (container == null || wrapper == null) return;
    
    // Set initial sizes to match window
    double baseWidth = this.ClientSize.Width;
    double baseHeight = this.ClientSize.Height;
    
    container.Width = baseWidth;
    container.Height = baseHeight;
    wrapper.Width = baseWidth;
    wrapper.Height = baseHeight;
  }

  private void DetectSystemScale()
  {
    if (this.DataContext is not MainWindowViewModel vm) return;

    // RenderScaling reflects the actual system/display scaling
    vm.SystemScale = this.RenderScaling;
  }

  private void UpdatePreviewBackground()
  {
    if (this.currentViewModel == null) return;
    Panel? wallpaperPanel = this.FindControl<Panel>("PreviewWallpaper");
    Panel? contentPanel = this.FindControl<Panel>("MainContentPanel");
    if (wallpaperPanel == null || contentPanel == null) return;

    switch (this.currentViewModel.SelectedWallpaper.Option)
    {
      case WallpaperOption.Psychodelic:
        wallpaperPanel.Background = this.tieDyeBrush;
        contentPanel.SetValue(Panel.BackgroundProperty, AvaloniaProperty.UnsetValue);
        break;
      case WallpaperOption.CustomLight:
        wallpaperPanel.Background = Brushes.Transparent;
        if (this.TryGetResource("LiquidGlassCustomWallpaperLight", this.ActualThemeVariant, out object? lightResource) &&
            lightResource is IBrush lightBrush)
        {
          contentPanel.Background = lightBrush;
        }
        else if (Application.Current!.TryGetResource("LiquidGlassCustomWallpaperLight", this.ActualThemeVariant, out object? lightAppResource) &&
                 lightAppResource is IBrush lightAppBrush)
        {
          contentPanel.Background = lightAppBrush;
        }

        break;
      case WallpaperOption.CustomDark:
        wallpaperPanel.Background = Brushes.Transparent;
        if (this.TryGetResource("LiquidGlassCustomWallpaperDark", this.ActualThemeVariant, out object? darkResource) &&
            darkResource is IBrush darkBrush)
        {
          contentPanel.Background = darkBrush;
        }
        else if (Application.Current!.TryGetResource("LiquidGlassCustomWallpaperDark", this.ActualThemeVariant, out object? darkAppResource) &&
                 darkAppResource is IBrush darkAppBrush)
        {
          contentPanel.Background = darkAppBrush;
        }

        break;
      case WallpaperOption.None:
      default:
        wallpaperPanel.Background = Brushes.Transparent;
        contentPanel.SetValue(Panel.BackgroundProperty, AvaloniaProperty.UnsetValue);
        break;
    }
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
    if (e.PropertyName == nameof(MainWindowViewModel.SelectedWallpaper))
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

#if ENABLE_ACCELERATE
  private void AddTreeDataGridTab()
  {
    TabControl? tabControl = this.FindControl<TabControl>("MainTabControl");
    if (tabControl == null) return;

    TabItem tabItem = new();
    SampleItemHeader header = new()
    {
      Title = "TreeDataGrid (Accelerate)",
      ApplicableTo = "Fluent"
    };
    tabItem.Header = header;

    TreeDataGridDemo demo = new();
    demo.DataContext = new TreeDataGridViewModel();
    tabItem.Content = demo;

    // Insert before "TreeView" to keep alphabetical order
    int insertIndex = -1;
    for (int i = 0; i < tabControl.Items.Count; i++)
    {
      if (tabControl.Items[i] is TabItem ti && ti.Header is SampleItemHeader h && h.Title == "TreeView")
      {
        insertIndex = i;
        break;
      }
    }

    if (insertIndex != -1)
    {
      tabControl.Items.Insert(insertIndex, tabItem);
    }
    else
    {
      tabControl.Items.Add(tabItem);
    }
  }
#endif
}