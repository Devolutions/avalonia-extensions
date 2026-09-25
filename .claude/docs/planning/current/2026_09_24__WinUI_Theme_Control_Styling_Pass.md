# WinUI theme — control-by-control visual styling pass

## Problem

`Devolutions.AvaloniaTheme.WinUI` has only the basic scaffolding from PR #564:
project setup, the `DevolutionsWinUiTheme`/`DevolutionsWinUiThemeGlobalStyles`
loaders, and the classic/Win11-Mica split (`Windows11MicaDetector` +
`ThemeResources.Windows11.axaml`). Three controls (`ListBox`, `ToggleButton`,
partial `DataGrid`) do have some existing styling in
`src/Devolutions.AvaloniaTheme.WinUI/Controls/`, but it was copied from
UniGetUI's Avalonia port and never verified against real WinUI — each of
those files now carries a header comment saying so. Those three are
marked `"WinUI": "🚧"` (in progress, unverified, excluded from visual
regression tests) in `samples/SampleApp/PageCatalog/page-catalog.jsonc`,
same as everything else needing a pass — MacOS, DevExpress, and Linux are
`"✅"` almost everywhere by comparison. The old code is kept in place purely
as a reference/starting point, not as a target look to preserve.

"WinUI 3" (the actual Microsoft design system/toolkit) is not a fourth theme —
it's the thing this theme is already emulating (see prior discussion in this
session). This plan is about carrying the existing scaffolding through to a
complete, real theme, control by control.

The work needs to be doable largely independently, without guessing at visual
details, even though the assistant runs on macOS and cannot execute the WinUI 3
Gallery Windows app directly.

## Research resources

Ranked by authority / ease of automated access:

1. **`microsoft/microsoft-ui-xaml` GitHub repo — ground truth templates.**
   `controls/dev/CommonStyles/<Control>_themeresources.xaml` (e.g.
   `Button_themeresources.xaml`, `CheckBox_themeresources.xaml`,
   `ComboBox_themeresources.xaml`) contains the literal WinUI 3 control
   templates: `VisualStateManager` states, template parts, and the exact
   brush/resource key names referenced. This is the single best source
   because it maps directly onto the "Naming Rules" and "port only what is
   needed" workflow already documented in
   [.github/skills/winui-control-theming/SKILL.md](/Users/amalchowperryman/git/avalonia-extensions.worktrees/winui-theme-recovery-check/.github/skills/winui-control-theming/SKILL.md).
   Fetch via `raw.githubusercontent.com/microsoft/microsoft-ui-xaml/main/controls/dev/CommonStyles/<file>`
   or the GitHub contents API (both worked fine from this session — no auth
   needed for public repo reads).
   Some controls don't have their own file and instead share
   `Common_themeresources.xaml` — check there first if a per-control file
   doesn't exist.

2. **`microsoft/WinUI-Gallery` GitHub repo — real usage samples.**
   `WinUIGallery/Samples/<Control>/` (MIT licensed, one folder per control,
   matching almost 1:1 with our control set) has the actual XAML markup the
   gallery app uses to demonstrate each control, including secondary
   variants (e.g. multiple `Button` styles, `ComboBox` states side by side).
   Useful for understanding intended composition and default property
   values, not just raw template internals. Treat as reference for
   understanding intent, not a source to copy verbatim.

3. **Microsoft Learn "Controls and patterns" docs.**
   `https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/<control>`
   (index at `.../controls/`) — short design guidance per control plus one
   static screenshot (`images/<control>.png`). `web_fetch` only returns
   markdown/alt-text for these images; use the browser tools
   (`openBrowserPage` + `screenshotPage`) to actually see the rendered
   screenshot when a quick visual gut-check is needed.

4. **Fluent 2 design site (`fluent2.microsoft.design`).**
   The actual component specs/redlines live behind Figma UI kits, which
   aren't fetchable by automated tools without a Figma account. Treat as
   optional manual reference only (e.g. if the user wants to open Figma
   themselves for a token value), not a blocking dependency for any batch.

5. **WinUI 3 Gallery Windows app (live, interactive).**
   Best fidelity for things static sources can't show well: hover/pressed/
   disabled/focus-visual states, animations, and Mica behavior on real
   Windows 11. Requires a live Windows host and isn't reachable from this
   Mac session. Use only as a tie-breaker for ambiguous interactive detail
   once 1–3 disagree or are silent on something specific.

### Windows VM option

This assistant can't remote-control the offered Windows VM from the current
Mac session. Two practical ways to still use it if resource 1–4 leave a gap:
- The user runs the Gallery app there and shares/pastes a screenshot into the
  chat (an image passed via chat can be viewed directly with the `view` tool).
- Start a separate session whose workspace targets the VM (`create_session`
  with `workspace` pointing at that machine, if the harness has connectivity
  to it), so an agent instance running there can build/run/screenshot the
  real app using its own tools.

Recommend defaulting to resources 1–3 for the bulk of the work and reserving
the VM/Gallery app for genuine tie-breaks, to keep the work independent.

## Scope

61 total catalog entries in `page-catalog.jsonc`; after excluding per-theme
`About`/`Menu`/`ContextMenu`/`MenuFlyout` clones (🪟/🍏/🐧 prefixed — those
track native-menu-pack demos, not general control styling) and the
Experiments-only pages (ActiPro Controls, System Colours, Toggle Buttons,
Search Highlights, Animated Icon, LG Wallpaper Tint), **~38 real control demo
pages** need a WinUI pass:

Already have existing (unverified, UniGetUI-derived) styling to treat as
reference only — verify from scratch against WinUI-Gallery, don't assume
correctness:
`ListBox` (`Controls/ListBox.axaml`), `ToggleButton` (`Controls/ToggleButton.axaml`),
`DataGrid` (`Controls/DataGrid.axaml`, partial — column header styling only).

Everything else is untouched. Suggested batching for PR-sized delivery
(mirrors the existing repo pattern of per-control-family PRs, e.g. "TreeDataGrid
header styling fixes", "Extract RectangleSelectionMarquee control"):

- **Batch 1 — Core input:** Button 🚧 (structurally done, fidelity pass
  pending — see "Handoff" section below), CheckBox 🚧 (same),
  RadioButton 🚧 (same), ToggleButton (extend), ComboBox, TextBox,
  NumericUpDown
- **Batch 2 — Selection/collection:** ListBox (extend), TreeView,
  TreeDataGrid, DataGrid (extend), DataGrid grouped, GridSplitter
- **Batch 3 — Menus & flyouts:** Menu, ContextMenu, MenuFlyout,
  FlyoutPresenter, DropDownButton, SplitButton
- **Batch 4 — Pickers:** CalendarDatePicker, TimePicker, TimePickerUpDown,
  ColorPicker, EnumPicker
- **Batch 5 — Remaining/finishing:** ScrollBar, ScrollViewer, Expander,
  ToolTip, Separator, TabControl / TabPane / NavBar, TagInput,
  SearchHighlightTextBlock, AutoCompleteBox, MultiComboBox, GroupedComboBox,
  EditableComboBox, CheckBoxListBox, GroupedListBox, GroupedTileListBox,
  HyperlinkButton, Control Alignment (sanity page, no dedicated control)

## Approach (repeat per control)

1. Pick the next control from the current batch.
2. Pull the real template from `microsoft-ui-xaml`
   (`<Control>_themeresources.xaml`, falling back to
   `Common_themeresources.xaml` if no dedicated file exists) — note
   `VisualState`s, template parts, and brush keys referenced.
3. Cross-check against the matching `WinUI-Gallery` sample folder for
   real-world usage/defaults.
4. Optionally spot-check the Microsoft Learn screenshot or (rarely) the
   Windows VM Gallery app for ambiguous interactive states.
5. Diff against the existing Avalonia Fluent `ControlTheme` for the same
   control (already the fallback) to scope the actual change — follow the
   existing "Naming Rules" / "port only what is needed" workflow in the
   `winui-control-theming` skill.
6. Implement/extend `Controls/<Control>.axaml`; add new shared tokens to
   `Accents/ThemeResources.axaml` only if genuinely reusable; add matching
   Mica overlay entries to `Accents/ThemeResources.Windows11.axaml` in the
   **same change**, per the skill's Mica rules (never let base/overlay drift).
7. Wire the new file into `Controls/_index.axaml`.
8. Verify visually via the SampleApp (WinUI classic + WinUI Mica dropdown
   entries) and Avalonia DevTools MCP screenshots; then set the page's
   `"WinUI"` status in `page-catalog.jsonc` to `🚧` (in progress, excluded
   from tests — still shows a visual "some work started" indicator). **Do
   not flip a control past `🚧` yourself.** Upgrading to `⚠️` (stable
   enough to guard, included in tests) or `✅` (fully verified) is always
   initiated by the user, once they've visually confirmed the styling is
   actually correct (e.g. against the live Gallery app on Windows) — not by
   the agent that did the styling work. This keeps early iterations from
   being slowed down by baseline-update churn: while a control sits at
   `🚧` there's no baseline to keep in sync, so quick back-and-forth style
   tweaks don't require regenerating screenshots each time.
   Button/CheckBox/RadioButton were briefly marked `✅` this way in this
   project's first PR and had to be walked back after user QA found real
   WinUI fidelity gaps — see the "Handoff" section below for the concrete
   example.
9. Once the user confirms a control is ready and flips it to `⚠️`/`✅`,
   add/extend its visual regression baselines
   (`UPDATE_BASELINES=true dotnet test ...` on each OS). If this is the
   **first** control to leave `🚧`, also add `ThemeId.WinUi` to
   `VisualDiscoveryThemes` in
   `tests/Devolutions.AvaloniaControls.VisualTests/PageDiscoveryTests.cs` in
   the same PR (see "Resolved" note below for why it's deliberately absent
   until then). Don't worry about `VisualDiscoveryThemes`/`GetTestPages()`
   asserting a WinUI page exists while iterating with everything at `🚧`
   still on a draft branch — that only needs to hold by the time the
   branch is actually reviewed for merge (and if nothing has reached
   `⚠️`/`✅` by then, there's no reason to merge yet anyway).
10. Update `src/Devolutions.AvaloniaTheme.WinUI/CHANGELOG.md` per existing
    convention; confirm with the user whether an entry is warranted per the
    "substantial change" threshold noted in repo instructions, don't add
    automatically for routine per-control work.

## Resolved

- **Button, CheckBox, and RadioButton are done (first Batch 1 PR).** All
  three were implemented from scratch (no existing file) by overriding each
  control's stock Avalonia Fluent `ControlTheme` (`BasedOn`) with real WinUI
  Fluent 2 values, since Avalonia's Fluent templates for these three controls
  already use the exact same resource-key names and template-part shape as
  real WinUI (`Button.xaml`/`CheckBox.xaml`/`RadioButton.xaml` — verified
  against the pinned `12.1.2` tag). New shared tokens added to
  `Accents/ThemeResources.axaml` for reuse by later controls:
  `ControlFillColor*`, `ControlStrokeColor*` (+ paired `Color` keys for
  gradient stops), `ControlStrongStrokeColor*`, `ControlAltFillColor*`,
  `ControlCornerRadius`, `ControlFillColorTransparentBrush`,
  `ControlElevationBorderBrush`/`AccentControlElevationBorderBrush`/
  `CircleElevationBorderBrush` (the gradient "elevation" borders). No Mica
  overlay entries were needed for any of the three — none of their brushes
  are Mica-sensitive (see the "Note" below on why Mica diffs may not show up
  until a control actually uses one of the Mica-swapped resources).
  **Gotcha hit and resolved:** don't alias real-WinUI resource keys (e.g.
  `ButtonBackground` → `ControlFillColorDefaultBrush`) via the
  `<StaticResource x:Key="X" ResourceKey="Y"/>` **element** syntax across
  files — it throws `KeyNotFoundException` at runtime when the alias and its
  `ThemeDictionaries`-scoped target live in different `MergeResourceInclude`d
  files. Override the `ControlTheme`'s `Setter`s/nested `Style` selectors
  directly with `{DynamicResource ...}` **attribute** syntax instead
  (matches the existing `DataGrid.axaml`/`ListBox.axaml`/`ToggleButton.axaml`
  precedent) — bypass the intermediate `ButtonBackground`-style key layer
  entirely rather than trying to redefine it.
  `ThemeId.WinUi` was added to `VisualDiscoveryThemes` in
  `PageDiscoveryTests.cs` and baselines were generated
  (`UPDATE_BASELINES=true dotnet test ...`) in the same PR, per the note
  below.
- **Visual regression harness knows how to test WinUI, but stays inert
  until a control is verified.** `ThemeId.WinUi` is in
  `SupportedThemes` (`VisualRegressionTests.cs`); each WinUI page would
  capture 4 screenshots per test (classic light/dark + Mica light/dark,
  suffixes `""`/`"_dark"`/`"_mica"`/`"_mica_dark"`) under one `WinUI`
  baseline folder, using `App.SetTheme(new WinUiClassicTheme()/WinUiMicaTheme())`
  the same way the SampleApp dropdown does — no direct
  `Windows11MicaDetector` manipulation needed in the test.
  `PageDiscoveryTests.cs`'s strict per-theme discovery check
  (`VisualDiscoveryThemes`) intentionally did **not** include `ThemeId.WinUi`
  until Button/CheckBox/RadioButton were verified and flipped to `✅` (see
  note above) — now included.
- **Status-symbol semantics for test inclusion were tightened.**
  `🚧` (in progress / actively unverified) is now **excluded** from visual
  regression tests via `PageCatalogEntry.ShouldTest`
  (`PageRegistry.IsInProgressSymbol`), separate from `⚠️` (imperfect but
  stable enough to guard existing coverage), which stays **included**. This
  lets a control show a "some work has started" indicator without forcing a
  baseline to be committed before it's ready. `IsNotSupportedSymbol`
  (`""`/`❌`) is unchanged.
  All 3 controls with existing UniGetUI-derived styling (DataGrid, ListBox,
  ToggleButton) are marked `"WinUI": "🚧"` — visually indicating "something
  is there" while being excluded from tests until each is actually verified
  against real WinUI. Each control file
  (`Controls/DataGrid.axaml`/`ListBox.axaml`/`ToggleButton.axaml`) also has a
  header comment stating its styling was copied from UniGetUI's Avalonia
  port, is unverified, and should not be assumed correct or complete. The
  old code is kept only as a reference, not deleted.
  Generate baselines (all 3 OSes, via `UPDATE_BASELINES=true dotnet test ...`)
  as part of each control's own PR once its styling is verified and its
  status flips to `✅`/`⚠️` — not before, and not from the old
  UniGetUI-derived code.
  Note (observed while sanity-checking the harness, then discarded): classic
  vs. Mica captures were visually identical or near-identical on the current
  3 pages, because the only Mica-swapped brushes today
  (`SettingsCardBackground`, `SettingsCardHoverBackground`) are only
  referenced by pseudo-classes (`:checked`, `:pressed`) that a static
  capture doesn't exercise. Expect real Mica diffs once a control PR adds
  Mica-sensitive hover/checked/pressed brushes.

## Handoff — 2026-09-25: Button/CheckBox/RadioButton need a fidelity pass on real Windows

**Status:** PR #669 (`agents/winui-button-checkbox-radio` branch, pushed as
draft) implements Button/CheckBox/RadioButton and is open for review, but
the user's visual QA against real Windows (not yet the Gallery app — that
part is still pending) found it's **not there yet**:

- All three "look much closer to [Avalonia's stock] Fluent than WinUI".
- Button specifically: the top edge reads as a heavy/dark inset line, making
  it look like an input field (recessed) rather than a raised button. This
  is very likely the default Avalonia Fluent `Button` template's border
  treatment (`BorderBrush`/`BorderThickness` combo inherited unchanged from
  the base `ControlTheme`) — real WinUI buttons in Light theme use a subtle
  **bottom**-only or very-low-contrast full border, not a dark top edge. Was
  not caught during this session's verification because DevTools screenshots
  were only eyeballed at a glance, not compared side-by-side against a real
  Windows screenshot.
- No hover/press/checked-state **animations** — real WinUI uses ~150-250ms
  brush-color/opacity easing transitions (implemented in real XAML via
  `VisualTransition`/`Storyboard` in the `VisualStateManager`, and in
  Avalonia terms would be `Transitions`/`ColorTransition` on the relevant
  template parts, likely added at the base Avalonia Fluent theme level
  already for pointerover/pressed — needs checking whether Avalonia's stock
  theme already had transitions we accidentally suppressed by fully
  replacing brushes without touching `Transitions`, or whether they were
  simply never present and need adding).
- **What did land correctly:** the system accent color flows through
  correctly (confirmed via DevTools `props` inspection this session), and
  per the user, "a good start" overall — the state-based resource wiring
  (Normal/PointerOver/Pressed/Disabled/Checked) and shared-token
  infrastructure in `ThemeResources.axaml` are sound; what's missing is
  fine-tuning of exact values/borders and the animation layer, not a
  structural redo.

**Decision:** the fine-tuning pass will continue directly on the user's
Windows VM (not this Mac session), so an agent there can install/run the
**WinUI 3 Gallery** Windows app and feed in direct screenshots for
comparison, rather than relying on static GitHub source + guesswork. The
`Screenshots + WinUI-Gallery source` combination this Mac session used
(research resources 1–3 above) got the structural wiring right but isn't
sufficient on its own for pixel/animation-level fidelity — resource 5 (the
live Gallery app) is now the primary tool, not a rare tie-breaker.

### What a new agent picking this up (on the Windows VM) needs to know

- **Branch:** `agents/winui-button-checkbox-radio`, already pushed; PR #669
  is open as a **draft** — push further fixup commits to the same branch/PR
  rather than opening a new one, unless the user says otherwise.
- **Button/CheckBox/RadioButton were downgraded from `"WinUI": "✅"` to
  `"🚧"` in `page-catalog.jsonc`** — back to "in progress", excluded from
  visual regression tests, since the earlier fidelity gaps mean they
  shouldn't be held to a baseline while actively being reworked. Their
  existing baselines
  (`tests/Devolutions.AvaloniaControls.VisualTests/Screenshots/Baseline/*/WinUI/`)
  are now stale reference-only artifacts, not enforced by tests — regenerate
  them once the control is confirmed ready (see below), don't try to keep
  them in sync while iterating at `🚧`.
  **Status upgrades past `🚧` (to `⚠️` or `✅`) are always initiated by the
  user, not by the agent doing the styling work** — once a fidelity pass is
  done and the user has visually confirmed it (e.g. against the live
  Gallery app), ask them which status it warrants rather than deciding
  unilaterally. This is deliberate: while a control sits at `🚧` there's no
  baseline to keep in sync, so quick iteration on borders/colors/animations
  isn't slowed down by baseline-update churn on every tweak.
  Note: with all three back at `🚧`, `ThemeId.WinUi` currently has zero
  `ShouldTest`-eligible pages again, which `PageDiscoveryTests.cs`'s
  `VisualDiscoveryThemes` list will complain about if a full test run
  happens while it's still listed there. **Don't "fix" this by removing
  `ThemeId.WinUi` from `VisualDiscoveryThemes`** — that's expected/fine
  during active iteration on a draft branch; it only needs to resolve by
  the time this branch is actually reviewed for merge (and if nothing has
  reached `⚠️`/`✅` by then, the branch isn't ready to merge yet anyway).
- **Files to revisit:**
  [Button.axaml](/Users/amalchowperryman/git/avalonia-extensions.worktrees/winui-theme-recovery-check/src/Devolutions.AvaloniaTheme.WinUI/Controls/Button.axaml),
  [CheckBox.axaml](/Users/amalchowperryman/git/avalonia-extensions.worktrees/winui-theme-recovery-check/src/Devolutions.AvaloniaTheme.WinUI/Controls/CheckBox.axaml),
  [RadioButton.axaml](/Users/amalchowperryman/git/avalonia-extensions.worktrees/winui-theme-recovery-check/src/Devolutions.AvaloniaTheme.WinUI/Controls/RadioButton.axaml)
  — all under `src/Devolutions.AvaloniaTheme.WinUI/Controls/`. Shared tokens
  they draw on live in
  [ThemeResources.axaml](/Users/amalchowperryman/git/avalonia-extensions.worktrees/winui-theme-recovery-check/src/Devolutions.AvaloniaTheme.WinUI/Accents/ThemeResources.axaml).
- **Specific things to check against the Gallery app first:**
  1. `Button`'s border: compare `BorderBrush`/`BorderThickness` Setters
     (currently likely still inheriting Avalonia's stock values rather than
     being explicitly overridden) against what real WinUI actually renders
     — check `Button_themeresources.xaml` for `ButtonBorderBrush`/
     `ButtonBorderThemeThickness` and whether it's uniform or asymmetric.
  2. Whether Avalonia's stock Fluent `Button`/`CheckBox`/`RadioButton`
     `ControlTheme`s already declare `Transitions` on the relevant template
     parts (check the pinned `12.1.2` tag source, same URLs used this
     session) — if yes, confirm our `BasedOn` overrides aren't accidentally
     dropping them; if no, add `ColorTransitions`/`BrushTransitions` with
     durations matching real WinUI's `ControlFastAnimationDuration` /
     `ControlNormalAnimationDuration` resources (defined in
     `Common_themeresources_any.xaml` — fetched already this session, values
     were noted but not re-verified here; re-check exact ms values).
  3. Re-screenshot all three side-by-side with the Gallery app in both Light
     and Dark before re-flipping any status/baseline.
- **Everything else from this pass should still hold:** the resource-key
  names, template-part names, and per-state wiring pattern (override
  `Setter`s/nested `Style` selectors with `{DynamicResource}` **attribute**
  syntax, never `<StaticResource x:Key=".." ResourceKey=".."/>` **element**
  syntax across files — see the "Gotcha" note above, still valid) don't need
  to be re-derived; this is a values/animation refinement, not a rewrite.
- **Full research-resources list and workflow steps above (sections
  "Research resources" and "Approach") still apply** — the Gallery app is
  now resource 5 promoted to primary use for this refinement, not a
  replacement for reading `microsoft-ui-xaml` source for the exact resource
  keys/values to plug in.

## Risks / open questions

- Some controls are compositional (built from primitives already styled
  elsewhere) and won't have a dedicated file in `microsoft-ui-xaml` or
  `WinUI-Gallery` — handle case by case, note it in the PR when it happens.
- Fluent 2 Figma kit is not automatable; don't block a batch on it.
- Re-enabling the WinUI package in `.github/workflows/build-package.yml`
  (removed in PR #585) is **out of scope** for this pass — call it out as its
  own explicit future decision/PR once enough batches land, don't
  reintroduce it silently as a side effect of a control PR.

## Todos

Tracked in SQL (`todos` table) — one entry per batch above, plus a
preliminary "confirm/add WinUI to visual regression harness" todo that
should land before or alongside Batch 1.

## Classic vs Mica: separate status columns and `↔️` (supersedes the single `"WinUI"` column above)

Decided 2026-09-25. References to a single `"WinUI"` status column / one `WinUI` baseline folder
with `_mica` suffixes elsewhere in this doc are superseded by this section.

- **"Classic" = WinUI 3 on a solid backdrop, not Win10-native.** Real WinUI 3 uses the same
  (translucent) control resources on Win10 and Win11; only the window backdrop differs (solid vs
  Mica). Classic also covers Windows Server 2019/2022 and Win11 with Mica unavailable
  (transparency effects off, RDP, battery saver). So differences are expected only on backdrop-like
  surfaces (page/window backgrounds, cards/layers, possibly acrylic flyouts/menus).
- **Separate catalog columns:** `WinUIClassic` and `WinUIMica` (`ThemeId.WinUiClassic` /
  `ThemeId.WinUiMica`), mirroring `MacClassic`/`LiquidGlass`. Each has its own baseline folder
  (`Baseline/<OS>/WinUiClassic/`, `.../WinUiMica/`). `"WinUI"` remains only the XAML
  `ThemeIsOneOf` family name, not a catalog column.
- **`↔️` = "same as WinUIMica"** (valid only in `WinUIClassic`): inherits Mica's status, stores no
  classic baselines; the visual test renders both variants (Light + Dark) and fails if they aren't
  pixel-identical. This catches later drift, but **cannot** catch a Mica-only look wrongly placed in
  the shared base resources (both would match while being wrong).
- **Therefore `↔️` is a deliberate, manual decision**, never a default: while a control is being
  styled, both columns carry the same status (agents stop at `🚧`; upgrades are user-initiated).
  Once the user considers Mica done, they ask for a classic review against the WinUI source/docs;
  if nothing should differ, classic becomes `↔️`, otherwise classic gets its own work/status/
  baselines.
- **Guard when porting from Win11 Gallery screenshots:** trace each look to its source —
  `*_themeresources.xaml` values go in the base `ThemeResources.axaml`; only backdrop-show-through
  effects go in the Mica overlay. Documented in the `winui-control-theming` skill.
- **Seeing classic on Win11:** turning off "Transparency effects" should make the Gallery fall back
  to a solid backdrop (to verify per surface on the VM).
- **Harness:** `PageDiscoveryTests.VisualDiscoveryThemes` gets `WinUiMica` (and `WinUiClassic`)
  once the first WinUI page leaves `🚧`.
- **Follow-up (not done):** `Windows11MicaDetector` only checks the OS build, not runtime
  conditions (transparency effects off, RDP, battery saver) where real WinUI falls back to a solid
  backdrop.
- **Merging with #669: done.** #669 was rebased onto this (2026-09-25) and converted as prescribed:
  `"WinUI": X` → `"WinUIClassic": X, "WinUIMica": X` for every page, `WinUI/*_mica*.png` →
  `WinUiMica/*.png` (dropping the `_mica` infix), and the non-mica files → `WinUiClassic/`.
  #669's own `GetWinUiCapturePlan()` / `WinUiCapturePlan_HasClassicAndMicaVariants` were dropped in
  favour of the per-variant cases introduced here. Classic was given the same status as Mica
  (the mechanical conversion) — it has **not** had the classic review that `↔️` requires.
