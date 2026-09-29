# Visual regression testing: CI authority and baseline ownership

## Goal

Fix the current visual regression testing workflow so it no longer depends on the
principal theme developer (amalchowperryman) personally regenerating and reconciling
baseline screenshots across three machines after every merge. Introduce a GitHub Actions
workflow that renders in one pinned, reproducible environment and becomes the
merge-blocking source of truth, while preserving fast local test feedback during
development.

This is **not** a rewrite of the existing harness (`Avalonia.Headless` + Skia +
custom `ImageComparer`), which already works well structurally. It is a change to
*where baselines are authoritative* and *how they get updated*.

## Context

### The problem

- Baselines are currently maintained per-OS (`macOS`, `Windows`, `Linux`) and committed
  to `tests/Devolutions.AvaloniaControls.VisualTests/Screenshots/Baseline/`
  (738 PNGs total, ~50 MB, 246 per OS as of 2026-09-24).
- The original intent (see
  `.claude/docs/planning/current/visual-regression-testing.md`) was to let every
  contributor run `UPDATE_BASELINES=true dotnet test` locally and commit the result.
- In practice, the principal dev works daily on macOS, so Mac baselines are reliable.
  Windows/Linux baselines were generated occasionally from personal VMs "as a
  convenience" for colleagues who don't run the suite themselves — but those
  colleagues' own machines produce different pixels (different font packages,
  fontconfig, distro version, Skia native assets, etc.), causing false-positive
  regressions when *they* run tests, or when a differently-configured Linux CI/VM is
  used to "double check" a PR.
- Net effect: only one person can reliably regenerate baselines, and they do so
  *after* a PR merges, across three environments, by hand.

### Why this happens (see research summary, condensed)

Visual baselines are an artifact of the *specific rendering environment*, not a
portable ground truth: font files/fallback, fontconfig, ICU/locale, DPI/render
scaling, Skia native asset versions, and GPU vs. software rendering all influence
pixel output. This is standard behavior for any pixel-diff visual test framework
(Playwright's own docs give the same warning). "Linux" is not one environment;
two different Linux boxes can legitimately disagree.

### Existing framework strengths worth preserving

- `Avalonia.Headless` + `.UseSkia()` rendering (`TestAppBuilder` in
  `tests/Devolutions.AvaloniaControls.VisualTests/SimpleVisualTest.cs`).
- Frame-stabilization loop before capture
  (`CaptureStableFrame` in `VisualRegressionTests.cs`) — avoids capturing
  mid-layout frames.
- Explicit theme + light/dark variant selection per test.
- `UPDATE_BASELINES` env var convention, `./devtest` wrapper script, and diff output
  already separated from committed baselines (`Screenshots/Test-Diffs`,
  gitignored).
- `DEVOLUTIONS_SKIP_WALLPAPER_TINT_SAMPLING` already demonstrates the project is
  willing to force deterministic fallbacks for headless runs — the same instinct
  needs to be applied more broadly (fonts, locale, timezone, render scale).

## References

- `.claude/docs/planning/current/visual-regression-testing.md` — original PoC plan
  that established the current harness; this doc supersedes its baseline-ownership
  approach but not its rendering approach.
- `tests/Devolutions.AvaloniaControls.VisualTests/VisualRegressionTests.cs` —
  main test loop, `SupportedThemes` list (currently `MacClassic`, `LiquidGlass`,
  `Linux`, `DevExpress`; WinUI variants exist in `App.axaml.cs` but are not yet
  in this list — plan needs to scale to cover them).
- `tests/Devolutions.AvaloniaControls.VisualTests/ImageComparer.cs` — exact
  pixel-diff comparator; keep for now, revisit tolerance only after environment
  determinism is solved.
- `README.md` ("## Testing" section) and `.claude/CLAUDE.md` ("### Testing") —
  developer-facing docs that need updating once the workflow changes land.
- `.github/workflows/build-package.yml` — only existing Actions workflow; shows the
  repo's `workflow_dispatch` conventions (manual trigger, input validation, PowerShell
  steps) to stay consistent with.
- Companion document for this plan:
  `docs/visual-regression-ci-proposal.md` — the same plan, written for sharing with
  the team and DevOps, to gather feedback and request a reviewer/pairing partner
  before implementation starts.
- External research (condensed into this plan; full citations available on request):
  Playwright screenshot determinism guidance, Avalonia Headless docs, GitHub Actions
  docs on `workflow_dispatch`/artifacts/branch protection, and the `Lattice` project
  as the closest public native-Avalonia + Verify + CI example found.

## Principles, key decisions

Agreed in conversation with the user (2026-09-24):

1. **One canonical, CI-rendered environment becomes merge-blocking.** A single
   pinned environment renders **all** themes (not a theme × OS matrix). This
   is the only baseline set that gates PRs. **Decided 2026-09-24: this
   environment is a pinned Windows runner**, not Linux — see "Canonical
   environment: Windows, not Linux" below for the reasoning. This was a
   business-priority call (DevExpress-on-Windows is by far the most important
   deployment target) reinforced by two independent technical findings, not
   just a preference.
2. **The principal dev's Mac baselines remain a personal, local convenience —
   not a shared responsibility.** They are not authoritative and colleagues are
   not expected to regenerate or rely on them. **Decided 2026-09-24:** this is
   now a first-class mechanism available to *every* contributor, not just the
   principal dev — see "Baseline vs. LocalBaselines split" below. This
   resolves the corresponding open question from the first draft of this plan.
3. **Stop asking colleagues to generate their own baselines** as a way of
   contributing to the *canonical* set. Nobody other than the CI maintainer
   workflow should ever write to the tracked `Baseline` directory. Anyone may
   freely generate their own personal `LocalBaselines` for day-to-day use.
4. **Local test runs stay in place as fast, advisory feedback**, not as an
   authority. A local failure means "differs from the committed canonical
   baseline," which may or may not indicate a real regression.
5. **No hosted third-party visual-review service (Argos/Percy/Applitools) at
   the start.** Revisit only if downloading/reviewing GitHub Actions artifacts
   proves to be a real bottleneck once the workflow is in daily use.
6. **Approved baseline changes are reviewed like source code**, via GitHub's
   built-in image diff (2-up/swipe/onion-skin) on the PR that updates them —
   not silently auto-committed.
7. **No privileged workflow ever executes fork-controlled code.** Baseline
   updates only run against branches in this repository.
8. **Design for growth.** More themes/variants (WinUI is already scaffolded in
   `App.axaml.cs` but not yet in the visual test `SupportedThemes` list) are
   coming. The single-canonical-environment model must not multiply baseline
   sets per additional theme × OS combination the way the current 3×3 approach
   does.
9. **DevOps involvement is required before implementation begins**, since the
   user is not experienced with authoring/configuring GitHub Actions workflows.
   `docs/visual-regression-ci-proposal.md` is the vehicle for that conversation.

### Canonical environment: Windows, not Linux (decided 2026-09-24)

Reconsidered after the user clarified deployment priorities: DevExpress-on-Windows
is by far the most important target; Linux/macOS themes have a much smaller
installed base, so an undetected issue there is lower-impact. This tipped the
recommendation from "prefer Linux for cost/reproducibility" to "prefer Windows,"
for reasons beyond just business priority:

- **Font licensing makes Linux structurally unable to render DevExpress
  faithfully.** The DevExpress theme's font stack is `Segoe UI, Tahoma,
  sans-serif`; the MacOS theme's is `SF Pro Text, Inter, Helvetica Neue,
  Segoe UI, sans-serif` (see
  `src/Devolutions.AvaloniaTheme.DevExpress/Accents/ThemeResources.axaml` and
  `src/Devolutions.AvaloniaTheme.MacOS/Accents/ThemeResources.axaml`). Per
  Microsoft's font redistribution FAQ
  (https://learn.microsoft.com/en-us/typography/fonts/font-faq), Windows
  system fonts are licensed for use *on Windows*, not for bundling into a
  Linux container. A Linux canonical renderer can therefore never actually
  render genuine Segoe UI/Tahoma — only whatever substitute happens to be
  configured — while a Windows GitHub-hosted runner has both natively and
  legally, with no bundling required.
- **The MacOS theme's font fallback is already less deterministic on Linux
  than on Windows, independent of the above.** `TestAppBuilder` in
  `tests/Devolutions.AvaloniaControls.VisualTests/SimpleVisualTest.cs` (the
  builder actually wired up via `[assembly: AvaloniaTestApplication(...)]`)
  does not call `.WithInterFont()`, unlike the unused `Program.cs` in the same
  project. That means `Inter` — the one entry in the MacOS font stack meant to
  be a reliable cross-platform anchor — likely never registers in headless
  test runs today. On Linux, the full fallback chain (`SF Pro Text` → `Inter`
  → `Helvetica Neue` → `Segoe UI` → generic `sans-serif`) has no deterministic
  match and bottoms out at whatever fontconfig substitutes. On Windows, it
  would deterministically resolve to `Segoe UI`, since that entry is native
  there. **Action: fix the missing `.WithInterFont()` call regardless of which
  OS is chosen** — this is a real, separate bug in the current harness.
- **The usual Windows-runner cost objection does not apply.** This repository
  is public, and GitHub's standard runners — Linux, Windows, and macOS alike —
  are free and unlimited on public repositories
  (https://docs.github.com/en/actions/reference/runners/github-hosted-runners).
- **What Windows gives up vs. Linux:** `jobs.<job_id>.container` (running a
  job inside an arbitrary, digest-pinned Docker image) is a Linux-runner-only
  Actions feature. On Windows, reproducibility instead relies on pinning the
  runner *label* (e.g. `windows-2022`, never `windows-latest`) and recording
  exact software versions from each run's "Set up job" log, since GitHub
  updates the underlying image on its own schedule rather than us freezing it
  by digest. This is a real, if smaller, precision loss to flag to DevOps and
  accept knowingly rather than overlook.

**Resulting shape:**
- Canonical, blocking: one pinned Windows runner (fixed label), rendering all
  four themes.
- Bundle **Open Sans** (SIL Open Font License, freely redistributable) as an
  explicit embedded font override for the Linux theme, so its rendering does
  not depend on whichever substitute the Windows box happens to have.
- Non-blocking, occasional (scheduled and/or manually dispatched): real macOS
  and real Linux jobs, specifically to catch genuine native-platform-only
  issues for the lower-install-base themes.

### Baseline vs. LocalBaselines split (decided 2026-09-24)

Directly resolves the open question below about whether to keep committing
the principal dev's Mac baselines, by generalizing the mechanism to every
contributor instead of special-casing one person:

- `Screenshots/Baseline/` stays tracked in Git and becomes write-only from the
  Phase 3 CI maintainer workflow. This is what CI always compares against.
- New `Screenshots/LocalBaselines/` directory, added to `.gitignore` alongside
  the existing `Screenshots/Test/` and `Screenshots/Test-Diffs/` entries.
  Running the existing `--update-baselines` / `UPDATE_BASELINES=true` command
  locally now writes here instead of the tracked folder — same familiar
  command, but no longer able to accidentally touch canonical baselines.
- The CI maintainer workflow (Phase 3) uses a distinctly-named variable, e.g.
  `UPDATE_CANONICAL_BASELINES=true`, and the test code should additionally
  refuse to honor it unless `GITHUB_ACTIONS=true` is set, throwing a clear
  error otherwise. This is a deliberate safety rail so the canonical-writing
  variable can't be misused by copy-pasting it into a local shell.
- **Comparison target while developing locally: decided 2026-09-24.** When
  running locally (not in CI) and a personal `LocalBaselines` image already
  exists for a given test, compare against that instead of the tracked
  canonical `Baseline`; otherwise fall back to canonical and print a note
  that first-run cross-platform noise (font/rasterization differences from
  the Windows-rendered canonical set) is expected until the developer
  generates their own local set. This avoids near-constant cosmetic-noise
  failures on non-canonical dev machines while keeping local runs meaningful
  as fast, day-to-day feedback.

### Open questions (resolve before/at start of Phase 2)

- [ ] What happens to the *existing* committed `Windows/` and `Linux/` baseline
      folders once the canonical CI baseline exists — delete immediately, or
      keep briefly as a fallback/reference during the transition?
- [ ] Does Devolutions have a preferred, already-maintained Windows runner
      image/version DevOps wants used for the canonical renderer, or any
      internal precedent for pinning Windows runner labels (since Windows
      doesn't support the container-digest pinning available on Linux)?
- [ ] Should the Windows/macOS "does this theme look right on its real target
      OS" check be a non-blocking scheduled job from the start, or deferred
      until there's a concrete reason to believe the canonical Windows renderer
      misses real platform-specific defects?
- [ ] Directory naming: keep `Screenshots/Baseline/{OS}/` with the OS
      subfolder simply matching whichever OS becomes canonical (e.g.
      `Baseline/Windows/`), or flatten/rename to something OS-agnostic like
      `Baseline/Canonical/` so the folder name doesn't imply "this is what
      Windows's baseline happens to be" versus "this is the one approved set"?

## Actions

### Phase 0: Align with the team and DevOps (no code yet)

- [ ] Share `docs/visual-regression-ci-proposal.md` with the team and DevOps.
- [ ] Collect feedback, in particular on: canonical container/base image choice,
      required-check policy, artifact retention, and who can review/pair on the
      first workflow PRs.
- [ ] Identify a DevOps reviewer/pairing partner for Phase 1–3 implementation.
- [ ] Resolve the "Open questions" above with input from that discussion.
- [ ] Decide whether to do this work on a dedicated branch (e.g.
      `visual-regression-ci`) and merge back when Phase 3 is done, or continue
      directly on top of the current session branch. **Ask the user.**
- [ ] Move this document to `.claude/docs/planning/current/` once Phase 1
      implementation starts, and update its status line.

### Phase 1: Canonical CI environment, non-blocking PR check

- [ ] Define the canonical rendering environment: a fixed Windows runner
      label (e.g. `windows-2022`, never `-latest`), per the "Canonical
      environment: Windows, not Linux" decision above. Windows doesn't support
      `jobs.<job_id>.container`, so reproducibility instead comes from pinning
      the label and recording exact tool versions from the job log.
- [ ] Fix the missing `.WithInterFont()` call in `TestAppBuilder`
      (`tests/Devolutions.AvaloniaControls.VisualTests/SimpleVisualTest.cs`) so
      the MacOS theme's `Inter` fallback actually registers in test runs.
- [ ] Bundle **Open Sans** (SIL OFL, freely redistributable) as an explicit
      embedded font for the Linux theme, so its rendering doesn't depend on
      whatever font substitution the Windows runner would otherwise perform.
- [ ] Pin inside that environment: .NET SDK version, Avalonia/Skia package
      versions (already pinned via `Common.props`/`AvaloniaVersion` — confirm
      the runner image matches), `LANG`/`LC_ALL`/`TZ`, and
      `DefaultThreadCurrentCulture`/`DefaultThreadCurrentUICulture` in test
      setup.
- [ ] Fix render scaling explicitly in the headless test setup rather than
      relying on platform defaults.
- [ ] Disable/neutralize animations, transitions, and caret blinking in the
      capture path (extend the existing `CaptureStableFrame` stabilization
      logic if needed).
- [ ] Add a new workflow, e.g. `.github/workflows/visual-tests.yml`, triggered
      on `pull_request` (and `workflow_dispatch` for on-demand runs via
      `gh workflow run visual-tests.yml --ref <branch>`).
- [ ] On failure, emit per-snapshot expected/actual/diff PNGs plus a summary
      (JSON and/or a job-summary table) and upload via `actions/upload-artifact`.
- [ ] Run this as **non-blocking** initially (report-only) against real PRs for
      an observation period to measure flakiness before it can gate merges.
- [ ] **Use a subagent** for drafting/iterating the workflow YAML and any
      linting (e.g. `actionlint`), since this involves a lot of trial-and-error
      log output that doesn't need to stay in the main context.
- [ ] Update this planning doc with observed flake rate and runtime findings.

### Phase 2: Consolidate baselines to the canonical set

- [ ] Decide (per open question) the fate of the existing `Windows/` and
      `Linux/` committed baseline folders.
- [ ] Generate the new single canonical baseline set (one folder, all themes)
      via the Phase 1 Windows workflow.
- [ ] Implement the `Baseline`/`LocalBaselines` split (see "Baseline vs.
      LocalBaselines split" above): add gitignored `Screenshots/LocalBaselines/`;
      repurpose `UPDATE_BASELINES=true` to write there; add the CI-only,
      `GITHUB_ACTIONS`-gated `UPDATE_CANONICAL_BASELINES=true` for Phase 3's
      workflow to write to the tracked `Baseline` folder; implement the
      local-comparison fallback (prefer `LocalBaselines` when present, else
      canonical with a cross-platform-noise notice), per the decision above.
- [ ] Extend `SupportedThemes` to include the WinUI variants once this is in
      place, so new themes/variants land directly in the scalable model
      instead of the old per-OS pattern.
- [ ] Update `README.md` ("## Testing") and `.claude/CLAUDE.md` ("### Testing")
      to describe the new authority model: local runs are advisory, CI is
      authoritative, here's how to reproduce a CI run locally/remotely.
- [ ] Commit checkpoint; update this planning doc's progress notes.

### Phase 3: Maintainer-triggered baseline update workflow

- [ ] Add a second workflow, e.g.
      `.github/workflows/update-visual-baselines.yml`, triggered only by
      `workflow_dispatch`, requiring a `target_branch` input.
- [ ] Validate the input branch exists in this repository (not a fork) before
      doing anything privileged.
- [ ] Run the same pinned canonical environment as Phase 1, regenerate
      baselines, stage **only** the known baseline directory (never
      `git add .`), and commit as a bot identity (e.g. `github-actions[bot]`)
      to the target branch.
- [ ] Document the maintainer command:
      `gh workflow run update-visual-baselines.yml --ref master -f target_branch=<branch>`.
- [ ] Confirm the resulting PNG diff renders correctly in GitHub's built-in
      image comparison (2-up/swipe/onion-skin) on the PR.
- [ ] Explicitly re-trigger the Phase 1 check after the bot's commit lands, if
      it doesn't already re-run automatically.
- [ ] Add a short runbook section (in `README.md` or a new
      `.claude/docs/reference/` evergreen doc — ask the user which) covering:
      what a local mismatch means, exact local and `gh` commands, how to read
      an artifact bundle, who can approve an appearance change, and how
      tolerance/thresholds are chosen (if any tolerance is ever introduced).

### Phase 4: Make the canonical check required, prepare for handoff

- [ ] Once flake rate from Phase 1's observation period is acceptably low,
      make the canonical visual workflow a required status check (branch
      protection / ruleset), with DevOps's help.
- [ ] Decide, based on real evidence gathered so far, whether a non-blocking
      real macOS/Linux "does it look right on the real target OS" job is
      worth keeping at all, per the open question above.
- [ ] Do a dry-run handoff test: have a teammate (not the principal dev)
      intentionally change a control's appearance, take it from a failing PR
      check through artifact review to an approved baseline update and green
      merge, without the principal dev doing it for them.
- [ ] Fix anything that dry run reveals is unclear or missing from the runbook.
- [ ] Move this document to `.claude/docs/planning/completed/` and commit.

### Phase 5 (deferred, only if needed)

- [ ] If GitHub's built-in artifact review becomes a genuine bottleneck for
      reviewers, evaluate adding **Argos** (accepts a plain screenshot
      directory from any test framework, branch-aware hosted review UI) on
      top of — not instead of — the deterministic CI capture. Document
      account/billing ownership, token/OIDC setup, and an exit plan before
      adopting, given the 6-month handoff horizon.
