namespace Devolutions.AvaloniaTheme.DevExpress;

using System.ComponentModel;
using Avalonia.Styling;
using Internal;

/// <summary>
/// Includes Devolutions's MacOs theme in an application.
/// </summary>
public class DevolutionsDevExpressTheme : Styles, ISupportInitialize
{
    private readonly IServiceProvider? sp;

    /// <summary> 
    /// Initializes a new instance of the <see cref="DevolutionsDevExpressTheme"/> class.
    ///
    /// Global Styles will also be loaded by default, unless `GlobalStyles`
    /// is set to false (`<DevolutionsDevExpressTheme GlobalStyles="False" />`)
    /// </summary>
    /// <param name="sp">The parent's service provider.</param>
    public DevolutionsDevExpressTheme(IServiceProvider? sp = null)
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
            ? new DevExpressThemeWithGlobalStyles(this.sp)
            : new DevExpressTheme(this.sp));
    }
}