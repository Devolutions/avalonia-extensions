namespace Devolutions.AvaloniaControls.Behaviors;

using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Platform;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactions.Custom;

public class PositionedPopupBehavior : AttachedToVisualTreeBehavior<Popup>
{
    public static readonly StyledProperty<Control?> PositionToProperty =
        AvaloniaProperty.Register<PositionedPopupBehavior, Control?>(nameof(PositionTo));

    public static readonly StyledProperty<bool> PositionToTemplatedParentProperty =
        AvaloniaProperty.Register<PositionedPopupBehavior, bool>(nameof(PositionToTemplatedParent));

    // Inject a border that will mask the border between the input and the popup
    public static readonly StyledProperty<bool> InjectFusionMaskProperty =
        AvaloniaProperty.Register<PositionedPopupBehavior, bool>(nameof(InjectFusionMask));

    public static readonly StyledProperty<Thickness> PopupBorderThicknessProperty = AvaloniaProperty.Register<PositionedPopupBehavior, Thickness>(
        nameof(PopupBorderThickness),
        new Thickness(1));

    private Border? fusionMask;

    private Panel? fusionMaskPanel;

    private bool isOpenedFromTop;

    private Control? originalPopupContent;

    private IDisposable? popupChildBoundSubscription;

    private IDisposable? popupChildSubscription;

    [ResolveByName]
    public Control? PositionTo
    {
        get => this.GetValue(PositionToProperty);
        set => this.SetValue(PositionToProperty, value);
    }

    public bool PositionToTemplatedParent
    {
        get => this.GetValue(PositionToTemplatedParentProperty);
        set => this.SetValue(PositionToTemplatedParentProperty, value);
    }

    public bool InjectFusionMask
    {
        get => this.GetValue(InjectFusionMaskProperty);
        set => this.SetValue(InjectFusionMaskProperty, value);
    }

    public Thickness PopupBorderThickness
    {
        get => this.GetValue(PopupBorderThicknessProperty);
        set => this.SetValue(PopupBorderThicknessProperty, value);
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (this.AssociatedObject is not null && this.fusionMaskPanel is not null)
        {
            this.fusionMaskPanel.Children.Clear();
            this.AssociatedObject.Child = this.originalPopupContent;
        }
    }

    protected override void OnAttachedToVisualTree(CompositeDisposable disposable)
    {
        if (this.AssociatedObject is null) return;

        if (this.PositionToTemplatedParent)
        {
            this.PositionTo = this.AssociatedObject.TemplatedParent as Control;
        }

        this.PositionTo ??= this.AssociatedObject.PlacementTarget;

        if (this.PositionTo is null) return;

        this.AssociatedObject.Opened += this.OnOpened;
        this.AssociatedObject.Closed += this.OnClosed;

        if (this.InjectFusionMask && this.PositionTo is TemplatedControl { Background: var background })
        {
            this.fusionMask ??= new Border
            {
                Name = "PopupPositionClassesBehavior_Panel",
                BorderBrush = background,
                Padding = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Left,
                CornerRadius = new CornerRadius(0),
            };

            this.originalPopupContent = this.AssociatedObject.Child!;
            this.AssociatedObject.Child = null;
            this.fusionMaskPanel = new Panel
            {
                Children =
                {
                    this.originalPopupContent,
                    this.fusionMask,
                },
            };
            this.AssociatedObject.Child = this.fusionMaskPanel;
        }

        this.popupChildSubscription = this.AssociatedObject.GetObservable(Popup.ChildProperty)
            .Subscribe(child =>
            {
                if (this.popupChildBoundSubscription != null)
                {
                    disposable.Remove(this.popupChildBoundSubscription);
                    this.popupChildBoundSubscription.Dispose();
                    this.popupChildBoundSubscription = null;
                }

                this.popupChildBoundSubscription = child?.GetObservable(Visual.BoundsProperty).Subscribe(b => this.UpdateDropdownOverflowPseudoClass(null, b));
                if (this.popupChildBoundSubscription is not null)
                {
                    disposable.Add(this.popupChildBoundSubscription);
                }
            });

        disposable.Add(this.popupChildSubscription);
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        if (this.AssociatedObject?.Child == null) return;

        this.UpdatePseudoClasses();

        if (this.AssociatedObject.Child?.GetVisualRoot() is PopupRoot popupRoot)
        {
            popupRoot.PositionChanged += this.OnPositionChanged;
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        this.RemoveTargetPseudoClass(":dropdown-open-from-top");
        this.RemoveTargetPseudoClass(":dropdown-overflow-left");
        this.RemoveTargetPseudoClass(":dropdown-overflow-right");

        if (this.AssociatedObject?.Child?.GetVisualRoot() is PopupRoot popupRoot)
        {
            popupRoot.PositionChanged -= this.OnPositionChanged;
        }
    }

    private void OnPositionChanged(object? sender, PixelPointEventArgs e) => this.UpdatePseudoClasses();

    private void AddTargetPseudoClass(string classToAdd) => (this.PositionTo?.Classes as IPseudoClasses)?.Add(classToAdd);

    private void RemoveTargetPseudoClass(string classToRemove) => (this.PositionTo?.Classes as IPseudoClasses)?.Remove(classToRemove);

    private void ToggleTargetPseudoClass(string classToRemove, bool toggle)
    {
        if (toggle)
        {
            this.AddTargetPseudoClass(classToRemove);
        }
        else
        {
            this.RemoveTargetPseudoClass(classToRemove);
        }
    }

    private void UpdateDropdownOverflowPseudoClass(Rect? newDropdownBounds, Rect? newPopupBounds, int? offsetLeft = null)
    {
        if (this.PositionTo is null || this.AssociatedObject?.Child?.IsAttachedToVisualTree() != true) return;

        offsetLeft ??= this.CalculateOffsetLeft();

        double dropdownWidth = (newDropdownBounds ?? this.PositionTo.Bounds).Width;
        double popupWidth = (newPopupBounds ?? this.AssociatedObject.Child.Bounds).Width;

        this.ToggleTargetPseudoClass(":dropdown-overflow-right", popupWidth - offsetLeft > dropdownWidth);
        this.ToggleTargetPseudoClass(":dropdown-overflow-left", offsetLeft > 0);

        if (Design.IsDesignMode)
        {
            this.ToggleTargetPseudoClass(":dropdown-overflow-right", popupWidth > dropdownWidth);
            this.RemoveTargetPseudoClass(":dropdown-overflow-left");
        }
    }

    private int? CalculateOffsetLeft()
    {
        if (Design.IsDesignMode || this.AssociatedObject?.Child == null) return null;

        return this.PositionTo?.PointToScreen(new Point(0, 0)).X - this.AssociatedObject.Child.PointToScreen(new Point(0, 0)).X;
    }

    private void UpdatePseudoClasses()
    {
        if (this.AssociatedObject?.Child?.IsAttachedToVisualTree() != true) return;

        int offsetLeft = this.CalculateOffsetLeft() ?? 0;
        this.UpdateDropdownOpenFromTop();
        this.CalculatePopupBorderMask(null, null, offsetLeft);
        this.UpdateDropdownOverflowPseudoClass(null, null, offsetLeft);
    }

    private void UpdateDropdownOpenFromTop()
    {
        if (Design.IsDesignMode || this.AssociatedObject?.Child == null) return;

        this.isOpenedFromTop = this.AssociatedObject.PointToScreen(new Point(0, 0)).Y > this.AssociatedObject.Child.PointToScreen(new Point(0, 0)).Y;
        this.ToggleTargetPseudoClass(":dropdown-open-from-top", this.isOpenedFromTop);
    }

    private void CalculatePopupBorderMask(Thickness? newFocusBorderThickness, Rect? newBounds, int? offsetLeft = null)
    {
        if (this.PositionTo is null) return;

        if (this.fusionMask is not null)
        {
            Thickness focusBorderThickness = newFocusBorderThickness ?? this.PopupBorderThickness;
            Rect bounds = newBounds ?? this.PositionTo.Bounds;

            this.fusionMask.Margin = new Thickness(focusBorderThickness.Left, 0, 0, 0);
            this.fusionMask.Width = bounds.Width - focusBorderThickness.Left - focusBorderThickness.Right;
        }

        if (this.AssociatedObject?.Child?.IsAttachedToVisualTree() != true) return;

        (bool isOutsideScreensBoundaries, bool isSplitBetweenScreens) = IsCutByScreenEdge(this.PositionTo);
        this.ToggleTargetPseudoClass(":is-split-between-screens", isSplitBetweenScreens);
        this.ToggleTargetPseudoClass(":is-outside-screens-boundaries", isOutsideScreensBoundaries);
        this.ToggleTargetPseudoClass(":is-cut-by-screen-edge", isSplitBetweenScreens || isOutsideScreensBoundaries);

        if (this.fusionMask is not null)
        {
            if (isSplitBetweenScreens || isOutsideScreensBoundaries)
            {
                this.fusionMask.Width = 0;
                this.fusionMask.BorderThickness = new Thickness(0);
            }
            else
            {
                offsetLeft ??= this.CalculateOffsetLeft();
                this.fusionMask.Margin = new Thickness(
                    this.fusionMask.Margin.Left + (int)offsetLeft!,
                    this.fusionMask.Margin.Top,
                    this.fusionMask.Margin.Right,
                    this.fusionMask.Margin.Bottom);

                this.fusionMask.BorderThickness = !this.isOpenedFromTop
                    ? new Thickness(0, this.PopupBorderThickness.Top, 0, 0)
                    : new Thickness(0, 0, 0, this.PopupBorderThickness.Bottom);
            }
        }
    }

    private static (bool isOutsideScreensBoundaries, bool isSplitBetweenScreens) IsCutByScreenEdge(Visual visual)
    {
        if (TopLevel.GetTopLevel(visual) is not { } topLevel || topLevel.Screens is null) return (false, false);

        PixelPoint topLeftPoint = visual.PointToScreen(new Point(0, 0));
        PixelPoint bottomRightPoint = visual.PointToScreen(new Point(visual.Bounds.Width, visual.Bounds.Height));

        Screen? screen1 = topLevel.Screens.ScreenFromPoint(topLeftPoint);
        Screen? screen2 = topLevel.Screens.ScreenFromPoint(bottomRightPoint);
        bool isOutsideScreensBoundaries = screen1 is null || screen2 is null;
        return (isOutsideScreensBoundaries, !isOutsideScreensBoundaries && screen1 != screen2);
    }
}