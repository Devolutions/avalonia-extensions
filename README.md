[![image](https://github.com/user-attachments/assets/6a7bca22-bd0c-45cc-b847-8ea0b7776a6f)](https://devolutions.net/)


# avalonia-themes
Custom Avalonia Themes developed by [Devolutions](https://devolutions.net/)

➡️ [MacOS Theme](https://github.com/Devolutions/avalonia-themes/blob/master/src/Devolutions.AvaloniaTheme.MacOS/README.md)

➡️ [DevExpress Theme](https://github.com/Devolutions/avalonia-themes/blob/master/src/Devolutions.AvaloniaTheme.DevExpress/README.md)

➡️ [Linux Theme](https://github.com/Devolutions/avalonia-themes/blob/master/src/Devolutions.AvaloniaTheme.Linux/README.md)

➡️ [Avalonia Controls](https://github.com/Devolutions/avalonia-themes/blob/master/src/Devolutions.AvaloniaControls/README.md)

# Sample App

Contributors can use the SampleApp to test, debug and document styles for the various controls under each theme.

## Debugging

The SampleApp attaches the Avalonia Dev Tools for inspecting controls (open with F12).

### Support for new AvaloniaUI Developer Tools
If you own a licence for the new Dev Tools in _Avalonia Accelerate_, you can set an environment variable in your IDE's debug configuration. 
For example, in Rider: 

- Open **Run > Edit Configurations**
- Pick your configuration for the SampleApp 
- In the **Environment Variables** field add `USE_AVALONIA_ACCELERATE_TOOLS=true`

Make sure DeveloperTools are installed:

```batch
dotnet tool install --global AvaloniaUI.DeveloperTools.<your_OS>
```
(replace `<your_OS>` with `Windows`, `macOS` or `Linux`)

The F12 key then opens the new Dev Tools, and F10 opens the old version

## Avalonia Accelerate Controls

We will soon start to add styles for at least some [Avalonia Accelerate](https://avaloniaui.net/accelerate) controls, starting with `TreeDataGrid` in the DevExpress theme.

To view and test Accelerate-licensed controls in the SampleApp:

1. Create a `.env` file in the repository root.
2. Add your license key: `AVALONIA_LICENSE_KEY=your_key_here`.
3. Rebuild the solution.

**Note:** If the controls don't appear or you see build errors, you may need to force a NuGet restore or invalidate your IDE caches (e.g., **File > Invalidate Caches** in Rider) to update the conditional package dependencies.

## Testing

There is limited visual regression testing available. DemoPagea are compared against baseline screenshots in `tests/Devolutions.AvaloniaControls.VisualTests/Screenshots/Baseline`. Diffs for failing tests are saved to `tests/Devolutions.AvaloniaControls.VisualTests/Screenshots/Test-Diffs`. 

### Limitations
- Interactive behaviours (e.g. pointerOver, popUpOpen, focus, etc.) are not tested
- Accelerate controls that depend on a licence (e.g. TreeDataGrid) are not tested

### Usage
- `dotnet test --filter "DisplayName~VisualRegressionTests”` - runs all tests
- `dotnet test` - runs all tests, plus some little unit tests (worth it for the time saved typing!)
- `dotnet test --filter "DisplayName~DevExpress”` - runs tests for all controls implemented in DevExpress
- `dotnet test --filter "DisplayName~Button”` - runs tests for Button under each of the themes it's implemented in
- `dotnet test --list-tests` - lists all test cases
  
🤍🖤 All tests are run for light & dark mode

## AI Assistant Instructions

If you're an AI assistant (like GitHub Copilot or Claude Code) working on this repository, comprehensive guidelines are available in **[`.claude/CLAUDE.md`](.claude/CLAUDE.md)**.

This includes:
- Repository structure and architecture
- Development workflows and commands  
- Coding standards and best practices
- Version control rules and commit guidelines
- Custom commands for theme switching and development 
