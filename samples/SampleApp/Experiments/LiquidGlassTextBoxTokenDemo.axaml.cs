namespace SampleApp.Experiments;

using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

public partial class LiquidGlassTextBoxTokenDemo : UserControl
{
  private static bool IsThemeVariantProperty(AvaloniaProperty property) =>
    property.Name is "RequestedThemeVariant" or "ActualThemeVariant";

  public LiquidGlassTextBoxTokenDemo()
  {
    this.InitializeComponent();
    if (Application.Current?.PlatformSettings is { } platformSettings)
    {
      platformSettings.ColorValuesChanged += (_, _) => this.RefreshTokenColorInfo();
    }

    if (Application.Current is { } app)
    {
      app.PropertyChanged += (_, e) =>
      {
        if (IsThemeVariantProperty(e.Property))
        {
          // Defer to Background priority so the token applier's deferred update (same priority,
          // registered earlier) runs first, and we read the already-updated resource value.
          Dispatcher.UIThread.Post(this.RefreshTokenColorInfo, DispatcherPriority.Background);
        }
      };
    }

    this.RefreshTokenColorInfo();
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
      if (Application.Current?.Resources[LiquidGlassTokenKeys.WallpaperDominantBrush] is ISolidColorBrush wallpaperBrush)
      {
        Color wallpaperColor = wallpaperBrush.Color;
        string wallpaperHex = $"#{wallpaperColor.A:X2}{wallpaperColor.R:X2}{wallpaperColor.G:X2}{wallpaperColor.B:X2}";
        if (wallpaperHexTextBox is not null)
        {
          wallpaperHexTextBox.Text = wallpaperHex;
        }

        wallpaperReadout.Text = $"{LiquidGlassTokenKeys.WallpaperDominantBrush}  (A={wallpaperColor.A}, R={wallpaperColor.R}, G={wallpaperColor.G}, B={wallpaperColor.B})";
      }
      else
      {
        if (wallpaperHexTextBox is not null)
        {
          wallpaperHexTextBox.Text = "Not available";
        }

        wallpaperReadout.Text = $"{LiquidGlassTokenKeys.WallpaperDominantBrush} is not available in Application resources.";
      }
    }

    if (Application.Current?.Resources[LiquidGlassTokenKeys.TextBoxBackgroundBrush] is ISolidColorBrush brush)
    {
      Color color = brush.Color;
      string tokenHex = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
      if (tokenHexTextBox is not null)
      {
        tokenHexTextBox.Text = tokenHex;
      }

      readout.Text = $"{LiquidGlassTokenKeys.TextBoxBackgroundBrush}  (A={color.A}, R={color.R}, G={color.G}, B={color.B})";
      return;
    }

    if (tokenHexTextBox is not null)
    {
      tokenHexTextBox.Text = "Not available";
    }

    readout.Text = $"{LiquidGlassTokenKeys.TextBoxBackgroundBrush} is not available in Application resources.";
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
