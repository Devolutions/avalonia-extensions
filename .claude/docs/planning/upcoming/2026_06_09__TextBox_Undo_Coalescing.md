# TextBox Undo: word/time-level coalescing

> Status: **Upcoming** — findings + plan parked for a dedicated PR (separate from the
> macOS TextBox context-menu work that produced this discovery).

## Goal / problem

Avalonia's `TextBox` pushes **one undo state per typed character**. Pressing Undo (Cmd/Ctrl+Z)
therefore removes a single letter at a time, which feels wrong: native text fields undo by
"action" (roughly a word, or a burst of typing between pauses), so a user who wants to drop the
last sentence/word doesn't have to mash Undo dozens of times.

We want themed `TextBox`es (DevExpress + macOS, and ideally all consumers of
`Devolutions.AvaloniaControls`) to undo at **word / typing-burst granularity**, matching the
intuition of native editors.

This is **not** scheduled into the current macOS context-menu PR — it gets its own branch + PR.

## Evidence (empirically confirmed)

A throwaway headless probe (`Avalonia.Headless.XUnit`) typed `"The quick brown fox jumps"` one
char at a time via `window.KeyTextInput(...)`, then drained `Undo()`:

```
Final text: 'The quick brown fox jumps'
Undo steps: 9   (UndoLimit=10 cap hit)
  jumps -> jump -> jum -> ju -> j -> (space) -> fox -> fo -> f -> 'brown '
```

i.e. **one undo state per character**, and `UndoLimit` (default **10**) means it can't even walk
back to empty for longer text.

## Root cause (looks like an unintentional regression)

In Avalonia 11.3.13 `src/Avalonia.Controls/TextBox.cs`:

- The class already contains coalescing machinery:
  - `private const int _maxCharsBeforeUndoSnapshot = 7;` (line ~350)
  - `_selectedTextChangesMadeSinceLastUndoSnapshot` counter
  - `SnapshotUndoRedo(bool ignoreChangeCount = true)` (line ~2332) only respects the counter
    when `ignoreChangeCount: false`.
  - `OnKeyDown` even calls `SnapshotUndoRedo(); // always snapshot in between words` on Space.
- **But** `CoerceText()` (line ~624) runs on *every* `TextProperty` mutation and calls
  `SnapshotUndoRedo()` with the default **`ignoreChangeCount: true`**, which snapshots
  unconditionally. Because every keystroke changes `Text`, the char-count threshold is never the
  deciding factor — the per-char snapshot in `CoerceText` always fires first. The coalescing
  constants are effectively **dead code**.

### Why it's probably unintentional
- Issue **#3370 "Don't create DispatcherTimers for TextBox undo/redo"** (closed 2021, by founder
  grokys) removed the per-instance `DispatcherTimer`. grokys described the *intended* design there:
  > "each text change pushes to the undo stack. If the change is made below a certain time
  > threshold then the undo stack entry is merged with the previous stack entry."
- Removing the timer removed the **time-based merge** but the `CoerceText` unconditional snapshot
  remained, so the char-based merge that was apparently meant to replace it never actually governs
  typing. The leftover `_maxCharsBeforeUndoSnapshot` constant strongly implies coalescing was
  intended but is now bypassed.

### Why we can't fix it from a theme/library
- All coalescing state (`_maxCharsBeforeUndoSnapshot`, the counter, `_undoRedoHelper`) is `private`.
- `CoerceText` is only overridable by **subclassing** `TextBox`; a theme/`ControlTheme` can't inject
  that into consumers' plain `TextBox`es.
- So fixing granularity on our side means **owning undo ourselves** (disable built-in, manage our
  own stack).

## Related Avalonia issues (searched 2026-06-09)

No existing issue specifically reports the per-character granularity / dead-code coalescing. Closest:

- **#3370** (closed) — removed the undo DispatcherTimer; origin of the lost time-based merge.
- **#5278** (open) — "Hitting undo immediately after typing loses typed text"; different symptom
  (redo loses text), same general area, from the 0.10 rc era.
- #9433, #336, #5795, #204 — programmatic undo/redo, bindings-vs-user input, disabling history,
  original implementation. None cover granularity.

➡️ **Action (needs user go-ahead): file a new upstream bug** — see draft below. It is worth
reporting since it appears to be an unintended regression with leftover dead code.

## Plan for the dedicated PR

### Approach: self-contained managed-undo attached behavior in the shared library
Location: `src/Devolutions.AvaloniaControls/Behaviors/TextBoxManagedUndo.cs` (shared by both themes
and any consumer).

- Attached `IsEnabledProperty` (bool). When true on a `TextBox`:
  - `SetCurrentValue(TextBox.IsUndoEnabledProperty, false)` to disable the broken built-in undo.
  - Maintain our own undo + redo stacks of snapshots `(text, caretIndex, selectionStart,
    selectionEnd)`.
  - Record snapshots from `TextProperty` changes using an **editor-style coalescing policy**:
    start a new undo group when, vs the previous edit, any of:
    1. **edit-kind change** (insert ↔ delete),
    2. **word boundary** (whitespace/newline/punctuation after non-boundary chars),
    3. **caret discontinuity** (edit not adjacent to previous — user clicked elsewhere),
    4. **time gap** > ~700 ms (typing pause);
    otherwise merge into the in-progress group.
  - Intercept Cmd/Ctrl+Z (undo) and Cmd+Shift+Z / Ctrl+Y (redo) via tunneling KeyDown.
  - Clean initial-load reset (subsumes today's `TextBoxUndoBehavior.ClearUndoOnLoaded`).
- Expose for the context menu:
  - `static ICommand Undo` / `static ICommand Redo` (operate on `CommandParameter` = the TextBox).
  - Attached `CanUndoProperty` / `CanRedoProperty` (bool) for `IsEnabled` bindings.

### Theme wiring (DevExpress + macOS, keep them consistent)
- TextBox style: replace `controls:TextBoxUndoBehavior.ClearUndoOnLoaded = True` with
  `…Behaviors:TextBoxManagedUndo.IsEnabled = True`.
- Context-menu **Undo** item → `Command = TextBoxManagedUndo.Undo`,
  `CommandParameter = {Binding $parent[TextBox]}`,
  `IsEnabled = {Binding $parent[TextBox].(…Behaviors:TextBoxManagedUndo.CanUndo)}`.
- **Recommended:** add a sibling **Redo** item (+ new `StringTextFlyoutRedoText` string in both
  themes, with the same overridable-localization comment pattern).
- Keep per-theme `TextBoxContextMenuCommands` (Delete / SelectAll) untouched.
- Remove the now-redundant per-theme `TextBoxUndoBehavior` once managed undo covers initial state.

### Validation
- Headless `ManagedUndoTests`: typing a sentence yields ~word-count (not char-count) undo states;
  redo restores; caret/selection restored; pause creates a boundary; click-then-type creates a
  boundary.
- Build both themes + SampleApp; run `DisplayName~TextBox` visual tests (popup menu → baselines
  unaffected).
- Manual eyeball via `/worksetup` (Classic/DevExpress + TextBox tab).

### Open decisions for the PR
- A) Include **Redo** in the context menu (recommended) or undo-only.
- B) Confirm coalescing policy (4-rule model) + ~700 ms pause threshold.
- C) Confirm home in shared `Devolutions.AvaloniaControls` (recommended) vs per-theme duplication.
- D) Should managed undo be **opt-in** (attached prop only set by our themes) or also exposed as a
  documented public API for consumers' own `TextBox`es?


