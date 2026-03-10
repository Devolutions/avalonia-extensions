namespace Devolutions.AvaloniaTheme.Linux.Behaviors;

using System;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

public static class ScrollBarShortBehavior
{
  private const double MinThumbSize = 48;
  private static readonly ConditionalWeakTable<ScrollBar, IDisposable> Subscriptions = new();

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
          if (Subscriptions.TryGetValue(scrollBar, out _)) return;

          var subscription = scrollBar.GetObservable(Visual.BoundsProperty).Subscribe(bounds =>
          {
            double relevantDimension = scrollBar.Orientation == Orientation.Vertical ? bounds.Height : bounds.Width;
            if (relevantDimension is 0.0) return;

            bool isShort = relevantDimension < MinThumbSize;

            ((IPseudoClasses)scrollBar.Classes).Set(":short", isShort);
          });

          Subscriptions.Add(scrollBar, subscription);
        }
        else
        {
          if (Subscriptions.TryGetValue(scrollBar, out var subscription))
          {
            subscription.Dispose();
            Subscriptions.Remove(scrollBar);
          }
          ((IPseudoClasses)scrollBar.Classes).Set(":short", false);
        }
      }
    });
  }

  public static void SetEnabled(ScrollBar element, bool value) => element.SetValue(EnabledProperty, value);

  public static bool GetEnabled(ScrollBar element) => element.GetValue(EnabledProperty);
}