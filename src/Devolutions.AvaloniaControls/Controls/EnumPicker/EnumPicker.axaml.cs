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

    public IReadOnlyCollection<EnumPickerItem> Items
    {
        get;
        set
        {
            try
            {
                this.UpdatingItems = true;
                this.SetAndRaise(ItemsProperty, ref field, value);
            }
            finally
            {
                this.UpdatingItems = false;
            }
        }
    } = [];

    internal EnumPickerItem? SelectedItem
    {
        get;
        set => this.SetAndRaise(SelectedItemProperty, ref field, value);
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

    private bool initialized;
    private bool isInitialValueSet;
    private bool isTemplateSet;
    private bool textOverridesDirty = true;
    private Dictionary<T, string> cachedTextOverrides = [];
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
        get;
        set => this.SetAndRaise(AlphabeticalOrderProperty, ref field, value);
    }

    /// <summary>
    /// Gets or sets the values that should not be listed, an excluded value overrides the same value being in <see cref="IncludedValues"/>
    /// </summary>
    public ICollection<T> ExcludedValues
    {
        get;
        set
        {
            if (field is INotifyCollectionChanged beforeCollection)
            {
                beforeCollection.CollectionChanged -= this.UpdateValues;
            }

            // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
            this.SetAndRaise(ExcludedValuesProperty, ref field, value ?? new AvaloniaList<T>());

            if (field is INotifyCollectionChanged afterCollection)
            {
                afterCollection.CollectionChanged += this.UpdateValues;
            }
        }
    } = new AvaloniaList<T>();

    /// <summary>
    /// Gets or sets the values that should be listed, values in <see cref="ExcludedValues"/> are still excluded
    /// </summary>
    public ICollection<T> IncludedValues
    {
        get;
        set
        {
            if (field is INotifyCollectionChanged beforeCollection)
            {
                beforeCollection.CollectionChanged -= this.UpdateValues;
            }

            // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
            this.SetAndRaise(IncludedValuesProperty, ref field, value ?? new AvaloniaList<T>());

            if (field is INotifyCollectionChanged afterCollection)
            {
                afterCollection.CollectionChanged += this.UpdateValues;
            }
        }
    } = new AvaloniaList<T>();

    /// <summary>
    /// Gets or sets the value of the selected item
    /// </summary>
    public T SelectedValue
    {
        get;
        set
        {
            if (this.isInitialValueSet)
            {
                this.SetAndRaise(SelectedValueProperty, ref field, value);
            }
            else
            {
                // We do not want to raise before having fully initialized the control, otherwise we might
                // update the bound property and raise OnChange events not actually driven by the user.
                field = value;
            }
        }
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
            // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
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
        if (this.TextOverrides.Count <= 0)
        {
            return [];
        }

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
    
    private void InvalidateTextOverrides() => this.textOverridesDirty = true;

    // Note: We do a manual List allocation, population and in-place sorting
    //       to avoid LINQ overhead.
    //
    //       Profiling show a ~3x improvement in speed + reduced GC pressure, which
    //       can be relevant since this can be a pretty hot path in some applications.
    private void UpdateValues()
    {
        if (!this.initialized || !this.isTemplateSet)
        {
            return;
        }
        
        T selection = this.SelectedValue;

        if (this.textOverridesDirty)
        {
            this.cachedTextOverrides = this.GetTextOverridesDictionary();
            this.textOverridesDirty = false;
        }

        HashSet<T>? includedSet = this.IncludedValues.Count > 0
            ? (this.IncludedValues as HashSet<T> ?? [..this.IncludedValues])
            : null;
        HashSet<T>? excludedSet = this.ExcludedValues.Count > 0
            ? (this.ExcludedValues as HashSet<T> ?? [..this.ExcludedValues])
            : null;

        var list = new List<EnumPickerItem>(this.allEnumValues.Count);
        EnumPickerItem? selectedItem = null;
        foreach (T val in this.allEnumValues)
        {
            if (includedSet != null && !includedSet.Contains(val))
            {
                continue;
            }

            if (excludedSet != null && excludedSet.Contains(val))
            {
                continue;
            }

            var item = new EnumPickerItem { EnumValue = val, Text = this.GetEnumText(val, this.cachedTextOverrides) };
            list.Add(item);

            if (EqualityComparer<T>.Default.Equals(val, selection))
            {
                selectedItem = item;
            }
        }

        Comparison<Enum>? sort = this.CustomSort;
        SortOrder alphabeticalOrder = this.AlphabeticalOrder;
        if (sort is not null || alphabeticalOrder != SortOrder.None)
        {
            list.Sort((a, b) =>
            {
                int result = 0;
                if (sort is not null)
                {
                    result = sort((T)a.EnumValue, (T)b.EnumValue);
                }

                if (result == 0 && alphabeticalOrder == SortOrder.Ascending)
                {
                    result = string.Compare(a.Text, b.Text, StringComparison.InvariantCultureIgnoreCase);
                }
                else if (result == 0 && alphabeticalOrder == SortOrder.Descending)
                {
                    result = string.Compare(b.Text, a.Text, StringComparison.InvariantCultureIgnoreCase);
                }

                return result;
            });
        }

        this.Items = list;

        // Directly sync SelectedItem since SetAndRaise won't fire OnPropertyChanged if the value is unchanged
        this.SelectedItem = selectedItem ?? (list.Count > 0 ? list[0] : null);

        this.isInitialValueSet = true;
    }

    private void UpdateValues(object? sender, NotifyCollectionChangedEventArgs e) => this.UpdateValues();

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        this.isTemplateSet = true;
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
            change.Property == AlphabeticalOrderProperty ||
            change.Property == CustomSortProperty)
        {
            this.UpdateValues();
        }
        else if (change.Property == TextProviderProperty ||
                 change.Property == TextOverridesProperty)
        {
            this.InvalidateTextOverrides();
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

    private void OnTextOverridePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        this.InvalidateTextOverrides();
        this.UpdateValues();
    }

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

        this.InvalidateTextOverrides();
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