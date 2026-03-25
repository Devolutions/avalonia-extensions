namespace Devolutions.AvaloniaTheme.Linux.Internal;

using System.Diagnostics.CodeAnalysis;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using AvaloniaControls.Helpers;

internal class LinuxYaruThemeWithGlobalStyles : Styles
{
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Precompiled XAML is preserved via ILLink.Descriptors.xml TrimmerRootDescriptor.")]
    public LinuxYaruThemeWithGlobalStyles(IServiceProvider? sp = null)
    {
        AvaloniaXamlLoader.Load(sp, this);

#if ENABLE_ACCELERATE
        if (AvaloniaAccelerateHelpers.IsTreeDataGridAvailable)
        {
            Uri treeDataGridUri = new("avares://Devolutions.AvaloniaTheme.Linux/Controls/TreeDataGrid.axaml");
            this.Resources.MergedDictionaries.Add(new ResourceInclude(treeDataGridUri) { Source = treeDataGridUri });
        }
#endif

#if DEBUG
        Uri themePreviewerUri = new("avares://Devolutions.AvaloniaTheme.Linux/Design/ThemePreviewer.axaml");
        this.Resources.MergedDictionaries.Add(new ResourceInclude(themePreviewerUri) { Source = themePreviewerUri });
#endif
    }
}
