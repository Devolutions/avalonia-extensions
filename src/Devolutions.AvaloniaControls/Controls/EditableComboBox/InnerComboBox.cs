namespace Devolutions.AvaloniaControls.Controls;

using System.Collections;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.VisualTree;

// ReSharper disable MemberHidesStaticFromOuterClass
public partial class EditableComboBox
{
    [TemplatePart("PART_TextBoxPresenter", typeof(ContentPresenter), IsRequired = true)]
    [TemplatePart("PART_Popup", typeof(Popup), IsRequired = true)]
    [TemplatePart("PART_InnerLeftContent", typeof(ItemsControl), IsRequired = true)]
    [TemplatePart("PART_InnerRightContent", typeof(ItemsControl), IsRequired = true)]
    [PseudoClasses(":dropdown-open-from-top", ":dropdown-overflow-left", ":dropdown-overflow-right", ":is-split-between-screens", ":is-outside-screens-boundaries")]
    public class InnerComboBox : ComboBox, INavigableContainer
    {
        public static readonly StyledProperty<Thickness> FocusedBorderThicknessProperty = AvaloniaProperty.Register<InnerComboBox, Thickness>(
            nameof(FocusedBorderThickness),
            Application.Current?.FindResource("TextControlBorderThemeThicknessFocused") as Thickness? ?? new Thickness(2));

        public static readonly DirectProperty<InnerComboBox, IEnumerable> InnerLeftContentProperty =
            AvaloniaProperty.RegisterDirect<InnerComboBox, IEnumerable>(
                nameof(InnerLeftContent),
                static o => o.InnerLeftContent,
                static (o, v) => o.InnerLeftContent = v);

        public static readonly DirectProperty<InnerComboBox, IEnumerable> InnerRightContentProperty =
            AvaloniaProperty.RegisterDirect<InnerComboBox, IEnumerable>(
                nameof(InnerRightContent),
                static o => o.InnerRightContent,
                static (o, v) => o.InnerRightContent = v);

        public static readonly StyledProperty<double> MaxDropDownWidthProperty =
            AvaloniaProperty.Register<InnerComboBox, double>(nameof(MaxDropDownWidth));

        public static readonly StyledProperty<bool> ValueFilterDropdownProperty =
            AvaloniaProperty.Register<InnerComboBox, bool>(nameof(ValueFilterDropdown));

        private readonly EditableComboBox parent;

        private ItemsControl? innerLeftContentControl;

        private ItemsControl? innerRightContentControl;

        private ContentPresenter? textboxContentPresenter;

        public InnerComboBox(EditableComboBox parent, InnerTextBox innerTextBox)
        {
            this.parent = parent;
            this._innerTextBox = innerTextBox;
        }

        public InnerTextBox _innerTextBox { get; init; }

        public Popup? _popup { get; private set; }

        public Thickness FocusedBorderThickness
        {
            get => this.GetValue(FocusedBorderThicknessProperty);
            set => this.SetValue(FocusedBorderThicknessProperty, value);
        }

        public IEnumerable InnerLeftContent { get; set; } = new AvaloniaList<Control>();

        public IEnumerable InnerRightContent { get; set; } = new AvaloniaList<Control>();

        public double MaxDropDownWidth
        {
            get => this.GetValue(MaxDropDownWidthProperty);
            set => this.SetValue(MaxDropDownWidthProperty, value);
        }

        public bool ValueFilterDropdown
        {
            get => this.GetValue(ValueFilterDropdownProperty);
            set => this.SetValue(ValueFilterDropdownProperty, value);
        }

        public IInputElement? GetControl(NavigationDirection direction, IInputElement? from, bool wrap)
        {
            return direction switch
            {
                NavigationDirection.Previous => this.GetPreviousControl(from),
                NavigationDirection.Next => this.GetNextControl(from),
                _ => null,
            };
        }

        protected override void ClearContainerForItemOverride(Control element)
        {
            base.ClearContainerForItemOverride(element);
            if (element is EditableComboBoxItem editableComboBoxItem) editableComboBoxItem.Value = string.Empty;
        }

        protected override void ContainerForItemPreparedOverride(Control container, object? item, int index)
        {
            base.ContainerForItemPreparedOverride(container, item, index);
            if (container is EditableComboBoxItem editableComboBoxItem)
            {
                if (this.ValueFilterDropdown)
                {
                    editableComboBoxItem.Bind(EditableComboBoxItem.FilterHighlightTextProperty,
                        this.parent.GetObservable((AvaloniaProperty)ValueProperty).ToBinding());
                }
                else
                {
                    editableComboBoxItem.ClearValue(EditableComboBoxItem.FilterHighlightTextProperty);
                    editableComboBoxItem.FilterHighlightText = null;
                }
            }
        }

        protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) =>
            new EditableComboBoxItem
            {
                Value = item?.ToString() ?? string.Empty,
            };

        protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
            this.NeedsContainer<EditableComboBoxItem>(item, out recycleKey);

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            this.innerLeftContentControl = e.NameScope.Get<ItemsControl>("PART_InnerLeftContent");
            this.innerRightContentControl = e.NameScope.Get<ItemsControl>("PART_InnerRightContent");

            this.textboxContentPresenter = e.NameScope.Get<ContentPresenter>("PART_TextBoxPresenter");
            this.textboxContentPresenter.Content = this._innerTextBox;

            this._popup = e.NameScope.Get<Popup>("PART_Popup");
            this._popup.Focusable = false;
            this._popup.IsTabStop = false;
            this._popup.PlacementTarget = this.parent;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (this.IsDropDownOpen) return;

            // TODO: Respect/implement all `KeyboardNavigation.TabNavigation` variants;
            //       for now, we only specifically handle `Continue`, otherwise we don't handle the event and bubble up.
            //       - sbergerondrouin 2025-05-21
            NavigationDirection? direction = e.Key.ToNavigationDirection(e.KeyModifiers);
            if (direction is NavigationDirection dir && (dir == NavigationDirection.Previous || dir == NavigationDirection.Next))
            {
                TopLevel topLevel = TopLevel.GetTopLevel(this)!;
                IFocusManager? focus = topLevel.FocusManager;
                IInputElement? focused = focus?.GetFocusedElement();

                IInputElement? nextControl = GetNextControl(this, dir, focused, false);
                if (nextControl is not null)
                {
                    e.Handled = true;
                    nextControl.Focus(NavigationMethod.Tab, e.KeyModifiers);
                    return;
                }

                Visual? containerChild = this.parent;
                while (containerChild.FindAncestorOfType<INavigableContainer>() is { } container &&
                       (containerChild = container as Visual) != null)
                {
                    nextControl = GetNextControl(container, dir, this.parent, false);
                    if (nextControl is not null)
                    {
                        e.Handled = true;
                        nextControl.Focus(NavigationMethod.Tab, e.KeyModifiers);
                        return;
                    }
                }

                while (containerChild?.FindAncestorOfType<Grid>() is { } grid)
                {
                    int index = -1;
                    if (containerChild is Control control) index = grid.Children.IndexOf(control);

                    int increment = dir == NavigationDirection.Previous ? -1 : 1;
                    index += increment;
                    for (; 0 <= index && index < grid.Children.Count; index += increment)
                    {
                        nextControl = grid.Children[index];
                        if (nextControl is { Focusable: true, IsEffectivelyVisible: true, IsEffectivelyEnabled: true })
                        {
                            e.Handled = true;
                            nextControl.Focus(NavigationMethod.Tab, e.KeyModifiers);
                            return;
                        }
                    }

                    containerChild = grid;
                }
            }

            // Passthrough the rest instead of base, to handle on EditableComboBox instead
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            // Pointer interactions handled by EditableComboBox
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            // Pointer interactions handled by EditableComboBox
        }

        protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
        {
            base.PrepareContainerForItemOverride(container, item, index);
            if (container != item)
            {
                if (container is EditableComboBoxItem editableComboBoxItem)
                    // FIXME: Verify if we shouldn't maybe bind instead ?
                {
                    editableComboBoxItem.Value = (item as EditableComboBoxItem)?.Value ?? item?.ToString() ?? string.Empty;
                }
            }
        }

        private static bool IsSplitBetweenScreen(Visual visual)
        {
            if (TopLevel.GetTopLevel(visual) is not { } topLevel) return false;

            PixelPoint topLeftPoint = visual.PointToScreen(new Point(0, 0));
            PixelPoint bottomRightPoint = visual.PointToScreen(new Point(visual.Bounds.Width, visual.Bounds.Height));
            Screen? screen1 = topLevel.Screens?.ScreenFromPoint(topLeftPoint);
            Screen? screen2 = topLevel.Screens?.ScreenFromPoint(bottomRightPoint);
            return screen1 is not null && screen2 is not null && screen1 != screen2;
        }

        private IInputElement? GetNextControl(IInputElement? from)
        {
            bool foundCurrent = Equals(from, this._innerTextBox);

            if (this.innerLeftContentControl != null)
            {
                foreach (object? item in this.innerLeftContentControl.Items)
                {
                    if (!foundCurrent)
                    {
                        foundCurrent = Equals(from, item);
                        continue;
                    }

                    Control? itemControl = item != null ? this.innerLeftContentControl.ContainerFromItem(item) : null;
                    if (itemControl?.Focusable == true && itemControl.IsEnabled) return itemControl;
                }
            }

            if (this.innerRightContentControl != null)
            {
                foreach (object? item in this.innerRightContentControl.Items)
                {
                    if (!foundCurrent)
                    {
                        foundCurrent = Equals(from, item);
                        continue;
                    }

                    Control? itemControl = item != null ? this.innerRightContentControl.ContainerFromItem(item) : null;
                    if (itemControl?.Focusable == true && itemControl.IsEnabled) return itemControl;
                }
            }

            return null;
        }

        private IInputElement? GetPreviousControl(IInputElement? from)
        {
            if (Equals(from, this._innerTextBox) || Equals(from, this) || Equals(from, this.parent)) return null;

            bool foundCurrent = false;

            if (this.innerRightContentControl != null)
            {
                foreach (object? item in this.innerRightContentControl.Items.Reverse())
                {
                    if (!foundCurrent)
                    {
                        foundCurrent = Equals(from, item);
                        continue;
                    }

                    Control? itemControl = item != null ? this.innerRightContentControl.ContainerFromItem(item) : null;
                    if (itemControl?.Focusable == true && itemControl.IsEnabled) return itemControl;
                }
            }

            if (this.innerLeftContentControl != null)
            {
                foreach (object? item in this.innerLeftContentControl.Items.Reverse())
                {
                    if (!foundCurrent)
                    {
                        foundCurrent = Equals(from, item);
                        continue;
                    }

                    Control? itemControl = item != null ? this.innerLeftContentControl.ContainerFromItem(item) : null;
                    if (itemControl?.Focusable == true && itemControl.IsEnabled) return itemControl;
                }
            }

            if (foundCurrent) return this._innerTextBox;

            return null;
        }
    }
}

// ReSharper enable MemberHidesStaticFromOuterClass