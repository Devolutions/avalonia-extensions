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
workflow, **published per-package** (versions differ between them). To check if something shipped, compare the
commit date to the latest published version of the relevant package:

```bash
curl -s "https://api.nuget.org/v3-flatcontainer/devolutions.avaloniatheme.macos/index.json" \
  | python3 -c "import sys,json; print(json.load(sys.stdin)['versions'][-1])"
```

Also: CHANGELOGs are only updated for substantial/breaking changes, not routine fixes. And PRs get no
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
  visual changes are intentional).

Testing references:
- **`README.md`** (`# Testing`) - Current `dotnet test` filters and baseline update commands
- **`tests/Devolutions.AvaloniaControls.VisualTests/`** - Baselines and diff outputs used by visual regression tests

