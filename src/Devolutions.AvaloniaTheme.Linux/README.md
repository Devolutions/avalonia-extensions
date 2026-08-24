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

> **Avalonia 12 required.** Packages `2026.6.17-avalonia12` and later require Avalonia 12. The last stable release compatible with Avalonia 11 is [`2026.6.16`](https://www.nuget.org/packages/Devolutions.AvaloniaTheme.Linux/2026.6.16).

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

### Menu pack (menu controls only)

Use the menu pack when you only want Linux menu styling but not the full `<DevolutionsLinuxYaruTheme />`
(e.g. to give consistent Menu chrome to custom-branded sections of your app).

The Linux theme has no OS-dependent sub-themes, so — unlike the MacOS pack, which has to pick between its
classic and LiquidGlass variants at runtime — this pack is a plain style include, the same shape as the
DevExpress pack.

```xaml
<Application ...>
  <Application.Styles>
    <!-- Required prerequisite: must be application-wide, not scoped to a subtree -->
    <FluentTheme />

    <!-- Linux menu pack -->
    <StyleInclude Source="avares://Devolutions.AvaloniaTheme.Linux/Controls/MenuPack.styles.axaml" />
  </Application.Styles>
</Application>
```

Exact menu pack URI:

`avares://Devolutions.AvaloniaTheme.Linux/Controls/MenuPack.styles.axaml`

Prerequisites and caveats:

- **`FluentTheme` must be loaded application-wide, in `Application.Styles` — not scoped to a subtree.** The
  Linux control themes build on Fluent's base templates, and `HorizontalMenuItem` derives from Fluent's
  `FluentTopLevelMenuItem`, which is resolved when the menu template is built.
- This pack includes styling for `ContextMenu`, `MenuFlyoutPresenter`, `Menu`, `MenuItem`, and the
  menu-specific helper styles (`Menu.styles.axaml`, `Separator.styles.axaml`).
- If you need full control coverage, use `<DevolutionsLinuxYaruTheme />` instead.

#### Resource contract

All menu tokens live in a single shared file, `Accents/MenuResources.axaml`, which is included by **both** the
full theme and the menu pack. There is one source of truth, so the pack can never drift from the full theme.

Two rules make the pack safe to layer over a host theme you do not control:

1. **Every key the pack owns is prefixed `LinuxMenu`** — it cannot collide with, or be silently overridden by,
   the host theme's keys.
2. **Values are pinned literals, never aliases.** Menu tokens do not resolve through generic theme-wide or
   Fluent-owned keys. A host theme that redefines e.g. `MenuFlyoutItemForeground` or `FlyoutThemeMaxWidth`
   cannot move menu visuals.

Rule 2 mattered more here than for the other packs. The Linux menu templates were derived from Fluent and read
Fluent's own generic keys directly (`MenuFlyoutItemBackground`, `MenuFlyoutPresenterBackground`,
`CheckMarkPathData`, ...). That is invisible under the full theme, but layered over another theme every one of
those keys resolved to the *host's* value, so the "Linux" menus rendered in the host's colours. Those templates
now read `LinuxMenu*` tokens only.

**Inherited from the host (by design):**

| Key | Why |
|-----|-----|
| `FluentTopLevelMenuItem` | Base for `HorizontalMenuItem` (the horizontal `TextBox` context flyout). Re-basing it would mean duplicating Fluent's top-level item template, which the pack deliberately avoids. |

**Typography is pinned, not inherited.** `LinuxMenuFontFamily` / `LinuxMenuFontSize` / `LinuxMenuFontWeight` are
owned by the pack. The point of the pack is that menus in a branded or opted-out section still match the
platform-themed menus elsewhere in the same app; inheriting the host's font defeats that, and pinning also keeps
menu geometry identical across hosts (font metrics change row heights).

**Row geometry is applied at `Style` level, not only in the `MenuItem` `ControlTheme`.** Host themes set menu
metrics with `Style`s of their own, and in Avalonia a `Style` setter outranks a `ControlTheme` setter — so a
`ControlTheme` alone would let the host resize menu rows.

**The pack styles menu content only.** It deliberately does not ship a `Separator` `ControlTheme`: that resource
is keyed `{x:Type Separator}`, so merging it would restyle *every* separator in your app rather than just menu
ones. Menu separators instead reuse the host's separator template — identical across Fluent, MacOS, Linux and
DevExpress — with every template-bound property pinned inside the menu-scoped selector.

**Overriding a token.** Because tokens are prefixed, you can retarget any single value without affecting the rest
of your app:

```xaml
<StyleInclude Source="avares://Devolutions.AvaloniaTheme.Linux/Controls/MenuPack.styles.axaml" />
<!-- ... later in your own resources ... -->
<SolidColorBrush x:Key="LinuxMenuSeparatorBrush" Color="#ff0000" />
```

These rules are enforced by `LinuxMenuPackContractTests` in the visual test project, which asserts that every
`LinuxMenu*` token resolves identically under the full theme and under "pack over a foreign host", that a hostile
host theme cannot leak into any of them, and that the menu templates never read an unowned resource key.

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
