using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using System;

namespace SampleApp.FluentThemeAddOns;

public class DataGridFluentTheme : Styles
{
    public DataGridFluentTheme(IServiceProvider? sp = null)
    {
        AvaloniaXamlLoader.Load(sp, this);
    }
}
