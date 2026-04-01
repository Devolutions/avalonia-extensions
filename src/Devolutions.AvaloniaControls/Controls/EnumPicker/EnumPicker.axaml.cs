namespace Devolutions.AvaloniaControls.Controls;

using System.Collections.Specialized;

using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

public abstract class EnumPicker : TemplatedControl
{
    internal const string DefaultFormat = "{0} ({1})";

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

    public static readonly StyledProperty<Comparison<Enum>?> CustomSortProperty =
        AvaloniaProperty.Register<EnumPicker, Comparison<Enum>?>(
            nameof(CustomSort));

    public static readonly StyledProperty<Func<Enum, string>?> TextProviderProperty = AvaloniaProperty.Register<EnumPicker, Func<Enum, string>?>(
        nameof(TextProvider));

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
    ///  Gets or sets a custom sort comparison for enum values. Applied as the primary sort.
    /// </summary>
    public Comparison<Enum>? CustomSort
    {
        get => this.GetValue(CustomSortProperty);
        set => this.SetValue(CustomSortProperty, value);
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
    private ICollection<T> excludedValues = new AvaloniaList<T>();
    private ICollection<T> includedValues = new AvaloniaList<T>();
    private bool initialized;
    private T selectedValue;
    private ICollection<EnumPickerTextOverride<T>> textOverrides = new AvaloniaList<EnumPickerTextOverride<T>>();
    
    public EnumPicker()
    {
        this.Items = this.allEnumValues.Select(enumValue => new EnumPickerItem { EnumValue = enumValue, Text = this.GetEnumText(enumValue, []) }).ToList();

        this.DidChangeTextOverrides();
    }

#pragma warning disable AVP1002
    public static readonly DirectProperty<EnumPicker<T>, SortOrder> AlphabeticalOrderProperty =
        AvaloniaProperty.RegisterDirect<EnumPicker<T>, SortOrder>(
            nameof(AlphabeticalOrder),
            o => o.AlphabeticalOrder,
            (o, v) => o.AlphabeticalOrder = v);

    public static readonly DirectProperty<EnumPicker<T>, ICollection<T>> ExcludedValuesProperty =
        AvaloniaProperty.RegisterDirect<EnumPicker<T>, ICollection<T>>(
            nameof(ExcludedValues),
            o => o.ExcludedValues,
            (o, v) => o.ExcludedValues = v);

    public static readonly DirectProperty<EnumPicker<T>, ICollection<T>> IncludedValuesProperty =
        AvaloniaProperty.RegisterDirect<EnumPicker<T>, ICollection<T>>(
            nameof(IncludedValues),
            o => o.IncludedValues,
            (o, v) => o.IncludedValues = v);

    public static readonly DirectProperty<EnumPicker<T>, T> SelectedValueProperty =
        AvaloniaProperty.RegisterDirect<EnumPicker<T>, T>(
            nameof(SelectedValue),
            o => o.SelectedValue,
            (o, v) => o.SelectedValue = v,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly DirectProperty<EnumPicker<T>, ICollection<EnumPickerTextOverride<T>>> TextOverridesProperty =
        AvaloniaProperty.RegisterDirect<EnumPicker<T>, ICollection<EnumPickerTextOverride<T>>>(
            nameof(TextOverrides),
            o => o.TextOverrides,
            (o, v) => o.TextOverrides = v);
#pragma warning restore AVP1002
    
    protected override Type StyleKeyOverride => typeof(EnumPicker);

    /// <summary>
    ///  Gets or sets alphabetical sorting of values by their texts (see <see cref="TextOverrides"/> and <see cref="EnumPicker.TextProvider"/>).
    ///  Applied as a secondary sort after <see cref="EnumPicker.CustomSort"/> when both are set.
    /// </summary>
    public SortOrder AlphabeticalOrder
    {
        get => this.alphabeticalOrder;
        set => this.SetAndRaise(AlphabeticalOrderProperty, ref this.alphabeticalOrder, value);
    }

    /// <summary>
    /// Gets or sets the values that should not be listed, an excluded value overrides the same value being in <see cref="IncludedValues"/>
    /// </summary>
    public ICollection<T> ExcludedValues
    {
        get => this.excludedValues;
        set
        {
            if (this.excludedValues is INotifyCollectionChanged beforeCollection)
            {
                beforeCollection.CollectionChanged -= this.UpdateValues;
            }

            this.SetAndRaise(ExcludedValuesProperty, ref this.excludedValues, value ?? new AvaloniaList<T>());

            if (this.excludedValues is INotifyCollectionChanged afterCollection)
            {
                afterCollection.CollectionChanged += this.UpdateValues;
            }
        }
    }

    /// <summary>
    /// Gets or sets the values that should be listed, values in <see cref="ExcludedValues"/> are still excluded
    /// </summary>
    public ICollection<T> IncludedValues
    {
        get => this.includedValues;
        set
        {
            if (this.includedValues is INotifyCollectionChanged beforeCollection)
            {
                beforeCollection.CollectionChanged -= this.UpdateValues;
            }
            
            this.SetAndRaise(IncludedValuesProperty, ref this.includedValues, value ?? new AvaloniaList<T>());

            if (this.includedValues is INotifyCollectionChanged afterCollection)
            {
                afterCollection.CollectionChanged += this.UpdateValues;
            }
        }
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
    ///  Gets or sets a collection of text overrides for specific enum values. Overrides take precedence over <see cref="EnumPicker.TextProvider"/>.
    ///  Two override types are supported:
    ///  <list type="bullet">
    ///    <item><see cref="EnumPickerDirectTextOverride{T}"/> — replaces the display text with a fixed string.</item>
    ///    <item><see cref="EnumPickerFormattedTextOverride{T}"/> — formats the display text using the enum value and a proxy enum value (e.g. "Default (Medium)").</item>
    ///  </list>
    /// </summary>
    public ICollection<EnumPickerTextOverride<T>> TextOverrides
    {
        get => this.textOverrides;
        set
        {
            this.WillChangeTextOverrides();
            this.SetAndRaise(TextOverridesProperty, ref this.textOverrides, value ?? new AvaloniaList<EnumPickerTextOverride<T>>());
            this.DidChangeTextOverrides();
        }
    }

    private void WillChangeTextOverrides()
    {
        if (this.textOverrides is INotifyCollectionChanged beforeCollection)
        {
            beforeCollection.CollectionChanged -= this.OnTextOverridesCollectionChanged;
        }

        foreach (EnumPickerTextOverride<T> item in this.textOverrides)
        {
            this.UnsubscribeOverrideItem(item);
        }
    }

    private void DidChangeTextOverrides()
    {
        if (this.textOverrides is INotifyCollectionChanged afterCollection)
        {
            afterCollection.CollectionChanged += this.OnTextOverridesCollectionChanged;
        }

        foreach (EnumPickerTextOverride<T> item in this.textOverrides)
        {
            this.SubscribeOverrideItem(item);
        }
    }

    private string GetEnumText(T value, Dictionary<T, string> textOverrideDictionary)
    {
        if (textOverrideDictionary.TryGetValue(value, out string? textOverride))
        {
            return textOverride;
        }

        if (this.TextProvider?.Invoke(value) is { } providedText)
        {
            return providedText;
        }

        return value.ToString();
    }

    private Dictionary<T, string> GetTextOverridesDictionary()
    {
        return this.TextOverrides.DistinctBy(textOverride => textOverride.Enum).ToDictionary(
            textOverride => textOverride.Enum,
            textOverride =>
            {
                switch (textOverride)
                {
                    case EnumPickerDirectTextOverride<T> directTextOverride:
                        return directTextOverride.Text;
                    case EnumPickerFormattedTextOverride<T> proxiedTextOverride:
                        try
                        {
                            return string.Format(proxiedTextOverride.Format, this.GetEnumText(proxiedTextOverride.Enum, []), this.GetEnumText(proxiedTextOverride.EnumOverride, []));
                        }
                        catch
                        {
                            // Fall back to default format if configured format is invalid
                            return string.Format(DefaultFormat,  this.GetEnumText(proxiedTextOverride.Enum, []), this.GetEnumText(proxiedTextOverride.EnumOverride, []));
                        }
                    default:
                            // Should not happen
                            return textOverride.Enum.ToString();
                }
            });
    }
    
    private void UpdateValues()
    {
        if (!this.initialized)
        {
            return;
        }
        
        T selection = this.SelectedValue;

        IEnumerable<T> values = this.allEnumValues;
        if (this.IncludedValues.Count > 0)
        {
            values = values.Intersect(this.IncludedValues);
        }
        
        values = values.Except(this.ExcludedValues);
        
        Dictionary<T, string> textOverrideDictionary = this.GetTextOverridesDictionary();
        
        IEnumerable<EnumPickerItem> items = values.Select(enumValue => new EnumPickerItem { EnumValue = enumValue, Text = this.GetEnumText(enumValue, textOverrideDictionary) });

        IOrderedEnumerable<EnumPickerItem>? orderedItems = null;

        if (this.CustomSort is { } sort)
        {
            // ReSharper disable once PossibleMultipleEnumeration
            orderedItems = items.OrderBy(val => (T)val.EnumValue, Comparer<T>.Create((a, b) => sort(a, b)));
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

    private void UpdateValues(object? sender, NotifyCollectionChangedEventArgs e) => this.UpdateValues();

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        this.UpdateValues();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        this.initialized = true;

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

    private void OnTextOverridePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e) => this.UpdateValues();

    private void OnTextOverridesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (EnumPickerTextOverride<T> item in e.OldItems.OfType<EnumPickerTextOverride<T>>())
            {
                this.UnsubscribeOverrideItem(item);
            }
        }

        if (e.NewItems != null)
        {
            foreach (EnumPickerTextOverride<T> item in e.NewItems.OfType<EnumPickerTextOverride<T>>())
            {
                this.SubscribeOverrideItem(item);
            }
        }

        this.UpdateValues();
    }

    private void SubscribeOverrideItem(EnumPickerTextOverride<T> item) => item.PropertyChanged += this.OnTextOverridePropertyChanged;

    private void UnsubscribeOverrideItem(EnumPickerTextOverride<T> item) => item.PropertyChanged -= this.OnTextOverridePropertyChanged;
}

public class EnumPickerItem
{
    public required object EnumValue { get; init; }

    public required string Text { get; init; }
}