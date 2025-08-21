namespace Devolutions.AvaloniaTheme.MacOS.Converters;

using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Layout;

public static partial class MacOSConverters

{
  // public static FuncValueConverter<Panel, bool> IsMultiColumnListBox { get; } =
  //   new(static panel => panel is WrapPanel { Orientation: Orientation.Vertical });
  public static FuncValueConverter<ListBox, bool> IsMultiColumnListBox { get; } =
    new(static listBox => listBox is ListBox { ItemsPanelRoot: WrapPanel { Orientation: Orientation.Vertical } });

  public static FuncValueConverter<ListBox, bool> IsNotMultiColumnListBox { get; } =
    new(static listBox => listBox is not ListBox { ItemsPanelRoot: WrapPanel });
}