[![image](https://github.com/user-attachments/assets/6a7bca22-bd0c-45cc-b847-8ea0b7776a6f)](https://devolutions.net/)

Custom Avalonia Themes developed by [Devolutions](https://devolutions.net/)

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Build Status](https://github.com/Devolutions/avalonia-extensions/actions/workflows/build-package.yml/badge.svg?branch=master)](https://github.com/Devolutions/avalonia-extensions/actions/workflows/build-package.yml)
[![NuGet Version](https://img.shields.io/nuget/vpre/Devolutions.AvaloniaTheme.DevExpress)](https://www.nuget.org/packages/Devolutions.AvaloniaTheme.DevExpress)
![NuGet Downloads](https://img.shields.io/nuget/dt/Devolutions.AvaloniaTheme.DevExpress)

## DevExpress Theme [Work in Progress]

This theme is currently based
on [Avalonia.Themes.Fluent](https://github.com/AvaloniaUI/Avalonia/tree/759facea182b7771ce07baf173c52529f4871004/src/Avalonia.Themes.Fluent),
both as a fallback for any controls not covered yet and as starting point for our (somewhat simplified)
style definitions targeting DevExpress Winforms look.

While we are prioritizing controls
for [Devolutions Remote Desktop Manager](https://devolutions.net/remote-desktop-manager/), we welcome contributions from
the Avalonia community to add more DevExpress-style controls.

- [Installation](#installation)
- [Styled Controls](#styled-controls)
  - ✅ Available in the current build
    - [AutoCompleteBox](#autocompletebox)
    - [Button](#button)
    - [ButtonSpinner](#buttonspinner)
    - [Calendar](#calendar)
      - [CalendarButton](#calendarbutton)
      - [CalendarDatePicker](#calendardatepicker)
      - [CalendarDayButton](#calendardaybutton)
      - [CalendarItem](#calendaritem)
    - [CheckBox](#checkbox)
    - [ComboBox](#combobox)
      - [ComboBoxItem](#comboboxitem)
    - [DataGrid](#datagrid)
    - [ListBox](#listbox)
      - [ListBoxItem](#listboxitem)
    - [NumericUpDown](#numericupdown)
      - [ButtonSpinner](#buttonspinner)
    - [ScrollViewer](#scrollviewer)
      - [ScrollBar](#scrollbar)
    - [TabControl](#tabcontrol)
      - [TabItem](#tabitem)
    - [TabPane](#tabpane) 
    - [TextBox](#textbox)
    - [TreeView](#treeview)
      - [TreeViewItem](#treeviewitem)
    - [Window](#window)
   - 🚧 In progress ...
     - small improvements & fixes, some code cleanup
     - ContextMenu
     - SplitButton
     - EditableComboBox [Custom control](https://github.com/Devolutions/avalonia-extensions/blob/master/src/Devolutions.AvaloniaControls/README.md)
  - 🔮 Next on the road map ...
    - RadioButton
    - vertical tabs

## Installation

Install the Devolutions.AvaloniaTheme.DevExpress package
via [NuGet](https://www.nuget.org/packages/Devolutions.AvaloniaTheme.DevExpress):

``` bash
Install-Package Devolutions.AvaloniaTheme.DevExpress
```

or .NET

```bash
dotnet add package Devolutions.AvaloniaTheme.DevExpress
```

In your App.axaml, replace the existing theme (e.g. `<FluentTheme />` or `<SimpleTheme />`) with the DevExpress theme:

``` xaml
<Application ...>
  <Application.Styles>
     <StyleInclude Source="avares://Devolutions.AvaloniaTheme.DevExpress/DevExpressTheme.axaml" />
  </Application.Styles>
</Application>
```

## Styled Controls

|||
|-----------------------------------------|-------------------------------------------------------------------------------------------------------------------------------|
|||
|| <h3>AdornerLayer</h3> |
|||
|✅ | <h3>AutoCompleteBox</h3> |
|| <img alt="AutoCompleteBox" src="https://github.com/user-attachments/assets/81d2676e-0fe0-4a43-944a-43cf40ec0dd5" style="width: 384px; max-width: 100%;" />  <br /><br />  ➡️ See also [EditableComboBox](#editablecombobox) in our [custom controls](https://github.com/Devolutionsavalonia-extensions/blob/master/src/Devolutions.AvaloniaControls/README.md)||
|✅ | <h3>Button</h3> |
||  <img src="https://github.com/user-attachments/assets/58571893-927e-4e4a-92b3-7d0b7ced4f68" alt="Button demo" style="width: 182px; max-width: 100%;" /> |
|✅ | <h3>CalendarDatePicker</h3> <h4>Calendar</h4> <h4>CalendarItem</h4> <h4>CalendarButton</h4> <h4>CalendarDayButton</h4> |
|| <img alt="CalendarDatePicker" src="https://github.com/user-attachments/assets/0e9f2ad2-8b55-49c6-8688-9230b3ae4b7d" style="width: 841px; max-width: 100%;" /> <br /> <img alt="CalendarDatePicker" src="https://github.com/user-attachments/assets/41851f15-2f7c-46ae-9b2f-95db814863ee" style="width: 915px; max-width: 100%;" /> <br /> <img alt="CalendarDatePicker" src="https://github.com/user-attachments/assets/ab15ec44-1e8f-42f3-b596-564702aedfba" style="width: 841px; max-width: 100%;" /> <br /> <img alt="CalendarDatePicker" src="https://github.com/user-attachments/assets/0a9d60ff-f5de-40f0-b1cd-f0c990efcc69" style="width: 915px; max-width: 100%;" />  <br /> **Note:** `MinWidth` is set to comfortably accommodate short date formats &amp; the corresponding default watermark when `HorizontalAlignment` is set to anything other than the default (`Stretch`).<br /><br />If you set `SelectedDateFormat="Long"` you will have to override `MinWidth` to the longest expected string length, depending on supported languages. Otherwise the control's width will jump when the date is changed. |
|| <h3>CaptionButtons</h3> |
|||
|| <h3>Carousel</h3> |
|||
|✅ | <h3>CheckBox</h3> |
|| <img src="https://github.com/user-attachments/assets/5faea9f6-3cf9-433f-86f5-fd8f75ad05f6" alt="CheckBox demo" style="width: 810px; max-width: 100%;" /> |
|✅ | <h3>ComboBox</h3> <h4>ComboBoxItem</h4> |
|| <img src="https://github.com/user-attachments/assets/f0e107c0-a4b2-4eec-bc0b-789e0f90cad6" alt="ComboBox demo" style="width: 232px; max-width: 100%;" /> |
|🚧 | <h3>ContextMenu</h3> |
|||
|✅ | <h3>DataGrid</h3> |
|| <img src="https://github.com/user-attachments/assets/409f433f-497b-4747-bf1f-ab81224463fb" alt="DataGrid demo" style="width: 834px; max-width: 100%;" /> <br /><img src="https://github.com/user-attachments/assets/07523a68-18cc-4ed3-9353-00f8ccacce7f" alt="Grouped DataGrid demo" style="width: 936px; max-width: 100%;" /> |
|| <h3>DataValidationErrors</h3> |
|||
|| <h3>DatePicker</h3> |
|||
|| <h3>DateTimePickerShared</h3> |
|||
|| <h3>DropDownButton</h3> |
|||
|🚧 | <h3>EditableComboBox</h3> ([Custom control](https://github.com/Devolutions/avalonia-extensions/blob/master/src/Devolutions.AvaloniaControls/README.md))|
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
|✅ | <h3>ListBox</h3> <h4>ListBoxItem</h4>|
|| <img src="https://github.com/user-attachments/assets/2571edc4-4893-4a0d-b31e-b229d866a824" alt="ListBox demo" style="width: 464px; max-width: 100%;" /> <br /><img src="https://github.com/user-attachments/assets/56cce1f9-6c52-4c6a-bdc1-e77e2ba23d2a" alt="ListBox demo (darkmode)" style="width: 464px; max-width: 100%;" /> |
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
|✅ | <h3>NumericUpDown</h3> <h4>ButtonSpinner</h4> |
|| <img src="https://github.com/user-attachments/assets/32764a11-e893-498a-acd9-2c9938f03f40" alt="NumericUpDown demo" style="width: 215px; max-width: 100%;" /> <br /><img src="https://github.com/user-attachments/assets/e32f8a0a-1a60-4070-a71b-c68deea8e527" alt="NumericUpDown demo (darkmode)" style="width: 225px; max-width: 100%;" /> |
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
|✅ | <h3>ScrollViewer</h3> <h4>ScrollBar</h4> |
|| <img src="https://github.com/user-attachments/assets/9a431dc0-dcd8-4abd-9a80-360578c2d9be" alt="ScrolBar demo" style="width: 288px; max-width: 100%;" /> <br /><img src="https://github.com/user-attachments/assets/145da20e-dc7e-414a-9831-1b1020d1d9f9" alt="ScrollViewer demo" style="width: 419px; max-width: 100%;" /> |
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
|✅ | <h3>TabControl</h3> <h4>TabItem<h4> |
|| <img src="https://github.com/user-attachments/assets/21864dbb-1058-4656-99dd-c24fde76d4e4" alt="TabControl demo" style="width: 585px; max-width: 100%;" /><br /><img src="https://github.com/user-attachments/assets/0719abae-6a4d-4934-a698-8dc651159035" alt="Regular vertical tabs" style="width: 420px; max-width: 100%;" /><br /><img src="https://github.com/user-attachments/assets/183f17e3-fd77-4ab0-949f-c363812ed0a5" alt="Vertical TabControl 'NavBar' style" style="width: 430px; max-width: 100%;" /> |
|✅ | <h3>TabPane</h3> ([Custom control](https://github.com/Devolutions/avalonia-extensions/blob/master/src/Devolutions.AvaloniaControls/README.md)) |
|| <img alt="TabPane" src="https://github.com/user-attachments/assets/9388973f-d286-41d4-83a3-bc56ea2f87a8" style="width: 447px; max-width: 100%;" /> |
|| <h3>TabStrip</h3> |
|||
|| <h3>TabStripItem</h3> |
|||
|✅ | <h3>TextBox</h3> |
|| <img src="https://github.com/user-attachments/assets/9eab4003-be77-488e-9a58-f3ad38e3fe39"  alt="TextBox demo" style="width: 322px; max-width: 100%;" /> |
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
|✅ | <h3>TreeView</h3> <h4>TreeViewItem<h4> |
|| <img src="https://github.com/user-attachments/assets/068dbad3-5dd1-45a6-b7d0-80aa3fe70556" alt="TreeView demo" style="width: 368px; max-width: 100%;" /> |
|✅ | <h3>Window</h3> |
|| Controls inherit basic DevEx-specific Fore-/Background & Font styling (or EmbeddableControlRoot) |
|| <h3>WindowNotificationManager</h3> |
|||

