namespace Devolutions.AvaloniaControls.MarkupExtensions;

using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Helpers;

public class ChangeColorOpacity(object color, object opacity) : MarkupExtension
{
  private static readonly FuncMultiValueConverter<object, object> Converter = new(static e =>
  {
    using var enumerator = e.GetEnumerator();
    enumerator.MoveNext();

    // Handle unset values gracefully (e.g., during theme switching)
    if (enumerator.Current == AvaloniaProperty.UnsetValue)
      return AvaloniaProperty.UnsetValue;

    var color = (Color)enumerator.Current!;
    enumerator.MoveNext();

    if (enumerator.Current == AvaloniaProperty.UnsetValue)
      return AvaloniaProperty.UnsetValue;

    var opacity = (double)enumerator.Current;

    return DoChangeColorOpacity(color, opacity);
  });

  protected readonly IBinding color = MarkupExtensionHelpers.GetBinding<Color>(color)!;

  protected readonly IBinding opacity = MarkupExtensionHelpers.GetBinding<double>(opacity)!;

  public override object ProvideValue(IServiceProvider serviceProvider) =>
    new MultiBinding
    {
      Bindings = [this.color, this.opacity],
      Converter = Converter,
    };

  private static Color DoChangeColorOpacity(Color color, double opacity) =>
    new(Math.Clamp((byte)(opacity * 255.0), (byte)0, (byte)255), color.R, color.G, color.B);
}