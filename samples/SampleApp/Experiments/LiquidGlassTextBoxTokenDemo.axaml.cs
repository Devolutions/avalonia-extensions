namespace SampleApp.Experiments;

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

    if (Application.Current?.Resources[LiquidGlassTokenKeys.TextBoxBackgroundBrush] is ISolidColorBrush brush)
    {
      Color color = brush.Color;
      readout.Text = $"{LiquidGlassTokenKeys.TextBoxBackgroundBrush} = #{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}  (A={color.A}, R={color.R}, G={color.G}, B={color.B})";
      return;
    }

    readout.Text = $"{LiquidGlassTokenKeys.TextBoxBackgroundBrush} is not available in Application resources.";
  }
}
