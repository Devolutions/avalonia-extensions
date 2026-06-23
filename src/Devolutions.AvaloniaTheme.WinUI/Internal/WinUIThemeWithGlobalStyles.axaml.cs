namespace Devolutions.AvaloniaTheme.WinUI.Internal;

using System.Diagnostics.CodeAnalysis;
using global::Avalonia.Markup.Xaml;
using global::Avalonia.Markup.Xaml.Styling;
using global::Avalonia.Styling;

internal class WinUIThemeWithGlobalStyles : Styles
{
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Precompiled XAML is preserved via ILLink.Descriptors.xml TrimmerRootDescriptor.")]
    public WinUIThemeWithGlobalStyles(IServiceProvider? sp = null)
    {
        AvaloniaXamlLoader.Load(sp, this);

#if DEBUG
        Uri themePreviewerUri = new("avares://Devolutions.AvaloniaTheme.WinUI/Design/ThemePreviewer.axaml");
        this.Resources.MergedDictionaries.Add(new ResourceInclude(themePreviewerUri) { Source = themePreviewerUri });
#endif
    }
}
