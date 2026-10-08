# Proposal: move visual regression testing onto GitHub Actions

_Status: proposal - looking for team feedback and a DevOps reviewer before
workflow implementation starts. The local baseline separation is in place._

## Current local step

The repository already separates personal, gitignored `LocalBaselines` from
tracked, target-platform-only `Baseline` images. Cross-platform baselines are
not required PR coverage, including for WinUI, whose canonical target is Windows.

Until GitHub runners take over, native themes use and update tracked images
only; non-native themes use and update personal images only. New worktrees
inherit native images through Git. `--initialize-local-baselines` fills missing
non-native personal images without rewriting tracked or existing personal
images. Agents always ask before initialization at session startup, including
for initially non-visual tasks. This removes redundant committed coverage now,
while retaining local feedback. The workflows proposed below replace that
temporary local publishing responsibility with consistent CI generation.

## The problem

Our screenshot regression suite has been valuable for catching accidental UI
changes, but its committed baselines are generated on individual
developer machines. Screenshot rendering depends on the exact operating system,
fonts, locale, rendering libraries, and runner image, so two machines running
the same nominal OS can still produce different pixels.

Canonical coverage follows the platforms on which the themes are deployed:

- DevExpress and WinUI primarily target Windows.
- MacClassic and LiquidGlass primarily target macOS.
- Linux/Yaru primarily targets Linux.

Even the reduced canonical set needs a consistent generation environment so
another developer can reliably approve and regenerate it.

## Proposed model

GitHub Actions becomes the authority for committed baselines. Each theme is
tested on its real target platform rather than on every operating system.

| GitHub-hosted runner | Canonical themes |
| --- | --- |
| Fixed Windows runner label | DevExpress, WinUiClassic, WinUiMica |
| Fixed macOS runner label | MacClassic, LiquidGlass |
| Fixed Linux runner label | Linux/Yaru |

Each theme has just one canonical platform, including both WinUI variants.
This preserves the platform fidelity that matters:

- DevExpress and WinUI render with the Windows font and rendering environment.
  DevExpress uses Microsoft system fonts
  such as Segoe UI and Tahoma.
- Mac themes render with the real macOS font and rendering environment.
- Linux/Yaru renders with the Linux font and rendering environment it is
  designed to represent.

Cross-platform theme rendering remains available locally when useful, but it is
not a release requirement and will not have committed CI baselines.

GitHub-hosted runner labels are versioned environments, not immutable machine
images. We will use fixed labels rather than `-latest`, pin the .NET SDK and
project dependencies, control other inputs such as locale and render scale,
and treat runner-image changes as deliberate maintenance events.

## Workflow 1: visual regression PR checks

A new workflow runs automatically for pull requests and can also be dispatched
manually for a branch before a PR is opened.

The Windows, macOS, and Linux jobs run in parallel. Each job:

1. Checks out and builds the PR.
2. Runs only the theme or themes assigned to that platform.
3. Compares screenshots with the tracked canonical baselines for that platform.
4. Publishes a concise job summary.
5. On failure, uploads expected, actual, and highlighted-diff images as a
   downloadable artifact.

The jobs will initially be report-only while we observe real PRs for runtime,
runner-image stability, and false positives. Once the checks are dependable,
all three platform jobs should become required checks.

Running three jobs does not triple the committed baseline count: each theme has
one canonical platform. The jobs also run in parallel, so expected PR latency
is approximately the duration of the slowest platform rather than the sum of
all three.

## Workflow 2: maintainer-triggered baseline updates

A separate `workflow_dispatch` workflow handles intentional appearance changes.
It accepts an in-repository target branch and uses the same platform definitions
as the PR workflow.

The update is coordinated so that multiple jobs cannot race to push:

1. Validate that the target branch belongs to this repository, not a fork.
2. Run Windows, macOS, and Linux baseline-generation jobs in parallel.
3. Have each job upload only its assigned generated baselines as an artifact.
4. Run one final aggregation job after all platform jobs succeed.
5. Download and validate all three artifacts.
6. Commit only the known canonical baseline directory under a bot identity and
   push one complete baseline update to the target branch.

The resulting PNG changes appear in the pull request like normal source changes.
GitHub's image viewer supports side-by-side, swipe, and onion-skin comparison,
so reviewers can decide whether the change is intentional before merging it.

No workflow with write credentials will execute code from an external fork.

## Local development

Local visual testing remains a first-class development tool:

- `./devtest visual` continues to run the visual test project.
- `./devtest visual --update-baselines` writes personal screenshots to a new
  gitignored `Screenshots/LocalBaselines/` tree for non-native themes.
  During the local transition, native themes update only tracked images.
- Native themes compare with tracked images; non-native themes compare only
  with personal images. Native personal copies are ignored.
- Personal images live directly in `LocalBaselines/{Theme}/` and belong to
  the current machine, without an OS subdirectory.
- A missing image from the appropriate set produces "Missing baseline"; a
  mismatch produces "Visual regression". Other OS images are never compared.
- On a clean starting revision, `./devtest visual --initialize-local-baselines`
  creates missing non-native images, preserves all existing baselines, and
  tests native images without updating them.

This lets any developer create stable cross-platform local feedback without
publishing those cross-platform images. Once CI takes over, local updates will
write only personal images and the GitHub baseline-update workflow will be the
only writer to `Screenshots/Baseline/`.
At that point, initialization will need personal images for native themes too;
today's tracked-native comparison rule is deliberately temporary.

## Baseline layout

The tracked baseline tree retains platform names because each directory now has
a clear meaning:

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

Pages that compare directly against a reference theme do not need their own
stored PNGs. The tracked tree contains no cross-platform combinations. CI will
regenerate the retained target-platform sets when the workflows are introduced.

Future themes must declare one canonical target platform when they are added to
the suite. This prevents the matrix from growing automatically as themes and
variants are introduced.

## Why GitHub artifacts instead of a visual-testing service?

Percy, Chromatic, Applitools, and similar services provide polished visual
review experiences, but they also introduce an external account, billing
ownership, access tokens, and another system the team must maintain.

GitHub already provides:

- required PR checks;
- downloadable failure artifacts;
- image comparison for committed PNG changes;
- manual workflow dispatch; and
- controlled bot permissions.

We should start with GitHub-native tooling and revisit a hosted service only if
artifact review becomes a demonstrated bottleneck.

## What we need from DevOps

1. **Review of the overall design**, including any existing workflow conventions
   in other Devolutions repositories that we should follow.
2. **Runner guidance** for fixed Windows, macOS, and Linux labels and how the
   team normally handles scheduled GitHub runner-image updates.
3. **Repository-setting guidance** for required checks, branch rules,
   `GITHUB_TOKEN` permissions, fork safety, concurrency, and artifact retention.
4. **A reviewer or pairing partner** for the initial workflow implementation,
   particularly the multi-platform artifact aggregation and bot-commit steps.

## Rollout

1. Make the test harness deterministic and separate local from canonical
   baseline writes.
2. Add the three native-platform PR jobs as report-only checks.
3. Regenerate and review the retained target-platform canonical sets on CI;
   redundant cross-platform baselines are already removed by the local step.
4. Add the secure, aggregated baseline-update workflow.
5. Observe the checks across normal PR activity and address any instability.
6. Make all three platform jobs required.
7. Have a teammate complete an intentional visual change and baseline update
   using only the documented workflow as a handoff test.

## Questions for the team and DevOps

- Which fixed GitHub-hosted runner labels should we standardize on?
- How long should expected/actual/diff artifacts be retained?
- Are there existing branch-rule and bot-permission patterns we should reuse?
- Are there concerns with keeping the reduced canonical PNG set in normal Git,
  rather than introducing Git LFS or an external service?
