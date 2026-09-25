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

- **Batch 1 — Core input:** Button, CheckBox, RadioButton, ToggleButton
  (extend), ComboBox, TextBox, NumericUpDown
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
   entries) and Avalonia DevTools MCP screenshots; then flip the page's
   `"WinUI"` status in `page-catalog.jsonc` from `❌`/`🚧` to `✅` (fully
   verified) or `⚠️` (stable enough to guard, but not perfect — still
   included in tests). Leave it `🚧` if the control genuinely isn't ready
   to be held to a baseline yet.
9. Add/extend visual regression baselines for the control being styled once
   its status is `✅`/`⚠️` (`UPDATE_BASELINES=true dotnet test ...` on each
   OS). If this is the **first** control to leave `🚧`, also add
   `ThemeId.WinUi` to `VisualDiscoveryThemes` in
   `tests/Devolutions.AvaloniaControls.VisualTests/PageDiscoveryTests.cs` in
   the same PR (see "Resolved" note below for why it's deliberately absent
   until then).
10. Update `src/Devolutions.AvaloniaTheme.WinUI/CHANGELOG.md` per existing
    convention; confirm with the user whether an entry is warranted per the
    "substantial change" threshold noted in repo instructions, don't add
    automatically for routine per-control work.

## Resolved

- **Visual regression harness knows how to test WinUI, but stays inert
  until a control is verified.** `ThemeId.WinUi` is in
  `SupportedThemes` (`VisualRegressionTests.cs`); each WinUI page would
  capture 4 screenshots per test (classic light/dark + Mica light/dark,
  suffixes `""`/`"_dark"`/`"_mica"`/`"_mica_dark"`) under one `WinUI`
  baseline folder, using `App.SetTheme(new WinUiClassicTheme()/WinUiMicaTheme())`
  the same way the SampleApp dropdown does — no direct
  `Windows11MicaDetector` manipulation needed in the test.
  `PageDiscoveryTests.cs`'s strict per-theme discovery check
  (`VisualDiscoveryThemes`) intentionally does **not** include `ThemeId.WinUi`
  yet, since that check asserts every listed theme has ≥1 testable page —
  add it there once the first control below is verified and flipped to
  `✅`/`⚠️` (that PR should add `ThemeId.WinUi` to both
  `VisualDiscoveryThemes` and generate its own baselines in the same
  change).
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
- **Merging with #669:** #669 still uses the single `"WinUI"` column and `WinUI/…_mica*.png`
  baselines. Whichever lands second must convert: `"WinUI": X` → `"WinUIClassic": X, "WinUIMica": X`,
  move `WinUI/*_mica*.png` → `WinUiMica/*.png` (dropping the `_mica` infix), and move the non-mica
  files to `WinUiClassic/` (or drop them if classic is `↔️`).
