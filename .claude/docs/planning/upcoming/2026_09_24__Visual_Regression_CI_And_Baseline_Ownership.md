# Visual regression testing: CI authority and baseline ownership

## Goal

Move canonical visual regression testing from individual developer machines to
GitHub Actions without losing fast local visual feedback.

Each theme will have one authoritative target platform:

| Canonical platform | Themes |
| --- | --- |
| Windows | DevExpress, WinUiClassic, WinUiMica |
| macOS | MacClassic, LiquidGlass |
| Linux | Linux/Yaru |

Pull requests will run all three native-platform jobs in parallel. After an
observation period, these jobs will become required checks. Intentional visual
changes will be approved through a separate maintainer-triggered workflow that
generates all affected canonical baselines and commits them once.

The existing `Avalonia.Headless` + Skia capture and image-comparison approach is
retained. This project changes baseline ownership, platform coverage, failure
review, and update safety rather than replacing the test framework.

## Context

Visual screenshots are products of a specific rendering environment. Fonts,
font fallback, locale, render scale, Skia/native dependencies, and operating
system image updates can all alter pixels. A screenshot produced on one
developer's machine is therefore not a dependable baseline for another
machine, even when both nominally run the same operating system.

The themes primarily represent their corresponding target platforms, so
canonical tests verify them there rather than maintaining a theme-by-OS
cross-product:

- DevExpress and WinUI on Windows, including the Windows font environment.
- MacClassic and LiquidGlass on macOS, including the real macOS font and
  rendering environment.
- Linux/Yaru on Linux.

The target-platform matrix produces one canonical set per theme, including
both WinUI variants. It is both smaller and more
representative than testing each theme across three operating systems.

## Implemented local transition

Before CI implementation, the local harness provides:

- Gitignored `Screenshots/LocalBaselines/{Theme}/`, containing only this
  machine's personal images, with no intermediate OS directory.
- Tracked `Screenshots/Baseline/{TargetOS}/{Theme}/` containing native
  target-platform coverage only.
- Native themes compare only with tracked images, and non-native themes
  compare only with personal images. Missing appropriate images fail explicitly;
  cross-OS images are never compared.
- `--update-baselines` / `UPDATE_BASELINES=true` updates that generate personal
  tracked images only for native themes and personal images only for
  non-native themes.
- `--initialize-local-baselines` captures non-native personal images without
  comparisons or WinUI identity checks and preserves tracked images. A non-empty
  personal folder requires explicit `y` confirmation; agents must surface this
  request and never approve overwrites automatically.
  Agents always ask once at session startup,
  including for non-visual tasks, before generating from clean starting code.
- Target-platform coverage guidance in `README.md`, `AGENTS.md`,
  `.claude/CLAUDE.md`, and `.github/copilot-instructions.md`, including Windows
  as the canonical target for WinUI variants.

Local canonical publishing is temporary. The CI phases below replace that
responsibility; they must not remove local all-theme convenience testing.
Both WinUI variants are mapped to Windows. Existing "same as reference" tests
continue to compare rendered themes directly without requiring separate
stored PNGs for the delegating variant.

Validation of the local transition:

- 27 baseline-routing, image-comparison, and page-discovery tests passed.
- 19 selected screenshot cases passed on macOS before and after a baseline
  update through `./devtest`.
- The update and comparison runs left all 266 retained canonical images
  byte-identical. Publishing rules for all three OS values are unit-tested;
  native Windows and Linux execution remains to be checked on those platforms.

GitHub-hosted runner labels are maintained images rather than immutable
machines. The workflows must use fixed labels instead of `-latest`, pin the
.NET SDK and project dependencies, control test inputs, and document how to
handle runner-image changes.

## References

- `docs/visual-regression-ci-proposal.md` - team/DevOps-facing summary of this
  plan and the requested review.
- `tests/Devolutions.AvaloniaControls.VisualTests/VisualRegressionTests.cs` -
  current test matrix, OS-based baseline resolution, capture path, and baseline
  update behavior.
- `tests/Devolutions.AvaloniaControls.VisualTests/SimpleVisualTest.cs` - active
  Avalonia headless test application builder.
- `tests/Devolutions.AvaloniaControls.VisualTests/ImageComparer.cs` - exact
  pixel comparison and diff generation.
- `tests/Devolutions.AvaloniaControls.VisualTests/.gitignore` - existing
  untracked test output directories; add `Screenshots/LocalBaselines/`.
- `README.md` and `.claude/CLAUDE.md` - developer testing documentation to
  update when the new workflow is implemented.
- `.github/workflows/build-package.yml` - existing repository workflow and
  PowerShell conventions.
- `.claude/docs/planning/current/visual-regression-testing.md` - original
  visual-test harness plan; still useful for capture implementation context.
- GitHub documentation for hosted runners, artifacts, `workflow_dispatch`,
  token permissions, and required status checks.

## Key decisions

### Native target-platform coverage

Canonical visual coverage is deliberately not a theme-by-OS cross-product.
Each theme has one target platform:

- Windows job: `DevExpress`, `WinUiClassic`, `WinUiMica`
- macOS job: `MacClassic`, `LiquidGlass`
- Linux job: `Linux`

The three jobs run on every pull request and in the baseline-update workflow.
They initially report without blocking merges. Once their runtime and
stability are proven, all three become required.

Cross-platform rendering remains available as advisory local testing but is
not backed by tracked baselines or merge requirements.

When a future theme or variant is added, it must declare one canonical target
platform. Adding a theme does not implicitly multiply it across every runner.

### Canonical environment controls

Use fixed GitHub-hosted runner labels, never `windows-latest`,
`macos-latest`, or `ubuntu-latest`. Fixed labels still receive periodic image
updates, so runner changes must be visible and handled as maintenance events.

Pin or explicitly control:

- .NET SDK version;
- Avalonia, Skia, and native package versions;
- locale, UI culture, and timezone;
- headless render scale and viewport dimensions;
- animations, transitions, caret blinking, and other time-dependent output;
- fonts that are not guaranteed by the target runner image.

Use native platform fonts where platform fidelity is the requirement:

- Segoe UI/Tahoma for DevExpress on Windows;
- the macOS system font environment for MacClassic/LiquidGlass;
- a known Open Sans installation or embedded test font for Linux/Yaru.

Fix the active headless `TestAppBuilder` so intended test fonts such as Inter
are actually registered. This improves local fallback behavior even though
canonical Mac screenshots use the real macOS environment.

Keep exact pixel comparison initially. Do not introduce tolerance to conceal
environment instability. Reconsider tolerance only after observing a stable,
controlled CI setup.

### Canonical baseline ownership

`Screenshots/Baseline/` remains tracked and contains only target-platform
combinations:

```text
Screenshots/Baseline/
|-- Windows/
|   |-- DevExpress/
|   |-- WinUiClassic/
|   `-- WinUiMica/
|-- macOS/
|   |-- MacClassic/
|   `-- LiquidGlass/
`-- Linux/
    `-- Linux/
```

Once CI owns baseline generation, only the maintainer-triggered GitHub workflow
may write this tree. Until then, native-platform local updates also publish here.

Cross-platform combinations have been removed in the local transition. The OS
directory names identify the target platform and the future runner responsible
for each baseline.

### Personal local baselines

Use gitignored `Screenshots/LocalBaselines/{Theme}/` for this machine's images.

The existing local command remains familiar:

```text
./devtest visual --update-baselines
```

It writes personal local baselines only for non-native themes. During the local
transition, native themes write only tracked target-platform images. Once CI
generation is available, remove that temporary publishing step so local
updates never modify tracked canonical files.

For local comparisons:

1. Native themes always use `Baseline/{CurrentOS}/{Theme}/`, ignoring any
   stale personal copies.
2. Non-native themes use only `LocalBaselines/{Theme}/`.
3. Missing appropriate images fail with "Missing baseline"; differing images
   fail with "Visual regression". Never compare another OS's images.
4. Capture non-native images from clean starting code using
   `./devtest visual --initialize-local-baselines`, after asking the user.
   Tracked images are never changed or compared; WinUI identity checks are
   not run. A non-empty personal folder requires explicit overwrite approval.
5. Once CI owns tracked images, change local initialization and comparison to
   use personal images for native themes too.

Introduce a separate canonical-write variable such as
`UPDATE_CANONICAL_BASELINES=true`. The harness must reject it unless
`GITHUB_ACTIONS=true`, with a clear error. This prevents accidental local
canonical updates.

### Failure review

Each PR job publishes:

- a short job summary identifying failed snapshots;
- expected images;
- actual images; and
- highlighted diff images.

Failure files are uploaded as GitHub Actions artifacts with a documented
retention period.

Intentional baseline changes are committed to the feature branch and reviewed
as ordinary PR changes. GitHub's PNG viewer provides side-by-side, swipe, and
onion-skin comparison.

Start without a third-party visual review service. Revisit
Percy/Chromatic/Applitools/Argos only if GitHub artifact review becomes a
demonstrated bottleneck.

### Secure baseline updates

The update workflow is manually dispatched with a target branch in this
repository. It must never run untrusted fork-controlled code with write
permissions.

The workflow uses four logical jobs:

1. Windows generates only DevExpress/WinUI baselines and uploads an artifact.
2. macOS generates only MacClassic/LiquidGlass baselines and uploads an
   artifact.
3. Linux generates only Linux/Yaru baselines and uploads an artifact.
4. An aggregation job runs only after all generation jobs succeed, validates
   the artifacts, stages only `Screenshots/Baseline/`, creates one bot commit,
   and pushes once.

Platform jobs must not push independently; that would risk races and partial
updates.

Use the same theme-to-platform configuration in PR comparison and baseline
generation so those workflows cannot drift.

## Open questions for DevOps

- Which fixed hosted-runner labels should the repository standardize on?
- Does Devolutions have an established way to monitor and respond to GitHub
  runner-image updates?
- What artifact-retention period should be used for failure images?
- Which branch-rule and required-check conventions should be followed?
- What is the preferred minimal `GITHUB_TOKEN` permission model for the
  aggregation/bot-commit job?
- Are there existing concurrency and fork-safety workflow patterns to reuse?

## Actions

### Phase 0: Team and DevOps alignment

- [ ] Share `docs/visual-regression-ci-proposal.md` with the team and DevOps.
- [ ] Identify a DevOps reviewer or pairing partner for workflow
      implementation.
- [ ] Resolve the runner-label, permissions, artifact-retention, concurrency,
      and branch-rule questions above.
- [ ] Confirm that the three target-platform jobs should all become required
      after the report-only observation period.
- [ ] Decide whether implementation should use a dedicated project branch.
- [ ] Move this plan to `.claude/docs/planning/current/` when implementation
      starts and record its status.

### Phase 1: Make the harness deterministic and target-aware

- [ ] Define one shared theme-to-platform mapping used by test discovery and
      both workflows:
      - Windows: DevExpress, WinUiClassic, WinUiMica
      - macOS: MacClassic, LiquidGlass
      - Linux: Linux
- [ ] Add a test filter or equivalent input so each CI job renders only its
      assigned themes while local runs can still render all supported themes.
- [ ] Pin locale, culture, timezone, render scale, viewport, and other
      deterministic capture inputs.
- [ ] Register intended fallback fonts in the active `TestAppBuilder`; provide
      a deterministic Open Sans source for the Linux job if the selected image
      does not guarantee it.
- [ ] Audit animations, transitions, and caret blinking; neutralize any output
      that can survive the existing stable-frame loop.
- [ ] Keep exact image comparison and verify repeated runs on each selected
      runner produce identical output.
- [ ] Add focused tests for theme/platform selection and baseline path
      resolution.
- [ ] Run the smallest relevant test/build commands and update this plan with
      findings.
- [ ] Commit the phase checkpoint following
      `.claude/docs/processes/git_commits.md`.

### Phase 2: Separate canonical and local baselines

- [x] Add `Screenshots/LocalBaselines/` to the visual-test `.gitignore`.
- [x] Route updates and comparisons to tracked native images and personal
      non-native images, without duplicate native personal images.
- [x] Add capture-only initialization of non-native personal images, with no
      tracked writes, comparisons, or WinUI identity checks. Require confirmation
      before overwriting a non-empty personal set.
- [x] Instruct agents to always ask once at new-session startup before
      initialization, including for non-visual tasks.
- [x] Preserve and byte-verify all 786 existing images in the local tree before
      pruning 520 cross-platform images; retain 266 unchanged native images.
- [ ] Remove temporary local canonical publishing once CI generation works.
- [ ] Add `UPDATE_CANONICAL_BASELINES=true` for CI and reject it unless
      `GITHUB_ACTIONS=true`.
- [ ] Ensure canonical generation can write only the platform/theme
      combinations assigned to the current job.
- [x] Add tests covering local writes, temporary native-only publishing,
      target-platform mapping, and comparison fallback behavior.
- [ ] Add tests for the future canonical CI-write mode and safety rejection.
- [x] Update `README.md` and `.claude/CLAUDE.md` with the local authority model.
- [ ] Run targeted visual tests and commit the phase checkpoint.

### Phase 3: Add report-only native PR checks

- [ ] Add `.github/workflows/visual-tests.yml` with fixed Windows, macOS, and
      Linux runner labels.
- [ ] Trigger on `pull_request` and `workflow_dispatch`.
- [ ] Run the three target-platform jobs in parallel and configure
      `concurrency` with `cancel-in-progress` for superseded branch runs.
- [ ] Pin the .NET SDK and all applicable workflow actions to approved
      versions.
- [ ] Produce readable job summaries and upload expected/actual/diff artifacts
      only as needed.
- [ ] Ensure fork PR checks use read-only permissions and never expose write
      credentials.
- [ ] Lint the workflow and validate it on a branch.
- [ ] Observe normal PR activity long enough to record runtime, queue behavior,
      false positives, and runner-image sensitivity.
- [ ] Fix instability before making any check required.
- [ ] Update this plan with observation results and commit the phase checkpoint.

### Phase 4: Generate and consolidate canonical baselines

- [ ] Generate Windows/DevExpress+WinUiClassic+WinUiMica, macOS/MacClassic+LiquidGlass, and
      Linux/Linux baselines on the selected CI runners.
- [ ] Review the complete PNG change set.
- [x] Remove redundant cross-platform baseline combinations, preserving local
      copies first.
- [ ] Verify that each PR job reads only its assigned canonical tree.
- [ ] Record the final baseline count and repository-size change.
- [ ] Commit the consolidated canonical baseline set.

### Phase 5: Add the aggregated baseline-update workflow

- [ ] Add `.github/workflows/update-visual-baselines.yml`, triggered only by
      `workflow_dispatch` with a target branch input.
- [ ] Validate that the target branch belongs to this repository before
      executing it with write permissions.
- [ ] Reuse the exact runner labels, environment setup, and theme mapping from
      the PR workflow.
- [ ] Generate per-platform artifacts in parallel without pushing from those
      jobs.
- [ ] Add a dependent aggregation job that downloads all artifacts, verifies
      the expected directory set, and rejects missing or unexpected files.
- [ ] Stage only the known canonical baseline directory; never use
      `git add .`.
- [ ] Create one bot-authored commit and push it to the target branch.
- [ ] Confirm that the PR check reruns after the baseline commit.
- [ ] Verify GitHub's image comparison experience on the resulting PR.
- [ ] Document the maintainer `gh workflow run` command and recovery steps.
- [ ] Security-review and lint the completed workflow before enabling it.
- [ ] Commit the phase checkpoint.

### Phase 6: Require checks and prove the handoff

- [ ] With DevOps, make all three stable platform jobs required checks.
- [ ] Write a concise runbook covering:
      - local commands and personal baseline generation;
      - manually dispatching PR checks before opening a PR;
      - finding and reviewing failure artifacts;
      - deciding whether a change is a regression or intentional;
      - dispatching the canonical baseline-update workflow;
      - handling runner-image updates and broad baseline churn.
- [ ] Have a teammate intentionally change a control and complete the full
      failure-review-baseline-update-green-merge path without assistance from
      the principal theme developer.
- [ ] Fix any gaps revealed by the handoff exercise.
- [ ] Update all developer documentation.
- [ ] Move this plan to `.claude/docs/planning/completed/` and commit.

### Phase 7: Optional future improvements

- [ ] Consider path-based change detection only after collecting evidence that
      running all three jobs on every PR is unnecessarily expensive or slow.
- [ ] Consider Git LFS only if repository growth becomes a demonstrated
      problem.
- [ ] Consider a hosted visual review service only if GitHub artifacts and PNG
      diffs are inadequate in daily use.
