# SampleApp Demo Registry and TreeView Navigation Plan

## Goal
Decouple visual test discovery from MainWindow UI structure by introducing a single source of truth for demo metadata, then use that same source to drive both:
1. SampleApp navigation UI (initially TabControl-compatible, next create a searchable TreeView).
2. Visual regression test case discovery (including dynamic entries).

This removes the current coupling between test discovery and MainWindow TabItem layout, and unlocks future navigation refactors without test breakage.

## Context
- Current visual tests discover pages using source parsing of MainWindow AXAML plus code-behind dynamic tab hints.
- This avoids prior runtime/headless discovery hangs, but it remains structurally coupled to specific UI markup patterns.
- Sample count has grown, and the current tab layout is no longer scaling well.
- A future TreeView-style navigation is desired, but was previously avoided due to discovery coupling.

## References
- samples/SampleApp/MainWindow.axaml
  - Current static TabItem composition and SampleItemHeader usage.
- samples/SampleApp/MainWindow.axaml.cs
  - Runtime/dynamic tab insertion pattern (for example AddTreeDataGridTab).
- tests/Devolutions.AvaloniaControls.VisualTests/VisualRegressionTests.cs
  - Current metadata discovery and test matrix generation.
- .claude/docs/planning/current/visual-regression-testing.md
  - Existing visual testing background and constraints.

## Working Session Cadence and Branch Hygiene
- Active branch: `agents/controls-registry-refactor-setup`
- Agreed workflow for this project:
  - At the start of each work session, rebase this branch onto `origin/master` to discover conflicts early.
  - Record decisions/progress in this document each day we make changes.
- Session log:
  - 2026-06-30: Kickoff review completed. Rebased `agents/controls-registry-refactor-setup` onto `origin/master` (already up to date).
    - 2026-07-06: Session-start rebase was blocked by local uncommitted registry work. Agreed team practice: create a clearly marked `[WIP]` commit before rebasing when needed, then squash later if desired.
    - 2026-07-06: Created WIP checkpoint commit, rebased successfully onto `origin/master`, and revisited the schema. Key change: move from demo-level applicability/status to a control x theme styling-state matrix.
      - 2026-07-06: Implemented the revised control-centric contract (`ThemeId`, `ControlStylingState`, `PageCatalogEntry`, `PageRegistry`) with explicit `ControlSource` and category paths.
        - 2026-07-07: Refactored contract to emoji-based per-theme status symbols plus `ExcludeFromTests` overrides (exclude-only), with shorthand helpers for readable registry entries.
          - 2026-07-07: Replaced the in-code catalog list with a human-edited JSONC catalog resource that is parsed into the internal control registry at runtime/startup.
            - 2026-07-07: Phase 2 discovery refactor completed: `VisualRegressionTests` now consumes `PageRegistry` directly instead of parsing `MainWindow.axaml` and `MainWindow.axaml.cs`.
              - 2026-07-08: Rebased `agents/controls-registry-refactor-setup` onto `origin/master` and completed the tab-compatible MainWindow composition refactor for Phase 3.
                - 2026-07-09: Follow-up cleanup after merge: removed the redundant per-entry `type` field from `page-catalog.jsonc`.
                  - The field had become authoring noise; runtime logic now uses the existing entry title where placeholder text needs a control name.
                  - Verification:
                    - `dotnet test --filter "FullyQualifiedName~PageCatalogTests|FullyQualifiedName~MainWindowNavigationTests"` ✅
                    - `dotnet test` ✅ (136/136)
                - 2026-07-09: Started the page-catalog migration to align navigation metadata with demos/pages (not just controls).
                  - Catalog schema migrated from a single `controls` array to sectioned `pages` with explicit `topLevelOrder`.
                  - Entry identity is now `uniqueTitle` (human-authored, globally unique in catalog) instead of a separate catalog key field.
                  - Added section-aware flattening in `PageRegistry`; `MainWindowNavigationBuilder` now consumes only the `"Control Demos"` section.
                  - Added support for omitted status blocks by defaulting missing theme statuses to `""` (no icon, excluded from tests).
                  - Added catalog legend entry for `""` and seeded non-control sections with `Overview` and `Control Alignment` pages (no status block).
                  - Startup settings block renamed to `sampleAppStartUpSettings` and now supports `selectedPage` (with `selectedTab` compatibility in runtime model).
                  - Verification:
                    - `dotnet test --filter "FullyQualifiedName~PageCatalogTests|FullyQualifiedName~MainWindowNavigationTests|FullyQualifiedName~PageDiscoveryTests"` ✅
                    - `dotnet test` ✅ (137/137)
                - 2026-07-09: Renamed catalog implementation surface from control-oriented naming to page-oriented naming.
                  - Renamed folder/resources to `samples/SampleApp/PageCatalog/page-catalog.jsonc` and updated SampleApp embedded-resource path.
                  - Renamed runtime API to `PageRegistry` + `PageCatalogEntry` and updated app/test imports to `SampleApp.PageCatalog`.
                  - Renamed `ControlCatalogTests` file/class to `PageCatalogTests` and updated test names for consistency.
                  - Verification:
                    - `dotnet test --filter "FullyQualifiedName~PageCatalogTests|FullyQualifiedName~MainWindowNavigationTests|FullyQualifiedName~PageDiscoveryTests"` ✅
                    - `dotnet test` ✅ (137/137)
                - 2026-07-09: Renamed page-catalog theme identifiers from `ControlThemeId`/`ControlThemeIds` to `ThemeId`/`ThemeIds` to avoid confusion with Avalonia `ControlTheme`.
                  - Updated page catalog runtime/test usages and moved `ControlThemeId.cs` to `ThemeId.cs`.
                  - Verification:
                    - `dotnet test --filter "FullyQualifiedName~PageCatalogTests|FullyQualifiedName~MainWindowNavigationTests|FullyQualifiedName~PageDiscoveryTests"` ✅
                    - `dotnet test` ✅ (137/137)
                - 2026-07-09: Implemented TreeView navigation first pass on MainWindow.
                  - Replaced the top-level `TabControl` with a left navigation `TreeView` + right `ContentControl` host.
                  - Navigation nodes are now built from `PageRegistry` section/page metadata, with section flattening for single-item sections where title matches section name.
                  - Page rendering reuses the catalog-driven content creation path (including Avalonia Pro placeholder behavior).
                  - Startup page selection and theme-recreate state restoration now preserve selected page title instead of tab index.
                  - Added experiments pages to `page-catalog.jsonc` and updated page-type resolution to support both `SampleApp.DemoPages.*` and `SampleApp.Experiments.*`.
                  - Updated `MainWindowNavigationTests` to validate tree-driven navigation/content behavior.
                  - Verification:
                    - `dotnet test --filter "FullyQualifiedName~PageCatalogTests|FullyQualifiedName~MainWindowNavigationTests|FullyQualifiedName~PageDiscoveryTests"` ✅
                    - `dotnet test` ✅ (137/137)
                - 2026-07-09: Applied TreeView UX polish and naming cleanup.
                  - Renamed `MainWindowTabBuilder` to `MainWindowNavigationBuilder` and `MainWindowTabsTests` to `MainWindowNavigationTests`.
                  - Expanded `"Control Demos"` by default in the TreeView.
                  - Sized the navigation pane from expanded tree content to avoid horizontal scrolling for the current flat demo list.
                  - Replaced red/green applicability bullets with catalog status symbols in `SampleItemHeader`.
                  - Added a right-side divider border for the navigation pane.
                  - Verification:
                    - `dotnet test --nologo --filter "FullyQualifiedName~PageCatalogTests|FullyQualifiedName~MainWindowNavigationTests|FullyQualifiedName~PageDiscoveryTests"` ✅ (11/11)
                    - `dotnet test --nologo` ✅ (137/137)
                - 2026-07-09: Follow-up polish based on review feedback.
                  - Removed hardcoded width cushion from TreeView width heuristic (`+88`) so navigation sizing tracks content more tightly.
                  - Updated page-node header rendering so all sections (not only `"Control Demos"`) display status symbols when present.
                  - Adjusted `SampleItemHeader` layout so pages without status symbols align with icon column start (conditional icon visibility + explicit icon margin).
                  - `TrySelectPageByTitle` now expands ancestor sections before selecting a page, so startup-selected entries in sections like `Experiments` reveal their location.
                  - Removed brittle tests that depended on specific mutable catalog entries/titles and switched to structural assertions.
                  - Verification:
                    - `dotnet test --nologo --filter "FullyQualifiedName~PageCatalogTests|FullyQualifiedName~MainWindowNavigationTests|FullyQualifiedName~PageDiscoveryTests"` ✅ (9/9)
                    - `dotnet test --nologo` ✅ (135/135) after excluding the wallpaper tint experiment from visual coverage in the catalog.
                - 2026-07-09: Addressed PR #580 Copilot review feedback.
                  - Removed duplicate page-content construction paths by relying on `SelectionChanged` for content updates (no extra `ShowPage` call after programmatic selection).
                  - Added a targeted navigation assertion that selecting a page in a collapsed non-`Control Demos` section expands its ancestor section.
                  - Strengthened page-discovery coverage by asserting `VisualRegressionTests.GetDemoPages()` only emits registry-backed, test-eligible demo/theme rows.
                  - Updated `.claude/CLAUDE.md` and `.claude/commands/worksetup.md` to match the current catalog-driven TreeView navigation model.
                  - Verification:
                    - `dotnet test --nologo --filter "FullyQualifiedName~PageCatalogTests|FullyQualifiedName~MainWindowNavigationTests|FullyQualifiedName~PageDiscoveryTests"` ✅ (9/9)
                    - `dotnet test --nologo` ✅ (135/135)
                - 2026-07-10: Addressed latest PR #580 reviewer feedback wave.
                  - Session-start upstream check: `origin/master` had 0 commits ahead of this branch (no rebase needed today).
                  - Cached rendered page content per catalog entry in `MainWindow` so navigation preserves interactive page state when switching away and back.
                  - Added direct symbol-semantics test coverage for `PageRegistry.IsNotSupportedSymbol` (`""`, `❌`, `✅`, `🚧`) to guard test-exclusion behavior independently of discovery assertions.
                  - Added navigation test coverage that re-selecting a page reuses the same content instance (state-preserving behavior).
                  - Verification:
                    - `dotnet test --nologo --filter "FullyQualifiedName~PageCatalogTests|FullyQualifiedName~MainWindowNavigationTests|FullyQualifiedName~PageDiscoveryTests"` ✅ (10/10)
                    - `dotnet test --nologo` ✅ (136/136)
                - 2026-07-10: Addressed follow-up PR #580 review comments (ordering + accessibility metadata).
                  - Fixed `topLevelOrder`/`pages` section-name case mismatch handling by resolving entries through a case-insensitive section map in `PageRegistry.CreateControls`.
                  - Added targeted regression test for case-mismatched section keys by invoking `CreateControls` with a crafted `PageCatalogFile`.
                  - Added minimal accessibility metadata by binding `AutomationProperties.HelpText` to `StatusTooltip` on the status symbol text, while preserving the visual tooltip.
                  - Verification:
                    - `dotnet test --nologo --filter "FullyQualifiedName~PageCatalogTests|FullyQualifiedName~MainWindowNavigationTests|FullyQualifiedName~PageDiscoveryTests"` ✅ (11/11)
                    - `dotnet test --nologo` ✅ (137/137)
                - 2026-07-10: Addressed follow-up PR #580 performance nit.
                  - Cached source-badge brushes in `MainWindowNavigationBuilder` so `GetSourceBadge` no longer allocates/parses colors per header build.
                  - Verification:
                    - `dotnet test --nologo --filter "FullyQualifiedName~PageCatalogTests|FullyQualifiedName~MainWindowNavigationTests|FullyQualifiedName~PageDiscoveryTests"` ✅ (11/11)
                    - `dotnet test --nologo` ✅ (137/137)

## Principles and Key Decisions
- One metadata source should define demo entries, applicability indicators, and optional ViewModel wiring.
- Test discovery must remain side-effect free (no runtime window probing during xUnit discovery).
- Navigation rendering (tabs/tree/etc.) must consume metadata, not define metadata.
- Keep migration incremental: avoid big-bang rewrite of MainWindow.
- Preserve existing behavior and ordering first, then iterate on grouping and UX.

## Proposed Metadata Model (Revised 2026-07-06)
The registry should be control-centric rather than demo-centric.

### Control entry
One entry per control/sampleable feature, for example:
- Key
- Display title
- Demo page type (or an explicit equivalent control-level reference; do not center the naming on the demo itself)
- Optional ViewModel type
- Source (for example `Avalonia`, `AvaloniaPro`, `Devolutions`)
- Category path, for example:
  - `Input`
  - `Selectors`
  - `Layout`
  - `Data/Grid`
  - `Devolutions/Custom Controls`
- Per-theme styling matrix (`ControlStylingState` by theme)

### Per-theme styling matrix
For each control and theme pair, store:
- `Status`:
  - `NotStyled` => `❌`
  - `InProgress` => `🚧`
  - `Done` => `✅`
- Per-theme flags/overrides:
  - `ExcludeFromTests`

Behavior:
- MainWindow status indicator for the active theme is derived from that theme's `ControlStylingState`.
- Visual tests should run by default for `InProgress` and `Done`.
- Visual tests should skip `NotStyled`.
- `ExcludeFromTests` can override the default and skip a theme even when styling status is not `NotStyled`.

Notes:
- Keep values strongly typed where possible (Type references, enums for theme IDs, source IDs, and status IDs).
- Do not add a custom ordering property for now; group by source/category and sort alphabetically within groups.
- `Experiments` remain outside this controls registry.
- `Control Alignment` and `Experiments` should remain outside the eventual TreeView as directly accessible navigation entries.

## Navigation Direction (Draft)
Potential first-level grouping candidates:
- Avalonia
- Avalonia Pro
- Devolutions

Under Avalonia, align subgrouping to Avalonia documentation categories where practical.

Reminder: exact grouping structure is intentionally not finalized in this document. A lightweight information architecture pass is required before TreeView implementation.

## Open Questions (Low-effort Clarifications)
Resolved on 2026-06-30:
1. Experiments stay outside the controls registry. They remain a separate area (potential future experiments-specific registry).
2. First pass keeps tab-compatible rendering path; TreeView follows in a later phase.
3. Theme applicability moves to a typed enum set now.
4. TreeView IA direction note: final MainWindow should keep `Experiments` and `Control Alignment` as navigation entries outside the TreeView so they stay immediately accessible.

Additional schema decisions on 2026-07-06:
5. Status is not a one-dimensional property of a demo; it is a per-theme property of a control (`ControlStylingState` matrix).
6. The model should use control-centric naming/logic rather than demo-centric naming where possible.
7. Keep `Source` explicit instead of encoding it through generic flags.
8. Use category paths for future TreeView grouping.
9. Do not add `HiddenInUiForTheme`.
10. Do not add a custom ordering property at this stage; alphabetical ordering within source/category groupings is preferred.
11. Use emoji symbols as status values in the matrix for scanability (`✅`, `❌`, `🚧`, `⚠️`) with a central symbol-description legend for tooltips/fallback text.
12. Test overrides are exclude-only (`excludeFromTests`): overrides may remove tests from otherwise testable statuses, but cannot force-test `❌` entries.
13. The authoring source of truth should be a human-readable file (JSON-style), not a large hand-maintained C# list. The app can parse that file into internal runtime objects.

## Phased Actions

### Phase 1: Define and Validate Registry Contract
- [x] Replace the first-pass demo-centric contract with the revised control-centric schema.
- [x] Define `ControlStylingState` / per-theme overrides and remove one-dimensional applicability/status assumptions.
- [x] Introduce explicit `Source` and category-path metadata.
- [x] Decide whether demo page linkage should stay explicit as a page type property or be convention-derived from control identity. (explicit `PageType` retained)
- [x] Re-run validation design against the revised schema.
- [x] Update this planning doc with final contract decisions and any deviations.

Acceptance criteria:
- A single registry can represent controls, their per-theme styling status, and test applicability.
- Existing dynamic entries can be represented without AXAML/code-behind scraping.

Implementation notes:
- 2026-06-30: A first-pass registry contract was implemented (`DemoThemeId`, `DemoDescriptor`, `DemoRegistry`, validation, tests).
- 2026-07-06: That first-pass shape is now considered provisional/superseded because it models applicability/status at the wrong level. The next implementation pass should refactor it to a control-centric schema with a control x theme styling-state matrix.
- 2026-07-06: Refactored the implementation to:
  - `ThemeId` / `ThemeIds`
  - `ControlStylingStatus`, `ControlStylingFlags`, and `ControlStylingState`
  - `ControlSource`
  - `PageCatalogEntry`
  - `PageRegistry`
- 2026-07-06: Kept demo linkage explicit via `PageType` rather than deriving `[ControlName]Demo` by convention.
- 2026-07-06: Populated explicit `Source` and category-path metadata for the current controls catalog.
- 2026-07-06: Validation now checks:
  - duplicate control keys
  - non-empty category paths
  - page/viewmodel type shape
  - complete theme coverage for every control entry
- 2026-07-06: Verification:
  - `dotnet test --filter "FullyQualifiedName~PageCatalogTests"` ✅
  - full `dotnet test` currently fails on existing visual diffs for `EditableComboBoxDemo` across 4 themes; those pages were not changed by this catalog refactor and need separate investigation before relying on a full-green suite.
- 2026-07-07: Refined implementation details:
  - Replaced enum-based status values with symbol-based `StatusByTheme` map and `ControlStatusSymbols` legend/description helpers.
  - Added exclude-only per-theme override collection (`ExcludeFromTests`) and preserved default test behavior:
    - `❌` => skip by default
    - other symbols => include by default
    - explicit exclude override => skip
  - Added/kept readability helpers in `PageRegistry` (`Supported(...)`, `InProgress(...)`, etc.) to keep entries concise.
- 2026-07-07: Replaced those in-code helpers as the authoring surface with an embedded JSONC catalog:
  - file: `samples/SampleApp/PageCatalog/page-catalog.jsonc`
  - parser: `System.Text.Json` with comment/trailing-comma tolerant options
  - internal registry still materializes typed `PageCatalogEntry` objects from that file
  - `statusSymbols` legend now comes from the catalog file itself
  - `excludeFromTests` is stored in the file as a theme->bool object, with only `true` entries having effect
- 2026-07-07: Verification:
  - `dotnet test --filter "FullyQualifiedName~PageCatalogTests"` ✅
  - earlier full `dotnet test` runs still failed with pre-existing `EditableComboBoxDemo` visual diffs in 4 themes.

### Phase 2: Switch Visual Tests to Registry-backed Discovery
- [x] Replace MainWindow source parsing in VisualRegressionTests discovery with registry consumption.
- [x] Keep test matrix behavior equivalent (page x applicable themes x optional viewmodel).
- [x] Ensure discovery remains static and deterministic (no UI creation in MemberData path).
- [x] Add guard assertions/logging when registry entry points to missing types.
- [ ] Run visual tests in normal mode and baseline-update mode to validate parity.
- [x] Update this planning doc with migration notes and any edge cases.

Acceptance criteria:
- VisualRegressionTests no longer parse MainWindow AXAML or code-behind.
- Test coverage set remains equivalent or intentionally documented.

Phase 2 notes (2026-07-07):
- `VisualRegressionTests.GetDemoPages()` now iterates `PageRegistry.All`, filters to the currently supported visual-test themes (`MacClassic`, `LiquidGlass`, `Linux`, `DevExpress`), and uses `PageCatalogEntry.ShouldTest(theme)` plus the registry-provided `ViewModelType`.
- Discovery now calls `PageRegistry.EnsureValid()` up front instead of relying on AXAML/code-behind parsing to fail later.
- Added regression coverage in `PageDiscoveryTests` to pin key discovery cases (`EditableComboBoxDemo`, `TreeDataGridDemo`) to their expected theme sets.
- Full `dotnet test` after the refactor produced 3 visual diffs, all for `TreeDataGridDemo` (`MacClassic`, `DevExpress`, `Linux`).
- `EditableComboBoxDemo` is still discovered and still covered; a focused run for `DisplayName~EditableComboBoxDemo` passed in all 4 expected themes during this session.

### Phase 3: Refactor MainWindow to Render from Registry (Tab-compatible)
- [x] Introduce a composition layer that builds current TabItems from registry entries.
- [x] Preserve current ordering, headers, and ViewModel assignment behavior.
- [x] Keep the existing UI shape stable in this phase to minimize regression risk.
- [x] Remove duplicated metadata literals from AXAML/code-behind once parity is confirmed.
- [x] Update this planning doc with before/after mapping notes.

Acceptance criteria:
- MainWindow no longer hardcodes most demo metadata in TabItem markup.
- Visual behavior remains functionally equivalent.

Phase 3 notes (2026-07-08):
- Kept `Overview`, `Control Alignment`, and `Experiments` as explicit top-level tabs in XAML, matching the earlier decision to keep non-control areas outside the future registry-driven navigation tree.
- Removed the long static run of control demo `TabItem`s from `MainWindow.axaml`.
- Added `MainWindowNavigationBuilder`, which now:
  - iterates `PageRegistry.All` in catalog order,
  - creates `SampleItemHeader` from registry metadata (`Title` + active-theme status symbol),
  - instantiates the demo page and optional ViewModel from the typed registry entry,
  - substitutes the Avalonia Pro placeholder message when `ENABLE_ACCELERATE` is not available.
- `MainWindow` now inserts the generated control tabs immediately before the `Control Alignment` tab, preserving the existing top-level tab layout while eliminating duplicated per-control metadata from AXAML/code-behind.
- Replaced the earlier brittle “pin a couple of demos” style of regression coverage with a maintainable headless `MainWindowNavigationTests` check that verifies the generated control-tab set, header metadata, and declared ViewModel wiring against the catalog itself.
- Verification:
  - `dotnet test --filter "FullyQualifiedName~MainWindowNavigationTests|FullyQualifiedName~PageCatalogTests|FullyQualifiedName~PageDiscoveryTests"` ✅
  - `dotnet test` ✅ (132/132)

### Phase 4: Plan Information Architecture for TreeView
- [ ] Draft the exact hierarchical structure for categories and subcategories.
- [ ] Validate category names against Avalonia docs terminology where useful.
- [ ] Decide handling for cross-cutting demos (single home vs multi-tag/search).
- [ ] Confirm where Experiments and ActiPro entries belong in the hierarchy.
- [ ] Review and approve grouping with user before implementation.
- [ ] Update this planning doc with final IA decisions.

Acceptance criteria:
- Approved category tree with explicit mapping for every registry entry.

### Phase 5: Implement TreeView Navigation
- [x] Add TreeView-based navigation bound to grouped registry data.
- [x] Implement selection-to-content loading with current DataContext behavior.
- [ ] Preserve keyboard navigation and accessibility basics.
- [ ] Keep fallback/feature flag path if needed for temporary coexistence with tabs.
- [ ] Run manual UI verification across themes and dark/light variants.
- [x] Update this planning doc with implementation notes and follow-ups.

Acceptance criteria:
- TreeView navigation is functional and complete for core demos.
- Visual tests remain unaffected due to registry-based discovery.

### Phase 6: Cleanup, Docs, and Lifecycle Move
- [ ] Remove obsolete parsing code and stale comments tied to old discovery assumptions.
- [ ] Update relevant docs with registry and navigation architecture.
- [ ] Move this doc from upcoming to current when development starts.
- [ ] Move this doc from current to completed once delivered.

Acceptance criteria:
- No remaining hard dependency between test discovery and MainWindow visual structure.

## Risks and Mitigations
- Risk: Metadata drift between registry and actual page constructors.
  - Mitigation: startup/test validation for type availability and constructor expectations.
- Risk: Grouping debates delaying implementation.
  - Mitigation: phase separation (contract first, IA finalization before TreeView build).
- Risk: Large UI diff in one change.
  - Mitigation: keep a tab-compatible registry phase before TreeView transition.

## Suggested Branching Checkpoint
- Optional early step: create a feature branch for this multi-phase effort before implementation begins.
- If used, plan an explicit merge-back checkpoint after user validation of TreeView grouping and behavior.
