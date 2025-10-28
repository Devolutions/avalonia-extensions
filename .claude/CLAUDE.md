# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

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
│   └── workon.md                             # /workon command
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
dotnet build samples/SampleApp/SampleApp.csproj && cd samples/SampleApp/bin/Debug/net9.0 && dotnet SampleApp.dll

# Alternative: Use dotnet run (faster, but theme detection won't work)
# This runs from repo root, causing app to fall back to OS-default theme
# dotnet run --project samples/SampleApp/SampleApp.csproj
```

The SampleApp provides:
- Visual demos of all styled controls
- Theme switching via /workon command (see .claude/commands/workon.md)
- Inspection:
  - if `USE_AVALONIA_ACCELERATE_TOOLS=true` env var is set:
    - F12 opens Avalonia Accelerate Dev Tools
    - F10 opens classic Avalonia Dev Tools
  - otherwise F12 opens classic Avalonia Dev Tools

**Note on Theme Detection:**
The app's theme detection (`DetectDesignTheme()` in App.axaml.cs) expects the working directory to be `bin/Debug/net9.0/`. When running from the repo root via `dotnet run`, the detection fails and the app falls back to the OS-default theme (MacOS on macOS, DevExpress on Windows, etc.). This is why building and running from the bin directory is required for proper theme detection configured via `/workon` command.

### Testing
There are no automated tests in this repository. Testing is done manually via the SampleApp.

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
All projects target `.NET 9.0` and use Avalonia `11.3.x` packages.

## CI/CD
The repository uses GitHub Actions for building and publishing NuGet packages. The workflow:
- Builds packages on Windows (required for code signing)
- Code signs DLLs using Azure Key Vault
- Publishes to nuget.org
- Supports individual or batch package publishing
- Automatic versioning based on date (yyyy.MM.dd.0) for scheduled builds

## Control Status Tracking
Each theme's README.md maintains a checklist of styled controls with status indicators:
- ✅ Available in current build
- 🚧 In progress
- 🔮 On the roadmap

Refer to individual theme README files for complete lists and visual examples.



# Development Rules
- Don't run terminal commands without asking permission
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
- Never push to git without explicit permission - except when ask to prepare a PR
- Never stage to git without explicit permission - except when ask to prepare a PR
- **NEVER EVER force-push without explicit permission** - If rebase goes wrong, we lose the working version!
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

# User Notifications via Dialog
**CRITICAL**: The user may be working on other things and not watching this conversation. You MUST use dialog notifications for important events.

## When to Show Dialogs
Always show a dialog for:
1. **Task completion** - When you finish any task
2. **Permission requests** - When you need permission for operations (database changes, npm commands, git operations, etc.)
3. **Important errors** - When something fails that requires user attention
4. **Configuration changes** - When you modify ports, environment variables, or other config

## Dialog Command Format
**IMPORTANT**: Use **double quotes** for the outer `-e` parameter to avoid escaping issues with colons and special characters:

```bash
osascript -e "display dialog \"Message here\" buttons {\"OK\"} default button \"OK\" with title \"Claude Code\""
```

### Task Completion Examples:
```bash
osascript -e "display dialog \"Task Complete ✅\n\nAdded priority dropdown menu\" buttons {\"OK\"} default button \"OK\" with title \"Claude Code\""
osascript -e "display dialog \"Task Complete ✅\n\nConnected New Task button to backend\" buttons {\"OK\"} default button \"OK\" with title \"Claude Code\""
```

### Permission Request Examples:
```bash
osascript -e "display dialog \"Permission Needed ⚠️\n\nNeed to run database migration\n\nMay I proceed?\" buttons {\"OK\"} default button \"OK\" with title \"Claude Code\""
osascript -e "display dialog \"Ready to Commit ✅\n\nStaging changes for commit\n\nMay I proceed?\" buttons {\"OK\"} default button \"OK\" with title \"Claude Code\""
```

### Multi-line Messages:
Use `\n` for line breaks. Keep messages concise but informative:
```bash
osascript -e "display dialog \"Commits Created ✅\n\n1. feat - Added description field\n2. chore - Updated ports\n\nAll changes committed!\" buttons {\"OK\"} default button \"OK\" with title \"Claude Code\""
```

## Common Mistakes to Avoid:
❌ DON'T use single quotes for outer command: `osascript -e 'display dialog "Text: value"'` (fails with colons)
✅ DO use double quotes for outer command: `osascript -e "display dialog \"Text - value\""`
❌ DON'T forget to escape inner quotes when using double quotes
✅ DO escape all inner quotes with backslash: `\"`