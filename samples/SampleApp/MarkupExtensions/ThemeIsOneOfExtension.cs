using System;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Devolutions.AvaloniaControls.Converters;
using SampleApp;

namespace SampleApp.MarkupExtensions;

/// <summary>
/// Markup extension that creates a binding to check if the current theme matches any of the specified theme names.
/// </summary>
/// <param name="themes">
/// A comma-separated list of theme names to check against the current theme.
/// </param>
public class ThemeIsOneOfExtension : MarkupExtension
{
    public ThemeIsOneOfExtension(string themes)
    {
        Themes = themes;
    }

    public string Themes { get; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return new Binding
        {
            Source = App.EffectiveCurrentThemeName,
            Converter = DevoConverters.IsOneOfConverter,
            ConverterParameter = Themes
        };
    }
}
