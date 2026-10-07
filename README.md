[![image](https://github.com/user-attachments/assets/6a7bca22-bd0c-45cc-b847-8ea0b7776a6f)](https://devolutions.net/)


# avalonia-themes
Custom Avalonia Themes developed by [Devolutions](https://devolutions.net/)

➡️ [MacOS Theme](https://github.com/Devolutions/avalonia-themes/blob/master/src/Devolutions.AvaloniaTheme.MacOS/README.md)

➡️ [DevExpress Theme](https://github.com/Devolutions/avalonia-themes/blob/master/src/Devolutions.AvaloniaTheme.DevExpress/README.md)

➡️ [🚧 WinUI Theme](https://github.com/Devolutions/avalonia-themes/blob/master/src/Devolutions.AvaloniaTheme.WinUI/README.md)

➡️ [Linux Theme](https://github.com/Devolutions/avalonia-themes/blob/master/src/Devolutions.AvaloniaTheme.Linux/README.md)

➡️ [Avalonia Controls](https://github.com/Devolutions/avalonia-themes/blob/master/src/Devolutions.AvaloniaControls/README.md)

## Package Compatibility

> **Avalonia 12 line.** Packages `2026.6.17-avalonia12` and later are on the Avalonia 12 line. The first stable Avalonia 12 release is `2026.6.23`, and `2026.6.16` is the last stable release compatible with Avalonia 11.
>
> Maintenance-only fixes for the Avalonia 11 line may still be published as `2026.6.16.x` for a limited time. See the [Avalonia 11 -> 12 breaking changes](https://docs.avaloniaui.net/docs/avalonia12-breaking-changes) for upstream migration guidance.

# Sample App

Contributors can use the SampleApp to test, debug and document styles for the various controls under each theme.

## Debugging

The SampleApp attaches the Avalonia Dev Tools for inspecting controls (open with F12).

## Avalonia Accelerate Controls

We will soon start to add styles for at least some [Avalonia Accelerate](https://avaloniaui.net/accelerate) controls, starting with `TreeDataGrid` in the DevExpress theme.

To view and test Accelerate-licensed controls in the SampleApp:

1. Create a `.env` file in the repository root.
2. Add your license key: `AVALONIA_LICENSE_KEY=your_key_here`.
3. Rebuild the solution.

**Note:** If the controls don't appear or you see build errors, you may need to force a NuGet restore or invalidate your IDE caches (e.g., **File > Invalidate Caches** in Rider) to update the conditional package dependencies.

## Testing

There is limited visual regression testing available. DemoPages are compared against personal screenshots in `tests/Devolutions.AvaloniaControls.VisualTests/Screenshots/LocalBaselines/{Theme}` when available, otherwise against a tracked image in `Screenshots/Baseline/{CurrentOS}/{Theme}`. Images from other operating systems are never compared. Personal baselines are gitignored and belong to this machine. Diffs for failing tests are saved to `tests/Devolutions.AvaloniaControls.VisualTests/Screenshots/Test-Diffs`.
Screenshots are captured at a fixed width (`1200`) with auto-calculated content height (capped at `3000`) to cover below-the-fold examples without requiring manual page-by-page configuration.

### Limitations
- Interactive behaviours (e.g. pointerOver, popUpOpen, focus, etc.) are not tested
- Accelerate controls that depend on a licence (e.g. TreeDataGrid) are not tested

### Canonical and Personal Baselines

Only screenshots from a theme's target platform are canonical and required in PRs:

| Theme | Canonical platform |
| --- | --- |
| DevExpress, WinUiClassic, WinUiMica | Windows |
| MacClassic, LiquidGlass | macOS |
| Linux/Yaru | Linux |

Cross-platform screenshots are a local developer convenience, not required PR coverage. In particular, Windows WinUI changes do **not** require macOS or Linux baseline images. Adding a new control still requires appropriate target-platform visual coverage.

Pages marked "same as reference" in the catalog compare the two themes' rendered output directly instead of storing a separate PNG for the delegating variant.

Ordinary runs prefer a personal baseline. If none exists, they compare only against a tracked image from the current OS, never the theme's target OS when it differs. Missing local baselines always fail explicitly:

- **Missing baseline:** neither a personal image nor a same-OS tracked image exists.
- **Missing local baseline:** the personal image is missing, but the screenshot matches a same-OS tracked image.
- **Missing local baseline** and **Visual regression:** the personal image is missing and the same-OS tracked image differs; the summary shows both rows.

With a personal image present, a mismatch is simply **Visual regression**. Run a deliberate baseline update to establish personal images on your machine; do not update merely to hide an unexplained regression. Personal snapshots do not automatically follow upstream changes, so refresh them deliberately when those changes are intentional.

Personal images live directly under `LocalBaselines/{Theme}/`, with no OS subdirectory. When migrating an older local tree, remove the other OS trees **before** moving your current OS's theme folders up one level: `Linux` is both an OS directory name and a theme name. Do not reuse other OS images. Each machine generates its own personal set.

**Temporary local publishing workflow (until GitHub runners own canonical generation):**

- `--update-baselines` generates personal images for all selected themes on your current OS.
- It also copies images into the tracked canonical tree **only for themes whose target platform is your current OS**. For example, on macOS it publishes MacClassic/LiquidGlass, but leaves Windows and Linux canonical files untouched.
- Review and commit only intentional target-platform changes in `Screenshots/Baseline/`. The rest remain in gitignored `LocalBaselines/`.
- A new clone has no personal images. You can generate them with the existing update command; be aware that it also publishes your native themes to the tracked tree.

The planned GitHub workflow will replace this temporary local publishing step. See [the CI proposal](docs/visual-regression-ci-proposal.md).

> **Note:** We're showing single quotes here, since in interactive `bash`/`zsh` (Linux/Mac), `!` triggers history expansion, so the following term is interpreted as a history variable, unless the filter string is single-quoted (`'...'`) instead of double-quoted. However, on Windows only PowerShell supports the single quotes - so you might want to get used to double quotes if you're always on Windows.


### Usage
- `dotnet test` - runs all tests
- `dotnet test --filter 'DisplayName~VisualRegressionTests'` - runs only visual regression tests
- `dotnet test --filter 'DisplayName!~VisualRegressionTests'` - runs only non-visual tests
- `dotnet test --filter 'DisplayName~DevExpress'` - runs tests for all controls implemented in DevExpress
- `dotnet test --filter 'DisplayName~Button'` - runs tests for Button under each of the themes it's implemented in
- `dotnet test --list-tests` - lists all test cases

#### Combining `DisplayName` filters
The xUnit/VSTest filter syntax uses `&` for AND, `|` for OR and `!` for NOT. 

- `dotnet test --filter 'DisplayName~Button&DisplayName~DevExpress'` - runs only tests whose display name contains both `Button` and `DevExpress`
- `dotnet test --filter 'DisplayName~ContextMenu|DisplayName~MenuFlyout'` - runs tests containing either `ContextMenu` or `MenuFlyout`
- `dotnet test --filter 'DisplayName~Button&DisplayName!~HyperlinkButton'` - runs tests containing `Button` but excluding `HyperlinkButton`


🆕 **Shorthand command & cleaner output:**
- `./devtest` (macOS/Linux/Git Bash) or `.\devtest` (Windows PowerShell/CMD) - runs all tests with a succinct summary.
- `./devtest visual` - runs all tests in `Devolutions.AvaloniaControls.VisualTests`.
- `./devtest nonvisual` - runs all tests in `Devolutions.AvaloniaControls.Tests`.
- `./devtest functional` - alias for `nonvisual`.
- `./devtest --filter EditableCombo` - shorthand for (`DisplayName~EditableCombo`).

The wrapper prints a visual regression summary for screenshot mismatches and missing
baselines, including the theme, page, variant, and output path.

**Updating baseline screenshots** when changes are intentional:
- `./devtest visual --update-baselines` - updates personal baselines for all selected visual tests and publishes only native target-platform images to the tracked canonical tree.
- `./devtest --update-baselines` - runs all projects with baseline updates enabled.
- **macOS/Linux:** `UPDATE_BASELINES=true dotnet test [filters]`
- **Windows (PowerShell):** `$env:UPDATE_BASELINES="true"; dotnet test [filters]; Remove-Item env:UPDATE_BASELINES`
- **Windows (Command Prompt):** `set UPDATE_BASELINES=true && dotnet test [filters] && set UPDATE_BASELINES=`
  
🤍🖤 All tests are run for light & dark mode
**However** to keep things reasonably quick, the dark mode test only runs if the light mode test for the same 
control has passed. This can lead to missed issues, when both versions have _different_ problems. (If in doubt, 
update, then delete the new dark baseline shot, and run the test again (light will no longer fail, and you can see 
what happens in dark))

## AI Assistant Instructions

If you're an AI assistant (like GitHub Copilot or Claude Code) working on this repository, comprehensive guidelines are available in **[`.claude/CLAUDE.md`](.claude/CLAUDE.md)**.

This includes:
- Repository structure and architecture
- Development workflows and commands  
- Coding standards and best practices
- Version control rules and commit guidelines
- Custom commands for theme switching and development 
