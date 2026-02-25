namespace Devolutions.AvaloniaTheme.DevExpress.Internal;

using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

internal class DevExpressThemeWithGlobalStyles : Styles
{
    public DevExpressThemeWithGlobalStyles(IServiceProvider? sp = null)
    {
        AvaloniaXamlLoader.Load(sp, this);

#if ENABLE_ACCELERATE
        if (DevExpressTheme.IsTreeDataGridAvailable())
        {
            Uri treeDataGridUri = new("avares://Devolutions.AvaloniaTheme.DevExpress/Controls/TreeDataGrid.axaml");
            this.Resources.MergedDictionaries.Add(new ResourceInclude(treeDataGridUri) { Source = treeDataGridUri });
        }
#endif

#if DEBUG
        Uri themePreviewerUri = new("avares://Devolutions.AvaloniaTheme.DevExpress/Design/ThemePreviewer.axaml");
        this.Resources.MergedDictionaries.Add(new ResourceInclude(themePreviewerUri) { Source = themePreviewerUri });
#endif
    }
}