// ReSharper disable UnusedMember.Global

namespace Devolutions.AvaloniaControls.Controls;

using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
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
using Avalonia.Metadata;
using Avalonia.VisualTree;
using Helpers;

[TemplatePart("PART_InnerTextBox", typeof(InnerComboBox), IsRequired = true)]
[TemplatePart("PART_InnerComboBox", typeof(InnerComboBox), IsRequired = true)]
[PseudoClasses(PC_DROPDOWN_OPEN, PC_PRESSED)]
[RequiresUnreferencedCode("BindingEvaluator require preserved types")]
[RequiresDynamicCode("BindingEvaluator require preserved types")]
public partial class EditableComboBox : SelectingItemsControl, IInputElement
{
    public const string PC_DROPDOWN_OPEN = ":dropdownopen";

    public const string PC_PRESSED = ":pressed";

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

    public static readonly StyledProperty<string?> PlaceholderTextProperty =
        TextBox.PlaceholderTextProperty.AddOwner<EditableComboBox>();

    [Obsolete("Use PlaceholderTextProperty instead.")]
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
        AvaloniaProperty.Register<EditableComboBox, double>(nameof(MaxDropDownHeight), 504);

    public static readonly StyledProperty<EditableComboBoxMode> ModeProperty =
        AvaloniaProperty.Register<EditableComboBox, EditableComboBoxMode>(nameof(Mode));

    public static readonly StyledProperty<bool> AllowCustomValueProperty =
        AvaloniaProperty.Register<EditableComboBox, bool>(nameof(AllowCustomValue), true);

    public static readonly StyledProperty<Func<object, string>?> GroupSelectorProperty =
        AvaloniaProperty.Register<EditableComboBox, Func<object, string>?>("GroupSelector");

    public static readonly StyledProperty<BindingBase?> GroupBindingProperty =
        AvaloniaProperty.Register<EditableComboBox, BindingBase?>(nameof(GroupBinding));

    public static readonly StyledProperty<bool> GroupOrderAlphabeticalProperty =
        AvaloniaProperty.Register<EditableComboBox, bool>(nameof(GroupOrderAlphabetical));

    public static readonly StyledProperty<Func<string, int>?> GroupOrderSelectorProperty =
        AvaloniaProperty.Register<EditableComboBox, Func<string, int>?>("GroupOrderSelector");

    public static readonly StyledProperty<BindingBase?> GroupOrderBindingProperty =
        AvaloniaProperty.Register<EditableComboBox, BindingBase?>(nameof(GroupOrderBinding));

    public static readonly StyledProperty<IBrush?> HeaderForegroundProperty =
        AvaloniaProperty.Register<EditableComboBox, IBrush?>(nameof(HeaderForeground));

    public static readonly StyledProperty<Thickness> HeaderMarginProperty =
        AvaloniaProperty.Register<EditableComboBox, Thickness>(nameof(HeaderMargin), new Thickness(4, 6, 8, 4));

    public static readonly StyledProperty<IDataTemplate?> HeaderTemplateProperty =
        AvaloniaProperty.Register<EditableComboBox, IDataTemplate?>(nameof(HeaderTemplate));

    /// <summary>
    /// Display name to use for the empty/null group key. When <c>null</c>, items in the empty
    /// group render without a header (e.g. a "current" item shown at the top of the drop-down).
    /// </summary>
    public static readonly StyledProperty<string?> EmptyGroupNameProperty =
        AvaloniaProperty.Register<EditableComboBox, string?>(nameof(EmptyGroupName));

    private CompositeDisposable? compositeDisposable;

    private readonly AvaloniaList<object?> filteredItems = new();

    private bool syncingSelectedItemFromInnerCombo;

    private readonly InnerComboBox innerComboBox;

    private readonly InnerTextBox innerTextBox;

    private static readonly object nullSourceKey = new();

    private Dictionary<object, string> realizedItems = new();

    private BindingEvaluator? bindingEvaluator = null;
    private Func<object, string>? selectedValueEvaluator = null;
    private bool selectedValueEvaluatorDirty = true;

    static EditableComboBox()
    {
        ItemsControl.FocusableProperty.Unregister(typeof(EditableComboBox));
        ItemsControl.IsTabStopProperty.Unregister(typeof(EditableComboBox));

        InputElement.FocusableProperty.OverrideDefaultValue<EditableComboBox>(true);

        GroupSelectorProperty.Changed.AddClassHandler<EditableComboBox>((x, _) => x.FillItems());
        GroupBindingProperty.Changed.AddClassHandler<EditableComboBox>((x, _) => x.FillItems());
        GroupOrderAlphabeticalProperty.Changed.AddClassHandler<EditableComboBox>((x, _) => x.FillItems());
        GroupOrderSelectorProperty.Changed.AddClassHandler<EditableComboBox>((x, _) => x.FillItems());
        GroupOrderBindingProperty.Changed.AddClassHandler<EditableComboBox>((x, _) => x.FillItems());
        EmptyGroupNameProperty.Changed.AddClassHandler<EditableComboBox>((x, _) => x.FillItems());
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
        this.GetObservable(PlaceholderTextProperty).Subscribe(value => this.innerTextBox.PlaceholderText = value);
#pragma warning disable CS0618 // Watermark is kept for back-compat; forward to PlaceholderText
        this.GetObservable(WatermarkProperty).Subscribe(value => this.PlaceholderText = value);
#pragma warning restore CS0618

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

    [Obsolete("Use GroupBinding instead for XAML-friendly, type-checked group selection.")]
    public Func<object, string>? GroupSelector
    {
        get => this.GetValue(GroupSelectorProperty);
        set => this.SetValue(GroupSelectorProperty, value);
    }

    [AssignBinding]
    [InheritDataTypeFromItems(nameof(ItemsSource))]
    public BindingBase? GroupBinding
    {
        get => this.GetValue(GroupBindingProperty);
        set => this.SetValue(GroupBindingProperty, value);
    }

    public bool GroupOrderAlphabetical
    {
        get => this.GetValue(GroupOrderAlphabeticalProperty);
        set => this.SetValue(GroupOrderAlphabeticalProperty, value);
    }

    public Func<string, int>? GroupOrderSelector
    {
        get => this.GetValue(GroupOrderSelectorProperty);
        set => this.SetValue(GroupOrderSelectorProperty, value);
    }

    /// <summary>
    /// Binding used to order the groups. It is evaluated against a single representative item of each group,
    /// so the bound value must be consistent for every item in the same group — i.e. a function of the same
    /// grouping defined by <see cref="GroupBinding"/>/<see cref="GroupSelector"/> (for example, bind to a
    /// property of the shared group object). The raw value is used for comparison, so a numeric property
    /// sorts numerically.
    /// </summary>
    [AssignBinding]
    [InheritDataTypeFromItems(nameof(ItemsSource))]
    public BindingBase? GroupOrderBinding
    {
        get => this.GetValue(GroupOrderBindingProperty);
        set => this.SetValue(GroupOrderBindingProperty, value);
    }

    public IBrush? HeaderForeground
    {
        get => this.GetValue(HeaderForegroundProperty);
        set => this.SetValue(HeaderForegroundProperty, value);
    }

    public Thickness HeaderMargin
    {
        get => this.GetValue(HeaderMarginProperty);
        set => this.SetValue(HeaderMarginProperty, value);
    }

    public IDataTemplate? HeaderTemplate
    {
        get => this.GetValue(HeaderTemplateProperty);
        set => this.SetValue(HeaderTemplateProperty, value);
    }

    public string? EmptyGroupName
    {
        get => this.GetValue(EmptyGroupNameProperty);
        set => this.SetValue(EmptyGroupNameProperty, value);
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

    public string? PlaceholderText
    {
        get => this.GetValue(PlaceholderTextProperty);
        set => this.SetValue(PlaceholderTextProperty, value);
    }

    [Obsolete("Use PlaceholderText instead.")]
    public string? Watermark
    {
#pragma warning disable CS0618
        get => this.GetValue(WatermarkProperty);
        set => this.SetValue(WatermarkProperty, value);
#pragma warning restore CS0618
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedItemProperty && !this.syncingSelectedItemFromInnerCombo)
        {
            object? newSelectedItem = change.NewValue;
            this.Value = this.realizedItems.GetValueOrDefault(GetSourceKey(newSelectedItem));

            this.SyncCommittedSelectionState();
        }
        else if (change.Property == ItemTemplateProperty)
        {
            this.selectedValueEvaluatorDirty = true;
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
        this.compositeDisposable.Add(this.GetObservable(ItemTemplateProperty).Subscribe(_ =>
        {
            this.selectedValueEvaluatorDirty = true;
            this.FillItems();
        }));
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
        this.innerTextBox.PlaceholderText = this.PlaceholderText;
        return true;
    }

    protected virtual string? CoerceText(string? value) => value;

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        recycleKey = null;
        return false;
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        if (e.Handled) return;

        // When the composite control itself receives focus (e.g. tabbed or clicked into, rather than a
        // specific inner element like a side button), route focus to the text box so the caret lands
        // there — the text box is the control's entry point.
        if (ReferenceEquals(e.Source, this)) this.innerTextBox.Focus();
    }

    protected override void OnInitialized()
    {
        this.innerTextBox.PlaceholderText = this.PlaceholderText;

        this.FillItems();

        // Sync Value from SelectedItem if it was bound before items were realized.
        // OnPropertyChanged fires during XAML binding initialization (before OnInitialized),
        // at which point realizedItems is still empty, causing it to set Value = null.
        // FillItems() above has now populated realizedItems, so we can resolve it here.
        if (this.Value == null && this.SelectedItem != null &&
            this.realizedItems.TryGetValue(GetSourceKey(this.SelectedItem), out string? itemValue))
        {
            this.Value = itemValue;
        }
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
                    // Skip a leading group header so the first arrow-down lands on a selectable item.
                    this.innerComboBox.SelectedIndex = this.NextSelectableIndex(-1, 1);
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

                this.innerTextBox.PlaceholderText = this.PlaceholderText;
                this.IsDropDownOpen = false;

                if (e.Key == Key.Enter)
                {
                    e.Handled = true;
                }
                else if (e.Key == Key.Tab)
                {
                    NavigationDirection? direction = e.Key.ToNavigationDirection(e.KeyModifiers);
                    if (direction is NavigationDirection dir && (dir == NavigationDirection.Previous || dir == NavigationDirection.Next)
                        && this.TryMoveFocusOutsideOnTab(dir, e.KeyModifiers))
                    {
                        e.Handled = true;
                        return;
                    }
                }
            }

            this.Focus();
        }
    }

    /// <summary>
    /// Moves keyboard focus to the next (or previous) focusable control that lies outside this
    /// EditableComboBox, using Avalonia's tree-wide tab traversal. Unlike walking a parent Grid's direct
    /// children, this works regardless of how the control is nested (e.g. wrapped in a StackPanel). The
    /// control's own inner content is navigated separately (in InnerComboBox) before this is reached.
    /// </summary>
    private bool TryMoveFocusOutsideOnTab(NavigationDirection direction, KeyModifiers keyModifiers)
    {
        if (TopLevel.GetTopLevel(this) is not Visual root)
        {
            return false;
        }

        // Single pass over the window's visual tree in document order (the default tab order). Walking the
        // whole tree (rather than a parent Grid's direct children) means this works no matter how the control
        // is nested, e.g. wrapped in a StackPanel. This control's own subtree (chrome + inner content) is
        // contiguous in that order, so we can find the focusable stop just outside it without materializing
        // the full list. Inner content was already navigated in InnerComboBox.
        bool forward = direction != NavigationDirection.Previous;
        InputElement? beforeSelf = null; // last stop before our subtree — the target when tabbing backward
        bool pastSelf = false;

        foreach (Visual visual in root.GetVisualDescendants())
        {
            if (visual is not InputElement { Focusable: true, IsEffectivelyVisible: true, IsEffectivelyEnabled: true } stop)
            {
                continue;
            }

            if (IsSelfOrDescendant(stop))
            {
                if (!forward)
                {
                    break; // going back: the last stop before our subtree (beforeSelf) is the answer
                }

                pastSelf = true;
                continue;
            }

            if (forward && pastSelf)
            {
                stop.Focus(NavigationMethod.Tab, keyModifiers); // first stop after our subtree
                return true;
            }

            beforeSelf = stop;
        }

        if (!forward && beforeSelf is not null)
        {
            beforeSelf.Focus(NavigationMethod.Tab, keyModifiers);
            return true;
        }

        return false;

        bool IsSelfOrDescendant(Visual v)
        {
            for (Visual? p = v; p is not null; p = p.GetVisualParent())
            {
                if (ReferenceEquals(p, this))
                {
                    return true;
                }
            }

            return false;
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.Handled && e.Source is Visual source)
        {
            if (this.innerComboBox.Popup?.IsInsidePopup(source) == true)
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
            this.PseudoClasses.Set(PC_PRESSED, true);
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (!e.Handled && e.Source is Visual source)
        {
            if (this.innerComboBox.Popup?.IsInsidePopup(source) == true)
            {
                // A release on a non-selectable group header is a no-op: swallow it but keep the
                // drop-down open (matches GroupedComboBox). A release on a real item closes as usual.
                // Headers are disabled and therefore transparent to hit-testing, so we can't rely on
                // e.Source — test the realized header containers' bounds against the pointer instead.
                if (!this.IsPointerOverHeader(e))
                {
                    this.innerComboBox.Popup.Close();
                }

                e.Handled = true;
            }
            else if (this.PseudoClasses.Contains(PC_PRESSED))
            {
                bool newIsOpen = !this.IsDropDownOpen;
                this.IsDropDownOpen = newIsOpen;
                e.Handled = true;
            }
        }

        this.PseudoClasses.Set(PC_PRESSED, false);
        base.OnPointerReleased(e);
    }

    private static string? CoerceText(AvaloniaObject sender, string? value) =>
        ((EditableComboBox)sender).CoerceText(value);

    private void UpdateBindingEvaluators(BindingBase binding)
    {
        if (this.selectedValueEvaluatorDirty)
        {
            this.bindingEvaluator ??= BindingEvaluator.FromItemsControl(this);
            this.selectedValueEvaluator = this.bindingEvaluator?.BuildFormattedGetter(binding);
            
            this.selectedValueEvaluatorDirty = false;
        }
    }


    private void FillItems(bool filter = false)
    {
        if (!this.IsInitialized) return;

        if ((this.ItemTemplate as EditableComboBoxDataTemplate)?.SelectedItemValue is { } binding)
        {
            this.UpdateBindingEvaluators(binding);
        }

        // Build a lightweight value-string lookup. Raw source objects go directly into filteredItems
        // so that InnerComboBox.NeedsContainerOverride returns true for them, enabling VSP virtualization.
        // Containers are created on-demand by PrepareContainerForItemOverride instead of all upfront.
        this.realizedItems = new Dictionary<object, string>(this.ItemsView.Count);
        for (int i = 0; i < this.ItemsView.Count; ++i)
        {
            object? item = this.ItemsView[i];

            string value;
            if (item is null)
            {
                value = string.Empty;
            }
            else if (item is EditableComboBoxItem comboBoxItem)
            {
                value = comboBoxItem.Value;
            }
            else if (this.selectedValueEvaluator is {} selectedValueEvaluator)
            {
                value = selectedValueEvaluator(item);
            }
            else
            {
                value = item.ToString() ?? string.Empty;
            }

            this.realizedItems[GetSourceKey(item)] = value;
        }

        if (filter && this.Mode == EditableComboBoxMode.Filter)
        {
            this.FilterItems();
        }
        else
        {
            this.ApplyDropdownItems(this.ItemsView);
        }

        this.SyncCommittedSelectionState();
    }

    private void FilterItems()
    {
        if (this.Mode != EditableComboBoxMode.Filter) return;

        string trimmedSearch = this.Value?.Trim() ?? string.Empty;
        this.ApplyDropdownItems(
            this.ItemsView
                .Where(item =>
                {
                    string itemValue = this.GetValueForItem(item);
                    return string.IsNullOrEmpty(trimmedSearch) || itemValue.Contains(trimmedSearch, StringComparison.OrdinalIgnoreCase);
                }));
    }

    /// <summary>
    /// Populates <see cref="filteredItems"/> (the drop-down's item source) from the given base items.
    /// When a group selector is configured, group headers are interleaved via <see cref="ComboBoxGroupedItemsBuilder"/>;
    /// otherwise the items are applied as-is (preserving the previous, un-grouped behavior).
    /// </summary>
    private void ApplyDropdownItems(IEnumerable<object?> baseItems)
    {
        this.filteredItems.Clear();

        Func<object, string>? groupSelector = this.ResolveGroupSelector();
        if (groupSelector is null)
        {
            this.filteredItems.AddRange(baseItems);
            return;
        }

        this.filteredItems.AddRange(
            ComboBoxGroupedItemsBuilder.Build(
                baseItems,
                groupSelector,
                this.ResolveGroupOrderSelector(groupSelector),
                this.GroupOrderAlphabetical,
                this.EmptyGroupName));
    }

    private Func<object, string>? ResolveGroupSelector()
    {
        if (this.GetValue(GroupBindingProperty) is { } binding)
        {
            this.bindingEvaluator ??= BindingEvaluator.FromItemsControl(this);
            return this.bindingEvaluator?.BuildFormattedGetter(binding);
        }

        return this.GetValue(GroupSelectorProperty);
    }

    private Func<object, object?>? ResolveGroupOrderSelector(Func<object, string>? groupSelector)
    {
        // GroupOrderBinding reads the sort order off each item (like GroupBinding reads the group key),
        // keeping the raw value so numeric orders sort numerically (10 after 2, not lexically).
        if (this.GetValue(GroupOrderBindingProperty) is { } binding)
        {
            this.bindingEvaluator ??= BindingEvaluator.FromItemsControl(this);
            return this.bindingEvaluator?.BuildRawGetter(binding);
        }

        // GroupOrderSelector maps a group key to an order; adapt it to operate on an item.
        if (this.GetValue(GroupOrderSelectorProperty) is { } orderFn && groupSelector is not null)
        {
            return item => orderFn(groupSelector(item));
        }

        return null;
    }

    /// <summary>
    /// True when the pointer is over a realized group-header container. Group headers are disabled
    /// (so they're transparent to hit-testing and never become <c>e.Source</c>); we test their bounds
    /// directly so a click on a header can be treated as a no-op that keeps the drop-down open.
    /// </summary>
    private bool IsPointerOverHeader(PointerEventArgs e)
    {
        foreach (Control container in this.innerComboBox.GetRealizedContainers())
        {
            if (container is GroupedComboBoxHeaderItem { IsVisible: true } header &&
                new Rect(header.Bounds.Size).Contains(e.GetPosition(header)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Finds the next drop-down entry in <paramref name="direction"/> (+1 / -1) that is a real,
    /// selectable item — skipping group headers (checked against the data list, so it is robust even
    /// before containers are realized) and any realized-but-disabled item containers. Returns -1 if none.
    /// </summary>
    private int NextSelectableIndex(int startIndex, int direction)
    {
        for (int i = startIndex + direction; i >= 0 && i < this.filteredItems.Count; i += direction)
        {
            if (this.filteredItems[i] is ComboBoxGroupHeader)
            {
                continue;
            }

            if (this.innerComboBox.ContainerFromIndex(i) is { IsEffectivelyEnabled: false })
            {
                continue;
            }

            return i;
        }

        return -1;
    }

    private string? GetSelectedItemValue()
    {
        object? selected = this.innerComboBox.SelectedItem;
        if (selected is null || selected is ComboBoxGroupHeader)
        {
            return null;
        }

        return this.realizedItems.TryGetValue(GetSourceKey(selected), out string? value) ? value : selected.ToString();
    }

    private static object GetSourceKey(object? item) => item ?? nullSourceKey;

    private string GetValueForItem(object? item)
    {
        // Group headers are non-selectable pseudo-items; they never map to a value.
        if (item is ComboBoxGroupHeader)
        {
            return string.Empty;
        }

        if (item is EditableComboBoxItem comboBoxItem)
        {
            return comboBoxItem.Value;
        }

        return this.realizedItems.TryGetValue(GetSourceKey(item), out string? value) ? value : item?.ToString() ?? string.Empty;
    }

    private void HighlightNextItem()
    {
        int next = this.NextSelectableIndex(this.innerComboBox.SelectedIndex, 1);
        if (next >= 0)
        {
            this.innerComboBox.SelectedIndex = next;
        }

        if (this.Mode == EditableComboBoxMode.Immediate) this.innerTextBox.SelectAll();
    }

    private void HighlightPreviousItem()
    {
        int previous = this.NextSelectableIndex(this.innerComboBox.SelectedIndex, -1);
        if (previous >= 0)
        {
            this.innerComboBox.SelectedIndex = previous;
        }

        if (this.Mode == EditableComboBoxMode.Immediate) this.innerTextBox.SelectAll();
    }

    private void OnCloseMenu()
    {
        this.RevertToSelectedItemIfNeeded();
        this.innerTextBox.PlaceholderText = this.PlaceholderText;
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
        this.FillItems();
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
        this.innerTextBox.PlaceholderText = this.innerComboBox.SelectedIndex >= 0 ? this.GetSelectedItemValue() : this.PlaceholderText;

        if (this.Mode == EditableComboBoxMode.Immediate && this.innerComboBox.SelectedIndex >= 0)
        {
            this.UpdateSelection();
            this.innerTextBox.PlaceholderText = this.PlaceholderText;
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
            this.Value = this.realizedItems.GetValueOrDefault(GetSourceKey(this.SelectedItem));
        }
    }

    private void SelectItemFromText()
    {
        if (string.IsNullOrEmpty(this.Value))
        {
            this.innerComboBox.SelectedIndex = -1;
            this.innerTextBox.PlaceholderText = this.PlaceholderText;
            return;
        }

        foreach (object? item in this.innerComboBox.ItemsView)
        {
            if (item is ComboBoxGroupHeader) continue;

            if (this.GetValueForItem(item) == this.Value)
            {
                this.innerComboBox.SelectedItem = item;
                return;
            }
        }

        this.innerComboBox.SelectedIndex = -1;
        this.innerTextBox.PlaceholderText = this.PlaceholderText;
    }

    private void UpdateSelection()
    {
        object? selected = this.innerComboBox.SelectedIndex >= 0 ? this.innerComboBox.SelectedItem : null;

        // Never commit a group header pseudo-item as the selection; leave the prior selection intact.
        if (selected is ComboBoxGroupHeader)
        {
            return;
        }

        this.syncingSelectedItemFromInnerCombo = true;
        try
        {
            this.SelectedItem = selected;
            this.SyncCommittedSelectionState();
        }
        finally
        {
            this.syncingSelectedItemFromInnerCombo = false;
        }

        this.Value = this.GetSelectedItemValue();
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
