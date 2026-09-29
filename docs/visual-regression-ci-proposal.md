# Proposal: move visual regression testing onto GitHub Actions

_Status: proposal — looking for feedback and a DevOps reviewer before we start
implementing. Not yet built._

## The problem, in one paragraph

We have an automated visual regression test suite (screenshot comparison for
every themed control) that's been genuinely useful for catching accidental UI
breakage. The catch: screenshot rendering is machine-dependent — different
fonts, font-fallback, OS package versions, and rendering libraries all produce
slightly different pixels, even between two machines running "the same" OS.
Right now, one person (me) maintains baseline screenshots from three separate
machines/VMs by hand, after each PR merges. That doesn't scale, it's slow, and
it means nobody else can safely regenerate baselines themselves. I'd like to
move that authority onto GitHub Actions so it's reproducible and doesn't
depend on any one person's machine.

## What I'm proposing

Two new GitHub Actions workflows, plus a change to how baseline images are
organized:

### 1. A PR check that renders in one fixed, pinned environment

- Runs automatically on every pull request (and can be triggered manually via
  `gh workflow run` for a branch that isn't in a PR yet).
- Renders all themes on a single pinned Windows runner, with a fixed .NET SDK
  version, fonts, locale/timezone, and Avalonia/Skia version — all pinned, so
  the same input always produces the same pixels.
- **Why Windows specifically, not Linux:** our DevExpress theme is by far our
  most important target (Windows desktop), and it uses Microsoft's own system
  fonts (Segoe UI, Tahoma). Those fonts are only licensed for use on Windows —
  a Linux machine can never legitimately have them, so it would always be
  rendering with a substitute font, no matter how carefully we pin everything
  else. A real Windows runner avoids that problem entirely for our most
  important theme, at the (smaller) cost of pinning a Windows runner *version*
  rather than an exact container image the way we could on Linux.
- On any pixel difference, it uploads the expected image, the new image, and
  a highlighted diff as a downloadable artifact, plus a short summary of what
  changed.
- **To start**, this check would be informational only (not merge-blocking),
  so we can see real-world flake rate for a couple of weeks before relying on
  it.
- We'd also like an occasional (scheduled or manually-triggered), non-blocking
  job on real macOS and real Linux runners, purely to catch cases where our
  lower-install-base themes look wrong on their actual native platform — see
  "Questions for the team" below.

### 2. A maintainer-triggered "update baselines" workflow

- Manually triggered (`workflow_dispatch`), takes a branch name as input.
- Only ever runs against branches in this repository — never against an
  external fork's code with write access, for security reasons.
- Regenerates screenshots in the same pinned Windows environment as the PR
  check, commits only the affected baseline images (nothing else) back to
  that branch under a bot identity, and pushes.
- The resulting image changes show up as an ordinary file diff on the PR,
  where GitHub already supports side-by-side / swipe / onion-skin image
  comparison — so reviewing "is this appearance change intentional?" doesn't
  need any new tooling.

### What doesn't change

- Nothing about the actual test code (`Avalonia.Headless` + Skia rendering,
  pixel comparison) needs to be rewritten. This is a change to *where
  baselines are authoritative* and *how they're approved*, not a new testing
  framework.
- Local `dotnet test` / `./devtest visual` keeps working for fast day-to-day
  feedback. It just stops being the thing that decides whether a PR is
  correct — CI does.
- Anyone (not just me) can still generate and keep their own personal
  baseline set locally for day-to-day convenience, the same way I already do
  on my Mac today. We're just moving that into an explicitly untracked,
  personal folder (never committed) so it can never be mistaken for, or
  accidentally overwrite, the one canonical set CI maintains.

## Why this instead of a paid visual-testing SaaS (Percy/Chromatic/Applitools)?

We looked at these. They're solid products, but they add an external
account, billing owner, and access-token surface to maintain, mostly to solve
a "review experience" problem we don't have yet — GitHub's built-in image
diff view is already good enough for our current volume of screenshots. If
reviewing artifacts by hand ever becomes a real bottleneck, adding one of
these later is straightforward and doesn't require re-architecting anything
above.

## What we'd like from DevOps

1. **Feedback on the approach** before we build it — anything you'd do
   differently, any existing conventions in other repos we should follow.
2. **Guidance on pinning a Windows runner** for the canonical renderer — is
   there a preferred/maintained Windows runner label or image version we
   should standardize on (we can't pin an exact container digest the way we
   could on Linux, so we'd rely on your guidance for how to track GitHub's
   periodic runner-image updates responsibly)?
3. **Guidance on repo settings**: branch protection / required status checks,
   `GITHUB_TOKEN` permissions for the bot-commit workflow, and artifact
   retention policy.
4. **A reviewer or pairing partner**, ideally someone comfortable with GitHub
   Actions YAML, since I don't have much experience authoring/configuring
   workflows myself and would like a second pair of eyes (or hands) once we
   start implementing — particularly for the bot-commit workflow, since it
   needs to be built carefully to avoid ever running with write credentials
   against untrusted code.

## Rollout plan (high level)

1. Stand up the PR check as report-only; watch it for a couple of weeks
   across real PRs to measure flakiness and runtime.
2. Consolidate today's three separate per-OS baseline sets into one canonical
   set produced by the pinned Windows environment above.
3. Add the maintainer-triggered baseline-update workflow.
4. Once the check is reliably green/red (few false positives), make it a
   required status check for merging.
5. Write a short runbook so this isn't tribal knowledge — how to read a
   failed check, how to approve an intentional appearance change, how to
   reproduce a CI run locally.

## Questions for the team

- Any objection to a report-only observation period before this becomes a
  required/blocking check?
- Should we keep a non-blocking "does this theme look right on its actual
  target OS" job on real macOS/Linux runners (occasional, scheduled or
  manual), given the single pinned Windows renderer becomes the one
  merge-blocking source of truth?
- Any concerns about committing screenshot PNGs to the repo long-term (current
  size: roughly 50 MB across ~250 images for a single platform's worth of
  baselines), versus moving to Git LFS or a hosted service later?

Please reply here (or grab me directly) with thoughts — happy to walk through
the reasoning behind any of the above in more detail.
