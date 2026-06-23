namespace Devolutions.AvaloniaTheme.WinUI;

using System.ComponentModel;
using global::Avalonia.Styling;
using Internal;

/// <summary>
/// Includes Devolutions's WinUI theme in an application.
/// </summary>
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
        this.Add(this.GlobalStyles
            ? new WinUIThemeWithGlobalStyles(this.sp)
            : new WinUITheme(this.sp));
    }
}
