# macOS native context-menu commands for TextBox

> Status: **Upcoming** — research overview + starting points. Captures the feasibility study done
> while shipping the themed macOS TextBox context menu (Undo/Cut/Copy/Paste/Delete/Select All).
> No further research needed before this is picked up; this is the jumping-off point.

## Goal / context

On macOS, native text fields show a large OS-provided context menu (Look Up, Translate, Search With
Google, Cut/Copy/Paste, Share, Writing Tools, Proofread, Rewrite, Spelling & Grammar, Substitutions,
Transformations, Speech, AutoFill, Services…). We were asked whether an Avalonia `TextBox` can
surface those, and — for the ones that can't come "for free" — which we could realistically add
ourselves.

**Key conclusion (already decided):** we **cannot** obtain the whole native menu, because Avalonia
doesn't use a native `NSTextView`. We shipped the curated themed menu instead (see the macOS theme
`TextBox.axaml`). This doc records the per-item feasibility and starting points so individual native
extras can be added later, piecemeal, if desired.

### Why the full native menu is unobtainable
That menu is assembled by AppKit around a real `NSTextView` via `-[NSTextView menuForEvent:]`,
walking the responder chain and consulting `NSTextInputClient`, the selection's
`NSAttributedString`, `NSSpellChecker`, the Writing Tools coordinator, the Translation framework,
`NSServices`, etc. Avalonia renders the whole window as a single Skia-drawn `NSView` (`AvnView`);
the `TextBox` is a custom Avalonia control, **not** an `NSTextField`/`NSTextView`. There is no
native text responder for AppKit to build the menu against, and no public API that says "give me the
standard text context menu for this string." The only way to get the *real* menu is to host an
actual `NSTextView` via `NativeControlHost`, which throws away all Avalonia theming/layout for that
field — not viable for a themed control.

## Feasibility overview

We already have a proven P/Invoke pattern for this in the repo:
`src/Devolutions.AvaloniaTheme.MacOS/Internal/WallpaperTintApplier.cs` uses
`objc_getClass` / `sel_registerName` / `objc_msgSend` against `/usr/lib/libobjc.A.dylib` plus
various framework dylibs. Native menu items below would follow the same approach. The window's
`NSView` is reachable via `TopLevel.TryGetPlatformHandle()`.

| Item | Difficulty | Notes / native API |
|------|-----------|--------------------|
| **Make Upper/Lower Case, Capitalize** (Transformations) | 🟢 Easy | Pure C# `string` ops — **no native interop**. |
| **Smart quotes / dashes toggling** (Substitutions) | 🟢 Easy | Pure C# string manipulation; could be a managed feature. |
| **Speech → Start/Stop Speaking** | 🟢 Easy | `NSSpeechSynthesizer` or `AVSpeechSynthesizer` via objc_msgSend. Self-contained. |
| **Spelling & Grammar** suggestions | 🟡 Moderate | `NSSpellChecker` shared instance; we build the submenu ourselves from returned guesses. |
| **Look Up "word"** | 🟡 Moderate | `-[NSView showDefinitionForAttributedString:atPoint:]` — needs the `NSView` + a screen point. |
| **Share…** | 🟡 Moderate | `NSSharingServicePicker` shown relative to a rect in the `NSView`. |
| **Translate "word"** | 🔴 Hard | Translation framework is Swift/async; no clean P/Invoke path. |
| **Writing Tools / Proofread / Rewrite** | 🔴 Hard | `WTWritingToolsViewController` (macOS 15.1+) needs a coordinator/delegate bound to a native text view. |
| **AutoFill** | ⚫ Impossible (practically) | Bound to responder chain / credential providers; no standalone API. |
| **Services** | ⚫ Impossible (practically) | Driven by `NSServicesMenuRequestor` on the responder chain. |
| **Search With Google** | 🟢 Easy (DIY) | Not really "native" — just open a search URL with the selection. Trivial in managed code. |

Legend: 🟢 Easy · 🟡 Moderate · 🔴 Hard · ⚫ Impossible-in-practice.

## Recommended sequencing for future work

1. **Managed-only wins first (no interop, cross-platform-safe):**
   - Transformations (Upper/Lower/Capitalize) and Substitutions (smart quotes/dashes). These are the
     most broadly useful for the consuming app and carry zero native risk. They could even live in
     the shared `Devolutions.AvaloniaControls` library rather than the macOS theme.
   - Optionally a DIY "Search With Google/…" that opens the default browser with the selection.
2. **Self-contained native extras (macOS theme only, guarded by OS checks):**
   - **Look Up**, **Share…**, **Speech** — highest "feels native" payoff, each independent.
3. **Skip unless there's strong demand:** Translate, Writing Tools/Proofread/Rewrite, AutoFill,
   Services — high effort, fragile, version-gated, and several are unreachable without a native
   responder.

## Starting points for the feasible items

All native calls should follow `WallpaperTintApplier.cs`: declare the `objc_*` P/Invokes, guard with
OS-version / selector-existence checks (`respondsToSelector:`), and fall back gracefully when a
selector/framework is unavailable. Wrap each feature so a failure is a no-op, never a crash.

### Managed (no interop)
- **Transformations:** operate on `TextBox.SelectedText` (or whole `Text` when no selection) using
  `CultureInfo`-aware `ToUpper`/`ToLower`/title-casing, then write back via
  `SetCurrentValue(TextBox.TextProperty, …)` and restore selection — mirror the existing
  `TextBoxContextMenuCommands.DeleteSelection` command pattern in the macOS theme.
- **Substitutions:** same write-back pattern; swap straight quotes/dashes for typographic ones (and
  vice-versa) over the selection.

### Speech (Start/Stop Speaking)
- `objc_getClass("NSSpeechSynthesizer")`, `alloc`/`init`, then
  `startSpeakingString:` with an `NSString` built from the selection (or `AVSpeechSynthesizer` +
  `AVSpeechUtterance` if preferred). Keep a reference so it isn't GC'd mid-speech; expose a "Stop
  Speaking" that calls `stopSpeaking`.

### Look Up
- Get the `NSView` from `TopLevel.TryGetPlatformHandle().Handle`.
- Build an `NSAttributedString` from the selected text.
- Call `-[NSView showDefinitionForAttributedString:atPoint:]`; the point is the caret/selection
  location in the view's (flipped) coordinate space — compute from the pointer position or the
  caret rect.

### Share…
- Construct `NSSharingServicePicker` with an `NSArray` of items (the selected `NSString`).
- Show it via `-[NSSharingServicePicker showRelativeToRect:ofView:preferredEdge:]` using the
  selection's rect within the `NSView`.

### Spelling & Grammar
- `+[NSSpellChecker sharedSpellChecker]`, then `guessesForWordRange:inString:language:inSpellDocumentWithTag:`
  (or `checkSpellingOfString:startingAt:`) to get candidates; build a submenu of `MenuItem`s whose
  commands replace the misspelled range via the managed write-back pattern.

## References

- `src/Devolutions.AvaloniaTheme.MacOS/Internal/WallpaperTintApplier.cs` — existing objc_msgSend /
  CoreFoundation / framework-dylib P/Invoke pattern to copy from (class lookup, selector
  registration, `respondsToSelector:` guarding, `CFRelease` discipline).
- `src/Devolutions.AvaloniaTheme.MacOS/Controls/TextBox.axaml` — the themed flyout these would extend
  (the `DefaultTextBoxContextFlyout` resource).
- `src/Devolutions.AvaloniaTheme.MacOS/Controls/TextBoxContextMenuCommands.cs` — the existing
  `ICommand` + selection-write-back pattern to mirror for managed commands.
- `TopLevel.TryGetPlatformHandle()` — obtains the `NSView` handle for native calls.

## Open decisions for whoever picks this up
- Which items are actually wanted by the consuming app? (Substitutions was flagged as "quite useful";
  Transformations likely too.)
- Managed Transformations/Substitutions in the **shared library** (cross-theme) vs **macOS theme
  only**? (They're not macOS-specific, so shared may be better.)
- Minimum macOS version to support / guard against for the native extras.
- Whether native extras should be opt-in via an attached property so consumers can disable them.
