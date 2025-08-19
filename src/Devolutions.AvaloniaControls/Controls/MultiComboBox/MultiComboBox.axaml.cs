namespace Devolutions.AvaloniaControls.Controls;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

public enum MultiComboBoxOverflowMode
{
    Scroll,
    Wrap,
}

public class MultiComboBox : Ursa.Controls.MultiComboBox
{
    public static readonly StyledProperty<MultiComboBoxOverflowMode> OverflowModeProperty =
        AvaloniaProperty.Register<MultiComboBox, MultiComboBoxOverflowMode>(nameof(OverflowMode));

    public static readonly DirectProperty<MultiComboBox, ScrollBarVisibility> ScrollbarVisibilityProperty =
        AvaloniaProperty.RegisterDirect<MultiComboBox, ScrollBarVisibility>(nameof(OverflowMode),
            o => o.ScrollbarVisibility,
            (o, v) => o.ScrollbarVisibility = v);

    public MultiComboBox()
    {
        OverflowModeProperty.Changed.Subscribe(_ => this.ScrollbarVisibility = this.OverflowMode switch
        {
            MultiComboBoxOverflowMode.Scroll => ScrollBarVisibility.Auto,
            MultiComboBoxOverflowMode.Wrap => ScrollBarVisibility.Disabled,
            _ => ScrollBarVisibility.Auto,
        });
    }

    public MultiComboBoxOverflowMode OverflowMode
    {
        get => this.GetValue(OverflowModeProperty);
        set => this.SetValue(OverflowModeProperty, value);
    }

    public ScrollBarVisibility ScrollbarVisibility { get; private set; } = ScrollBarVisibility.Auto;

    protected override void OnInitialized()
    {
        this.FixInitializationFromAxamlItems();
        base.OnInitialized();
    }

    private void FixInitializationFromAxamlItems()
    {
        var isCleared = false;

        foreach (var item in this.Items)
        {
            if (item is MultiComboBoxItem multiComboBoxItem)
            {
                if (!isCleared)
                {
                    this.Selection.Clear();
                    this.SelectedItems?.Clear();
                    isCleared = true;
                }

                multiComboBoxItem.DataContext = multiComboBoxItem.Content;
                if (multiComboBoxItem.IsSelected)
                {
                    this.SelectedItems?.Add(multiComboBoxItem.DataContext);
                }
            }
        }
    }

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        recycleKey = item;
        return item is not MultiComboBoxItem;
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new MultiComboBoxItem();

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        if (item is MultiComboBoxItem containerItem)
        {
            container.DataContext = containerItem.Content;
            return;
        }

        base.PrepareContainerForItemOverride(container, item, index);
    }
}