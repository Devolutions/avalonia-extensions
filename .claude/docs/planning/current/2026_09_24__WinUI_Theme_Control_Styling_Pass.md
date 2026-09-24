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
therefore being treated as starting from scratch alongside everything else:
in `samples/SampleApp/PageCatalog/page-catalog.jsonc` every page is
`"WinUI": "❌"`, whereas MacOS, DevExpress, and Linux are `"✅"` almost
everywhere. The old code is kept in place purely as a reference/starting
point, not as a target look to preserve.

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
   `"WinUI"` status in `page-catalog.jsonc` from `❌`/`🚧` to `✅` (or leave
   `🚧` if only partially covered, with a note).
9. Add/extend visual regression baselines for the control being styled (the
   WinUI test harness is now wired up — see "Resolved" note below — so this
   is just `UPDATE_BASELINES=true dotnet test ...` on each OS as usual).
10. Update `src/Devolutions.AvaloniaTheme.WinUI/CHANGELOG.md` per existing
    convention; confirm with the user whether an entry is warranted per the
    "substantial change" threshold noted in repo instructions, don't add
    automatically for routine per-control work.

## Resolved

- **Visual regression harness now knows about WinUI.** `ThemeId.WinUi` was
  added to `SupportedThemes` (`VisualRegressionTests.cs`) and
  `VisualDiscoveryThemes` (`PageDiscoveryTests.cs`). Each WinUI page now
  captures 4 screenshots per test (classic light/dark + Mica light/dark,
  suffixes `""`/`"_dark"`/`"_mica"`/`"_mica_dark"`) under one `WinUI`
  baseline folder, using `App.SetTheme(new WinUiClassicTheme()/WinUiMicaTheme())`
  the same way the SampleApp dropdown does — no direct
  `Windows11MicaDetector` manipulation needed in the test.
  **All 3 previously-`🚧` pages (DataGrid, ListBox, ToggleButton) were reset
  to `"WinUI": "❌"`** and each control file
  (`Controls/DataGrid.axaml`/`ListBox.axaml`/`ToggleButton.axaml`) now has a
  header comment stating its styling was copied from UniGetUI's Avalonia
  port, is unverified, and should not be assumed correct or complete. The
  old code is kept only as a reference, not deleted. Because of this, the
  WinUI harness currently discovers **zero** testable pages (all `❌`) —
  `PageDiscoveryTests` was adjusted so its "every discovery theme has ≥1
  page" guard only applies when the catalog actually has a qualifying page
  for that theme, so a temporarily-empty WinUI doesn't fail that test. The
  harness will pick up cases automatically once the first control in a
  batch below is verified and flipped to `✅`/`🚧`.
  Generate baselines (all 3 OSes, via `UPDATE_BASELINES=true dotnet test ...`)
  as part of each control's own PR once its styling is verified — not
  before, and not from the old UniGetUI-derived code.
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
