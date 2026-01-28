[![image](https://github.com/user-attachments/assets/6a7bca22-bd0c-45cc-b847-8ea0b7776a6f)](https://devolutions.net/)

Custom Avalonia Themes developed by [Devolutions](https://devolutions.net/)

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Build Status](https://github.com/Devolutions/avalonia-extensions/actions/workflows/build-package.yml/badge.svg?branch=master)](https://github.com/Devolutions/avalonia-extensions/actions/workflows/build-package.yml)
[![NuGet Version](https://img.shields.io/nuget/vpre/Devolutions.AvaloniaTheme.Linux)](https://www.nuget.org/packages/Devolutions.AvaloniaTheme.Linux)
![NuGet Downloads](https://img.shields.io/nuget/dt/Devolutions.AvaloniaTheme.Linux)

## Linux  Theme [Work in Progress]

This theme is currently based
on [Avalonia.Themes.Fluent](https://github.com/AvaloniaUI/Avalonia/tree/759facea182b7771ce07baf173c52529f4871004/src/Avalonia.Themes.Fluent),
both as a fallback for any controls not covered yet and as starting point for our style definitions targeting a
Linux look similar to Ubuntu’s default “Yaru” GTK theme..

While we are prioritizing controls
for [Devolutions Remote Desktop Manager](https://devolutions.net/remote-desktop-manager/), we welcome contributions from
the Avalonia community to add more controls.

- [Installation](#installation)
- [Styled Controls](#styled-controls)
  - Available in the current build
    - [ComboBox](#combobox)
      - [ComboBoxItem](#comboboxitem)
  - 🚧 In progress ...
    - Dark mode support
  - 🔮 Next on the road map ...

## Installation

Install the Devolutions.AvaloniaTheme.Linux package
via [NuGet](https://www.nuget.org/packages/Devolutions.AvaloniaTheme.Linux):

``` bash
Install-Package Devolutions.AvaloniaTheme.Linux
```

or .NET

```bash
dotnet add package Devolutions.AvaloniaTheme.Linux
```

In your App.axaml, replace the existing theme (e.g. `<FluentTheme />` or `<SimpleTheme />`) with the Linux theme:

``` xaml
<Application ...>
  <Application.Styles>
     <DevolutionsLinuxYaruTheme />
  </Application.Styles>
</Application>
```

## Styled Controls

Below are some screenshots from the [SampleApp test and demo pages](https://github.com/Devolutions/avalonia-extensions/tree/master/samples/SampleApp/DemoPages) - feel free to check out the code there for more detailed usage examples. For an always up-to-date visual reference you can also browse the [baseline screenshots](https://github.com/Devolutions/avalonia-extensions/tree/master/tests/Devolutions.AvaloniaControls.VisualTests/Screenshots/Baseline/Linux). 

||                                                                                                 |
|-----------------------------------------|-----------------------------------------------------------------------------------------------------------------------------|
|||
|| <h3>AdornerLayer</h3> |
|||
|| <h3>AutoCompleteBox</h3> |
|||
|| <h3>Button</h3> |
|||
|| <h3>ButtonSpinner</h3> |
|||
|| <h3>Calendar</h3> |
|||
|| <h3>CalendarButton</h3> |
|||
|| <h3>CalendarDatePicker</h3> |
|||
|| <h3>CalendarDayButton</h3> |
|||
|| <h3>CalendarItem</h3> |
|||
|| <h3>CaptionButtons</h3> |
|||
|| <h3>Carousel</h3> |
|||
|| <h3>CheckBox</h3> |
|||
|✅ | <h3>ComboBox</h3> <h4>ComboBoxItem</h4> |
|| <img alt="ComboBox" src="https://github.com/user-attachments/assets/dffff816-a0d8-4dc1-8906-ae5b4946690c" style="width: 360px; max-width: 100%;" /> <img alt="ComboBox - dark mode" src="https://github.com/user-attachments/assets/3010743c-f9dd-4446-9a31-cc717504cfc6" style="width: 360px; max-width: 100%;" /> |
|| <h3>ContextMenu</h3> |
|||
|| <h3>DataGrid</h3> |
|||
|| <h3>DataValidationErrors</h3> |
|||
|| <h3>DatePicker</h3> |
|||
|| <h3>DateTimePickerShared</h3> |
|||
|| <h3>DropDownButton</h3> |
|||
|| <h3>EmbeddableControlRoot</h3> |
|||
|| <h3>Expander</h3> |
|||
|| <h3>FluentControls</h3> |
|||
|| <h3>FlyoutPresenter</h3> |
|||
|| <h3>GridSplitter</h3> |
|||
|| <h3>HeaderedContentControl</h3> |
|||
|| <h3>HyperlinkButton</h3> |
|||
|| <h3>ItemsControl</h3> |
|||
|| <h3>Label</h3> |
|||
|| <h3>ListBox</h3> |
|||
|| <h3>ListBoxItem</h3> |
|||
|| <h3>ManagedFileChooser</h3> |
|||
|| <h3>Menu</h3> |
|||
|| <h3>MenuFlyoutPresenter</h3> |
|||
|| <h3>MenuItem</h3> |
|||
|| <h3>MenuScrollViewer</h3> |
|||
|| <h3>NotificationCard</h3> |
|||
|| <h3>NumericUpDown</h3> |
|||
|| <h3>OverlayPopupHost</h3> |
|||
|| <h3>PathIcon</h3> |
|||
|| <h3>PopupRoot</h3> |
|||
|| <h3>ProgressBar</h3> |
|||
|| <h3>RadioButton</h3> |
|||
|| <h3>RefreshContainer</h3> |
|||
|| <h3>RefreshVisualizer</h3> |
|||
|| <h3>RepeatButton</h3> |
|||
|| <h3>ScrollViewer</h3> <h4>ScrollBar</h4> |
|||
|| <h3>SelectableTextBlock</h3> |
|||
|| <h3>Separator</h3> |
|||
|| <h3>Slider</h3> |
|||
|| <h3>SplitButton</h3> |
|||
|| <h3>SplitView</h3> |
|||
|| <h3>TabControl</h3> <h4>TabItem<h4> |
|||
|| <h3>TabStrip</h3> |
|||
|| <h3>TabStripItem</h3> |
|||
|| <h3>TextBox</h3> |
|||
|| <h3>TextSelectionHandle</h3> |
|||
|| <h3>ThemeVariantScope</h3> |
|||
|| <h3>TimePicker</h3> |
|||
|| <h3>TitleBar</h3> |
|||
|| <h3>ToggleButton</h3> |
|||
|| <h3>ToggleSwitch</h3> |
|||
|| <h3>ToolTip</h3> |
|||
|| <h3>TransitioningContentControl</h3> |
|||
|| <h3>TreeView</h3> <h4>TreeViewItem<h4> |
|||
|| <h3>Window</h3> |
|||
|| <h3>WindowNotificationManager</h3> |
|||

