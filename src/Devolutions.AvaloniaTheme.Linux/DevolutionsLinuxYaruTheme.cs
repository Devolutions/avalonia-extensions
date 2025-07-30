namespace Devolutions.AvaloniaTheme.Linux;

using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

/// <summary>
/// Includes Devolutions's Linux theme, based on the GTK Yaru theme, in an application.
/// </summary>
public class DevolutionsLinuxYaruTheme : Styles
{
    /// <summary> 
    /// Initializes a new instance of the <see cref="DevolutionsLinuxYaruTheme"/> class.
    /// </summary>
    /// <param name="sp">The parent's service provider.</param>
    public DevolutionsLinuxYaruTheme(IServiceProvider? sp = null)
    {
        AvaloniaXamlLoader.Load(sp, this);
#if DEBUG
        Uri themePreviewerUri = new("avares://Devolutions.AvaloniaTheme.Linux/Design/ThemePreviewer.axaml");
        this.Resources.MergedDictionaries.Add(new ResourceInclude(themePreviewerUri) { Source = themePreviewerUri });
#endif
    }
}