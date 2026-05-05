// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace Devolutions.AvaloniaControls.Controls;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.VisualTree;

public class EditableComboBoxItem : TemplatedControl, ISelectable
{
    public static readonly StyledProperty<string?> FilterHighlightTextProperty =
        AvaloniaProperty.Register<EditableComboBoxItem, string?>(nameof(FilterHighlightText));

    public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty =
        AvaloniaProperty.Register<EditableComboBoxItem, IDataTemplate?>(nameof(ContentTemplate));

    public static readonly StyledProperty<bool> IsSelectedProperty = SelectingItemsControl.IsSelectedProperty.AddOwner<EditableComboBoxItem>();

    public static readonly StyledProperty<bool> IsCommittedSelectedProperty =
        AvaloniaProperty.Register<EditableComboBoxItem, bool>(nameof(IsCommittedSelected));

    public static readonly StyledProperty<string> ValueProperty =
        AvaloniaProperty.Register<EditableComboBoxItem, string>(nameof(Value), defaultBindingMode: BindingMode.OneTime);

    private static readonly Point invalidPoint = new(double.NaN, double.NaN);

    private Point pointerDownPoint = invalidPoint;

    public EditableComboBoxItem() { }

    public IDataTemplate? ContentTemplate
    {
        get => this.GetValue(ContentTemplateProperty);
        set => this.SetValue(ContentTemplateProperty, value);
    }

    public string? FilterHighlightText
    {
        get => this.GetValue(FilterHighlightTextProperty);
        set => this.SetValue(FilterHighlightTextProperty, value);
    }

    public bool IsSelected
    {
        get => this.GetValue(IsSelectedProperty);
        set => this.SetValue(IsSelectedProperty, value);
    }

    public bool IsCommittedSelected
    {
        get => this.GetValue(IsCommittedSelectedProperty);
        set => this.SetValue(IsCommittedSelectedProperty, value);
    }

    [Content]
    public required string Value
    {
        get => this.GetValue(ValueProperty);
        set => this.SetValue(ValueProperty, value);
    }

    internal object? OriginalSourceItem { get; set; }

    public override string ToString() =>
        this.Value;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        this.pointerDownPoint = invalidPoint;

        if (!e.Handled && this.GetOwner() is { } owner)
        {
            PointerPoint p = e.GetCurrentPoint(this);

            if (p.Properties.PointerUpdateKind is PointerUpdateKind.LeftButtonPressed or PointerUpdateKind.RightButtonPressed)
            {
                if (p.Pointer.Type == PointerType.Mouse)
                    // If the pressed point comes from a mouse, perform the selection immediately.
                {
                    e.Handled = owner.UpdateValueFromItemPointerEvent(this, e);
                }
                else
                    // Otherwise perform the selection when the pointer is released as to not
                    // interfere with gestures.
                {
                    this.pointerDownPoint = p.Position;
                }
                // Ideally we'd set handled here, but that would prevent the scroll gesture
                // recognizer from working.
                ////e.Handled = true;
            }
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (!e.Handled && !double.IsNaN(this.pointerDownPoint.X) && e.InitialPressMouseButton is MouseButton.Left or MouseButton.Right)
        {
            PointerPoint point = e.GetCurrentPoint(this);
            IPlatformSettings? settings = TopLevel.GetTopLevel(e.Source as Visual)?.PlatformSettings;
            Size tapSize = settings?.GetTapSize(point.Pointer.Type) ?? new Size(4, 4);
            Rect tapRect = new Rect(this.pointerDownPoint, new Size()).Inflate(new Thickness(tapSize.Width, tapSize.Height));

            if (new Rect(this.Bounds.Size).ContainsExclusive(point.Position) && tapRect.ContainsExclusive(point.Position) &&
                this.GetOwner() is { } owner)
            {
                if (owner.UpdateValueFromItemPointerEvent(this, e))
                {
                    // As we only update selection from touch/pen on pointer release, we need to raise
                    // the pointer event on the owner to trigger a commit.
                    if (e.Pointer.Type != PointerType.Mouse)
                    {
                        object? sourceBackup = e.Source;
                        owner.RaiseEvent(e);
                        e.Source = sourceBackup;
                    }

                    e.Handled = true;
                }
            }
        }

        this.pointerDownPoint = invalidPoint;
    }

    private EditableComboBox? GetOwner()
    {
        object? container = ItemsControl.ItemsControlFromItemContainer(this);
        return container switch
        {
            EditableComboBox c => c,
            ComboBox innerComboBox => innerComboBox.FindAncestorOfType<EditableComboBox>(),
            _ => this.Parent as EditableComboBox,
        };
    }
}