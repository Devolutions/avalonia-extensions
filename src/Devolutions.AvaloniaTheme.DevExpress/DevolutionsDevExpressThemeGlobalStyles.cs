namespace Devolutions.AvaloniaTheme.DevExpress;

using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

/// <summary>
/// Include Devolution's DevExpress theme's optional global styles in an application.
/// </summary>
public class DevolutionsDevExpressThemeGlobalStyles : Styles
{
    /// <summary> 
    /// Initializes a new instance of the <see cref="DevolutionsDevExpressThemeGlobalStyles"/> class.
    /// </summary>
    /// <param name="sp">The parent's service provider.</param>
    public DevolutionsDevExpressThemeGlobalStyles(IServiceProvider? sp = null)
    {
        AvaloniaXamlLoader.Load(sp, this);
        
#if ENABLE_ACCELERATE
        if (IsTreeDataGridAvailable())
        {
            Uri treeDataGridStylesUri = new("avares://Devolutions.AvaloniaTheme.DevExpress/Controls/TreeDataGrid.styles.axaml");
            this.Add(new StyleInclude(treeDataGridStylesUri) { Source = treeDataGridStylesUri });
        }
#endif
    }
    
    internal static bool IsTreeDataGridAvailable() =>
        Type.GetType("Avalonia.Controls.TreeDataGrid, Avalonia.Controls.TreeDataGrid") is not null;
}