namespace Devolutions.AvaloniaControls.Behaviors;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;

/// <summary>
/// Keeps the selected row of a ComboBox drop-down aligned with the closed ComboBox, macOS style:
/// the text visible in the closed control should not appear to move when the list opens.
/// </summary>
/// <remarks>
/// <para>
/// A purely arithmetic offset (<c>SelectedIndex * ItemHeight</c>) only works while the whole list
/// fits in the popup. As soon as the list scrolls, the ComboBox scrolls the selected row into view,
/// so the row is no longer at <c>SelectedIndex * ItemHeight</c> inside the popup and the arithmetic
/// answer is wrong by however much that scroll moved it. Near a screen edge the window manager
/// clamps the popup as well, which the arithmetic cannot see at all.
/// </para>
/// <para>
/// This behavior therefore measures rather than predicts. Once the popup has been laid out and
/// positioned it compares where the selected row actually is on screen against the closed ComboBox,
/// and closes the gap with whichever lever is still available:
/// </para>
/// <list type="number">
///   <item><description>Move the popup. The window manager clamps this to the screen, which is the
///   behavior we want at the screen edges.</description></item>
///   <item><description>If the popup did not actually move - it was clamped, or it is already
///   showing the end of a long list - scroll the list instead. For large option sets this is what
///   does the real work.</description></item>
/// </list>
/// <para>
/// The template's <c>VerticalOffset</c> binding still supplies the initial estimate, so short lists
/// are already right when the popup first appears and this pass finds nothing to do. Corrections
/// use <see cref="AvaloniaObject.SetCurrentValue{T}" /> so that binding stays alive and keeps
/// re-seeding the estimate when the selection changes.
/// </para>
/// </remarks>
public static class ComboBoxPopupAlignmentBehavior
{
    /// <summary>Sub-pixel differences are not worth another layout pass.</summary>
    private const double Tolerance = 0.5;

    /// <summary>
    /// Safety net for the measure/correct loop. Two or three passes are enough in practice; the cap
    /// only exists so a pathological template cannot spin the layout system. It is generous because
    /// a virtualized list far from the realized window needs several passes just to produce the row.
    /// </summary>
    private const int MaxPasses = 12;

    /// <summary>
    /// A correction larger than this is not a nudge, it is a bad measurement - most likely taken
    /// while the popup was between positions. Applying it would fling the drop-down away from the
    /// ComboBox, so it is ignored and the next layout pass gets to measure again.
    /// </summary>
    private const double MaxCorrection = 2000;

    public static readonly AttachedProperty<bool> EnableProperty =
        AvaloniaProperty.RegisterAttached<ComboBox, bool>("Enable", typeof(ComboBoxPopupAlignmentBehavior));

    private static readonly AttachedProperty<Aligner?> AlignerProperty =
        AvaloniaProperty.RegisterAttached<ComboBox, Aligner?>("Aligner", typeof(ComboBoxPopupAlignmentBehavior));

    static ComboBoxPopupAlignmentBehavior()
    {
        EnableProperty.Changed.Subscribe(args =>
        {
            if (args.Sender is not ComboBox comboBox) return;

            comboBox.TemplateApplied -= OnTemplateApplied;
            comboBox.GetValue(AlignerProperty)?.Dispose();
            comboBox.SetValue(AlignerProperty, null);

            if (!args.NewValue.GetValueOrDefault<bool>()) return;

            comboBox.TemplateApplied += OnTemplateApplied;

            // The template may already have been applied by the time the setter runs.
            if (comboBox.GetVisualDescendants().OfType<Popup>().FirstOrDefault(p => p.Name == "PART_Popup") is { } popup)
            {
                Attach(comboBox, popup);
            }
        });
    }

    public static void SetEnable(ComboBox element, bool value) => element.SetValue(EnableProperty, value);

    public static bool GetEnable(ComboBox element) => element.GetValue(EnableProperty);

    private static void OnTemplateApplied(object? sender, TemplateAppliedEventArgs e)
    {
        if (sender is not ComboBox comboBox) return;
        if (e.NameScope.Find<Popup>("PART_Popup") is not { } popup) return;

        Attach(comboBox, popup);
    }

    private static void Attach(ComboBox comboBox, Popup popup)
    {
        Aligner? existing = comboBox.GetValue(AlignerProperty);
        if (existing?.Popup == popup) return;

        existing?.Dispose();
        comboBox.SetValue(AlignerProperty, new Aligner(comboBox, popup));
    }

    /// <summary>
    /// Owns the subscriptions for one ComboBox/Popup pair. It is stored on the ComboBox itself, so
    /// ComboBox, template, popup and aligner all stay collectable together.
    /// </summary>
    private sealed class Aligner : IDisposable
    {
        private readonly ComboBox comboBox;

        private TopLevel? popupRoot;

        private int pass;

        private double previousMisalignment;

        private bool passScheduled;

        /// <summary>
        /// Identifies one opening of the popup, so a pass queued for a previous opening can tell
        /// that it is stale and do nothing.
        /// </summary>
        private int generation;

        /// <summary>The most recent offset the template's binding produced.</summary>
        private double bindingOffset;

        /// <summary>Set while this behavior writes the offset, so it does not observe itself.</summary>
        private bool applyingCorrection;

        internal Popup Popup { get; }

        internal Aligner(ComboBox comboBox, Popup popup)
        {
            this.comboBox = comboBox;
            this.Popup = popup;

            this.bindingOffset = popup.VerticalOffset;

            popup.PropertyChanged += this.OnPopupPropertyChanged;
            popup.Opened += this.OnPopupOpened;
            popup.Closed += this.OnPopupClosed;

            // The offset alone is not a reliable signal that the selection moved: the converter
            // clamps at -effectivePopupHeight, so every sufficiently large index produces the same
            // value and stepping between two of them raises no property change at all.
            comboBox.SelectionChanged += this.OnSelectionChanged;

            // Enabling the behavior on an already-open drop-down (switching the attached property
            // on at runtime, or a theme applied to a ComboBox that is showing its list) would
            // otherwise wait for an Opened event that has already been and gone.
            if (popup.IsOpen) this.ReArm();
        }

        public void Dispose()
        {
            this.Popup.PropertyChanged -= this.OnPopupPropertyChanged;
            this.Popup.Opened -= this.OnPopupOpened;
            this.Popup.Closed -= this.OnPopupClosed;
            this.comboBox.SelectionChanged -= this.OnSelectionChanged;
            this.Unsubscribe();

            // Being disposed mid-open (the behavior switched off while the drop-down is showing)
            // would otherwise leave our correction behind with no Closed handler left to clear it.
            this.RestoreBindingOffset();
        }

        /// <summary>
        /// Remembers the offset the template's binding produces, as distinct from the corrections
        /// this behavior writes over it. The binding re-evaluates whenever the selection changes -
        /// including while the drop-down is open, which is the normal way a selection is made - so
        /// the latest value it produced is the one to hand back on close.
        /// </summary>
        /// <remarks>
        /// A write from anywhere but this behavior also invalidates the alignment we had converged
        /// on, so the measure/correct loop is re-armed. Without that, choosing a different item from
        /// an open drop-down would leave the new selection sitting wherever the binding's arithmetic
        /// estimate put it - which for a long list is the very error this behavior exists to correct
        /// - until the popup was closed and reopened.
        /// </remarks>
        private void OnPopupPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property != Popup.VerticalOffsetProperty || this.applyingCorrection) return;

            this.bindingOffset = this.Popup.VerticalOffset;
            this.ReArm();
        }

        /// <summary>
        /// A selection made from the open drop-down moves the row that has to end up over the
        /// ComboBox, so the alignment has to be worked out again.
        /// </summary>
        private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e) => this.ReArm();

        /// <summary>
        /// Restarts the measure/correct loop for a drop-down that is already open, after something
        /// outside this behavior moved the popup.
        /// </summary>
        private void ReArm()
        {
            if (!this.Popup.IsOpen) return;
            if (this.Popup.Child is not { } child || !child.IsAttachedToVisualTree()) return;
            if (TopLevel.GetTopLevel(child) is not { } root) return;

            // Retires anything still queued against the alignment we are abandoning.
            this.Unsubscribe();

            this.pass = 0;
            this.previousMisalignment = double.NaN;
            this.popupRoot = root;
            root.LayoutUpdated += this.OnLayoutUpdated;
            this.SchedulePass();
        }

        /// <summary>
        /// Hands the offset back to the template's binding. <c>ClearValue</c> is not enough: it drops
        /// the binding's contribution along with the correction and leaves the offset at zero.
        /// </summary>
        private void RestoreBindingOffset()
        {
            this.applyingCorrection = true;
            try
            {
                this.Popup.SetCurrentValue(Popup.VerticalOffsetProperty, this.bindingOffset);
            }
            finally
            {
                this.applyingCorrection = false;
            }
        }

        private void OnPopupOpened(object? sender, EventArgs e)
        {
            this.Unsubscribe();

            if (this.comboBox.SelectedIndex < 0) return;
            if (this.Popup.Child is not { } child || !child.IsAttachedToVisualTree()) return;
            if (TopLevel.GetTopLevel(child) is not { } root) return;

            this.pass = 0;
            this.previousMisalignment = double.NaN;

            // The popup is not positioned yet at Opened time - measuring here would compare against
            // a stale location. The first layout pass after opening is the earliest useful moment.
            this.popupRoot = root;
            root.LayoutUpdated += this.OnLayoutUpdated;
        }

        private void OnPopupClosed(object? sender, EventArgs e)
        {
            this.Unsubscribe();

            // Drop our correction so the template's binding owns the offset again. Otherwise the
            // correction applied for the row we just aligned to would still be in place at the next
            // open, on top of a fresh estimate for a possibly different selection - which is what
            // made a re-open of a far-down selection land off by a row, or wildly below the
            // ComboBox.
            this.RestoreBindingOffset();
        }

        private void Unsubscribe()
        {
            // Retire any queued pass, whether or not we are currently subscribed.
            this.generation++;
            this.passScheduled = false;

            if (this.popupRoot is null) return;

            this.popupRoot.LayoutUpdated -= this.OnLayoutUpdated;
            this.popupRoot = null;
        }

        private void OnLayoutUpdated(object? sender, EventArgs e) => this.RunPass();

        /// <summary>
        /// Schedules another measuring pass under our own steam. Relying on
        /// <see cref="Layoutable.LayoutUpdated" /> alone is not enough: a pass can end with nothing
        /// laid out afterwards (a virtualized row that is not realized yet, or a correction the
        /// popup absorbed silently), and then the drop-down just sits there misaligned until some
        /// unrelated event - moving the mouse over it - happens to trigger layout and let the loop
        /// continue. That produced a visible late "jump" into place.
        /// </summary>
        private void SchedulePass()
        {
            if (this.passScheduled || this.popupRoot is null) return;

            this.passScheduled = true;
            int scheduledFor = this.generation;
            Dispatcher.UIThread.Post(
                () =>
                {
                    // A pass queued for one opening must not run against a later one. Without this,
                    // closing and reopening before the callback ran would let it measure the new
                    // popup before its first positioning layout - exactly the stale-coordinate
                    // reading this timing code exists to avoid.
                    if (scheduledFor != this.generation) return;

                    this.passScheduled = false;
                    if (this.popupRoot is not null) this.RunPass();
                },
                DispatcherPriority.Loaded);
        }

        private void RunPass()
        {
            if (!this.Popup.IsOpen || this.pass++ >= MaxPasses)
            {
                this.Unsubscribe();
                return;
            }

            Control? container = this.ResolveSelectedContainer();
            if (container is null)
            {
                // Not realized yet - ResolveSelectedContainer has asked for it, so try again rather
                // than giving up on a list that is merely still virtualizing.
                this.SchedulePass();
                return;
            }

            double misalignment = this.Misalignment(container);
            if (Math.Abs(misalignment) <= Tolerance)
            {
                this.Unsubscribe();
                return;
            }

            if (Math.Abs(misalignment) > MaxCorrection)
            {
                // Reject the sample outright: it is almost certainly measured mid-reposition. It
                // must not reach the popupIsStuck comparison either, or two identical bad readings
                // in a row would look like a clamped popup and send TryScroll off to an endpoint.
                this.previousMisalignment = double.NaN;
                this.SchedulePass();
                return;
            }

            // If the previous correction did not move the row, the popup is clamped - by the screen
            // edge, or because the list is already scrolled to an end. Scrolling is the only lever
            // left, and if that cannot help either we are as close as this ComboBox can get.
            bool popupIsStuck = !double.IsNaN(this.previousMisalignment)
                                && Math.Abs(misalignment - this.previousMisalignment) <= Tolerance;

            this.previousMisalignment = misalignment;

            if (popupIsStuck)
            {
                if (this.TryScroll(misalignment))
                {
                    this.SchedulePass();
                }
                else
                {
                    this.Unsubscribe();
                }

                return;
            }

            // SetCurrentValue keeps the template's VerticalOffset binding intact.
            this.applyingCorrection = true;
            try
            {
                this.Popup.SetCurrentValue(Popup.VerticalOffsetProperty, this.Popup.VerticalOffset + misalignment);
            }
            finally
            {
                this.applyingCorrection = false;
            }

            this.SchedulePass();
        }

        private Control? ResolveSelectedContainer()
        {
            int index = this.comboBox.SelectedIndex;
            if (index < 0) return null;

            Control? container = this.comboBox.ContainerFromIndex(index);
            if (container is null)
            {
                // Virtualized far down a large list: realize it first.
                this.comboBox.ScrollIntoView(index);
                container = this.comboBox.ContainerFromIndex(index);
            }

            return container?.IsAttachedToVisualTree() == true && container.Bounds.Height > 0 ? container : null;
        }

        /// <summary>Scrolls the list to take up misalignment the popup itself could not.</summary>
        /// <returns><c>true</c> if the list moved, so another measuring pass is worthwhile.</returns>
        private bool TryScroll(double misalignment)
        {
            if (this.Popup.Child is not { } child) return false;
            if (child.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault() is not { } scroller) return false;

            double scrollable = Math.Max(0, scroller.Extent.Height - scroller.Viewport.Height);
            if (scrollable <= 0) return false;

            // Scrolling up by `misalignment` moves the row down the screen by the same amount.
            double target = Math.Clamp(scroller.Offset.Y - misalignment, 0, scrollable);
            if (Math.Abs(target - scroller.Offset.Y) <= Tolerance) return false;

            scroller.SetCurrentValue(ScrollViewer.OffsetProperty, scroller.Offset.WithY(target));
            return true;
        }

        /// <summary>
        /// How far the selected row must move down the screen to sit on the closed ComboBox, in the
        /// popup's own units. Centres are compared rather than top edges, so row and ComboBox do not
        /// have to share padding or height to look aligned.
        /// </summary>
        private double Misalignment(Control container)
        {
            if (!this.comboBox.IsAttachedToVisualTree() || !container.IsAttachedToVisualTree()) return 0;

            // The result is spent in the popup's own units - its ScrollViewer offset and its
            // VerticalOffset - so the popup root's scaling is the one that applies. A native popup
            // can land on a monitor whose DPI differs from the owner window's, and using the owner
            // scale there would over- or under-correct on every pass.
            double scaling = (TopLevel.GetTopLevel(container) ?? TopLevel.GetTopLevel(this.comboBox))?.RenderScaling ?? 1;
            if (scaling <= 0) scaling = 1;

            double comboBoxCentre = this.comboBox.PointToScreen(new Point(0, this.comboBox.Bounds.Height / 2)).Y;
            double containerCentre = container.PointToScreen(new Point(0, container.Bounds.Height / 2)).Y;

            return (comboBoxCentre - containerCentre) / scaling;
        }
    }
}
