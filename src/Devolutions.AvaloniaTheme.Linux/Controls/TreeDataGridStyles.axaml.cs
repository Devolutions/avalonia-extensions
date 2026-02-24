using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace Devolutions.AvaloniaTheme.Linux.Controls;

public class TreeDataGridStyles : Styles
{
    public TreeDataGridStyles(IServiceProvider? sp = null)
    {
        AvaloniaXamlLoader.Load(sp, this);
    }
}
