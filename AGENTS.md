# AI Assistant Guidelines

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

## Quick Reference

For human developers, see the main [`README.md`](README.md) for getting started.

Key AI assistant resources in `.claude/`:
- **`CLAUDE.md`** - Primary instructions and project overview
- **`commands/`** - Custom slash commands for theme switching, commits, etc.
- **`commands/commit.md`** - Commit safety rules and `/worksetup` file exclusions
- **`commands/worksetup.md`** - Theme/tab/scale setup workflow for `samples/SampleApp/`
- **`docs/`** - Process documentation and planning materials
- **`local/`** - Personal commands and docs (gitignored, developer-specific — check if it exists)

> **Worktree setup:** If `.claude/local/` only contains `README.md` (or `.vscode/` is missing), run `bash scripts/setup-worktree.sh` to copy local config from the main worktree.

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

