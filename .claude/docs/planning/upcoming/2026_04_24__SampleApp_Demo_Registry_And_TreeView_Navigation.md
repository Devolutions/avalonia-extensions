# SampleApp Demo Registry and TreeView Navigation Plan

## Goal
Decouple visual test discovery from MainWindow UI structure by introducing a single source of truth for demo metadata, then use that same source to drive both:
1. SampleApp navigation UI (initially TabControl-compatible, future TreeView-friendly).
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

## Principles and Key Decisions
- One metadata source should define demo entries, applicability, and optional ViewModel wiring.
- Test discovery must remain side-effect free (no runtime window probing during xUnit discovery).
- Navigation rendering (tabs/tree/etc.) must consume metadata, not define metadata.
- Keep migration incremental: avoid big-bang rewrite of MainWindow.
- Preserve existing behavior and ordering first, then iterate on grouping and UX.

## Proposed Metadata Model (Draft)
A shared descriptor object (name TBD), for example:
- Demo page type (or factory)
- Display title
- Applicable themes
- Optional ViewModel type (or factory)
- Optional flags (Experimental, Pro, Devolutions, HiddenFromTests, etc.)
- Group path for hierarchical navigation, for example:
  - Avalonia/Input
  - Avalonia/Data
  - Avalonia/Layout
  - Avalonia Pro/Actipro
  - Devolutions/Custom Controls

Notes:
- Keep descriptor values strongly typed where possible (Type references, enum for theme IDs).
- Allow explicit ordering within groups.

## Navigation Direction (Draft)
Potential first-level grouping candidates:
- Avalonia
- Avalonia Pro
- Devolutions

Under Avalonia, align subgrouping to Avalonia documentation categories where practical.

Reminder: exact grouping structure is intentionally not finalized in this document. A lightweight information architecture pass is required before TreeView implementation.

## Open Questions (Low-effort Clarifications)
1. Should experiments remain outside the main registry (separate area), or become registry entries with an Experimental flag?
2. For first pass, should the UI continue to render tabs from registry data, then switch to TreeView later, or should we move directly to TreeView?
3. For theme applicability, do you want to keep string values for flexibility, or move to a typed enum set now?

## Phased Actions

### Phase 1: Define and Validate Registry Contract
- [ ] Create a shared demo descriptor type in SampleApp (location TBD, likely Models or a dedicated DemoCatalog folder).
- [ ] Define typed theme applicability values and conversion helpers needed by current UI and tests.
- [ ] Include support for dynamic/demo-only entries currently added in code-behind.
- [ ] Add minimal unit-like validation checks (or startup asserts) for duplicate keys, missing page types, and invalid ViewModel types.
- [ ] Update this planning doc with final contract decisions and any deviations.

Acceptance criteria:
- A single list can represent all currently testable demos.
- Existing dynamic entries can be represented without AXAML/code-behind scraping.

### Phase 2: Switch Visual Tests to Registry-backed Discovery
- [ ] Replace MainWindow source parsing in VisualRegressionTests discovery with registry consumption.
- [ ] Keep test matrix behavior equivalent (page x applicable themes x optional viewmodel).
- [ ] Ensure discovery remains static and deterministic (no UI creation in MemberData path).
- [ ] Add guard assertions/logging when registry entry points to missing types.
- [ ] Run visual tests in normal mode and baseline-update mode to validate parity.
- [ ] Update this planning doc with migration notes and any edge cases.

Acceptance criteria:
- VisualRegressionTests no longer parse MainWindow AXAML or code-behind.
- Test coverage set remains equivalent or intentionally documented.

### Phase 3: Refactor MainWindow to Render from Registry (Tab-compatible)
- [ ] Introduce a composition layer that builds current TabItems from registry entries.
- [ ] Preserve current ordering, headers, and ViewModel assignment behavior.
- [ ] Keep the existing UI shape stable in this phase to minimize regression risk.
- [ ] Remove duplicated metadata literals from AXAML/code-behind once parity is confirmed.
- [ ] Update this planning doc with before/after mapping notes.

Acceptance criteria:
- MainWindow no longer hardcodes most demo metadata in TabItem markup.
- Visual behavior remains functionally equivalent.

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
- [ ] Add TreeView-based navigation bound to grouped registry data.
- [ ] Implement selection-to-content loading with current DataContext behavior.
- [ ] Preserve keyboard navigation and accessibility basics.
- [ ] Keep fallback/feature flag path if needed for temporary coexistence with tabs.
- [ ] Run manual UI verification across themes and dark/light variants.
- [ ] Update this planning doc with implementation notes and follow-ups.

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
