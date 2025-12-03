namespace SampleApp;

using System;
using Avalonia.Markup.Xaml;

/// <summary>
/// SampleApp custom styles for AoT compatibility.
/// </summary>
public class SampleAppStyles : Avalonia.Styling.Styles
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SampleAppStyles"/> class.
    /// </summary>
    /// <param name="sp">The parent's service provider.</param>
    public SampleAppStyles(IServiceProvider? sp = null)
    {
        AvaloniaXamlLoader.Load(sp, this);
    }
}