namespace Devolutions.AvaloniaTheme.MacOS.Converters;

using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Data.Converters;
using Avalonia.VisualTree;

public class LongestItemWidthConverter : IValueConverter
{
  public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    if (value is ListBox listBox)
    {
      // Console.WriteLine($"listBox.ItemsPanelRoot: {listBox.ItemsPanel}");

      // if (listBox.ItemsPanel is ItemsPanelTemplate template)
      // {
      //   Panel? probe = template.Build(); // off-tree probe
      //   if (probe is WrapPanel wp)
      //   {
      //     Console.WriteLine($"[Probe] WrapPanel (Orientation={wp.Orientation})");
      //     return wp.Orientation == Orientation.Vertical;
      //   }
      //
      //   // Console.WriteLine($"template.Content: {template.GetType()}");
      //   bool isWrap = value is ListBox { ItemsPanelRoot: WrapPanel };
      //   bool isVerticalWrap = value is ListBox { ItemsPanelRoot: WrapPanel { Orientation: Orientation.Vertical } };
      IEnumerable<Visual> visualDescendants = listBox.GetVisualDescendants().ToList();
      ItemsPresenter? presenter = visualDescendants.OfType<ItemsPresenter>().FirstOrDefault();

      if (presenter != null)
      {
        Console.WriteLine($"presenter width: {presenter?.DesiredSize.Width}");
        return presenter?.DesiredSize.Width;
      }
      // {
      //   Panel? probe = presenter.ItemsPanel.Build();
      //   if (probe != null)
      //   {
      //     Console.WriteLine($"probe width: {probe?.DesiredSize.Width}");
      //     return probe?.DesiredSize.Width;
      //   }
      // }
      // foreach (ILogical v in visualDescendants)
      // {
      //   Console.WriteLine($"-  {v.GetType().Name} ");
      // }

      // Console.WriteLine($"isWrap: {isWrap}");
      // Console.WriteLine($"isVerticalWrap: {isVerticalWrap}");
      // }
    }

    return 12;
  }

  public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
    throw new NotImplementedException();
}