namespace Devolutions.AvaloniaControls.Behaviors;

using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;

public abstract class BaseBehavior<TSelf, TTarget> : IDisposable
    where TSelf : BaseBehavior<TSelf, TTarget>, new()
    where TTarget : Visual
{
    private static readonly ConditionalWeakTable<TTarget, TSelf> AttachedData = [];
    // ReSharper disable once StaticMemberInGenericType
    private static readonly Dictionary<AvaloniaProperty, bool> RegisteredProperties = [];

    protected TTarget Target { get; private set; } = null!;
    
    protected virtual void Init(TTarget target)
    {
        this.Target = target;
    }

    protected static void RegisterBehaviorProperty<T>(AttachedProperty<T> prop, Func<T, bool>? isEnabled = null)
    {
        RegisteredProperties.Add(prop, false);
        prop.Changed.Subscribe(args =>
        {
            if (args.Sender is TTarget target)
            {
                RegisteredProperties[prop] = isEnabled is not null
                    ? args.NewValue.HasValue && isEnabled(args.NewValue.Value)
                    : args.NewValue.HasValue && args.Priority != BindingPriority.Unset;
                if (RegisteredProperties.Values.Any(static v => v))
                {
                    EnableBehavior(target);
                }
                else
                {
                    DisableBehavior(target);
                }
            }
        });
    }

    protected virtual void OnLoaded() {}
    protected virtual void OnDataContextChanged() {}
    protected virtual void OnAttachedToVisualTree() {}
    protected virtual void OnDetachedFromVisualTree() {}

    private static void EnableBehavior(TTarget target)
    {
        // safety
        DisableBehavior(target);
        
        var behavior = new TSelf();
        behavior.Init(target);
        AttachedData.AddOrUpdate(target, behavior);

        target.DataContextChanged += OnDataContextChanged;

        if (target is Control control)
        {
            if (control.IsLoaded)
            {
                behavior.OnLoaded();
            }
            else
            {
                control.Loaded += OnLoaded;
            }
        }
        
        target.AttachedToVisualTree += OnAttachedToVisualTree;
        target.DetachedFromVisualTree += OnDetachedFromVisualTree;
    }

    private static void DisableBehavior(TTarget target)
    {
        if (AttachedData.TryGetValue(target, out TSelf? behavior))
        {
            behavior.Dispose();
            AttachedData.Remove(target);
        }

        target.DataContextChanged -= OnDataContextChanged;
        if (target is Control control)
        {
            control.Loaded -= OnLoaded;
        }

        target.DetachedFromVisualTree -= OnDetachedFromVisualTree;
    }

    private static void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is TTarget target && AttachedData.TryGetValue(target, out var behavior) && target is Control control)
        {
            control.Loaded -= OnLoaded;
            behavior.OnLoaded();
        }
    }
    
    private static void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is TTarget target)
        {
            if (!AttachedData.TryGetValue(target, out TSelf? behavior))
            {
                EnableBehavior(target);
                AttachedData.TryGetValue(target, out behavior);
            }
            
            behavior?.OnAttachedToVisualTree();
        }
    }

    private static void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is TTarget target)
        {
            DisableBehavior(target);
            if (AttachedData.TryGetValue(target, out TSelf? behavior))
            {
                behavior.OnDetachedFromVisualTree();
            }
        }
    }

    private static void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (sender is TTarget target && AttachedData.TryGetValue(target, out TSelf? behavior))
        {
            behavior.OnDataContextChanged();
        }
    }

    public abstract void Dispose();
}