# AI Assistant Guidelines

## ⚠️ REQUIRED: Session Pre-flight Check

**Before starting any task**, run this check:

```bash
ls .claude/local/
```

If the output contains **only `README.md`** (no other files), or if `.vscode/` is missing, you **must** run the worktree setup script before doing anything else:

```bash
bash scripts/setup-worktree.sh
```

On Windows: `pwsh scripts/setup-worktree.ps1`

This copies gitignored local config (personal commands, `.vscode/` settings) from the main worktree. Skipping it means you'll be missing developer-specific tooling and commands for this session.

---

## Development baseline initialization

At the start of every new agent session, after local configuration setup and
before implementation, always ask once whether the user wants a full clean
development baseline set, including for non-visual tasks. Do not repeat the
question within the same session.

If approved, run `./devtest visual --initialize-local-baselines` on the clean
starting revision. This captures non-native personal images without comparisons
or WinUI identity checks, and never writes tracked images. If the command
warns that LocalBaselines is non-empty, surface the overwrite confirmation
through the user-question tool. Never automatically answer or pipe `y` unless
the user explicitly approves overwriting the existing set.
Never initialize from already modified code without explicit user approval;
use a separate ordinary test run to check native images and regressions.
Do not copy personal baselines automatically between worktrees.

> **Note:** This file has been superseded by more comprehensive documentation in the `.claude/` directory.

For detailed instructions on working with this repository as an AI assistant, please see:

**[`.claude/CLAUDE.md`](.claude/CLAUDE.md)** - Main documentation covering:
- Repository overview and structure
- Development commands and workflows
- Architecture and design patterns
- Coding rules and best practices
- Version control guidelines
- Custom commands (`/worksetup`, `/commit`, `/explain`, `/simplify`)
- SampleApp + visual regression test workflow (see [`README.md`](README.md) Testing section)

## ⚠️ Two things agents get wrong

**1. Sign your GitHub comments.** The `gh` CLI authenticates as the developer running it, so anything you post
(PR descriptions, comments, review replies) looks like the human wrote it. End agent-authored GitHub content with a
footer, stating the current agent/tool, and resolving the username dynamically with `gh api user --jq .login` (never hardcode it — this file is
shared across the team):

```markdown
_Posted by <agent/tool name> (agent), on behalf of @<login>._
```


**2. This repo has NO git tags.** `git tag --contains <commit>` returns nothing for *every* commit, so it will
falsely tell you a change was never released. Releases are date-based NuGet packages published via a manual
workflow, **published per-package** (versions differ between them). 

CHANGELOGs are only updated for substantial/breaking changes, not routine fixes - confirm with the user, if a changelog entry seems to be appropriate. And PRs get no
automated CI checks (the workflow is `workflow_dispatch`-only), so missing checks are expected.

See [`.claude/CLAUDE.md`](.claude/CLAUDE.md) — "GitHub Identity for Agents" and "Releases & Versioning".

## Quick Reference

For human developers, see the main [`README.md`](README.md) for getting started.

Key AI assistant resources in `.claude/`:
- **`CLAUDE.md`** - Primary instructions and project overview
- **`commands/`** - Custom slash commands for theme switching, commits, etc.
- **`commands/commit.md`** - Commit safety rules and `/worksetup` file exclusions
- **`commands/worksetup.md`** - Theme/tab/scale setup workflow for `samples/SampleApp/`
- **`docs/`** - Process documentation and planning materials
- **`local/`** - Personal commands and docs (gitignored, developer-specific — check if it exists)

Key Development Workflows:

- **Building/Running:**
  `dotnet build samples/SampleApp/SampleApp.csproj && cd samples/SampleApp/bin/Debug/net10.0 && dotnet SampleApp.dll` (
  Required for proper theme detection)
- **Notifications:** Use non-blocking `osascript -e $'display dialog "..."' &` (with ANSI-C quoting and `&`) for
  critical alerts.
- **Accelerate Controls:** Requires `.env` with `AVALONIA_LICENSE_KEY=your_key_here` at repository root.
- **Testing:** `dotnet test` (Use `UPDATE_BASELINES=true dotnet test` on macOS/Linux to update baseline screenshots if
  visual changes are intentional). Updates write tracked native baselines and personal non-native baselines. Use `./devtest visual --initialize-local-baselines` for capture-only personal initialization without changing tracked images; a non-empty personal set requires overwrite confirmation.
- **Visual coverage / PR reviews:** Only target-platform baselines are canonical and required: DevExpress and WinUI variants on Windows, MacClassic/LiquidGlass on macOS, Linux/Yaru on Linux. Do not require macOS/Linux WinUI screenshots or other cross-platform baseline combinations; they are local developer conveniences. See the Testing section of `README.md`.

Testing references:
- **`README.md`** (`# Testing`) - Current `dotnet test` filters and baseline update commands
- **`tests/Devolutions.AvaloniaControls.VisualTests/`** - Baselines and diff outputs used by visual regression tests
