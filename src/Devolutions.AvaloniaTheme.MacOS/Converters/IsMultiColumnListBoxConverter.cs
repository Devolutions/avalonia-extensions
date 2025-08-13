namespace Devolutions.AvaloniaTheme.MacOS.Converters;

using Avalonia.Controls;
using Avalonia.Data.Converters;

public static partial class MacOSConverters
{
  public static FuncValueConverter<ListBox, bool> IsMultiColumnListBox { get; } =
    new(static listBox => listBox is ListBox { ItemsPanelRoot: WrapPanel });

  public static FuncValueConverter<ListBox, bool> IsNotMultiColumnListBox { get; } =
    new(static listBox => listBox is not ListBox { ItemsPanelRoot: WrapPanel });
}