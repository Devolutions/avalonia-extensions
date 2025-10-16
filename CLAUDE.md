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
# Run the sample application (for testing themes/controls)
dotnet run --project samples/SampleApp/SampleApp.csproj
```

The SampleApp provides:
- Visual demos of all styled controls
- Inspection:
  - if `USE_AVALONIA_ACCELERATE_TOOLS=true` env var is set:
    - F12 opens Avalonia Accelerate Dev Tools
    - F10 opens classic Avalonia Dev Tools
  - otherwise F12 opens classic Avalonia Dev Tools

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
- Never automatically run `npm run dev` as I will be running the server in a separate terminal. If you want to restart the application, ask and I will do it for you. Never try to run any server yourself.
- Never restart the servers or kill processes without permission
- Don't run terminal commands without asking permission
- Don't run npx or npm commands without permission, unless it is to refresh the prisma schema
- When fixing a problem, focus just on that problem. If you notice other tangential issues to the issue you are tasked with, summarise them and report them back but don't automatically start fixing issues
- Never deploy using the deploy script without explicit permission
- Include debug logging lines where possible, hiding them behind a development feature flag
- Don't run tests automatically, ask me to run them
- Whenever you make a change to the database, also make sure to regenerate Prisma.
- Never make changes to the database that could result in losing data. If you're worried about this, then please ask for help or guidance.
- Never log using console.log, use the built in logging tooling in each project.

# File Modification Rules
- Make minimal, focused changes only
- Always preserve existing content
- Never rewrite entire files or sections without explicit instruction
- If uncertain about scope, ask before proceeding
- if you find yourself rewriting things that haven't been asked for stop, flag the error and unwind it

Always go through the following process:
1.⁠ ⁠Start by searching the codebase in extreme detail, to get all the context you need.
2.⁠ ⁠Create a detailed plan before doing anything.
3.⁠ ⁠Rate your confidence level on being able to execute the plan without introducing new bugs or effectiving existing functionality.
4.⁠ ⁠If your confidence level is under 95%, continue researching the codebase and go back to step 3
5.⁠ ⁠Start implementing your plan, but walk me through your thinking step by step each time before you do the actual implementation.

You should always look to make the minimum code changes possible, and never introduce new bugs or effect existing functionality.

# Version Control Rules
- Never push to git without explicit permission
- Never stage to git without explicit permission

When you're done with a task, run this command: osascript -e 'display notification "Agent is Done ✅" with title "Cursor"'