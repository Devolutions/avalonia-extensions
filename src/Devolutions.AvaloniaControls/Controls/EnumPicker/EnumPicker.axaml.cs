namespace Devolutions.AvaloniaControls.Controls;

using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

public abstract class EnumPicker : TemplatedControl
{
    public enum SortOrder
    {
        None,
        Ascending,
        Descending,
    }
    private EnumPickerItem? selectedItem;
    private IReadOnlyCollection<EnumPickerItem> items = [];

    public static readonly DirectProperty<EnumPicker, EnumPickerItem?> SelectedItemProperty =
        AvaloniaProperty.RegisterDirect<EnumPicker, EnumPickerItem?>(
            nameof(SelectedItem),
            o => o.SelectedItem,
            (o, v) => o.SelectedItem = v,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly DirectProperty<EnumPicker, IReadOnlyCollection<EnumPickerItem>> ItemsProperty =
        AvaloniaProperty.RegisterDirect<EnumPicker, IReadOnlyCollection<EnumPickerItem>>(
            nameof(Items),
            o => o.Items,
            (o, v) => o.Items = v);

    public static readonly StyledProperty<Func<Enum, string>?> TextProviderProperty = AvaloniaProperty.Register<EnumPicker, Func<Enum, string>?>(
        nameof(TextProvider),
        defaultBindingMode: BindingMode.TwoWay);

    protected bool UpdatingItems;

    internal IReadOnlyCollection<EnumPickerItem> Items
    {
        get => this.items;
        set
        {
            try
            {
                this.UpdatingItems = true;
                this.SetAndRaise(ItemsProperty, ref this.items, value);
            }
            finally
            {
                this.UpdatingItems = false;
            }
        }
    }

    internal EnumPickerItem? SelectedItem
    {
        get => this.selectedItem;
        set => this.SetAndRaise(SelectedItemProperty, ref this.selectedItem, value);
    }

    /// <summary>
    /// Gets or sets the function that provides text for enum values
    /// </summary>
    public Func<Enum, string>? TextProvider
    {
        get => this.GetValue(TextProviderProperty);
        set => this.SetValue(TextProviderProperty, value);
    }
}

public class EnumPicker<T> : EnumPicker where T : struct, Enum
{
    private readonly IReadOnlyCollection<T> allEnumValues = Enum.GetValues<T>();

    private SortOrder alphabeticalOrder;
    private Comparison<T>? customSort;
    private IReadOnlyCollection<T>? excludedValues;
    private IReadOnlyCollection<T>? includedValues;
    private T selectedValue;
    private IReadOnlyDictionary<T, string>? textOverrides;

#pragma warning disable AVP1002
    public static readonly DirectProperty<EnumPicker<T>, SortOrder> AlphabeticalOrderProperty =
        AvaloniaProperty.RegisterDirect<EnumPicker<T>, SortOrder>(
            nameof(AlphabeticalOrder),
            o => o.AlphabeticalOrder,
            (o, v) => o.AlphabeticalOrder = v,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly DirectProperty<EnumPicker<T>, Comparison<T>?> CustomSortProperty =
        AvaloniaProperty.RegisterDirect<EnumPicker<T>, Comparison<T>?>(
            nameof(CustomSort),
            o => o.CustomSort,
            (o, v) => o.CustomSort = v,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly DirectProperty<EnumPicker<T>, IReadOnlyCollection<T>?> ExcludedValuesProperty =
        AvaloniaProperty.RegisterDirect<EnumPicker<T>, IReadOnlyCollection<T>?>(
            nameof(ExcludedValues),
            o => o.ExcludedValues,
            (o, v) => o.ExcludedValues = v,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly DirectProperty<EnumPicker<T>, IReadOnlyCollection<T>?> IncludedValuesProperty =
        AvaloniaProperty.RegisterDirect<EnumPicker<T>, IReadOnlyCollection<T>?>(
            nameof(IncludedValues),
            o => o.IncludedValues,
            (o, v) => o.IncludedValues = v,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly DirectProperty<EnumPicker<T>, T> SelectedValueProperty =
        AvaloniaProperty.RegisterDirect<EnumPicker<T>, T>(
            nameof(SelectedValue),
            o => o.SelectedValue,
            (o, v) => o.SelectedValue = v,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly DirectProperty<EnumPicker<T>, IReadOnlyDictionary<T, string>?> TextOverridesProperty =
        AvaloniaProperty.RegisterDirect<EnumPicker<T>, IReadOnlyDictionary<T, string>?>(
            nameof(TextOverrides),
            o => o.TextOverrides,
            (o, v) => o.TextOverrides = v,
            defaultBindingMode: BindingMode.TwoWay);
#pragma warning restore AVP1002
    
    protected override Type StyleKeyOverride => typeof(EnumPicker);

    /// <summary>
    ///  Gets or sets alphabetical sorting of values by their texts (see <see cref="TextOverrides"/> and <see cref="EnumPicker.TextProvider"/>).
    ///  Applied as a secondary sort after <see cref="CustomSort"/> when both are set.
    /// </summary>
    public SortOrder AlphabeticalOrder
    {
        get => this.alphabeticalOrder;
        set => this.SetAndRaise(AlphabeticalOrderProperty, ref this.alphabeticalOrder, value);
    }

    /// <summary>
    ///  Gets or sets a custom sort comparison for values. Applied as the primary sort, with <see cref="AlphabeticalOrder"/> acting as a tiebreaker when both are set.
    /// </summary>
    public Comparison<T>? CustomSort
    {
        get => this.customSort;
        set => this.SetAndRaise(CustomSortProperty, ref this.customSort, value);
    }

    /// <summary>
    /// Gets or sets the values that should not be listed, an excluded value overrides the same value being in <see cref="IncludedValues"/>
    /// </summary>
    public IReadOnlyCollection<T>? ExcludedValues
    {
        get => this.excludedValues;
        set => this.SetAndRaise(ExcludedValuesProperty, ref this.excludedValues, value);
    }

    /// <summary>
    /// Gets or sets the values that should be listed, values in <see cref="ExcludedValues"/> are still excluded
    /// </summary>
    public IReadOnlyCollection<T>? IncludedValues
    {
        get => this.includedValues;
        set => this.SetAndRaise(IncludedValuesProperty, ref this.includedValues, value);
    }

    /// <summary>
    /// Gets or sets the value of the selected item
    /// </summary>
    public T SelectedValue
    {
        get => this.selectedValue;
        set => this.SetAndRaise(SelectedValueProperty, ref this.selectedValue, value);
    }

    /// <summary>
    ///  Gets or sets a dictionary that matches enum value to their text, values in this dictionary override <see cref="EnumPicker.TextProvider"/>
    /// </summary>
    public IReadOnlyDictionary<T, string>? TextOverrides
    {
        get => this.textOverrides;
        set => this.SetAndRaise(TextOverridesProperty, ref this.textOverrides, value);
    }

    private string GetEnumText(T value)
    {
        if (this.TextOverrides?.TryGetValue(value, out string? text) == true)
        {
            return text;
        }

        if (this.TextProvider?.Invoke(value) is { } providedText)
        {
            return providedText;
        }

        return value.ToString();
    }

    private void UpdateValues()
    {
        T? selection = this.SelectedValue;

        IEnumerable<T> values = (this.IncludedValues ?? this.allEnumValues).Except(this.ExcludedValues ?? []);
        IEnumerable<EnumPickerItem> items = values.Select(enumValue => new EnumPickerItem { EnumValue = enumValue, Text = this.GetEnumText(enumValue) });

        IOrderedEnumerable<EnumPickerItem>? orderedItems = null;

        if (this.CustomSort is { } sort)
        {
            // ReSharper disable once PossibleMultipleEnumeration
            orderedItems = items.OrderBy(val => (T)val.EnumValue, Comparer<T>.Create(sort));
        }

        if (this.AlphabeticalOrder == SortOrder.Ascending)
        {
            orderedItems = orderedItems is null
                // ReSharper disable once PossibleMultipleEnumeration
                ? items.OrderBy(val => val.Text, StringComparer.InvariantCultureIgnoreCase)
                : orderedItems.ThenBy(val => val.Text, StringComparer.InvariantCultureIgnoreCase);
        }
        else if (this.AlphabeticalOrder == SortOrder.Descending)
        {
            orderedItems = orderedItems is null
                // ReSharper disable once PossibleMultipleEnumeration
                ? items.OrderByDescending(val => val.Text, StringComparer.InvariantCultureIgnoreCase)
                : orderedItems.ThenByDescending(val => val.Text, StringComparer.InvariantCultureIgnoreCase);
        }

        if (orderedItems is not null)
        {
            items = orderedItems;
        }

        // ReSharper disable once PossibleMultipleEnumeration
        this.Items = items.ToList();

        // Directly sync SelectedItem since SetAndRaise won't fire OnPropertyChanged if the value is unchanged
        this.SelectedItem = this.Items.FirstOrDefault(val => val.EnumValue.Equals(selection)) ?? this.Items.FirstOrDefault();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        this.UpdateValues();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IncludedValuesProperty ||
            change.Property == ExcludedValuesProperty ||
            change.Property == TextProviderProperty   ||
            change.Property == AlphabeticalOrderProperty ||
            change.Property == CustomSortProperty ||
            change.Property == TextOverridesProperty)
        {
            this.UpdateValues();
        }
        else if (change.Property == SelectedItemProperty)
        {
            this.SelectedValue = (T?)change.GetNewValue<EnumPickerItem?>()?.EnumValue ?? default;
        }
        else if (change.Property == SelectedValueProperty)
        {
            if (this.UpdatingItems)
            {
                this.SelectedItem = null;
            }
            else
            {
                T newValue = change.GetNewValue<T>();
                this.SelectedItem = this.Items.FirstOrDefault(val => val.EnumValue.Equals(newValue)) ?? this.Items.FirstOrDefault();
                this.SelectedValue = (T?)this.SelectedItem?.EnumValue ?? default;
            }
        }
    }
}

public class EnumPickerItem
{
    public required object EnumValue { get; init; }

    public required string Text { get; init; }
}