namespace Devolutions.AvaloniaControls.Behaviors;

using System;
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

    /// <summary>
    /// How many consecutive, time-spaced confirmations must measure the same misalignment before
    /// the popup is treated as clamped rather than merely not yet repositioned. <see
    /// cref="Aligner.Misalignment"/> uses <c>PointToScreen</c>/<c>PointToClient</c>, which round-trip
    /// through the platform's window position - and on Linux (X11, including under a Wayland session
    /// via XWayland) that position is only updated when a <c>ConfigureNotify</c> event lands,
    /// asynchronously after the popup is requested to move. Counting consecutive *dispatcher* passes
    /// is not enough of a gate on its own: a pass posted at <see cref="DispatcherPriority.Loaded"/>
    /// runs again within a fraction of a millisecond, far faster than a round-trip through the X
    /// server, so several such passes can measure the same stale, pre-move position and satisfy the
    /// streak before <c>ConfigureNotify</c> ever lands - misreading "not moved yet" as "clamped" and
    /// sending <see cref="Aligner.TryScroll"/> off against a reading that was never real. Requiring
    /// the repeats to additionally be spaced by <see cref="StuckConfirmationDelay"/> of genuine wall
    /// clock time (see <see cref="Aligner.ScheduleStuckConfirmation"/>) gives that round-trip a
    /// chance to land and change the reading before it is trusted.
    /// </summary>
    private const int StuckStreakThreshold = 3;

    /// <summary>
    /// Real time to wait before re-measuring a misalignment that already looked unchanged, so a
    /// pending <c>ConfigureNotify</c> (see <see cref="StuckStreakThreshold"/>) has a chance to land
    /// and move the reading before another identical sample is allowed to count towards the streak.
    /// </summary>
    private static readonly TimeSpan StuckConfirmationDelay = TimeSpan.FromMilliseconds(60);

    /// <summary>
    /// How long after the loop believes it is aligned to take one final look. The popup's real move
    /// on X11 (including under a Wayland session, via XWayland) completes asynchronously, so the
    /// reading that ended the loop can have been taken while the popup was still travelling and only
    /// looked aligned in passing. Layout listeners are dropped as soon as the loop finishes - keeping
    /// them would make the drop-down fight the user's own scrolling - so this single delayed check is
    /// what distinguishes "aligned right now" from "settled", without re-reacting to everything else.
    /// </summary>
    private static readonly TimeSpan SettleVerificationDelay = TimeSpan.FromMilliseconds(120);

    /// <summary>
    /// Opt-in tracing, for diagnosing alignment on a real desktop where this loop cannot practically
    /// be stepped through: set <c>DEVO_COMBOBOX_ALIGN_TRACE=1</c> to print one line per measuring
    /// pass, recording the measurement, the levers applied and which exit path was taken.
    /// </summary>
    private static readonly bool TraceEnabled =
        Environment.GetEnvironmentVariable("DEVO_COMBOBOX_ALIGN_TRACE") == "1";

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

        /// <summary>
        /// Consecutive passes in a row that measured the same misalignment as the one before them.
        /// </summary>
        private int stuckStreak;

        /// <summary>
        /// <see cref="Environment.TickCount64"/> when the current <see cref="stuckStreak"/> began,
        /// i.e. when <see cref="previousMisalignment"/> was first measured. Dispatcher passes at
        /// <see cref="DispatcherPriority.Loaded"/> can repeat within a fraction of a millisecond, far
        /// faster than the real, asynchronous window-move round-trip on X11 - so the streak alone is
        /// not trustworthy evidence that the popup is genuinely stuck rather than merely not yet
        /// repositioned. See <see cref="StuckConfirmationDelay"/>.
        /// </summary>
        private long streakStartTicks;

        /// <summary>
        /// Set once the delayed settle check (see <see cref="SettleVerificationDelay"/>) has been
        /// used for the current opening, so a popup that genuinely cannot be aligned re-verifies at
        /// most once rather than rescheduling itself indefinitely.
        /// </summary>
        private bool settleVerified;

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
            this.stuckStreak = 0;
            this.settleVerified = false;
            this.popupRoot = root;
            root.LayoutUpdated += this.OnLayoutUpdated;
            if (root is WindowBase popupWindow) popupWindow.PositionChanged += this.OnPopupRootPositionChanged;
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
            this.stuckStreak = 0;
            this.settleVerified = false;

            // The popup is not positioned yet at Opened time - measuring here would compare against
            // a stale location. The first layout pass after opening is the earliest useful moment.
            this.popupRoot = root;
            root.LayoutUpdated += this.OnLayoutUpdated;
            if (root is WindowBase popupWindow) popupWindow.PositionChanged += this.OnPopupRootPositionChanged;
        }

        /// <summary>
        /// Takes one last look after <see cref="SettleVerificationDelay"/>, once the popup's
        /// asynchronous move on X11 has had time to complete, and restarts the loop if the drop-down
        /// turns out to have settled somewhere other than where the final reading suggested.
        /// </summary>
        /// <remarks>
        /// This deliberately does not keep the layout listeners alive: re-reacting to every layout
        /// pass makes the drop-down fight the user's own scrolling, which is a far worse bug than the
        /// residual it would fix. A single delayed check gets the async move without that cost. It is
        /// also guarded by <see cref="settleVerified"/>, so a drop-down that genuinely cannot be
        /// aligned - clamped against a screen edge with the list already scrolled to its end - checks
        /// once and stops, rather than re-arming itself for as long as it stays open.
        /// </remarks>
        private void ScheduleSettleVerification()
        {
            // Captured after Unsubscribe's bump, so a close/reopen in the meantime still retires it.
            int scheduledFor = this.generation;
            DispatcherTimer.RunOnce(
                () =>
                {
                    if (scheduledFor != this.generation || !this.Popup.IsOpen) return;

                    if (this.ResolveSelectedContainer() is not { } container) return;
                    double misalignment = this.Misalignment(container);
                    if (Math.Abs(misalignment) <= Tolerance) return;

                    this.Trace($"settle check found {misalignment:F2} - re-aligning");
                    this.ReArm();

                    // ReArm resets this for the fresh loop it starts - correct when re-arming for a
                    // genuine external change, but here it would let the settle check re-arm itself
                    // for as long as the drop-down stayed open. Mark it after the fact instead.
                    this.settleVerified = true;
                },
                SettleVerificationDelay);
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
            if (this.popupRoot is WindowBase popupWindow) popupWindow.PositionChanged -= this.OnPopupRootPositionChanged;
            this.popupRoot = null;
        }

        private void OnLayoutUpdated(object? sender, EventArgs e) => this.RunPass();

        /// <summary>
        /// The popup's real on-screen position on X11 (including under a Wayland session, via
        /// XWayland) only becomes known asynchronously - the compositor answers a move request with
        /// a <c>ConfigureNotify</c> that can land after this behavior's own layout-driven passes have
        /// already measured and corrected against a stale position. Reacting to
        /// <see cref="WindowBase.PositionChanged"/> directly, rather than waiting for the next
        /// unrelated layout pass or the dispatcher-post retry to notice, is what makes the popup
        /// settle promptly instead of visibly jumping once the mouse happens to trigger layout.
        /// </summary>
        private void OnPopupRootPositionChanged(object? sender, PixelPointEventArgs e) => this.RunPass();

        /// <summary>
        /// Re-measures after real time has passed, rather than after the next dispatcher tick, so a
        /// pending <c>ConfigureNotify</c> gets a genuine chance to land first. <see
        /// cref="SchedulePass"/> alone is not a substitute: it can fire again within a fraction of a
        /// millisecond, which is what let a stuck streak build up entirely from stale, pre-move
        /// readings (see <see cref="StuckStreakThreshold"/>).
        /// </summary>
        private void ScheduleStuckConfirmation()
        {
            int scheduledFor = this.generation;
            DispatcherTimer.RunOnce(
                () =>
                {
                    if (scheduledFor != this.generation) return;
                    this.RunPass();
                },
                StuckConfirmationDelay);
        }

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

        /// <summary>
        /// Writes one line of <see cref="TraceEnabled">opt-in</see> diagnostics for the current pass.
        /// </summary>
        private void Trace(string message)
        {
            if (!TraceEnabled) return;

            Console.WriteLine(
                $"[combo-align] gen {this.generation} pass {this.pass} sel {this.comboBox.SelectedIndex}: {message}");
        }

        private void RunPass()
        {
            if (!this.Popup.IsOpen || this.pass++ >= MaxPasses)
            {
                if (this.Popup.IsOpen) this.Trace($"giving up after {MaxPasses} passes");
                this.Unsubscribe();
                return;
            }

            Control? container = this.ResolveSelectedContainer();
            if (container is null)
            {
                // Not realized yet - ResolveSelectedContainer has asked for it, so try again rather
                // than giving up on a list that is merely still virtualizing.
                this.Trace("selected container not realized yet");
                this.SchedulePass();
                return;
            }

            double misalignment = this.Misalignment(container);
            if (Math.Abs(misalignment) <= Tolerance)
            {
                this.Trace($"aligned ({misalignment:F2})");
                this.Unsubscribe();

                // Unsubscribe has dropped the layout listeners, so from here nothing would ever
                // re-measure. That is deliberate - staying subscribed makes the drop-down fight the
                // user's own scrolling - but it also means a reading taken while the popup was still
                // travelling would be the last word, leaving a small permanent residual that
                // reopening reproduces exactly. Take one delayed look to tell the two apart.
                if (!this.settleVerified) this.ScheduleSettleVerification();

                return;
            }

            if (Math.Abs(misalignment) > MaxCorrection)
            {
                // Reject the sample outright: it is almost certainly measured mid-reposition. It
                // must not reach the popupIsStuck comparison either, or two identical bad readings
                // in a row would look like a clamped popup and send TryScroll off to an endpoint.
                this.Trace($"rejected outsized sample {misalignment:F2}");
                this.previousMisalignment = double.NaN;
                this.stuckStreak = 0;
                this.SchedulePass();
                return;
            }

            // If several passes in a row measure the same misalignment, the popup is clamped - by
            // the screen edge, or because the list is already scrolled to an end - and scrolling is
            // the only lever left. A single repeat is not enough to conclude that, and neither is
            // merely counting dispatcher passes: on X11 (including under a Wayland session via
            // XWayland), the popup's real move lands asynchronously via ConfigureNotify, which can
            // take longer than several Loaded-priority dispatcher passes fired back to back. Without
            // also requiring StuckConfirmationDelay of genuine wall-clock time to have passed, this
            // streak could be satisfied entirely from stale, pre-move readings - misreading "hasn't
            // moved yet" as "clamped" and sending TryScroll off against a position that was never
            // final. Treating an in-flight move as "stuck" made the very first opening of a far-down
            // selection settle on the wrong row - scrolling to cover a gap that a moment later the
            // popup would have closed on its own.
            bool sameAsLastTime = !double.IsNaN(this.previousMisalignment)
                                   && Math.Abs(misalignment - this.previousMisalignment) <= Tolerance;
            if (sameAsLastTime)
            {
                this.stuckStreak++;
            }
            else
            {
                this.stuckStreak = 1;
                this.streakStartTicks = Environment.TickCount64;
            }

            bool popupIsStuck = this.stuckStreak >= StuckStreakThreshold
                                 && Environment.TickCount64 - this.streakStartTicks >= StuckConfirmationDelay.TotalMilliseconds;

            this.previousMisalignment = misalignment;

            if (sameAsLastTime && !popupIsStuck)
            {
                // The reading has not changed since the last pass, so it cannot yet reflect the
                // correction we already applied - the popup's move lands asynchronously. Acting on it
                // again would apply the same correction twice, overshooting to roughly double what was
                // needed; the misalignment then comes back with the same magnitude and the opposite
                // sign, and the loop oscillates until it runs out of passes. Wait for real time to
                // pass instead, so a pending ConfigureNotify can land and change the reading. Only an
                // unchanged reading that survives that wait means the popup is genuinely clamped.
                this.Trace($"misalign {misalignment:F2}, streak {this.stuckStreak} - awaiting time confirmation");
                this.pass--; // Waiting for confirmation should not count against MaxPasses.
                this.ScheduleStuckConfirmation();
                return;
            }

            if (popupIsStuck)
            {
                bool scrolled = this.TryScroll(misalignment);
                this.Trace($"misalign {misalignment:F2} - popup stuck, scroll {(scrolled ? "applied" : "unavailable")}");

                if (scrolled)
                {
                    this.SchedulePass();
                }
                else
                {
                    this.Unsubscribe();
                }

                return;
            }

            this.Trace($"misalign {misalignment:F2} - moving popup (offset {this.Popup.VerticalOffset:F2} -> {this.Popup.VerticalOffset + misalignment:F2})");

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
        /// How much <see cref="Popup.VerticalOffset"/> must change for the selected row to sit on
        /// the closed ComboBox. Centres are compared rather than top edges, so row and ComboBox do
        /// not have to share padding or height to look aligned.
        /// </summary>
        /// <remarks>
        /// A previous version of this method tried to avoid <c>PointToScreen</c>/<c>PointToClient</c>
        /// entirely, using <see cref="Visual.TranslatePoint"/> to express both centres in the popup's
        /// own root instead. That was based on a mistaken premise: it assumed the ComboBox's popup
        /// renders as a native platform window entirely disconnected from the main window's visual
        /// tree, so that <c>PointToScreen</c>/<c>PointToClient</c> would not be meaningful across the
        /// two. On Linux, though, there is no separate Wayland backend in Avalonia at all - even
        /// under a Wayland session the app runs through <c>Avalonia.X11</c> (via XWayland), whose
        /// <c>Position</c>/<c>PointToScreen</c>/<c>PointToClient</c> are not identity passthroughs:
        /// they track the window's real position, kept current via <c>ConfigureNotify</c> events.
        /// <para>
        /// <see cref="Visual.TranslatePoint"/> requires the two visuals to share a common ancestor
        /// reachable by walking <c>VisualParent</c> pointers. The popup's root and the ComboBox's own
        /// window are two separate native windows: the popup only keeps a *logical* parent link back
        /// to the ComboBox's top level (used for focus routing), not a visual-tree one. So
        /// <c>TranslatePoint</c> silently returned <c>null</c> for every corner case where the popup
        /// was not merely an overlay layer on top of the same window - which is exactly what happens
        /// in the headless test host (its popups default to an overlay layer sharing one visual tree
        /// with the main window), but not in a real windowed app. That made every measurement return
        /// <c>0</c>, i.e. no correction, on the genuine article - matching what was reported: the
        /// error was never corrected even on reopening.
        /// </para>
        /// <para>
        /// The fix is to go back to <c>PointToScreen</c>/<c>PointToClient</c>, the same approach the
        /// macOS behavior uses, converting both screen points back through the popup's own top level
        /// rather than dividing by a scale factor - see the macOS implementation's remarks for why.
        /// </para>
        /// </remarks>
        private double Misalignment(Control container)
        {
            if (!this.comboBox.IsAttachedToVisualTree() || !container.IsAttachedToVisualTree()) return 0;
            if ((TopLevel.GetTopLevel(container) ?? TopLevel.GetTopLevel(this.comboBox)) is not { } popupRoot)
            {
                return 0;
            }

            PixelPoint comboBoxCentre = this.comboBox.PointToScreen(new Point(0, this.comboBox.Bounds.Height / 2));
            PixelPoint containerCentre = container.PointToScreen(new Point(0, container.Bounds.Height / 2));

            return popupRoot.PointToClient(comboBoxCentre).Y - popupRoot.PointToClient(containerCentre).Y;
        }
    }
}
