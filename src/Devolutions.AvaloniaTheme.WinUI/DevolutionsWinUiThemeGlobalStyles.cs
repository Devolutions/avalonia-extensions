namespace Devolutions.AvaloniaTheme.WinUI;

using System.Diagnostics.CodeAnalysis;
using global::Avalonia.Markup.Xaml;
using global::Avalonia.Styling;

public class DevolutionsWinUiThemeGlobalStyles : Styles
{
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Precompiled XAML is preserved via ILLink.Descriptors.xml TrimmerRootDescriptor.")]
    public DevolutionsWinUiThemeGlobalStyles(IServiceProvider? sp = null)
    {
        AvaloniaXamlLoader.Load(sp, this);
    }
}
