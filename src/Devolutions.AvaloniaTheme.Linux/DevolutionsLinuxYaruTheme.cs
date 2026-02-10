namespace Devolutions.AvaloniaTheme.Linux;

using System.ComponentModel;
using Avalonia.Styling;
using Internal;

/// <summary>
/// Includes Devolutions's Linux theme, based on the GTK Yaru theme, in an application.
/// </summary>
public class DevolutionsLinuxYaruTheme : Styles, ISupportInitialize
{
    private readonly IServiceProvider? sp;

    static DevolutionsLinuxYaruTheme()
    {
        AvaloniaControls.Initialization.Initialize();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DevolutionsLinuxYaruTheme"/> class.
    ///
    /// Global Styles will also be loaded by default, unless `GlobalStyles`
    /// is set to false (`<DevolutionsLinuxYaruTheme GlobalStyles="False" />`)
    /// </summary>
    /// <param name="sp">The parent's service provider.</param>
    public DevolutionsLinuxYaruTheme(IServiceProvider? sp = null)
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
            ? new LinuxYaruThemeWithGlobalStyles(this.sp)
            : new LinuxYaruTheme(this.sp));
    }
}