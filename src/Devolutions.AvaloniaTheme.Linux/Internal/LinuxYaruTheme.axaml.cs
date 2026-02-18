namespace Devolutions.AvaloniaTheme.Linux.Internal;

using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

internal class LinuxYaruTheme : Styles
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LinuxYaruTheme"/> class.
    /// </summary>
    /// <param name="sp">The parent's service provider.</param>
    public LinuxYaruTheme(IServiceProvider? sp = null)
    {
        AvaloniaXamlLoader.Load(sp, this);
#if DEBUG
        Uri themePreviewerUri = new("avares://Devolutions.AvaloniaTheme.Linux/Design/ThemePreviewer.axaml");
        this.Resources.MergedDictionaries.Add(new ResourceInclude(themePreviewerUri) { Source = themePreviewerUri });
#endif
    }
}
