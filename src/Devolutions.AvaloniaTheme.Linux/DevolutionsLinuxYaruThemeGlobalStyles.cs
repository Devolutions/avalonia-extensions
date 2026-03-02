namespace Devolutions.AvaloniaTheme.Linux;

using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using AvaloniaControls.Helpers;

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
        
#if ENABLE_ACCELERATE
        if (AvaloniaAccelerateHelpers.IsTreeDataGridAvailable)
        {
            Uri treeDataGridStylesUri = new("avares://Devolutions.AvaloniaTheme.Linux/Controls/TreeDataGrid.styles.axaml");
            this.Add(new StyleInclude(treeDataGridStylesUri) { Source = treeDataGridStylesUri });
        }
#endif
    }
}
