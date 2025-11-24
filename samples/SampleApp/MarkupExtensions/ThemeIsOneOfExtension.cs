using System;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Devolutions.AvaloniaControls.Converters;
using SampleApp;

namespace SampleApp.MarkupExtensions;

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
            Converter = new IsOneOfConverter(),
            ConverterParameter = Themes
        };
    }
}
