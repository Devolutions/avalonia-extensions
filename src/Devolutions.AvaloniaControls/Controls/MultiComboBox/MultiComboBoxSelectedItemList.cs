// Vendored from Ursa.Avalonia, MIT licensed.
//   source:  https://github.com/irihitech/Ursa.Avalonia/blob/e72d50d5e41efa75424c3d2b39f4131a2293b155/src/Ursa/Controls/ComboBox/MultiComboBoxSelectedItemList.cs
//   commit:  e72d50d5e41efa75424c3d2b39f4131a2293b155  (shipped as Irihi.Ursa 2.0.1)
//
// We used four types out of Ursa's 491 and wrapped two of them in empty
// subclasses purely to re-expose them under our own namespace. Vendoring drops
// both the Irihi.Ursa and Irihi.Avalonia.Shared package references, removes those
// wrappers, and stops Ursa loading during theme construction at application start.
//
// Changes from upstream:
//   - namespace moved to Devolutions.AvaloniaControls.Controls
// See THIRD-PARTY-NOTICES.md for the full licence text.
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;

namespace Devolutions.AvaloniaControls.Controls;

public class MultiComboBoxSelectedItemList: ItemsControl
{
    public static readonly StyledProperty<ICommand?> RemoveCommandProperty = AvaloniaProperty.Register<MultiComboBoxSelectedItemList, ICommand?>(
        nameof(RemoveCommand));

    public ICommand? RemoveCommand
    {
        get => GetValue(RemoveCommandProperty);
        set => SetValue(RemoveCommandProperty, value);
    }
    
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        return NeedsContainer<ClosableTag>(item, out recycleKey);
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
    {
        return new ClosableTag();
    }

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        if (container is ClosableTag tag)
        {
            tag.Command = RemoveCommand;
            if (item is Layoutable visualContent)
            {
                var visualBrush = new VisualBrush
                {
                    Visual = visualContent,
                };
                var rectangle = new Rectangle()
                {
                    Fill = visualBrush,
                    Width = visualContent.Bounds.Width,
                    Height = visualContent.Bounds.Height,
                };
                tag.Content = rectangle;
                tag.DataContext = item;
            }
        }
    }
}