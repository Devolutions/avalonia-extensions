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

    public static readonly StyledProperty<EnumPickerItem?> SelectedItemProperty = AvaloniaProperty.Register<EnumPicker, EnumPickerItem?>(
        nameof(SelectedItem),
        defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IReadOnlyList<EnumPickerItem>> ItemsProperty =
        AvaloniaProperty.Register<EnumPicker, IReadOnlyList<EnumPickerItem>>(
            nameof(Items),
            defaultBindingMode: BindingMode.OneWay,
            defaultValue: []);

    public static readonly StyledProperty<Func<object, string>?> TextProviderProperty = AvaloniaProperty.Register<EnumPicker, Func<object, string>?>(
        nameof(TextProvider),
        defaultBindingMode: BindingMode.TwoWay);

    internal IReadOnlyCollection<EnumPickerItem> Items
    {
        get => this.GetValue(ItemsProperty);
        set => this.SetValue(ItemsProperty, value);
    }

    internal EnumPickerItem? SelectedItem
    {
        get => this.GetValue(SelectedItemProperty);
        set => this.SetValue(SelectedItemProperty, value);
    }

    /// <summary>
    /// Gets or sets the function that provides text for enum values/>
    /// </summary>
    public Func<object, string>? TextProvider
    {
        get => this.GetValue(TextProviderProperty);
        set => this.SetValue(TextProviderProperty, value);
    }
}

public class EnumPicker<T> : EnumPicker where T : struct, Enum
{
    private readonly IReadOnlyCollection<T> allEnumValues = Enum.GetValues<T>();
    
    // ReSharper disable once StaticMemberInGenericType
    public static readonly StyledProperty<SortOrder> AlphabeticalOrderProperty = AvaloniaProperty.Register<EnumPicker, SortOrder>(
        nameof(AlphabeticalOrder),
        defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<Comparison<T>?> CustomSortProperty = AvaloniaProperty.Register<EnumPicker, Comparison<T>?>(
        nameof(CustomSort),
        defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IReadOnlyCollection<T>?> ExcludedValuesProperty =
        AvaloniaProperty.Register<EnumPicker, IReadOnlyCollection<T>?>(
            nameof(ExcludedValues),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IReadOnlyCollection<T>?> IncludedValuesProperty =
        AvaloniaProperty.Register<EnumPicker, IReadOnlyCollection<T>?>(
            nameof(IncludedValues),
            defaultBindingMode: BindingMode.TwoWay);
    
    public static readonly StyledProperty<T?> SelectedValueProperty = AvaloniaProperty.Register<EnumPicker, T?>(
        nameof(SelectedValue),
        defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IReadOnlyDictionary<T, string>?> TextOverridesProperty =
        AvaloniaProperty.Register<EnumPicker, IReadOnlyDictionary<T, string>?>(
            nameof(TextOverrides),
            defaultBindingMode: BindingMode.TwoWay);

    protected override Type StyleKeyOverride => typeof(EnumPicker);
    
    /// <summary>
    ///  Gets or sets alphabetical sorting of values by their texts (see <see cref="TextOverrides"/> and <see cref="EnumPicker.TextProvider"/>).
    ///  Applied as a secondary sort after <see cref="CustomSort"/> when both are set.
    /// </summary>
    public SortOrder AlphabeticalOrder
    {
        get => this.GetValue(AlphabeticalOrderProperty);
        set => this.SetValue(AlphabeticalOrderProperty, value);
    }

    /// <summary>
    ///  Gets or sets a custom sort comparison for values. Applied as the primary sort, with <see cref="AlphabeticalOrder"/> acting as a tiebreaker when both are set.
    /// </summary>
    public Comparison<T>? CustomSort
    {
        get => this.GetValue(CustomSortProperty);
        set => this.SetValue(CustomSortProperty, value);
    }

    /// <summary>
    /// Gets or sets the values that should not be listed, an excluded value overrides the same value being in <see cref="IncludedValues"/>
    /// </summary>
    public IReadOnlyCollection<T>? ExcludedValues
    {
        get => this.GetValue(ExcludedValuesProperty);
        set => this.SetValue(ExcludedValuesProperty, value);
    }

    /// <summary>
    /// Gets or sets the values that should be listed, values in <see cref="ExcludedValues"/> are still excluded
    /// </summary>
    public IReadOnlyCollection<T>? IncludedValues
    {
        get => this.GetValue(IncludedValuesProperty);
        set => this.SetValue(IncludedValuesProperty, value);
    }

    /// <summary>
    /// Gets or sets the value of the selected item
    /// </summary>
    public T? SelectedValue
    {
        get => this.GetValue(SelectedValueProperty);
        set => this.SetValue(SelectedValueProperty, value);
    }

    /// <summary>
    ///  Gets or sets a dictionary that matches enum value to their text, values in this dictionary override <see cref="EnumPicker.TextProvider"/>
    /// </summary>
    public IReadOnlyDictionary<T, string>? TextOverrides
    {
        get => this.GetValue(TextOverridesProperty);
        set => this.SetValue(TextOverridesProperty, value);
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
        T? selectedValue = this.SelectedValue;

        IEnumerable<T> values = (this.IncludedValues ?? this.allEnumValues).Except(this.ExcludedValues ?? []);
        IEnumerable<EnumPickerItem> items = values.Select(enumValue => new EnumPickerItem { EnumValue = enumValue, Text = this.GetEnumText(enumValue) });

        IOrderedEnumerable<EnumPickerItem>? orderedItems = null;

        if (this.CustomSort is { } customSort)
        {
            // ReSharper disable once PossibleMultipleEnumeration
            orderedItems = items.OrderBy(val => (T)val.EnumValue, Comparer<T>.Create(customSort));
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

        // Directly sync SelectedItem since SetValue won't fire OnPropertyChanged if the value is unchanged
        this.SelectedItem = this.Items.FirstOrDefault(val => val.EnumValue.Equals(selectedValue)) ?? this.Items.FirstOrDefault();
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
            this.SetValue(SelectedValueProperty, (T?)change.GetNewValue<EnumPickerItem?>()?.EnumValue);
        }
        else if (change.Property == SelectedValueProperty)
        {
            T? newValue = change.GetNewValue<T?>();
            this.SelectedItem = this.Items.FirstOrDefault(val => val.EnumValue.Equals(newValue));
        }
    }
}

public class EnumPickerItem
{
    public required object EnumValue { get; init; }

    public required string Text { get; init; }
}