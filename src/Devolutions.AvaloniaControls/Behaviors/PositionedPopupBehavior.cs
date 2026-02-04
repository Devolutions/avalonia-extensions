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
    /// <summary>
    /// Control onto which the state will be reported.
    /// This target control will receive the relevant pseudoclasses.
    /// <br /><br />
    /// Defaults to the TemplateParent if not set.
    /// </summary>
    public static readonly StyledProperty<Control?> ReportToProperty =
        AvaloniaProperty.Register<PositionedPopupBehavior, Control?>(nameof(ReportTo));

    /// <summary>
    /// Visual relative to which the popup will be positioned.
    /// <br /><br />
    /// Defaults to the TemplateParent if not set.
    /// </summary>
    public static readonly StyledProperty<Control?> PositionToProperty =
        AvaloniaProperty.Register<PositionedPopupBehavior, Control?>(nameof(PositionTo));

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

    private Control? positionTarget;
    private Control? reportTarget;

    public Control? ReportTo
    {
        get => this.GetValue(ReportToProperty);
        set => this.SetValue(ReportToProperty, value);
    }

    [ResolveByName]
    public Control? PositionTo
    {
        get => this.GetValue(PositionToProperty);
        set => this.SetValue(PositionToProperty, value);
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

        if (this.AssociatedObject is not null)
        {
            this.AssociatedObject.Opened -= this.OnOpened;
            this.AssociatedObject.Closed -= this.OnClosed;
        }

        if (this.AssociatedObject is not null && this.fusionMaskPanel is not null)
        {
            this.fusionMaskPanel.Children.Clear();
            this.AssociatedObject.Child = this.originalPopupContent;
        }
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
        this.RemovePseudoClass(":dropdown-open-from-top");
        this.RemovePseudoClass(":dropdown-overflow-left");
        this.RemovePseudoClass(":dropdown-overflow-right");

        if (this.AssociatedObject?.Child?.GetVisualRoot() is PopupRoot popupRoot)
        {
            popupRoot.PositionChanged -= this.OnPositionChanged;
        }
    }

    private void OnPositionChanged(object? sender, PixelPointEventArgs e) => this.UpdatePseudoClasses();

    private void AddPseudoClass(string classToAdd) => (this.reportTarget?.Classes as IPseudoClasses)?.Add(classToAdd);

    private void RemovePseudoClass(string classToRemove) => (this.reportTarget?.Classes as IPseudoClasses)?.Remove(classToRemove);

    private void TogglePseudoClass(string classToRemove, bool toggle)
    {
        if (toggle)
        {
            this.AddPseudoClass(classToRemove);
        }
        else
        {
            this.RemovePseudoClass(classToRemove);
        }
    }

    private void UpdateDropdownOverflowPseudoClass(Rect? newDropdownBounds, Rect? newPopupBounds, int? offsetLeft = null)
    {
        if (this.positionTarget is null || this.AssociatedObject?.Child?.IsAttachedToVisualTree() != true) return;

        offsetLeft ??= this.CalculateOffsetLeft();

        double dropdownWidth = (newDropdownBounds ?? this.positionTarget.Bounds).Width;
        double popupWidth = (newPopupBounds ?? this.AssociatedObject.Child.Bounds).Width;

        this.TogglePseudoClass(":dropdown-overflow-right", popupWidth - offsetLeft > dropdownWidth);
        this.TogglePseudoClass(":dropdown-overflow-left", offsetLeft > 0);

        if (Design.IsDesignMode)
        {
            this.TogglePseudoClass(":dropdown-overflow-right", popupWidth > dropdownWidth);
            this.RemovePseudoClass(":dropdown-overflow-left");
        }
    }

    private int? CalculateOffsetLeft()
    {
        if (Design.IsDesignMode || this.AssociatedObject?.Child == null) return null;

        return this.positionTarget?.PointToScreen(new Point(0, 0)).X - this.AssociatedObject.Child.PointToScreen(new Point(0, 0)).X;
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
        this.TogglePseudoClass(":dropdown-open-from-top", this.isOpenedFromTop);
    }

    private void CalculatePopupBorderMask(Thickness? newFocusBorderThickness, Rect? newBounds, int? offsetLeft = null)
    {
        if (this.positionTarget is null) return;

        if (this.fusionMask is not null)
        {
            Thickness focusBorderThickness = newFocusBorderThickness ?? this.PopupBorderThickness;
            Rect bounds = newBounds ?? this.positionTarget.Bounds;

            this.fusionMask.Margin = new Thickness(focusBorderThickness.Left, 0, 0, 0);
            this.fusionMask.Width = bounds.Width - focusBorderThickness.Left - focusBorderThickness.Right;
        }

        if (this.AssociatedObject?.Child?.IsAttachedToVisualTree() != true) return;

        (bool isOutsideScreensBoundaries, bool isSplitBetweenScreens) = IsCutByScreenEdge(this.positionTarget);
        this.TogglePseudoClass(":is-split-between-screens", isSplitBetweenScreens);
        this.TogglePseudoClass(":is-outside-screens-boundaries", isOutsideScreensBoundaries);
        this.TogglePseudoClass(":is-cut-by-screen-edge", isSplitBetweenScreens || isOutsideScreensBoundaries);

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
                    this.fusionMask.Margin.Left + (offsetLeft ?? 0),
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

    protected override IDisposable OnAttachedToVisualTreeOverride()
    {
        var disposable = new CompositeDisposable();

        if (this.AssociatedObject is null) return disposable;

        this.reportTarget = this.ReportTo ?? this.AssociatedObject.TemplatedParent as Control;
        this.positionTarget = this.PositionTo ?? this.AssociatedObject.PlacementTarget ?? this.AssociatedObject.TemplatedParent as Control;

        if (this.reportTarget is null || this.positionTarget is null) return disposable;

        this.AssociatedObject.Opened += this.OnOpened;
        this.AssociatedObject.Closed += this.OnClosed;

        disposable.Add(this.positionTarget.GetObservable(Visual.BoundsProperty).Subscribe(b =>
        {
            this.UpdateDropdownOverflowPseudoClass(b, null);
            this.CalculatePopupBorderMask(null, b);
        }));

        if (this.InjectFusionMask && this.reportTarget is TemplatedControl { Background: var background })
        {
            this.fusionMask ??= new Border
            {
                Name = "PopupPositionClassesBehavior_FusionBorder",
                BorderBrush = background,
                Padding = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Left,
                CornerRadius = new CornerRadius(0),
            };

            this.originalPopupContent = this.AssociatedObject.Child!;
            this.AssociatedObject.Child = null;
            this.fusionMaskPanel = new Panel
            {
                Name = "PopupPositionClassesBehavior_Panel",
                Children =
                {
                    this.originalPopupContent,
                    this.fusionMask,
                },
            };
            this.AssociatedObject.Child = this.fusionMaskPanel;

            disposable.Add(this.reportTarget.GetObservable(Border.BackgroundProperty).Subscribe(newBg =>
            {
                this.fusionMask.BorderBrush = newBg;
            }));
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

        return disposable;
    }
}