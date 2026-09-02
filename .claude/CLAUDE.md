# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

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

# Repository Information

## Overview

This repository contains custom Avalonia themes and controls developed by Devolutions. It includes:
- **Devolutions.AvaloniaTheme.MacOS** - Native macOS look based on AppKit
- **Devolutions.AvaloniaTheme.DevExpress** - DevExpress WinForms style theme
- **Devolutions.AvaloniaTheme.Linux** - Linux/Ubuntu Yaru GTK theme style
- **Devolutions.AvaloniaControls** - Reusable custom controls, converters, and markup extensions

All themes use Avalonia.Themes.Fluent as a fallback for controls not yet styled.

## Repository Structure

```
src/
├── Devolutions.AvaloniaTheme.MacOS/         # MacOS theme with AppKit styling
│   ├── Controls/                             # Control-specific styles
│   ├── Accents/                              # Resources and Assets
│   ├── Converters/                           # Theme-specific converters
│   └── GlobalStyles.axaml                    # Global style definitions
├── Devolutions.AvaloniaTheme.DevExpress/    # DevExpress theme
├── Devolutions.AvaloniaTheme.Linux/         # Linux theme
└── Devolutions.AvaloniaControls/            # Shared custom controls
    ├── Controls/                             # Custom control implementations
    ├── Converters/                           # Value converters
    ├── MarkupExtensions/                     # XAML markup extensions
    └── Behaviors/                            # Attached behaviors

samples/
└── SampleApp/                                # Demo application
    ├── PageCatalog/                       # JSONC source-of-truth + registry loader
    ├── DemoPages/                            # Control demo pages
    └── Experiments/                          # Experimental features
```

## Claude Code Organization

**IMPORTANT**: All files intended for Claude Code's information, documentation, or processes MUST be stored within the `.claude/` directory structure.

```
.claude/
├── CLAUDE.md                                 # This file - main project instructions
├── commands/                                 # Custom slash commands
│   ├── explain.md                            # /explain command
│   ├── simplify.md                           # /simplify command
│   └── worksetup.md                          # /worksetup command
├── local/                                    # Personal commands & docs (gitignored, developer-specific)
│   └── README.md                             # Describes the local/ convention
└── docs/                                     # Claude-specific documentation
    └── processes/                            # Process documentation
        └── ide_connection.md                 # IDE integration guide
```


### File Placement Rules
When creating or organizing files for Claude Code:
- **Documentation**: `.claude/docs/` (e.g., planning docs, architecture notes, process guides)
- **Processes**: `.claude/docs/processes/` (e.g., workflow guides, troubleshooting)
- **Planning**: `.claude/docs/planning/` (e.g., feature planning, design decisions)
- **Commands**: `.claude/commands/` (slash commands only)
- **Personal/local**: `.claude/local/` (developer-specific commands and docs — gitignored, check if it exists and consult its contents)

**Never** create Claude-related files in the root-level `docs/` or other project directories - they belong exclusively in `.claude/`.

## Development Commands

### Building
```bash
# Build entire solution
dotnet build avalonia-extensions.sln

# Build specific project
dotnet build src/Devolutions.AvaloniaTheme.MacOS
dotnet build src/Devolutions.AvaloniaControls
```

### Running the Sample App
```bash
# IMPORTANT: For proper theme detection, build first then run from bin directory
# This ensures the app can detect the configured theme from App.axaml
dotnet build samples/SampleApp/SampleApp.csproj && cd samples/SampleApp/bin/Debug/net10.0 && dotnet SampleApp.dll

# Alternative: Use dotnet run (faster, but theme detection won't work)
# This runs from repo root, causing app to fall back to OS-default theme
# dotnet run --project samples/SampleApp/SampleApp.csproj
```

The SampleApp provides:
- Visual demos of all styled controls
- Theme switching via /worksetup command (see .claude/commands/worksetup.md)
- Inspection:
  - F12 opens Avalonia Accelerate Dev Tools

### Runtime Navigation Convention (SampleApp)
- SampleApp navigation is generated at runtime from `samples/SampleApp/PageCatalog/page-catalog.jsonc` via `MainWindowNavigationBuilder` and `PageRegistry`.
- `samples/SampleApp/MainWindow.axaml` hosts the navigation shell (`TreeView` + content host); page entries come from catalog `pages.*[].uniqueTitle`.
- To add/update a page entry, edit catalog metadata first (section, title, source, category, per-theme status, optional view model), then ensure the corresponding page/viewmodel types exist.
- `/worksetup` can still adjust local theme/tab/scale development state, but those local defaults should not be committed (follow `.claude/commands/commit.md`).

**Note on Theme Detection:**
The app's theme detection (`DetectDesignTheme()` in App.axaml.cs) expects the working directory to be 
`bin/Debug/net10.0/`. When running from the repo root via `dotnet run`, the detection fails and the app falls back 
to the OS-default theme (MacOS on macOS, DevExpress on Windows, etc.). This is why building and running from the bin directory is required for proper theme detection configured via `/worksetup` command.

### Testing
Automated tests are available and should be used:
- `dotnet test` runs the repository test suite, including visual regression tests.
- `dotnet test --filter "DisplayName~VisualRegressionTests"` runs only visual regression tests.
- `dotnet test --filter "DisplayName!~VisualRegressionTests"` runs only non-visual tests.
- `./devtest visual` / `./devtest nonvisual` / `./devtest functional` provide the same split with concise output; these are wrapper shorthands, not native `dotnet test` arguments.
- Catalog and discovery behavior is covered in `tests/Devolutions.AvaloniaControls.VisualTests/` (for example `PageCatalogTests`, `VisualRegressionTests`, and `MainWindowNavigationTests`).

Manual validation via SampleApp is still important for exploratory UI checks and theme behavior.

### DevTools MCP (Live UI inspection with AI agents)
- Prefer DevTools MCP for runtime UI inspection tasks (visual tree, properties, styles, screenshots, input simulation) instead of relying only on human visual checks.
- User-level MCP config is stored in `~/Library/Application Support/Code/User/mcp.json` (not `~/.vscode/mcp.json` in this environment).
- Required app instrumentation:
  - `AvaloniaUI.DiagnosticsSupport` package installed
  - `.WithDeveloperTools()` on `AppBuilder` **or** `this.AttachDeveloperTools()` in `Application`
  - Keep DevTools instrumentation development-only (Debug), since MCP enables live inspection, runtime property mutation, and synthetic input.
- Typical MCP flow for `attach-to-app`:
  1. `attach-to-app` with no id (enumerates available running clients)
  2. Call `attach-to-app` again with selected process id
  3. Use `tree`, `search`, `props`, `styles`, `screenshot`, etc.

#### Important licensing note (observed in this repo/session)
- We observed that forcing `AVALONIA_TOOLS_LICENSE_KEY` into MCP server env could cause:
  - `Authenticated session does not have access to required product: avalonia-developer-tools-console`
- In this setup, MCP attach worked when that env var was **not** forced in MCP config (while an authenticated DevTools session already existed).
- First-time/expired sessions may prompt for Avalonia portal credentials in a GUI dialog; after successful sign-in, local auth state is reused by DevTools.
- If MCP attach fails with product/license errors:
  - update DevTools first (`dotnet tool update --global AvaloniaUI.DeveloperTools`; install only if missing)
  - verify license key and entitlement
  - only for the exact error above (and if a cached authenticated session exists), try removing explicit MCP `env` override for `AVALONIA_TOOLS_LICENSE_KEY`
  - if a portal sign-in dialog appears, pause and ask the user to complete it (do not enter credentials from the agent)
  - retry attach

### Packaging
```bash
# Pack a specific theme for NuGet
dotnet pack src/Devolutions.AvaloniaTheme.MacOS -o package

# Version is updated by CI/CD workflow, not manually
```

## Architecture Notes

### Theme Structure
Each theme follows a consistent pattern:
- Base theme class (e.g., `DevolutionsMacOsTheme`) extends Avalonia's theme system
- `ThemeRoot.axaml` includes all control styles and resources
- `GlobalStyles.axaml` defines global styling applied to all controls
- Individual control styles are organized in `Controls/` subdirectories
- Falls back to `Avalonia.Themes.Fluent` for unstyled controls

### Development vs Release Configuration
Projects use different references based on build configuration:
- **Debug**: Uses `ProjectReference` to `Devolutions.AvaloniaControls`
- **Release**: Uses `PackageReference` to published NuGet package
- This allows local development without publishing intermediate versions

### Custom Controls in AvaloniaControls
The `Devolutions.AvaloniaControls` package contains:
- **EditableComboBox**: Combo box with editable text field
- **SearchHighlightTextBlock**: Text block with search term highlighting
- **TabPane**: Extended TabControl for alternate styling
- **Converters**: Color, thickness, and corner radius manipulation utilities
- **MarkupExtensions**: Binding helpers (AddBinding, MultiplyBinding, AndBinding, etc.)

### Platform-Specific Considerations
- **Linux**: SkiaSharp.NativeAssets.Linux pinned to 3.116.1 due to known issues with newer versions
- **macOS**: Inactive window behavior shows subdued accent colors
- All themes support both light and dark modes

## Target Framework
All projects target `.NET 10.0` and use Avalonia `12.0.x` packages.

## CI/CD
The repository uses GitHub Actions for building and publishing NuGet packages. The workflow:
- Builds packages on Windows (required for code signing)
- Code signs DLLs using Azure Key Vault
- Publishes to nuget.org
- Supports individual or batch package publishing
- Automatic stable versioning based on date (`yyyy.MM.dd.0`), with optional prerelease suffixes for migration/testing builds
- **No git tags are created** — see "Releases & Versioning" below for how to check whether a change has shipped
- Note: the workflow has no `pull_request` trigger, so PRs do not get automated CI checks. An absence of checks
  on a PR is expected, not a failure.

## Control Status Tracking
Each theme's README.md maintains a checklist of styled controls with status indicators:
- ✅ Available in current build
- 🚧 In progress
- 🔮 On the roadmap

Refer to individual theme README files for complete lists and visual examples.



# Development Rules
- Don't run terminal commands without asking permission, except cd, mkdir, editing & creating files within the repo, building, running and closing the app. 
- NEVER ask the user to allow a command without a short explanation of what it is supposed to accomplish.
- When fixing a problem, focus just on that problem. If you notice other tangential issues to the issue you are tasked with, summarise them and report them back but don't automatically start fixing issues

# File Modification Rules
- Make minimal, focused changes only
- Always preserve existing content
- Never rewrite entire files or sections without explicit instruction
- If uncertain about scope, ask before proceeding
- if you find yourself rewriting things that haven't been asked for stop, flag the error and unwind it

Always go through the following process:
1. Start by searching the codebase in extreme detail, to get all the context you need.
2. Create a detailed plan before doing anything.
3. Rate your confidence level on being able to execute the plan without introducing new bugs or affecting existing
   functionality.
4. If your confidence level is under 95%, continue researching the codebase and go back to step 3
5. Start implementing your plan, but walk me through your thinking step by step each time before you do the actual implementation.

You should always look to make the minimum code changes possible, and never introduce new bugs or affect existing
functionality.

# Version Control Rules
- **ALWAYS consult `.claude/commands/commit.md` before creating ANY commit** (not just when user types `/commit`)
  - Check master branch defaults for SampleApp files (theme and tab selection)
  - Follow commit message format guidelines
  - Apply workon exclusion rules
- Never push to git without explicit permission (when you're asked to create a PR you may push (but if a force-push 
  is required, ask first))
- Never stage to git without explicit permission
- **NEVER EVER force-push without explicit permission, EVEN when asked to create a PR** - If something went wrong, we 
  lose the last working version on the remote!
- After completing a rebase, ALWAYS ask the user to test before pushing
- If force-push is needed, explain why and wait for explicit approval

## Commit Message Guidelines
- When asked to make temporary commits, clearly label them as such in the commit message
- Use `[temp]` prefix for temporary commits that represent work-in-progress
- Examples:
  - `[temp] most styling working - but scrolling not working`
  - `[temp] added basic theme structure - needs polish`
  - `[temp] horizontal layout working - vertical needs fixes`
- Regular (non-temporary) commits should follow standard patterns:
  - `[ComponentName] Description of changes`
  - Include details in commit body about what was done and why
  - Always include Claude Code attribution footer

# GitHub Identity for Agents

**The `gh` CLI authenticates as the developer who configured it.** Anything an agent posts to GitHub
 (PR descriptions, issue comments, review replies) appears to come from that developer, with no visual indication
 that an agent wrote it.  Always make agent-authored GitHub content clearly attributable.

**Sign every agent-authored GitHub comment, PR body, and review reply** with an italic footer as the last line.
**Determine the username dynamically** — never hardcode it, since these instructions are shared across the team:

```bash
gh api user --jq .login
```

Then sign with that value:

```markdown
_Posted by Claude Code (agent), on behalf of @<login>._
```

For example, if `gh api user --jq .login` returns `octocat`, the footer reads
`_Posted by Claude Code (agent), on behalf of @octocat._`

Rules:
- **Never hardcode a specific username** in a signature or in these docs — always resolve it per-session with
  `gh api user --jq .login`. The authenticated account differs per developer.
- Do **not** sign as just "Copilot" — GitHub Copilot's own reviewer already posts under that name, so it is
  ambiguous. Name the actual agent/tool (e.g. "Claude Code").
- Applies to: PR bodies (`gh pr create`/`edit`), issue and PR comments, and replies to review threads.
- Does **not** apply to git commit messages — those already carry a `Co-authored-by:` trailer and the commit
  author metadata makes provenance clear.
- When replying to a review thread, sign the reply even if the thread is on your own PR.

# Releases & Versioning

**This repository does NOT use git tags.** Do not use `git tag`, `git describe`, or `git tag --contains` to
determine whether a change has shipped — there are no tags, so every such check silently reports "unreleased"
and will mislead you.

Packages are published to nuget.org through a GitHub Actions workflow (`.github/workflows/build-package.yml`,
triggered manually via `workflow_dispatch`). Versions are **date-based**.

**To check whether a commit has been released**, compare the commit's date against the latest published
package version:

```bash
# Latest published version of the package you care about — packages are published
# individually, so versions differ between them. Check the specific one(s) your
# change touches (e.g. devolutions.avaloniatheme.devexpress, devolutions.avaloniacontrols).
curl -s "https://api.nuget.org/v3-flatcontainer/devolutions.avaloniatheme.macos/index.json" \
  | python3 -c "import sys,json; print(json.load(sys.stdin)['versions'][-1])"

# Date of the commit in question
git log -1 --format=%ad --date=short <commit>
```

If the PR's merge-commit date is before that package's latest published version, the change has shipped in it.
Human-readable page: <https://www.nuget.org/packages/Devolutions.AvaloniaTheme.MacOS>

## CHANGELOGs

Each theme has a `CHANGELOG.md`, but they are **only updated for substantial or breaking changes** — not for
routine fixes. Do not add a CHANGELOG entry for an ordinary bug fix; most PRs correctly touch no changelog at
all. When in doubt, ask.

# User Notifications via Dialog
**CRITICAL**: The user may be working on other things and not watching this conversation. You MUST use dialog notifications for important events.

## When to Show Dialogs
Always show a dialog for:
1. **Task completion** - When you finish any task
2. **Permission requests** - When you need permission for operations (database changes, npm commands, git operations, etc.)
3. **Important errors** - When something fails that requires user attention
4. **Configuration changes** - When you modify ports, environment variables, or other config

## Dialog Command Format

### Non-Blocking Dialogs (RECOMMENDED)
**IMPORTANT**: Always run dialogs in the background with `&` so they don't block execution:

```bash
osascript -e $'display dialog "Message here" buttons {"OK"} default button "OK" with title "GitHub Copilot"' &
```

**Key syntax rules for zsh:**
- Use `$'...'` (ANSI-C quoting) to properly interpret `\n` escape sequences
- Use single quotes inside for strings (no escaping needed)
- Escape single quotes with `\'` when needed inside strings
- Add `&` at the end to run in background (non-blocking)

### Task Completion Examples:
```bash
osascript -e $'display dialog "Task Complete ✅\n\nAdded priority dropdown menu" buttons {"OK"} default button "OK" with title "GitHub Copilot"' &

osascript -e $'display dialog "Task Complete ✅\n\nBranch: feature-name\n\nFiles changed:\n  • file1.cs\n  • file2.axaml\n\nReady to push!" buttons {"OK"} default button "OK" with title "GitHub Copilot"' &
```

### Permission Request Examples:
```bash
osascript -e $'display dialog "Permission Needed ⚠️\n\nNeed to run database migration\n\nMay I proceed?" buttons {"OK"} default button "OK" with title "GitHub Copilot"' &

osascript -e $'display dialog "Ready to Commit ✅\n\nStaging changes for commit\n\nMay I proceed?" buttons {"OK"} default button "OK" with title "GitHub Copilot"' &
```

### Alternative: Display Notifications
For less critical updates, you can use macOS notifications (also non-blocking):
```bash
osascript -e $'display notification "Message body" with title "Title" subtitle "Subtitle" sound name "Glass"'
```

## Common Mistakes to Avoid:
❌ DON'T use double quotes with escaped `\"`: `osascript -e "display dialog \"Text\"..."` (fails in zsh with `\n`)
✅ DO use `$'...'` syntax: `osascript -e $'display dialog "Text"...'`
❌ DON'T forget the `&` at the end: Dialog will block execution
✅ DO add `&` for non-blocking: `osascript ... ' &`
❌ DON'T use plain single quotes: `osascript -e 'display dialog "Text\n"'` (doesn't interpret `\n`)
✅ DO use ANSI-C quoting: `osascript -e $'display dialog "Text\n"'`