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

