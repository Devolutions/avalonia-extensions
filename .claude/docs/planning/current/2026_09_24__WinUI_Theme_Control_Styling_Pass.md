# WinUI theme — control-by-control visual styling pass

## Problem

`Devolutions.AvaloniaTheme.WinUI` started with the basic scaffolding from PR #564:
project setup, the `DevolutionsWinUiTheme`/`DevolutionsWinUiThemeGlobalStyles`
loaders, and the classic/Win11-Mica split (`Windows11MicaDetector` +
`ThemeResources.Windows11.axaml`). Three controls (`ListBox`, `ToggleButton`,
partial `DataGrid`) do have some existing styling in
`src/Devolutions.AvaloniaTheme.WinUI/Controls/`, but it was copied from
UniGetUI's Avalonia port and never verified against real WinUI — each of
those files now carries a header comment saying so. Those three are
marked `🚧` in both WinUI catalog columns (in progress, unverified, excluded
from visual regression tests). Button is now `WinUIMica: ✅` and
`WinUIClassic: ↔️`; CheckBox and RadioButton are `❌` in both columns pending
separate fidelity passes. The old UniGetUI code is kept purely as a
reference/starting point, not as a target look to preserve.

"WinUI 3" (the actual Microsoft design system/toolkit) is not a fourth theme —
it's the thing this theme is already emulating (see prior discussion in this
session). This plan is about carrying the existing scaffolding through to a
complete, real theme, control by control.

The work needs to be doable independently without guessing at visual
details. On macOS/Linux, use Gallery screenshots supplied from a Windows
host; on Windows, compare with the live Gallery directly.

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
   [winui-control-theming](../../../../.github/skills/winui-control-theming/SKILL.md).
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
   Use alongside the sources to check actual size, borders, alignment,
   hover/pressed/disabled/focus states, animations, and Mica behavior.
   Static resources alone were insufficient for Button fidelity. Requires a
   Windows host; screenshots shared from the Windows VM also work.

### Windows VM option

If working from a Mac session, two practical ways to use the Windows VM:
- The user runs the Gallery app there and shares/pastes a screenshot into the
  chat (an image passed via chat can be viewed directly with the `view` tool).
- Start a separate session whose workspace targets the VM (`create_session`
  with `workspace` pointing at that machine, if the harness has connectivity
  to it), so an agent instance running there can build/run/screenshot the
  real app using its own tools.

Use resources 1–3 for names and structure, then compare against live Gallery
screenshots before claiming visual fidelity.

## Scope

The catalog includes per-theme
`About`/`Menu`/`ContextMenu`/`MenuFlyout` clones (🪟/🍏/🐧 prefixed — those
track native-menu-pack demos, not general control styling) and the
Experiments-only pages (ActiPro Controls, System Colours, Toggle Buttons,
Search Highlights, Animated Icon, LG Wallpaper Tint). The remaining control
demo pages need a WinUI pass; use the current catalog, not a fixed count,
as pages are added and split.

Already have existing (unverified, UniGetUI-derived) styling to treat as
reference only — verify from scratch against WinUI-Gallery, don't assume
correctness:
`ListBox` (`Controls/ListBox.axaml`), `ToggleButton` (`Controls/ToggleButton.axaml`),
`DataGrid` (`Controls/DataGrid.axaml`, partial — column header styling only).

Everything else is untouched. Suggested batching for PR-sized delivery
(mirrors the existing repo pattern of per-control-family PRs, e.g. "TreeDataGrid
header styling fixes", "Extract RectangleSelectionMarquee control"):

- **Batch 1 — Core input:** Button ✅ (Mica) / ↔️ (classic), complete in
  PR #669; CheckBox ❌ and RadioButton ❌ on separate branches awaiting
  their fidelity passes (see "Split" below); ToggleButton (extend),
  ComboBox, TextBox, NumericUpDown
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
4. Compare with the live Gallery app (or user-supplied screenshots) in
   Light/Dark and relevant states; spot-check Microsoft Learn as useful.
5. Diff against the existing Avalonia Fluent `ControlTheme` for the same
   control (already the fallback) to scope the actual change — follow the
   existing "Naming Rules" / "port only what is needed" workflow in the
   `winui-control-theming` skill.
6. Implement/extend `Controls/<Control>.axaml`; add new shared tokens to
   `Accents/ThemeResources.axaml` only if genuinely reusable; add matching
   Mica overlay entries to `Accents/ThemeResources.Windows11.axaml` in the
   **same change**, per the skill's Mica rules (never let base/overlay drift).
7. Wire the new file into `Controls/_index.axaml`.
8. Verify via the SampleApp's WinUI classic + Mica dropdown entries,
   DevTools screenshots, and the Gallery comparison. Mark **both**
   `WinUIClassic` and `WinUIMica` `🚧` in `page-catalog.jsonc` while styling
   is in progress; `🚧` excludes visual regression tests. **Do not upgrade
   past `🚧` yourself.** Upgrades to `⚠️` (testable but imperfect) or `✅`
   (verified) are user-initiated after visual confirmation. Keep both
   columns at the same status until a separate classic review determines
   whether they should differ; only after that review can the user choose
   classic `↔️` (identical to Mica). See "Classic vs Mica" below.
9. Once the user confirms a variant is ready (`⚠️`/`✅`), add its baselines
   (`UPDATE_BASELINES=true dotnet test ...` on each OS) under
   `Baseline/<OS>/WinUiMica/` or `WinUiClassic/`. A classic `↔️` instead
   uses pixel-equality tests against Mica and stores **no** classic baseline.
   `PageDiscoveryTests.VisualDiscoveryThemes` already includes
   `ThemeId.WinUiClassic` and `ThemeId.WinUiMica`; do not add the removed
   `ThemeId.WinUi`. Ensure each variant has at least one testable page by
   merge time (Button already provides both on this branch).
10. For substantial or breaking changes, confirm with the user whether
    `src/Devolutions.AvaloniaTheme.WinUI/CHANGELOG.md` needs an entry.
    Routine per-control styling does not require one.

## Resolved

- **Button is complete in PR #669; CheckBox and RadioButton are follow-ups.**
  Button uses a `BasedOn` override of Avalonia Fluent's `ControlTheme`
  (setters and template-part selectors), not a replacement template.
  Compared against the WinUI 3 Gallery, its height, bottom border, accent
  elevation, and text alignment required adjustments beyond copying WinUI
  values: flipping the absolute 3px elevation gradient with a transform
  instead of reversing its endpoints; setting `BackgroundSizing` to
  `InnerBorderEdge` (default) / `OuterBorderEdge` (accent); and compensating
  for Inter vs. Segoe UI font metrics with `ButtonMinHeight` and padding.
  The precise accent-colour difference is deferred. CheckBox and RadioButton
  are unverified first-pass ports on separate branches, **not** wired into
  this branch's theme; see "Split" below.
- **Control-specific aliases must live with their semantic tokens.**
  Root-level `<StaticResource x:Key="ButtonBackground"
  ResourceKey="ControlFillColorDefaultBrush"/>` in a separate
  `Controls/Button.axaml` dictionary cannot resolve the parent theme's
  `ThemeDictionaries`-scoped target at runtime (`KeyNotFoundException`);
  the build does not catch this. The working Button implementation defines
  its `ButtonBackground`/state aliases **inside each Light and Dark theme
  dictionary** in `Accents/ThemeResources.axaml`, alongside the shared
  tokens. `Controls/Button.axaml` consumes them via `{DynamicResource
  ButtonBackground}` and corresponding state keys. Follow the same
  dictionary-scope rule for future controls, rather than bypassing aliases
  or putting them at the root of a control file.
- **Visual regression harness tests separate WinUI variants.**
  `ThemeId.WinUiClassic` and `ThemeId.WinUiMica` are in
  `SupportedThemes` and `PageDiscoveryTests.VisualDiscoveryThemes`.
  Each has Light/Dark tests. `WinUIMica` uses baselines under
  `Baseline/<OS>/WinUiMica/`; Button's `WinUIClassic: ↔️` compares its
  pixels with Mica instead of storing classic baselines. Other controls can
  use `Baseline/<OS>/WinUiClassic/` if a classic review finds differences.
- **Status-symbol semantics for test inclusion were tightened.**
  `🚧` (in progress / actively unverified) is now **excluded** from visual
  regression tests via `PageCatalogEntry.ShouldTest`
  (`PageRegistry.IsInProgressSymbol`), separate from `⚠️` (imperfect but
  stable enough to guard existing coverage), which stays **included**. This
  lets a control show a "some work has started" indicator without forcing a
  baseline to be committed before it's ready. `IsNotSupportedSymbol`
  (`""`/`❌`) is unchanged.
  All 3 controls with existing UniGetUI-derived styling (DataGrid, ListBox,
  ToggleButton) are marked `🚧` in both WinUI catalog columns — visually
  indicating "something is there" while being excluded from tests until
  each is actually verified against real WinUI. Each control file
  (`Controls/DataGrid.axaml`/`ListBox.axaml`/`ToggleButton.axaml`) also has a
  header comment stating its styling was copied from UniGetUI's Avalonia
  port, is unverified, and should not be assumed correct or complete. The
  old code is kept only as a reference, not deleted.
  Generate baselines (all 3 OSes, via `UPDATE_BASELINES=true dotnet test ...`)
  as part of each control's own PR once its styling is verified and its
  status flips to `✅`/`⚠️` — not before, and not from the old
  UniGetUI-derived code.
  Mica-sensitive page and panel surfaces can legitimately differ; do not
  assume controls are identical just because static screenshots match.

## Handoff — 2026-09-25: initial fidelity findings (historical)

The first Button/CheckBox/RadioButton port used WinUI resource names but
still looked too much like stock Avalonia Fluent. Comparing the Button demo
side-by-side with the live WinUI 3 Gallery on Windows revealed why: its
absolute elevation gradient had been flipped by reversing the endpoints,
which painted the strong border along the top and sides; Avalonia needed
explicit `BackgroundSizing`; and Inter's metrics required a minimum height
and padding adjustment to match Segoe UI. The corrected Button is in
PR #669 (see "Resolved" above). Accent-colour muting is still deferred.

CheckBox and RadioButton were **not** given that Gallery fidelity pass.
For each, compare Light/Dark and interactive states with the Gallery,
re-check the gradient flip, `BackgroundSizing`, font metrics, and transitions
against the actual WinUI and Avalonia templates, and only then ask the user
to upgrade its catalog status. The original first-pass files are preserved
on their own branches, as described below. This historical handoff is not
an instruction to add them back to PR #669.

## Split — 2026-09-26: Button ships alone; CheckBox/RadioButton moved to their own branches

The Button fidelity pass (see handoff above) succeeded, but CheckBox and
RadioButton had not been through it yet. Rather than hold the Button work —
and grow the PR — the branch was split so Button can merge on its own.

**What happened:**

- `agents/winui-button-checkbox-radio` now contains **Button only**. Its
  `Controls/CheckBox.axaml` and `Controls/RadioButton.axaml` were deleted,
  removed from `Controls/_index.axaml`, their eight baseline PNGs deleted,
  and both pages set back to `❌` in **both** WinUI catalog columns.
- The unverified first-pass ports were preserved on two new branches cut
  from `master`: **`WinUI-CheckBox`** and **`WinUI-RadioButton`**, one
  control file each, not wired into `_index.axaml`, catalog left at `❌`.

**Read this before resuming either branch:**

1. **Rebase onto master first.** Both control files reference shared tokens
   that ship with the Button PR and do **not** exist on master until it
   merges: `ControlFillColorTransparentBrush`,
   `ControlStrongStrokeColorDefaultBrush`,
   `ControlStrongStrokeColorDisabledBrush`, and the four
   `ControlAltFillColor*` brushes — plus, for RadioButton,
   `CircleElevationBorderBrush`, `ControlElevationBorderBrush` and
   `AccentControlElevationBorderBrush`. Missing dynamic resources may not
   fail the build; check resource resolution at runtime rather than
   interpreting a Fluent-looking control as evidence the port is wrong.
2. **Check for the same defect classes Button had** — these files were written
   in the same initial pass. The gradient-flip mistranslation, missing
   `BackgroundSizing`, and Inter-vs-Segoe metric differences are documented
   in "Resolved" above; verify which apply to each control rather than
   assuming identical fixes.
3. **Wire the file into `Controls/_index.axaml`** as part of the fidelity
   pass — until then the theme is inert by design.

**Deliberately left behind on the Button branch:** seven currently unused
brushes (the four `ControlAltFillColor*` brushes, the two
`ControlStrongStrokeColor*` brushes, and `CircleElevationBorderBrush`)
that only CheckBox/RadioButton consumed are still defined in
`Accents/ThemeResources.axaml` even though nothing on that branch references
them. Removing them would only force an identical re-add (and a conflict) in
both new branches. This is a conscious exception to the skill's "remove
speculative tokens" rule, valid only because the consumers are known and
already written.

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

## Classic vs Mica: separate status columns and `↔️`

Decided 2026-09-25. The workflow above uses this per-variant model.

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
- **Harness:** `PageDiscoveryTests.VisualDiscoveryThemes` includes `WinUiMica` and
  `WinUiClassic` now that Button has left `🚧`.
- **Follow-up (not done):** `Windows11MicaDetector` only checks the OS build, not runtime
  conditions (transparency effects off, RDP, battery saver) where real WinUI falls back to a solid
  backdrop.
- **Merging with #669: done.** #669 was rebased onto this (2026-09-25) and converted as prescribed:
  `"WinUI": X` → `"WinUIClassic": X, "WinUIMica": X` for every page, `WinUI/*_mica*.png` →
  `WinUiMica/*.png` (dropping the `_mica` infix), and the non-mica files → `WinUiClassic/`.
  #669's own `GetWinUiCapturePlan()` / `WinUiCapturePlan_HasClassicAndMicaVariants` were dropped in
  favour of the per-variant cases introduced here. The mechanical conversion initially gave
  Button the same status in both columns; a subsequent classic review of WinUI source and
  Mica overlay dependencies found no Button-specific difference. Button is now
  `WinUIClassic: ↔️` / `WinUIMica: ✅`, with no classic Button baselines and a pixel-equality
  test instead. Do not infer the same result for any other control without its own review.
