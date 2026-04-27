// ReSharper disable UnusedMember.Global

namespace Devolutions.AvaloniaControls.Controls;

using System.Collections;
using System.Collections.Specialized;
using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Extensions;

[TemplatePart("PART_InnerTextBox", typeof(InnerComboBox), IsRequired = true)]
[TemplatePart("PART_InnerComboBox", typeof(InnerComboBox), IsRequired = true)]
[PseudoClasses(PC_DropdownOpen, PC_Pressed)]
public partial class EditableComboBox : SelectingItemsControl, IInputElement
{
    public const string PC_DropdownOpen = ":dropdownopen";

    public const string PC_Pressed = ":pressed";

    public static readonly StyledProperty<TimeSpan> CaretBlinkIntervalProperty =
        AvaloniaProperty.Register<EditableComboBox, TimeSpan>(nameof(CaretBlinkInterval), TimeSpan.FromMilliseconds(500));

    public static readonly StyledProperty<IBrush?> CaretBrushProperty = AvaloniaProperty.Register<EditableComboBox, IBrush?>(nameof(CaretBrush));

    public static readonly StyledProperty<int> CaretIndexProperty = AvaloniaProperty.Register<EditableComboBox, int>(nameof(CaretIndex));

    public new static readonly StyledProperty<bool> FocusableProperty =
        AvaloniaProperty.Register<EditableComboBox, bool>(nameof(Focusable), true);

    public static readonly StyledProperty<bool> IsDropDownOpenProperty =
        AvaloniaProperty.Register<EditableComboBox, bool>(nameof(IsDropDownOpen));

    public new static readonly StyledProperty<bool> IsTabStopProperty =
        AvaloniaProperty.Register<EditableComboBox, bool>(nameof(IsTabStop), true);

    public static readonly StyledProperty<int> MaxLengthProperty = AvaloniaProperty.Register<EditableComboBox, int>(nameof(MaxLength));

    public static readonly StyledProperty<IBrush?> SelectionBrushProperty =
        AvaloniaProperty.Register<EditableComboBox, IBrush?>(nameof(SelectionBrush));

    public static readonly StyledProperty<int> SelectionEndProperty = AvaloniaProperty.Register<EditableComboBox, int>(nameof(SelectionEnd));

    public static readonly StyledProperty<IBrush?> SelectionForegroundBrushProperty =
        AvaloniaProperty.Register<EditableComboBox, IBrush?>(nameof(SelectionForegroundBrush));

    public static readonly StyledProperty<int> SelectionStartProperty = AvaloniaProperty.Register<EditableComboBox, int>(nameof(SelectionStart));

    public static readonly StyledProperty<string?> WatermarkProperty = AvaloniaProperty.Register<EditableComboBox, string?>(nameof(Watermark));

    public static readonly StyledProperty<string?> ValueProperty = AvaloniaProperty.Register<EditableComboBox, string?>(
        nameof(Value),
        coerce: CoerceText,
        defaultBindingMode: BindingMode.TwoWay,
        enableDataValidation: true);

    public static readonly StyledProperty<bool> ClearOnOpenProperty = AvaloniaProperty.Register<EditableComboBox, bool>(nameof(ClearOnOpen));

    public static readonly StyledProperty<Thickness> FocusedBorderThicknessProperty = AvaloniaProperty.Register<EditableComboBox, Thickness>(
        nameof(FocusedBorderThickness),
        Application.Current?.FindResource("TextControlBorderThemeThicknessFocused") as Thickness? ?? new Thickness(2));

    public static readonly StyledProperty<IBrush?> IconDefaultFillProperty = AvaloniaProperty.Register<EditableComboBox, IBrush?>(
        nameof(IconDefaultFill),
        Application.Current?.FindResource("SvgInputIconFill") as IBrush);

    public static readonly DirectProperty<EditableComboBox, IEnumerable> InnerLeftContentProperty =
        AvaloniaProperty.RegisterDirect<EditableComboBox, IEnumerable>(
            nameof(InnerLeftContent),
            static o => o.InnerLeftContent,
            static (o, v) => o.InnerLeftContent = v);

    public static readonly DirectProperty<EditableComboBox, IEnumerable> InnerLeftOfDropDownArrowContentProperty =
        AvaloniaProperty.RegisterDirect<EditableComboBox, IEnumerable>(
            nameof(InnerLeftOfDropDownArrowContent),
            static o => o.InnerLeftOfDropDownArrowContent,
            static (o, v) => o.InnerLeftOfDropDownArrowContent = v);

    public static readonly DirectProperty<EditableComboBox, IEnumerable> InnerRightContentProperty =
        AvaloniaProperty.RegisterDirect<EditableComboBox, IEnumerable>(
            nameof(InnerRightContent),
            static o => o.InnerRightContent,
            static (o, v) => o.InnerRightContent = v);

    public static readonly StyledProperty<double> MaxDropDownWidthProperty =
        AvaloniaProperty.Register<EditableComboBox, double>(nameof(MaxDropDownWidth), 500);

    public static readonly StyledProperty<double> MaxDropDownHeightProperty =
        AvaloniaProperty.Register<EditableComboBox, double>(nameof(MaxDropDownHeight), 200);

    public static readonly StyledProperty<EditableComboBoxMode> ModeProperty =
        AvaloniaProperty.Register<EditableComboBox, EditableComboBoxMode>(nameof(Mode));

    public static readonly StyledProperty<bool> AllowCustomValueProperty =
        AvaloniaProperty.Register<EditableComboBox, bool>(nameof(AllowCustomValue), true);

    private CompositeDisposable? compositeDisposable;

    private readonly AvaloniaList<object?> filteredItems = new();

    private bool syncingSelectedItemFromInnerCombo;

    private readonly InnerComboBox innerComboBox;

    private readonly InnerTextBox innerTextBox;

    private static readonly object NullSourceKey = new();

    private Dictionary<object, string> realizedItems = new();

    static EditableComboBox()
    {
        ItemsControl.FocusableProperty.Unregister(typeof(EditableComboBox));
        ItemsControl.IsTabStopProperty.Unregister(typeof(EditableComboBox));

        InputElement.FocusableProperty.OverrideDefaultValue<EditableComboBox>(true);
    }

    public EditableComboBox()
    {
        this.innerTextBox = new InnerTextBox
        {
            Name = "PART_InnerTextBox",
            Focusable = true,

            [!!TextBox.TextProperty] = this[!!ValueProperty],
            [!!TextBox.CaretIndexProperty] = this[!!CaretIndexProperty],
            [!!TextBox.SelectionStartProperty] = this[!!SelectionStartProperty],
            [!!TextBox.SelectionEndProperty] = this[!!SelectionEndProperty],
            [!TextBox.CaretBlinkIntervalProperty] = this[!CaretBlinkIntervalProperty],
            [!TextBox.SelectionBrushProperty] = this[!SelectionBrushProperty],
            [!TextBox.SelectionForegroundBrushProperty] = this[!SelectionForegroundBrushProperty],
            [!TextBox.CaretBrushProperty] = this[!CaretBrushProperty],
        };

        this.innerTextBox.TextChanged += this.OnTextChanged;
        this.GetObservable(WatermarkProperty).Subscribe(watermark => this.innerTextBox.Watermark = watermark);

        this.innerComboBox = new InnerComboBox(this, this.innerTextBox)
        {
            Name = "PART_InnerComboBox",
            Focusable = false,
            IsTabStop = false,
            AutoScrollToSelectedItem = true,

            ItemsSource = this.filteredItems,

            [!ForegroundProperty] = this[!ForegroundProperty],
            [!BackgroundProperty] = this[!BackgroundProperty],
            [!BorderBrushProperty] = this[!BorderBrushProperty],
            [!BorderThicknessProperty] = this[!BorderThicknessProperty],
            [!CornerRadiusProperty] = this[!CornerRadiusProperty],
            [!PaddingProperty] = this[!PaddingProperty],
            [!MinHeightProperty] = this[!MinHeightProperty],
            [!IsEnabledProperty] = this[!IsEnabledProperty],
            [!InnerComboBox.InnerLeftContentProperty] = this[!InnerLeftContentProperty],
            [!InnerComboBox.InnerLeftOfDropDownArrowContentProperty] = this[!InnerLeftOfDropDownArrowContentProperty],
            [!InnerComboBox.InnerRightContentProperty] = this[!InnerRightContentProperty],

            [!!ComboBox.IsDropDownOpenProperty] = this[!!IsDropDownOpenProperty],
            [!InnerComboBox.FocusedBorderThicknessProperty] = this[!FocusedBorderThicknessProperty],
            [!InnerComboBox.MaxDropDownWidthProperty] = this[!MaxDropDownWidthProperty],
        };

        this.GetObservable(ModeProperty).Subscribe(mode => this.innerComboBox.ValueFilterDropdown = mode == EditableComboBoxMode.Filter);
        this.innerComboBox.SelectionChanged += this.OnSelectionChanged;
        this.innerTextBox.LostFocus += this.OnInnerTextBoxLostFocus;

        this.Template = new FuncControlTemplate((_, namescope) =>
        {
            this.innerTextBox.RegisterInNameScope(namescope);
            this.innerComboBox.RegisterInNameScope(namescope);
            return new DataValidationErrors
            {
                Content = this.innerComboBox,
                ClipToBounds = false,
            };
        });
    }

    public TimeSpan CaretBlinkInterval
    {
        get => this.GetValue(CaretBlinkIntervalProperty);
        set => this.SetValue(CaretBlinkIntervalProperty, value);
    }

    public IBrush? CaretBrush
    {
        get => this.GetValue(CaretBrushProperty);
        set => this.SetValue(CaretBrushProperty, value);
    }

    public int CaretIndex
    {
        get => this.GetValue(CaretIndexProperty);
        set => this.SetValue(CaretIndexProperty, value);
    }

    public bool ClearOnOpen
    {
        get => this.GetValue(ClearOnOpenProperty);
        set => this.SetValue(ClearOnOpenProperty, value);
    }

    public new bool Focusable
    {
        get => this.innerTextBox.Focusable;
        set => this.innerTextBox.Focusable = value;
    }


    public Thickness FocusedBorderThickness
    {
        get => this.GetValue(FocusedBorderThicknessProperty);
        set => this.SetValue(FocusedBorderThicknessProperty, value);
    }

    public IBrush? IconDefaultFill
    {
        get => this.GetValue(IconDefaultFillProperty);
        set => this.SetValue(IconDefaultFillProperty, value);
    }

    public IEnumerable InnerLeftContent { get; set; } = new AvaloniaList<Control>();

    public IEnumerable InnerLeftOfDropDownArrowContent { get; set; } = new AvaloniaList<Control>();

    public IEnumerable InnerRightContent { get; set; } = new AvaloniaList<Control>();

    public bool IsDropDownOpen
    {
        get => this.GetValue(IsDropDownOpenProperty);
        set => this.SetValue(IsDropDownOpenProperty, value);
    }

    public new bool IsTabStop
    {
        get => this.innerTextBox.IsTabStop;
        set => this.innerTextBox.IsTabStop = value;
    }

    public double MaxDropDownHeight
    {
        get => this.GetValue(MaxDropDownHeightProperty);
        set => this.SetValue(MaxDropDownHeightProperty, value);
    }

    public double MaxDropDownWidth
    {
        get => this.GetValue(MaxDropDownWidthProperty);
        set => this.SetValue(MaxDropDownWidthProperty, value);
    }

    public int MaxLength
    {
        get => this.GetValue(MaxLengthProperty);
        set => this.SetValue(MaxLengthProperty, value);
    }

    public EditableComboBoxMode Mode
    {
        get => this.GetValue(ModeProperty);
        set => this.SetValue(ModeProperty, value);
    }

    public bool AllowCustomValue
    {
        get => this.GetValue(AllowCustomValueProperty);
        set => this.SetValue(AllowCustomValueProperty, value);
    }

    public IBrush? SelectionBrush
    {
        get => this.GetValue(SelectionBrushProperty);
        set => this.SetValue(SelectionBrushProperty, value);
    }

    public int SelectionEnd
    {
        get => this.GetValue(SelectionEndProperty);
        set => this.SetValue(SelectionEndProperty, value);
    }

    public IBrush? SelectionForegroundBrush
    {
        get => this.GetValue(SelectionForegroundBrushProperty);
        set => this.SetValue(SelectionForegroundBrushProperty, value);
    }

    public int SelectionStart
    {
        get => this.GetValue(SelectionStartProperty);
        set => this.SetValue(SelectionStartProperty, value);
    }

    public string? Value
    {
        get => this.GetValue(ValueProperty);
        set => this.SetValue(ValueProperty, value);
    }

    public string? Watermark
    {
        get => this.GetValue(WatermarkProperty);
        set => this.SetValue(WatermarkProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedItemProperty && !this.syncingSelectedItemFromInnerCombo)
        {
            object? newSelectedItem = change.NewValue;
            this.Value = this.realizedItems.TryGetValue(GetSourceKey(newSelectedItem), out string? itemValue)
                ? itemValue
                : null;

            this.SyncCommittedSelectionState();
        }
    }

    protected override void UpdateDataValidation(AvaloniaProperty property, BindingValueType state, Exception? error)
    {
        base.UpdateDataValidation(property, state, error);

        if (property == ValueProperty)
        {
            DataValidationErrors.SetError(this, error);
            this.innerComboBox.Classes.Set("error", error != null);
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        this.compositeDisposable = new CompositeDisposable();
        this.Items.CollectionChanged += this.Items_CollectionChanged;
        this.compositeDisposable.Add(this.innerComboBox.GetObservable(ComboBox.IsDropDownOpenProperty).Subscribe(this.OnInnerDropDownOpenChanged));
        this.compositeDisposable.Add(this.GetObservable(ItemsSourceProperty).Subscribe(_ => this.FillItems()));
        this.compositeDisposable.Add(this.GetObservable(ItemTemplateProperty).Subscribe(_ => this.FillItems()));
        this.compositeDisposable.Add(this.GetObservable(ValueProperty).Subscribe(_ => this.FilterItems()));

        // Design-time support: Check if DataValidationErrors.Error is set in the previewer
        if (Design.IsDesignMode)
        {
            this.compositeDisposable.Add(
                this.GetObservable(DataValidationErrors.ErrorsProperty).Subscribe(error =>
                {
                    if (error is null) return;
                    using var enumerator = error.GetEnumerator();
                    this.innerComboBox.Classes.Set("error", enumerator.MoveNext());
                }));
        }
    }

    private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
    {
        this.FillItems(true);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        // This currently won't properly support runtime-change of the Template, but we accept this for now
        this.compositeDisposable?.Dispose();
        this.compositeDisposable = null;
        this.Items.CollectionChanged -= this.Items_CollectionChanged;
    }

    public new bool Focus(NavigationMethod method = NavigationMethod.Unspecified, KeyModifiers keyModifiers = KeyModifiers.None) =>
        this.innerTextBox.Focus(method, keyModifiers);

    internal bool UpdateValueFromItemPointerEvent(EditableComboBoxItem source, PointerEventArgs e)
    {
        this.syncingSelectedItemFromInnerCombo = true;
        try
        {
            this.SelectedItem = source.OriginalSourceItem;
            this.SyncCommittedSelectionState();
        }
        finally
        {
            this.syncingSelectedItemFromInnerCombo = false;
        }

        this.Value = source.Value;
        this.innerTextBox.SelectAll();
        this.innerTextBox.Watermark = this.Watermark;
        return true;
    }

    protected virtual string? CoerceText(string? value) =>
        value;

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        recycleKey = null;
        return false;
    }

    protected override void OnGotFocus(GotFocusEventArgs e)
    {
        if (e.Handled) return;

        if (e.Source is InnerTextBox) this.innerTextBox.Focus();
    }

    protected override void OnInitialized()
    {
        this.innerTextBox.Watermark = this.Watermark;

        this.FillItems();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Handled) return;

        bool isUp = e.Key == Key.Up;
        bool isDown = e.Key == Key.Down;
        bool isImmediate = this.Mode == EditableComboBoxMode.Immediate;

        if (!this.IsDropDownOpen)
        {
            if (isDown || (isImmediate && isUp))
            {
                this.IsDropDownOpen = true;

                if (isImmediate)
                {
                    if (isUp) this.HighlightPreviousItem();

                    if (isDown) this.HighlightNextItem();
                }
                else if (this.innerComboBox.SelectedIndex == -1)
                {
                    this.innerComboBox.SelectedIndex = 0;
                }

                e.Handled = true;
                return;
            }
        }

        if (this.IsDropDownOpen)
        {
            if (e.Key == Key.Escape)
            {
                this.IsDropDownOpen = false;
                e.Handled = true;
            }
            else if (isUp)
            {
                this.HighlightPreviousItem();
                e.Handled = true;
            }
            else if (isDown)
            {
                this.HighlightNextItem();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter || e.Key == Key.Tab)
            {
                if (this.innerComboBox.SelectedIndex >= 0)
                {
                    this.UpdateSelection();
                    this.innerTextBox.SelectAll();
                }

                this.innerTextBox.Watermark = this.Watermark;
                this.IsDropDownOpen = false;

                if (e.Key == Key.Enter)
                {
                    e.Handled = true;
                }
                else if (e.Key == Key.Tab)
                {
                    NavigationDirection? direction = e.Key.ToNavigationDirection(e.KeyModifiers);
                    if (direction is NavigationDirection dir && (dir == NavigationDirection.Previous || dir == NavigationDirection.Next))
                    {
                        Visual? containerChild = this;
                        while (containerChild.FindAncestorOfType<INavigableContainer>() is { } container &&
                               (containerChild = container as Visual) != null)
                        {
                            IInputElement? nextControl = GetNextControl(container, dir, this, false);
                            if (nextControl is not null)
                            {
                                e.Handled = true;
                                nextControl.Focus(NavigationMethod.Tab, e.KeyModifiers);
                                return;
                            }
                        }
                    }
                }
            }

            this.Focus();
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.Handled && e.Source is Visual source)
        {
            if (this.innerComboBox._popup?.IsInsidePopup(source) == true)
            {
                e.Handled = true;
                return;
            }
        }

        if (this.IsDropDownOpen)
        {
            // When a drop-down is open with OverlayDismissEventPassThrough enabled and the control
            // is pressed, close the drop-down
            this.IsDropDownOpen = false;
            e.Handled = true;
        }
        else
        {
            this.PseudoClasses.Set(PC_Pressed, true);
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (!e.Handled && e.Source is Visual source)
        {
            if (this.innerComboBox._popup?.IsInsidePopup(source) == true)
            {
                this.innerComboBox._popup.Close();
                e.Handled = true;
            }
            else if (this.PseudoClasses.Contains(PC_Pressed))
            {
                bool newIsOpen = !this.IsDropDownOpen;
                this.IsDropDownOpen = newIsOpen;
                e.Handled = true;
            }
        }

        this.PseudoClasses.Set(PC_Pressed, false);
        base.OnPointerReleased(e);
    }

    private static string? CoerceText(AvaloniaObject sender, string? value) =>
        ((EditableComboBox)sender).CoerceText(value);


    private void FillItems(bool filter = false)
    {
        if (!this.IsInitialized) return;

        BindingEvaluator? evaluator = this.ItemTemplate is EditableComboBoxDataTemplate { SelectedItemValue: { } binding }
            ? new BindingEvaluator(binding)
            : null;

        // Build a lightweight value-string lookup. Raw source objects go directly into filteredItems
        // so that InnerComboBox.NeedsContainerOverride returns true for them, enabling VSP virtualization.
        // Containers are created on-demand by PrepareContainerForItemOverride instead of all upfront.
        this.realizedItems = new Dictionary<object, string>(this.ItemsView.Count);
        for (int i = 0; i < this.ItemsView.Count; ++i)
        {
            object? item = this.ItemsView[i];
            string value = item is EditableComboBoxItem comboBoxItem
                ? comboBoxItem.Value
                : this.GetItemStringValue(item, evaluator);
            this.realizedItems[GetSourceKey(item)] = value;
        }

        if (filter && this.Mode == EditableComboBoxMode.Filter)
        {
            this.FilterItems();
        }
        else
        {
            this.filteredItems.Clear();
            this.filteredItems.AddRange(this.ItemsView.Select(CloneIfInlineItem));
        }

        this.SyncCommittedSelectionState();
    }

    private void FilterItems()
    {
        if (this.Mode != EditableComboBoxMode.Filter) return;

        string trimmedSearch = this.Value?.Trim() ?? string.Empty;
        this.filteredItems.Clear();
        this.filteredItems.AddRange(
            this.ItemsView
                .Where(item =>
                {
                    string itemValue = this.GetValueForItem(item);
                    return string.IsNullOrEmpty(trimmedSearch) || itemValue.Contains(trimmedSearch, StringComparison.OrdinalIgnoreCase);
                })
                .Select(CloneIfInlineItem));
    }

    private string? GetSelectedItemValue()
    {
        object? selected = this.innerComboBox.SelectedItem;
        if (selected is null)
        {
            return null;
        }

        return this.realizedItems.TryGetValue(GetSourceKey(selected), out string? value) ? value : selected.ToString();
    }

    private static object GetSourceKey(object? item)
    {
        // For cloned EditableComboBoxItem instances, resolve to the original source item
        // so that dictionary lookups match realizedItems keys (which use original items).
        if (item is EditableComboBoxItem { OriginalSourceItem: { } originalItem } && !ReferenceEquals(item, originalItem))
        {
            return originalItem;
        }

        return item ?? NullSourceKey;
    }

    /// <summary>
    /// Inline XAML EditableComboBoxItem items are Visuals that cannot be re-added to a panel
    /// after removal. Clone them so that filtering (clear + re-add) works correctly.
    /// Non-EditableComboBoxItem source objects are passed through unchanged (they get fresh
    /// generated containers via NeedsContainerOverride).
    /// </summary>
    private static object? CloneIfInlineItem(object? item)
    {
        if (item is not EditableComboBoxItem comboBoxItem)
        {
            return item;
        }
        
        EditableComboBoxItem clone = comboBoxItem.Clone();
        clone.OriginalSourceItem ??= comboBoxItem;
        return clone;
    }

    private string GetValueForItem(object? item)
    {
        if (item is EditableComboBoxItem comboBoxItem)
        {
            return comboBoxItem.Value;
        }

        return this.realizedItems.TryGetValue(GetSourceKey(item), out string? value) ? value : item?.ToString() ?? string.Empty;
    }

    private void HighlightNextItem()
    {
        bool isFirst = true;
        while (this.innerComboBox.SelectedIndex < this.filteredItems.Count - 1)
        {
            // NOTE: Setting SelectedIndex to an out-of-bound value will actually result in Avalonia setting the SelectedIndex
            //       to -1, which would break this logic (we would always be < Count -1).
            if (!isFirst && this.innerComboBox.SelectedIndex < 0) return;

            isFirst = false;

            this.innerComboBox.SelectedIndex += 1;
            Control? container = this.innerComboBox.ContainerFromIndex(this.innerComboBox.SelectedIndex);
            if (container?.IsEffectivelyEnabled == true) break;
        }

        if (this.Mode == EditableComboBoxMode.Immediate) this.innerTextBox.SelectAll();
    }

    private void HighlightPreviousItem()
    {
        while (this.innerComboBox.SelectedIndex > 0)
        {
            this.innerComboBox.SelectedIndex -= 1;
            Control? container = this.innerComboBox.ContainerFromIndex(this.innerComboBox.SelectedIndex);
            if (container?.IsEffectivelyEnabled == true) break;
        }

        if (this.Mode == EditableComboBoxMode.Immediate) this.innerTextBox.SelectAll();
    }

    private void OnCloseMenu()
    {
        this.RevertToSelectedItemIfNeeded();
        this.innerTextBox.Watermark = this.Watermark;
    }

    private void OnInnerDropDownOpenChanged(bool value)
    {
        if (!this.IsInitialized) return;

        if (value)
        {
            this.OnOpenMenu();
        }
        else
        {
            this.OnCloseMenu();
        }
    }

    private void OnInnerTextBoxLostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (!this.IsDropDownOpen)
        {
            this.RevertToSelectedItemIfNeeded();
        }
    }

    private void OnOpenMenu()
    {
        if (this.ClearOnOpen)
        {
            this.innerComboBox.SelectedIndex = -1;
        }
        else
        {
            this.SelectItemFromText();
        }

        this.Focus();
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        this.innerTextBox.Watermark = this.innerComboBox.SelectedIndex >= 0 ? this.GetSelectedItemValue() : this.Watermark;

        if (this.Mode == EditableComboBoxMode.Immediate && this.innerComboBox.SelectedIndex >= 0)
        {
            this.UpdateSelection();
            this.innerTextBox.Watermark = this.Watermark;
        }
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        this.SelectItemFromText();
    }

    private void RevertToSelectedItemIfNeeded()
    {
        if (this.AllowCustomValue) return;

        if (this.innerComboBox.SelectedIndex < 0)
        {
            this.Value = this.realizedItems.TryGetValue(GetSourceKey(this.SelectedItem), out string? revertToValue)
                ? revertToValue
                : null;
        }
    }

    private void SelectItemFromText()
    {
        if (string.IsNullOrEmpty(this.Value))
        {
            this.innerComboBox.SelectedIndex = -1;
            this.innerTextBox.Watermark = this.Watermark;
            return;
        }

        foreach (object? item in this.innerComboBox.ItemsView)
        {
            if (this.GetValueForItem(item) == this.Value)
            {
                this.innerComboBox.SelectedItem = item;
                return;
            }
        }

        this.innerComboBox.SelectedIndex = -1;
        this.innerTextBox.Watermark = this.Watermark;
    }

    private void UpdateSelection()
    {
        this.syncingSelectedItemFromInnerCombo = true;
        try
        {
            this.SelectedItem = this.innerComboBox.SelectedIndex >= 0 ? this.innerComboBox.SelectedItem : null;
            this.SyncCommittedSelectionState();
        }
        finally
        {
            this.syncingSelectedItemFromInnerCombo = false;
        }

        this.Value = this.GetSelectedItemValue();
    }

    private string GetItemStringValue(object? item, BindingEvaluator? evaluator)
    {
        if (this.ItemTemplate is EditableComboBoxDataTemplate && evaluator != null)
        {
            return evaluator.Evaluate(item) ?? string.Empty;
        }

        return item?.ToString() ?? string.Empty;
    }

    private sealed class BindingEvaluator : StyledElement
    {
        private static readonly StyledProperty<string?> ResultProperty =
            AvaloniaProperty.Register<BindingEvaluator, string?>(nameof(Result));

        public string? Result
        {
            get => this.GetValue(ResultProperty);
            set => this.SetValue(ResultProperty, value);
        }

        public BindingEvaluator(IBinding binding)
        {
            this.Bind(ResultProperty, binding);
        }

        public string? Evaluate(object? item)
        {
            this.DataContext = item;
            return this.Result;
        }
    }

    private void SyncCommittedSelectionState()
    {
        object selectedKey = GetSourceKey(this.SelectedItem);

        // Only update currently-realized containers. Non-visible ones are set correctly
        // in InnerComboBox.PrepareContainerForItemOverride when scrolled into view.
        for (int i = 0; i < this.filteredItems.Count; i++)
        {
            if (this.innerComboBox.ContainerFromIndex(i) is EditableComboBoxItem comboBoxItem)
            {
                comboBoxItem.IsCommittedSelected = Equals(GetSourceKey(comboBoxItem.OriginalSourceItem), selectedKey);
            }
        }
    }
}