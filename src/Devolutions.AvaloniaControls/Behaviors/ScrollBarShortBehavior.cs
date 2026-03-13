namespace Devolutions.AvaloniaControls.Behaviors;

using System;
using System.Linq;
using System.Reactive.Disposables;
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

          var boundsSubscription = scrollBar.GetObservable(Visual.BoundsProperty).Subscribe(bounds =>
          {
            ApplyShortClasses(scrollBar, bounds);
          });

          // Subscribe to TemplateApplied to handle the initial-load case where BoundsProperty
          // fires before the visual template is built. Once the template is applied, the Track
          // exists in the visual tree and we can invalidate it to pick up the correct
          // pseudo-class styles (MinHeight/MinWidth) right away, without waiting for a scroll event.
          EventHandler<TemplateAppliedEventArgs> templateHandler = (_, _) => InvalidateTrack(scrollBar);
          scrollBar.TemplateApplied += templateHandler;

          var subscription = new CompositeDisposable(
            boundsSubscription,
            Disposable.Create(() => scrollBar.TemplateApplied -= templateHandler));
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

  private static void ApplyShortClasses(ScrollBar scrollBar, Rect bounds)
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

    // ScrollBar template styles alter the Track's Thumb MinHeight/MinWidth on pseudo-class
    // change. Explicitly invalidate the Track so it re-measures with the updated style values,
    // since Avalonia does not always propagate pseudo-class-driven style changes into template
    // children automatically.
    if (changed)
    {
      InvalidateTrack(scrollBar);
    }
  }

  private static void InvalidateTrack(ScrollBar scrollBar)
  {
    var track = scrollBar.GetVisualDescendants().OfType<Track>().FirstOrDefault();
    track?.InvalidateMeasure();
  }

  public static void SetEnabled(ScrollBar element, bool value) => element.SetValue(EnabledProperty, value);

  public static bool GetEnabled(ScrollBar element) => element.GetValue(EnabledProperty);

  public static void SetShortScrollBarMaxSize(ScrollBar element, double value) => element.SetValue(ShortScrollBarMaxSizeProperty, value);

  public static double GetShortScrollBarMaxSize(ScrollBar element) => element.GetValue(ShortScrollBarMaxSizeProperty);
}
