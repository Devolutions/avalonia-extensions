namespace Devolutions.AvaloniaControls.Behaviors;

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.VisualTree;

public static class ScrollBarShortBehavior
{
  private static readonly ConditionalWeakTable<ScrollBar, IDisposable> Subscriptions = [];

  public static readonly AttachedProperty<bool> EnabledProperty =
    AvaloniaProperty.RegisterAttached<ScrollBar, bool>("Enabled", typeof(ScrollBarShortBehavior));

  public static readonly AttachedProperty<double> ShortScrollBarMaxSizeProperty =
    AvaloniaProperty.RegisterAttached<ScrollBar, double>("ShortScrollBarMaxSize", typeof(ScrollBarShortBehavior), 43.0);

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

            double maxSize = scrollBar.GetValue(ShortScrollBarMaxSizeProperty);
            bool isVeryShort = relevantDimension < maxSize;
            bool isShort = relevantDimension < (maxSize * 2) && !isVeryShort;

            var classes = (IPseudoClasses)scrollBar.Classes;
            bool changed = classes.Contains(":veryshort") != isVeryShort || classes.Contains(":short") != isShort;
            classes.Set(":veryshort", isVeryShort);
            classes.Set(":short", isShort);
            
            if (changed)
            {
                // ScrollBar template styles dynamically alter the Track's Thumb MinHeight/MinWidth on pseudo-class change.
                // Avalonia often won't propagate this layout invalidation into the Track automatically without scrolling.
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    var track = scrollBar.GetVisualDescendants().OfType<Track>().FirstOrDefault();
                    if (track != null)
                    {
                        track.InvalidateMeasure();
                        track.InvalidateArrange();
                    }
                });
            }
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
          var classes = (IPseudoClasses)scrollBar.Classes;
          classes.Set(":short", false);
          classes.Set(":veryshort", false);
        }
      }
    });
  }

  public static void SetEnabled(ScrollBar element, bool value) => element.SetValue(EnabledProperty, value);

  public static bool GetEnabled(ScrollBar element) => element.GetValue(EnabledProperty);

  public static void SetShortScrollBarMaxSize(ScrollBar element, double value) => element.SetValue(ShortScrollBarMaxSizeProperty, value);

  public static double GetShortScrollBarMaxSize(ScrollBar element) => element.GetValue(ShortScrollBarMaxSizeProperty);
}
