namespace Devolutions.AvaloniaTheme.DevExpress;

using System.Diagnostics.CodeAnalysis;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using AvaloniaControls.Helpers;

/// <summary>
/// Include Devolution's DevExpress theme's optional global styles in an application.
/// </summary>
public class DevolutionsDevExpressThemeGlobalStyles : Styles
{
    /// <summary> 
    /// Initializes a new instance of the <see cref="DevolutionsDevExpressThemeGlobalStyles"/> class.
    /// </summary>
    /// <param name="sp">The parent's service provider.</param>
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Precompiled XAML is preserved via ILLink.Descriptors.xml TrimmerRootDescriptor.")]
    public DevolutionsDevExpressThemeGlobalStyles(IServiceProvider? sp = null)
    {
        AvaloniaXamlLoader.Load(sp, this);
        
#if ENABLE_ACCELERATE
        if (AvaloniaAccelerateHelpers.IsTreeDataGridAvailable)
        {
            Uri treeDataGridStylesUri = new("avares://Devolutions.AvaloniaTheme.DevExpress/Controls/TreeDataGrid.styles.axaml");
            this.Add(new StyleInclude(treeDataGridStylesUri) { Source = treeDataGridStylesUri });
        }
#endif
    }
}