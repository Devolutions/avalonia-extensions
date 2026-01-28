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
    - [ColorPicker](#colorpicker)
    - [ComboBox](#combobox)
      - [ComboBoxItem](#comboboxitem)
    - [DataGrid](#datagrid)
    - [EditableComboBox](#editablecombobox) ([Custom control](https://github.com/Devolutions/avalonia-extensions/blob/master/src/Devolutions.AvaloniaControls/README.md))
    - [Expander](#expander)
    - [ListBox](#listbox)
      - [ListBoxItem](#listboxitem)
    - [NumericUpDown](#numericupdown)
      - [ButtonSpinner](#buttonspinner)
    - [RadioButton](#radiobutton)
    - [ScrollViewer](#scrollviewer)
      - [ScrollBar](#scrollbar)
    - [Separator](#separator)
    - [TabControl](#tabcontrol)
      - [TabItem](#tabitem)
    - [TabPane](#tabpane)
    - [TagInput](#taginput) ([Custom control (Ursa)](https://github.com/Devolutions/avalonia-extensions/blob/master/src/Devolutions.AvaloniaControls/README.md))
    - [TextBox](#textbox)
    - [TreeView](#treeview)
      - [TreeViewItem](#treeviewitem)
    - [Window](#window)
  - 🚧 In progress ...
    - small improvements & fixes, some code cleanup
    - ContextMenu
    - SplitButton
  - 🔮 Next on the road map ...
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
     <DevolutionsDevExpressTheme />
  </Application.Styles>
</Application>
```

**Note:** Some global Styles will also be loaded by default, you can opt out by setting `GlobalStyles` to false (`<DevolutionsDevExpressTheme GlobalStyles="False" />`). GlobalStyles are also available as a separate tag `<DevolutionsDevExpressThemeGlobalStyles />` to cover scenarios where consumers would like to scope them to some control instead of including them globally. This is may be necessary to prevent styles from "bleeding out" in cases where that might be undesirable.

## Styled Controls

Most of the images below are screenshots from the [SampleApp test and demo pages](https://github.com/Devolutions/avalonia-extensions/tree/master/samples/SampleApp/DemoPages) - feel free to check out the code there for more detailed usage examples.

For an always up-to-date visual reference you can also browse the [baseline screenshots](https://github.com/Devolutions/avalonia-extensions/tree/master/tests/Devolutions.AvaloniaControls.VisualTests/Screenshots/Baseline/DevExpress).

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
|| <img alt="CheckBox demos" src="https://github.com/user-attachments/assets/784deb51-1566-4c5b-abf1-be7e995ecacf" style="width: 659px; max-width: 100%;" /> <br /><img alt="CheckBox demos - dark mode" src="https://github.com/user-attachments/assets/9bbe6be9-7174-48c0-a83b-b94bbdfaee8b" style="width: 659px; max-width: 100%;" /> <br /><img alt="CheckBox interactivity" src="https://github.com/user-attachments/assets/476336ef-4ca8-4ed1-ba82-a5bd5e127375" style="width: 300px; max-width: 100%;" /> <img alt="CheckBox interactivity" src="https://github.com/user-attachments/assets/70fed8bd-62c0-4df2-88d7-55dd62f29539" style="width: 300px; max-width: 100%;" /> |
|✅ | <h3>ColorPicker</h3> |
|| <img alt="ColorPicker" src="https://github.com/user-attachments/assets/8b3ebce8-6cd8-405e-9817-ead388d282f1" style="width: 1166px; max-width: 100%;" /> <br /><img alt="ColorPicker - dark mode" src="https://github.com/user-attachments/assets/4b85f0f6-7aa1-43c0-9dbe-2f5e56e3ff52" style="width: 1166px; max-width: 100%;" /> |
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
|✅ | <h3>EditableComboBox</h3> ([Custom control](https://github.com/Devolutions/avalonia-extensions/blob/master/src/Devolutions.AvaloniaControls/README.md))|
|| <img width="392" alt="EditableComboBox" src="https://github.com/user-attachments/assets/4762e4ce-07a1-420e-899a-2a355fc920e1" style="width: 392px; max-width: 100%;" /> <br /><img width="403" alt="EditableComboBox - dark mode" src="https://github.com/user-attachments/assets/716a10e2-8dfd-49f4-a767-5feb26c434e3" style="width: 403px; max-width: 100%;" /> |
|| <h3>EmbeddableControlRoot</h3> |
|||
|✅ | <h3>Expander</h3> |
|| <img alt="Expanders" src="https://github.com/user-attachments/assets/289b4d9c-4052-42bf-a7e9-efd432393e44" style="width: 932px; max-width: 100%;"/><br /><br /><img alt="Expanders - dark mode" src="https://github.com/user-attachments/assets/59ab26cd-6992-457a-b349-fda308c6ca49" style="width: 932px; max-width: 100%;"/> |
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
|| <img alt="ListBox demos" src="https://github.com/user-attachments/assets/ef36c281-71fd-4fc6-911f-55490de05d32" style="width: 927px; max-width: 100%;" /> <br /><img alt="ListBox demos - darkmode" src="https://github.com/user-attachments/assets/19d4d810-588f-42d6-9750-fb21329034b3" style="width: 927px; max-width: 100%;" /> |
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
|✅ | <h3>RadioButton</h3> |
|| <img alt="RadioButtons Light" src="https://github.com/user-attachments/assets/a8ab6ada-be6a-4286-b329-4edbad9928f0" style="width: 276px; max-width: 100%;"/> <br /><img alt="RadioButtons Dark" src="https://github.com/user-attachments/assets/b0ce93a9-6076-4209-a281-1d11d8f55f8f" style="width: 276px; max-width: 100%;"/> <br /><img alt="RadioButtons Dark" src="https://github.com/user-attachments/assets/4e4e8779-d7f9-48ea-8cdd-8db4e0ec918e" style="width: 276px; max-width: 100%;"/> <br /><img alt="RadioButtons Dark" src="https://github.com/user-attachments/assets/a350e916-c423-4c39-afaf-4f9173a91a24" style="width: 276px; max-width: 100%;"/> |
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
|✅ | <h3>Separator</h3> |
|| <img alt="Separator demo" src="https://github.com/user-attachments/assets/4c10e8ab-9d19-4c1e-9891-e61ba8ad3a97" style="width: 758px; max-width: 100%;" /> <br /><img alt="Separator demo - dark mode" src="https://github.com/user-attachments/assets/4cff138b-35c5-4580-9aba-5b4b40c42de6" style="width: 758px; max-width: 100%;" /> |
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
|✅ | <h3>TagInput</h3> ([Custom control (Ursa)](https://github.com/Devolutions/avalonia-extensions/blob/master/src/Devolutions.AvaloniaControls/README.md)) |
|| Input control for managing tags (keywords, labels, recipients). Supports adding/removing tags, separators, max count, duplicate prevention. <br /><img src="https://github.com/user-attachments/assets/82518cbf-e99a-4111-99e4-f4b9ec7cf9d6" alt="TagInput" style="width: 528px; max-width: 100%;" /> <br /><img src="https://github.com/user-attachments/assets/a543f119-a272-4b54-a250-e9acba572b19" alt="TagInput dark" style="width: 528px; max-width: 100%;" /> |
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

