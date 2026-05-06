namespace SampleApp.Experiments;

using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;

public partial class LiquidGlassWallpaperTintDemo : UserControl
{
  private const string TextBoxBackgroundBrushKey = "TextBoxBackgroundBrush";
  private const string WallpaperDominantBrushKey = "LG.Wallpaper.DominantBrush";

  private Application? subscribedApp;
  private IPlatformSettings? subscribedPlatformSettings;

  private static bool IsThemeVariantProperty(AvaloniaProperty property) =>
    property.Name is "RequestedThemeVariant" or "ActualThemeVariant";

  public LiquidGlassWallpaperTintDemo()
  {
    this.InitializeComponent();
  }

  protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
  {
    base.OnAttachedToVisualTree(e);

    if (Application.Current?.PlatformSettings is { } platformSettings)
    {
      this.subscribedPlatformSettings = platformSettings;
      platformSettings.ColorValuesChanged += this.OnPlatformColorValuesChanged;
    }

    if (Application.Current is { } app)
    {
      this.subscribedApp = app;
      app.PropertyChanged += this.OnApplicationPropertyChanged;
    }

    // Defer the initial read to Background priority so it runs after WallpaperTintApplier's
    // own deferred HookAndApply (same priority, registered first) has written the resources.
    Dispatcher.UIThread.Post(this.RefreshTokenColorInfo, DispatcherPriority.Background);
  }

  protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
  {
    base.OnDetachedFromVisualTree(e);

    if (this.subscribedPlatformSettings is not null)
    {
      this.subscribedPlatformSettings.ColorValuesChanged -= this.OnPlatformColorValuesChanged;
      this.subscribedPlatformSettings = null;
    }

    if (this.subscribedApp is not null)
    {
      this.subscribedApp.PropertyChanged -= this.OnApplicationPropertyChanged;
      this.subscribedApp = null;
    }
  }

  private void OnPlatformColorValuesChanged(object? sender, PlatformColorValues e) => this.RefreshTokenColorInfo();

  private void OnApplicationPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
  {
    if (IsThemeVariantProperty(e.Property))
    {
      // Defer to Background priority so the token applier's deferred update (same priority,
      // registered earlier) runs first, and we read the already-updated resource value.
      Dispatcher.UIThread.Post(this.RefreshTokenColorInfo, DispatcherPriority.Background);
    }
  }

  private void RefreshTokenColorInfo()
  {
    if (this.FindControl<TextBlock>("TokenColorInfoTextBlock") is not { } readout)
    {
      return;
    }

    TextBox? tokenHexTextBox = this.FindControl<TextBox>("TokenColorHexTextBox");
    TextBox? wallpaperHexTextBox = this.FindControl<TextBox>("WallpaperColorHexTextBox");

    if (this.FindControl<TextBlock>("WallpaperColorInfoTextBlock") is { } wallpaperReadout)
    {
      if (Application.Current?.Resources[WallpaperDominantBrushKey] is ISolidColorBrush wallpaperBrush)
      {
        Color wallpaperColor = wallpaperBrush.Color;
        string wallpaperHex = $"#{wallpaperColor.A:X2}{wallpaperColor.R:X2}{wallpaperColor.G:X2}{wallpaperColor.B:X2}";
        if (wallpaperHexTextBox is not null)
        {
          wallpaperHexTextBox.Text = wallpaperHex;
        }

        wallpaperReadout.Text = $"{WallpaperDominantBrushKey}  (A={wallpaperColor.A}, R={wallpaperColor.R}, G={wallpaperColor.G}, B={wallpaperColor.B})";
      }
      else
      {
        if (wallpaperHexTextBox is not null)
        {
          wallpaperHexTextBox.Text = "Not available";
        }

        wallpaperReadout.Text = $"{WallpaperDominantBrushKey} is not available in Application resources.";
      }
    }

    if (Application.Current?.Resources[TextBoxBackgroundBrushKey] is ISolidColorBrush brush)
    {
      Color color = brush.Color;
      string tokenHex = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
      if (tokenHexTextBox is not null)
      {
        tokenHexTextBox.Text = tokenHex;
      }

      readout.Text = $"{TextBoxBackgroundBrushKey}  (A={color.A}, R={color.R}, G={color.G}, B={color.B})";
      return;
    }

    if (tokenHexTextBox is not null)
    {
      tokenHexTextBox.Text = "Not available";
    }

    readout.Text = $"{TextBoxBackgroundBrushKey} is not available in Application resources.";
  }

  private async void CopyWallpaperColorHex(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
  {
    if (this.FindControl<TextBox>("WallpaperColorHexTextBox") is { Text: { Length: > 0 } text })
    {
      await this.CopyTextToClipboardAsync(text);
    }
  }

  private async void CopyTokenColorHex(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
  {
    if (this.FindControl<TextBox>("TokenColorHexTextBox") is { Text: { Length: > 0 } text })
    {
      await this.CopyTextToClipboardAsync(text);
    }
  }

  private async Task CopyTextToClipboardAsync(string text)
  {
    if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
    {
      await clipboard.SetTextAsync(text);
    }
  }
}
