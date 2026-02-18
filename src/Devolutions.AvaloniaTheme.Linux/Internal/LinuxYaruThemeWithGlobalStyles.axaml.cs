namespace Devolutions.AvaloniaTheme.Linux.Internal;

using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

internal class LinuxYaruThemeWithGlobalStyles : Styles
{
    public LinuxYaruThemeWithGlobalStyles(IServiceProvider? sp = null)
    {
        AvaloniaXamlLoader.Load(sp, this);
#if DEBUG
        Uri themePreviewerUri = new("avares://Devolutions.AvaloniaTheme.Linux/Design/ThemePreviewer.axaml");
        this.Resources.MergedDictionaries.Add(new ResourceInclude(themePreviewerUri) { Source = themePreviewerUri });
#endif
    }
}
