namespace SampleApp;

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

public partial class MainWindow : Window
{
  private bool suppressThemeChangeEvents;

  public MainWindow()
  {
    this.InitializeComponent();
#if DEBUG
    bool useAccelerate = Environment.GetEnvironmentVariable("USE_AVALONIA_ACCELERATE_TOOLS")?.ToLowerInvariant() == "true";

    if (useAccelerate)
    {
      // Enable Accelerate dev tools (AvaloniaUI.DiagnosticsSupport) - requiring a licence to use
      (Application.Current as App)?.AttacheDevToolsOnce();
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
}