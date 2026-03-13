namespace Devolutions.AvaloniaControls.Controls;

using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

public abstract class EnumPicker : TemplatedControl
{
    public static readonly StyledProperty<EnumPickerItem?> SelectedItemProperty = AvaloniaProperty.Register<EnumPicker, EnumPickerItem?>(
        nameof(SelectedItem),
        defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IReadOnlyList<EnumPickerItem>> ItemsProperty =
        AvaloniaProperty.Register<EnumPicker, IReadOnlyList<EnumPickerItem>>(
            nameof(Items),
            defaultBindingMode: BindingMode.OneWay,
            defaultValue: []);

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
}

public class EnumPicker<T> : EnumPicker where T : struct, Enum
{
    private readonly IReadOnlyCollection<T> allEnumValues = Enum.GetValues<T>();

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

    // ReSharper disable once StaticMemberInGenericType
    public static readonly StyledProperty<bool> SortAlphabeticallyProperty = AvaloniaProperty.Register<EnumPicker, bool>(
        nameof(SortAlphabetically),
        defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IReadOnlyDictionary<T, string>?> TextOverridesProperty =
        AvaloniaProperty.Register<EnumPicker, IReadOnlyDictionary<T, string>?>(
            nameof(TextOverrides),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<Func<T, string>?> TextProviderProperty = AvaloniaProperty.Register<EnumPicker, Func<T, string>?>(
        nameof(TextProvider),
        defaultBindingMode: BindingMode.TwoWay);

    protected override Type StyleKeyOverride => typeof(EnumPicker);

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
        set
        {
            this.SelectedItem = this.Items.FirstOrDefault(val => val.EnumValue.Equals(value)) ?? this.Items.FirstOrDefault();
            this.SetValue(SelectedValueProperty, (T?)this.SelectedItem?.EnumValue);
        }
    }
    
    /// <summary>
    ///  Gets or sets whether values get sorted alphabetically by their texts (see <see cref="TextOverrides"/> and <see cref="TextProvider"/>)
    /// </summary>
    public bool SortAlphabetically
    {
        get => this.GetValue(SortAlphabeticallyProperty);
        set => this.SetValue(SortAlphabeticallyProperty, value);
    }

    /// <summary>
    ///  Gets or sets a dictionary that matches enum value to their text, values in this dictionary override <see cref="TextProvider"/>
    /// </summary>
    public IReadOnlyDictionary<T, string>? TextOverrides
    {
        get => this.GetValue(TextOverridesProperty);
        set => this.SetValue(TextOverridesProperty, value);
    }

    /// <summary>
    /// Gets or sets the function that provides text for enum values, overriden on values in <see cref="TextOverrides"/>
    /// </summary>
    public Func<T, string>? TextProvider
    {
        get => this.GetValue(TextProviderProperty);
        set => this.SetValue(TextProviderProperty, value);
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

        IEnumerable<T>? values = (this.IncludedValues ?? this.allEnumValues).Except(this.ExcludedValues ?? []);
        IEnumerable<EnumPickerItem>? items = values.Select(enumValue => new EnumPickerItem { EnumValue = enumValue, Text = this.GetEnumText(enumValue) });

        if (this.SortAlphabetically)
        {
            items = items.OrderBy(val => val.Text, StringComparer.InvariantCultureIgnoreCase);
        }
        
        this.Items = items.ToList();
        this.SelectedValue = selectedValue;
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
            change.Property == SortAlphabeticallyProperty ||
            change.Property == TextOverridesProperty)
        {
            this.UpdateValues();
        }
        else if (change.Property == SelectedItemProperty)
        {
            this.SetValue(SelectedValueProperty, (T?)change.GetNewValue<EnumPickerItem?>()?.EnumValue);
        }
    }
}

public class EnumPickerItem
{
    public required object EnumValue { get; init; }

    public required string Text { get; init; }
}