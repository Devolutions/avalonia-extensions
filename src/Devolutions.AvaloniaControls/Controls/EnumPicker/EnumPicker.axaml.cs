namespace Devolutions.AvaloniaControls.Controls;

using System.Collections;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;

using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.LogicalTree;

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

    public static readonly DirectProperty<EnumPicker, IEnumerable> InnerLeftContentProperty =
        AvaloniaProperty.RegisterDirect<EnumPicker, IEnumerable>(
            nameof(InnerLeftContent),
            static o => o.InnerLeftContent,
            static (o, v) => o.InnerLeftContent = v);

    public static readonly DirectProperty<EnumPicker, IEnumerable> InnerLeftOfDropDownArrowContentProperty =
        AvaloniaProperty.RegisterDirect<EnumPicker, IEnumerable>(
            nameof(InnerLeftOfDropDownArrowContent),
            static o => o.InnerLeftOfDropDownArrowContent,
            static (o, v) => o.InnerLeftOfDropDownArrowContent = v);

    public static readonly DirectProperty<EnumPicker, IEnumerable> InnerRightContentProperty =
        AvaloniaProperty.RegisterDirect<EnumPicker, IEnumerable>(
            nameof(InnerRightContent),
            static o => o.InnerRightContent,
            static (o, v) => o.InnerRightContent = v);

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

    /// <summary>
    /// Gets or sets the content rendered inside the picker, before the value (leftmost slot).
    /// </summary>
    public IEnumerable InnerLeftContent { get; set; } = new AvaloniaList<Control>();

    /// <summary>
    /// Gets or sets the content rendered inside the picker, just before the drop-down arrow.
    /// </summary>
    public IEnumerable InnerLeftOfDropDownArrowContent { get; set; } = new AvaloniaList<Control>();

    /// <summary>
    /// Gets or sets the content rendered inside the picker, after the drop-down arrow (rightmost slot).
    /// </summary>
    public IEnumerable InnerRightContent { get; set; } = new AvaloniaList<Control>();
}

public class EnumPicker<T> : EnumPicker where T : struct, Enum
{
    private readonly T[] allEnumValues = Enum.GetValues<T>();

    private bool initialized;
    private bool isInitialValueSet;
    private bool isTemplateSet;
    private bool textOverridesDirty = true;
    private HashSet<T>? includedSet = null;
    private bool includedSetDirty = true;
    private HashSet<T>? excludedSet = null;
    private bool excludedSetDirty = true;
    private Dictionary<T, string> cachedTextOverrides = [];
    private ICollection<EnumPickerTextOverride<T>> textOverrides = new AvaloniaList<EnumPickerTextOverride<T>>();
    
    public EnumPicker()
    {
        Func<Enum, string>? textProvider = this.TextProvider;
        List<EnumPickerItem> items = new(this.allEnumValues.Length);
        for (var i = 0; i < this.allEnumValues.Length; ++i)
        {
            T v = this.allEnumValues[i];
            items.Add(new EnumPickerItem<T> { Value = v, EnumValue = v, Text = GetEnumText(v, EmptyOverrides, textProvider) });
        }
        this.Items = items;

        this.DidChangeTextOverrides();
    }

    private static readonly Dictionary<T, string> EmptyOverrides = [];

    // Cached sort delegate to avoid per-call closure allocation. Reads cached fields.
    private Comparison<EnumPickerItem>? cachedSortComparison;
    private Comparison<Enum>? sortSnapshot;
    private SortOrder alphabeticalOrderSnapshot;

#pragma warning disable AVP1002
    public static readonly DirectProperty<EnumPicker<T>, SortOrder> AlphabeticalOrderProperty =
        AvaloniaProperty.RegisterDirect<EnumPicker<T>, SortOrder>(
            nameof(AlphabeticalOrder),
            o => o.AlphabeticalOrder,
            (o, v) => o.AlphabeticalOrder = v);

    public static readonly DirectProperty<EnumPicker<T>, IList<T>> ExcludedValuesProperty =
        AvaloniaProperty.RegisterDirect<EnumPicker<T>, IList<T>>(
            nameof(ExcludedValues),
            o => o.ExcludedValues,
            (o, v) => o.ExcludedValues = v);

    public static readonly DirectProperty<EnumPicker<T>, IList<T>> IncludedValuesProperty =
        AvaloniaProperty.RegisterDirect<EnumPicker<T>, IList<T>>(
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
    public IList<T> ExcludedValues
    {
        get;
        set
        {
            if (field is INotifyCollectionChanged beforeCollection)
            {
                beforeCollection.CollectionChanged -= this.UpdateExcludedValues;
            }

            this.excludedSetDirty = true;
            // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
            this.SetAndRaise(ExcludedValuesProperty, ref field, value ?? new AvaloniaList<T>());

            if (field is INotifyCollectionChanged afterCollection)
            {
                afterCollection.CollectionChanged += this.UpdateExcludedValues;
            }
        }
    } = new AvaloniaList<T>();

    /// <summary>
    /// Gets or sets the values that should be listed, values in <see cref="ExcludedValues"/> are still excluded
    /// </summary>
    public IList<T> IncludedValues
    {
        get;
        set
        {
            if (field is INotifyCollectionChanged beforeCollection)
            {
                beforeCollection.CollectionChanged -= this.UpdateIncludedValues;
            }

            this.includedSetDirty = true;
            // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
            this.SetAndRaise(IncludedValuesProperty, ref field, value ?? new AvaloniaList<T>());

            if (field is INotifyCollectionChanged afterCollection)
            {
                afterCollection.CollectionChanged += this.UpdateIncludedValues;
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
        => GetEnumText(value, textOverrideDictionary, this.TextProvider);

    private static string GetEnumText(T value, Dictionary<T, string> textOverrideDictionary, Func<Enum, string>? textProvider)
    {
        if (textOverrideDictionary.TryGetValue(value, out string? textOverride))
        {
            return textOverride;
        }

        if (textProvider?.Invoke(value) is { } providedText)
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

        // Hoist property reads — each goes through Avalonia's property system, non-trivial cost in a hot loop.
        Func<Enum, string>? textProvider = this.TextProvider;
        Comparison<Enum>? sort = this.CustomSort;
        SortOrder alphabeticalOrder = this.AlphabeticalOrder;
        IList<T> included = this.IncludedValues;
        IList<T> excluded = this.ExcludedValues;
        Dictionary<T, string> overrides = this.cachedTextOverrides;

        // Micro-optimization;
        // this is faster than using new HashSet(this.ExcludedValues), at least at the time of writing,
        // since the implementation in the constructor from Enumerable will :
        //   - do an additional type-check to check if it's a ICollection
        //   - initialize with the collection count as capacity
        //   - call UnionWith(<provided_enumerable>)
        //   - trim excess
        //
        // we also use a manual for loop instead of `UnionWith`, since `UnionWith` (again, at the time
        // of writing), does an additional null-check + a foreach on the enumerable, and I'm not certain
        // that the foreach will always be am efficient iteration over lists instead of always using
        // the enumerable's iterator.
        //
        // Finally we also provide the comparer, since we need the same one for the 2 collections + our
        // own comparison further down below.
        // - sbergerondrouin 2026-05-22
        int includedCount = included.Count;
        if (this.includedSetDirty && includedCount > 0)
        {
            var includedSet = new HashSet<T>(includedCount, EqualityComparer<T>.Default);
            for (var i = 0; i < includedCount; ++i)
            {
                includedSet.Add(included[i]);
            }
            this.includedSet = includedSet;
            this.includedSetDirty = false;
        }

        int excludedCount = excluded.Count;
        if (this.excludedSetDirty && excludedCount > 0)
        {
            var excludedSet = new HashSet<T>(excludedCount, EqualityComparer<T>.Default);
            for (var i = 0; i < excludedCount; ++i)
            {
                excludedSet.Add(excluded[i]);
            }
            this.excludedSet = excludedSet;
            this.excludedSetDirty = false;
        }

        // We do a manual List allocation, population and then in-place sorting
        // to avoid LINQ overhead.
        //
        // Profiling show a ~3x improvement in speed + reduced GC pressure, which
        // can be relevant since this can be a pretty hot path in some applications.
        var list = new List<EnumPickerItem>(this.allEnumValues.Length);
        EnumPickerItem? selectedItem = null;
        for (var i = 0; i < this.allEnumValues.Length; ++i)
        {
            T val = this.allEnumValues[i];

            if (includedCount > 0 && !this.includedSet!.Contains(val))
            {
                continue;
            }

            if (excludedCount > 0 && this.excludedSet!.Contains(val))
            {
                continue;
            }

            var item = new EnumPickerItem<T> { Value = val, EnumValue = val, Text = GetEnumText(val, overrides, textProvider) };
            list.Add(item);

            if (EnumEquals(val, selection))
            {
                selectedItem = item;
            }
        }

        if (sort is not null || alphabeticalOrder != SortOrder.None)
        {
            // Cache a single Comparison<EnumPickerItem> delegate that reads cached snapshot fields,
            // avoiding per-call closure allocation.
            this.sortSnapshot = sort;
            this.alphabeticalOrderSnapshot = alphabeticalOrder;
            this.cachedSortComparison ??= this.CompareItems;
            list.Sort(this.cachedSortComparison);
        }

        this.Items = list;

        // Directly sync SelectedItem since SetAndRaise won't fire OnPropertyChanged if the value is unchanged
        this.SelectedItem = selectedItem ?? (list.Count > 0 ? list[0] : null);

        this.isInitialValueSet = true;
    }

    private int CompareItems(EnumPickerItem a, EnumPickerItem b)
    {
        var result = 0;
        Comparison<Enum>? sort = this.sortSnapshot;
        if (sort is not null)
        {
            result = sort(((EnumPickerItem<T>)a).Value, ((EnumPickerItem<T>)b).Value);
        }

        if (result != 0)
        {
            return result;
        }

        SortOrder order = this.alphabeticalOrderSnapshot;
        if (order == SortOrder.Ascending)
        {
            return string.Compare(a.Text, b.Text, StringComparison.InvariantCultureIgnoreCase);
        }

        if (order == SortOrder.Descending)
        {
            return string.Compare(b.Text, a.Text, StringComparison.InvariantCultureIgnoreCase);
        }

        return 0;
    }

    // Devirtualized enum equality. Enums in C# can use any integral underlying type
    // (byte/sbyte/short/ushort/int/uint/long/ulong), so we dispatch on size which the
    // JIT folds to a constant for a given T, eliminating the branches entirely.
    //
    // The Unsafe.SizeOf<T>() calls are JIT intrinsics that resolve to a constant per
    // generic instantiation. Combined with AggressiveInlining, the dead branches are
    // dropped at codegen time — at runtime, EnumPicker<MyIntEnum>.EnumEquals reduces
    // to a single 32-bit compare with no size check, no jump table, no branches.
    // Do NOT "optimize" this further by caching the size in a static field or routing
    // through a delegate: both add an indirection the JIT cannot fold away, making
    // them strictly slower than the current shape.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool EnumEquals(T a, T b)
    {
        if (Unsafe.SizeOf<T>() == 1)
        {
            return Unsafe.As<T, byte>(ref a) == Unsafe.As<T, byte>(ref b);
        }
        if (Unsafe.SizeOf<T>() == 2)
        {
            return Unsafe.As<T, ushort>(ref a) == Unsafe.As<T, ushort>(ref b);
        }
        if (Unsafe.SizeOf<T>() == 4)
        {
            return Unsafe.As<T, uint>(ref a) == Unsafe.As<T, uint>(ref b);
        }
        if (Unsafe.SizeOf<T>() == 8)
        {
            return Unsafe.As<T, ulong>(ref a) == Unsafe.As<T, ulong>(ref b);
        }
        return EqualityComparer<T>.Default.Equals(a, b);
    }

    private void UpdateIncludedValues(object? sender, NotifyCollectionChangedEventArgs e)
    {
        this.includedSetDirty = true;
        this.UpdateValues();
    }

    private void UpdateExcludedValues(object? sender, NotifyCollectionChangedEventArgs e)
    {
        this.excludedSetDirty = true;
        this.UpdateValues();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        this.isTemplateSet = true;
        this.UpdateValues();
    }

    protected override void OnInitialized()
    {
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
            if (!((ILogical)this).IsAttachedToLogicalTree)
            {
                // Transient null fired by InvalidateStyles during logical-tree detach.
                // Don't propagate — it would write default(T) back to the bound VM.
                return;
            }

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

internal sealed class EnumPickerItem<T> : EnumPickerItem where T : struct, Enum
{
    public required T Value { get; init; }
}