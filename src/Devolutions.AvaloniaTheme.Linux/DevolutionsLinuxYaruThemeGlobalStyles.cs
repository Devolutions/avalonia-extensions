namespace Devolutions.AvaloniaTheme.Linux;

using Avalonia.Markup.Xaml;
using Avalonia.Styling;

/// <summary>
/// Include Devolution's Linux theme's optional global styles in an application.
/// </summary>
public class DevolutionsLinuxYaruThemeGlobalStyles : Styles
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DevolutionsLinuxYaruThemeGlobalStyles"/> class.
    /// </summary>
    /// <param name="sp">The parent's service provider.</param>
    public DevolutionsLinuxYaruThemeGlobalStyles(IServiceProvider? sp = null)
    {
        AvaloniaXamlLoader.Load(sp, this);
    }
}
