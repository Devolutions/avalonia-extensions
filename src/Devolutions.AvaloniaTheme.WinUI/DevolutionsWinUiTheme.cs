namespace Devolutions.AvaloniaTheme.WinUI;

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using global::Avalonia.Markup.Xaml.Styling;
using global::Avalonia.Styling;
using Internal;

/// <summary>
/// Includes Devolutions's WinUI theme in an application.
/// </summary>
[RequiresUnreferencedCode("StyleInclude and ResourceInclude use AvaloniaXamlLoader.Load which dynamically loads referenced assembly with Avalonia resources. Note, StyleInclude and ResourceInclude defined in XAML are resolved compile time and are safe with trimming and AOT.")]
public class DevolutionsWinUiTheme : Styles, ISupportInitialize
{
    private readonly IServiceProvider? sp;

    static DevolutionsWinUiTheme()
    {
        global::Devolutions.AvaloniaControls.Initialization.Initialize();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DevolutionsWinUiTheme"/> class.
    ///
    /// Global styles will also be loaded by default, unless <c>GlobalStyles</c>
    /// is set to false (<c>&lt;DevolutionsWinUiTheme GlobalStyles="False" /&gt;</c>).
    /// </summary>
    /// <param name="sp">The parent's service provider.</param>
    public DevolutionsWinUiTheme(IServiceProvider? sp = null)
    {
        this.sp = sp;
    }

    /// <summary>
    /// Controls if global styles should be loaded.
    /// </summary>
    public bool GlobalStyles { get; set; } = true;

    public void BeginInit() { }

    public void EndInit()
    {
        // Conditionally apply the Windows 11 Mica surface overrides above the base theme.
        // This sits one level above WinUITheme/WinUIThemeWithGlobalStyles so it reliably
        // overrides the base ThemeResources (which are flattened into the theme's own
        // ThemeDictionaries via MergeResourceInclude and would otherwise take priority).
        if (Windows11MicaDetector.IsMicaSupported())
        {
            global::System.Uri micaUri = new("avares://Devolutions.AvaloniaTheme.WinUI/Accents/ThemeResources.Windows11.axaml");
            this.Resources.MergedDictionaries.Add(new ResourceInclude(micaUri) { Source = micaUri });
        }

        this.Add(this.GlobalStyles
            ? new WinUIThemeWithGlobalStyles(this.sp)
            : new WinUITheme(this.sp));
    }
}
