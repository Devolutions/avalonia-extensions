namespace Devolutions.AvaloniaControls.Controls;

using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;

public class ToggleButtonGroup : ListBox
{
    public static readonly StyledProperty<double?> ItemsMinWidthProperty =
        AvaloniaProperty.Register<ToggleButtonGroup, double?>(nameof(ItemsMinWidth));

    public static readonly StyledProperty<ICommand?> ItemClickCommandProperty =
        AvaloniaProperty.Register<ToggleButtonGroup, ICommand?>(nameof(ItemClickCommand));

    public static readonly StyledProperty<bool> EqualItemsSizeProperty =
        AvaloniaProperty.Register<ToggleButtonGroup, bool>(nameof(EqualItemsSize));

    public static readonly StyledProperty<Thickness> ItemsPaddingProperty =
        AvaloniaProperty.Register<ToggleButtonGroup, Thickness>(nameof(ItemsPadding));

    public static readonly StyledProperty<Thickness> ItemsLeftContentPaddingProperty =
        AvaloniaProperty.Register<ToggleButtonGroup, Thickness>(nameof(ItemsLeftContentPadding));

    public static readonly StyledProperty<Thickness> ItemsRightContentPaddingProperty =
        AvaloniaProperty.Register<ToggleButtonGroup, Thickness>(nameof(ItemsRightContentPadding));

    private bool isUpdatingEqualSizes;

    static ToggleButtonGroup()
    {
        SelectionModeProperty.OverrideDefaultValue<ToggleButtonGroup>(SelectionMode.Single | SelectionMode.AlwaysSelected);
        EqualItemsSizeProperty.Changed.AddClassHandler<ToggleButtonGroup>((x, e) => x.OnEqualItemsSizeChanged(e));
    }

    public double? ItemsMinWidth
    {
        get => this.GetValue(ItemsMinWidthProperty);
        set => this.SetValue(ItemsMinWidthProperty, value);
    }

    public ICommand? ItemClickCommand
    {
        get => this.GetValue(ItemClickCommandProperty);
        set => this.SetValue(ItemClickCommandProperty, value);
    }

    public bool EqualItemsSize
    {
        get => this.GetValue(EqualItemsSizeProperty);
        set => this.SetValue(EqualItemsSizeProperty, value);
    }

    public Thickness ItemsPadding
    {
        get => this.GetValue(ItemsPaddingProperty);
        set => this.SetValue(ItemsPaddingProperty, value);
    }

    public Thickness ItemsLeftContentPadding
    {
        get => this.GetValue(ItemsLeftContentPaddingProperty);
        set => this.SetValue(ItemsLeftContentPaddingProperty, value);
    }

    public Thickness ItemsRightContentPadding
    {
        get => this.GetValue(ItemsRightContentPaddingProperty);
        set => this.SetValue(ItemsRightContentPaddingProperty, value);
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) =>
        new ToggleButtonGroupItem();

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        this.NeedsContainer<ToggleButtonGroupItem>(item, out recycleKey);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (this.EqualItemsSize)
        {
            this.UpdateEqualItemsSizes();
        }
    }

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);

        // Always subscribe to property changes, regardless of EqualItemsSize state
        // This ensures consistent subscription/unsubscription lifecycle
        if (container is ToggleButtonGroupItem toggleItem)
        {
            toggleItem.PropertyChanged += this.OnContainerPropertyChanged;
        }

        if (this.EqualItemsSize && !this.isUpdatingEqualSizes)
        {
            // Schedule size update after layout pass
            Dispatcher.UIThread.Post(this.UpdateEqualItemsSizes, DispatcherPriority.Loaded);
        }
    }

    protected override void ClearContainerForItemOverride(Control element)
    {
        base.ClearContainerForItemOverride(element);

        // Unsubscribe from property changes
        if (element is ToggleButtonGroupItem toggleItem)
        {
            toggleItem.PropertyChanged -= this.OnContainerPropertyChanged;
        }

        if (this.EqualItemsSize && !this.isUpdatingEqualSizes)
        {
            // Schedule size update after layout pass
            Dispatcher.UIThread.Post(this.UpdateEqualItemsSizes, DispatcherPriority.Loaded);
        }
    }

    private void OnContainerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (this.EqualItemsSize && !this.isUpdatingEqualSizes &&
            (e.Property == ContentControl.ContentProperty ||
             e.Property == DesiredSizeProperty))
        {
            Dispatcher.UIThread.Post(this.UpdateEqualItemsSizes, DispatcherPriority.Loaded);
        }
    }

    private void OnEqualItemsSizeChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            this.UpdateEqualItemsSizes();
        }
        else
        {
            // Reset widths to auto
            foreach (var container in this.GetRealizedContainers().OfType<ToggleButtonGroupItem>())
            {
                container.Width = double.NaN;
            }
        }
    }

    private void UpdateEqualItemsSizes()
    {
        if (this.isUpdatingEqualSizes || !this.EqualItemsSize)
        {
            return;
        }

        try
        {
            this.isUpdatingEqualSizes = true;

            var realizedContainers = this.GetRealizedContainers();
            ICollection<Control> containers = realizedContainers as ICollection<Control> ?? realizedContainers.ToList();
            if (containers.Count == 0)
            {
                return;
            }

            // Reset all widths to auto to get natural measurements
            foreach (var container in containers)
            {
                container.Width = double.NaN;
            }

            // Force layout update to get accurate measurements
            this.UpdateLayout();

            double minWidth = containers.Select(static container => container.DesiredSize.Width).Prepend(0).Max();
            if (minWidth > 0)
            {
                foreach (var container in containers)
                {
                    container.Width = minWidth;
                }
            }
        }
        finally
        {
            this.isUpdatingEqualSizes = false;
        }
    }
}