namespace Devolutions.AvaloniaTheme.MacOS;

using System.ComponentModel;
using Avalonia.Styling;
using Internal;

/// <summary>
/// Includes Devolutions's MacOs theme in an application.
/// </summary>
public class DevolutionsMacOsTheme : Styles, ISupportInitialize
{
    private readonly IServiceProvider? sp;

    static DevolutionsMacOsTheme()
    {
        AvaloniaControls.Initialization.Initialize();
    }

    /// <summary> 
    /// Initializes a new instance of the <see cref="DevolutionsMacOsTheme"/> class.
    ///
    /// Global Styles will also be loaded by default, unless `GlobalStyles`
    /// is set to false (`<DevolutionsMacOsTheme GlobalStyles="False" />`)
    /// </summary>
    /// <param name="sp">The parent's service provider.</param>
    public DevolutionsMacOsTheme(IServiceProvider? sp = null)
    {
        this.sp = sp;
    }

    /// <summary>
    /// Control if global styles should be loaded
    /// </summary>
    public bool GlobalStyles { get; set; } = true;

    public void BeginInit() { }

    public void EndInit()
    {
        this.Add(this.GlobalStyles
            ? new MacOsThemeWithGlobalStyles(this.sp)
            : new MacOsTheme(this.sp));
    }
}