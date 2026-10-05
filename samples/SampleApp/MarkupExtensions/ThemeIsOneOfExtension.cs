using System;
using Avalonia.Markup.Xaml;
using Devolutions.AvaloniaControls.Converters;
using SampleApp;

namespace SampleApp.MarkupExtensions;

/// <summary>
/// Markup extension that creates a binding to check if the current theme matches any of the specified theme names.
/// </summary>
/// <param name="themes">
/// A comma-separated list of theme names to check against the current theme.
/// WinUI also accepts the resolved WinUiClassic and WinUiMica variant names.
/// </param>
public class ThemeIsOneOfExtension : MarkupExtension
{
    public ThemeIsOneOfExtension(string themes)
    {
        this.Themes = themes;
    }

    public string Themes { get; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        bool matchesTheme = Equals(true, DevoConverters.IsOneOfConverter.Convert(
            App.EffectiveCurrentThemeName,
            typeof(bool),
            this.Themes,
            System.Globalization.CultureInfo.CurrentCulture));

        return matchesTheme || Equals(true, DevoConverters.IsOneOfConverter.Convert(
            App.EffectiveCatalogThemeName,
            typeof(bool),
            this.Themes,
            System.Globalization.CultureInfo.CurrentCulture));
    }
}
