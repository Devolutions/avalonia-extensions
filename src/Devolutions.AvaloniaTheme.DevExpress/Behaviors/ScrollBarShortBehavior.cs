namespace Devolutions.AvaloniaTheme.DevExpress.Behaviors;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

public static class ScrollBarShortBehavior
{
  private const double MinThumbSize = 43;

  public static readonly AttachedProperty<bool> EnabledProperty =
    AvaloniaProperty.RegisterAttached<ScrollBar, bool>("Enabled", typeof(ScrollBarShortBehavior));

  static ScrollBarShortBehavior()
  {
    EnabledProperty.Changed.Subscribe(args =>
    {
      if (args.Sender is ScrollBar scrollBar)
      {
        if (args.NewValue.GetValueOrDefault<bool>())
        {
          scrollBar.GetObservable(Visual.BoundsProperty).Subscribe(bounds =>
          {
            if (bounds.Width == 0 && bounds.Height == 0) return;

            bool isShort = scrollBar.Orientation == Orientation.Vertical
              ? bounds.Height < MinThumbSize
              : bounds.Width < MinThumbSize;

            ((IPseudoClasses)scrollBar.Classes).Set(":short", isShort);
          });
        }
      }
    });
  }

  public static void SetEnabled(ScrollBar element, bool value) => element.SetValue(EnabledProperty, value);

  public static bool GetEnabled(ScrollBar element) => element.GetValue(EnabledProperty);
}