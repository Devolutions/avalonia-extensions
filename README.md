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

## AI Assistant Instructions

If you're an AI assistant (like GitHub Copilot or Claude Code) working on this repository, comprehensive guidelines are available in **[`.claude/CLAUDE.md`](.claude/CLAUDE.md)**.

This includes:
- Repository structure and architecture
- Development workflows and commands  
- Coding standards and best practices
- Version control rules and commit guidelines
- Custom commands for theme switching and development 