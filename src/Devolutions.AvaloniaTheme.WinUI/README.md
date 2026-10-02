[![image](https://github.com/user-attachments/assets/6a7bca22-bd0c-45cc-b847-8ea0b7776a6f)](https://devolutions.net/)

Custom Avalonia Themes developed by [Devolutions](https://devolutions.net/)

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Build Status](https://github.com/Devolutions/avalonia-extensions/actions/workflows/build-package.yml/badge.svg?branch=master)](https://github.com/Devolutions/avalonia-extensions/actions/workflows/build-package.yml)
[![NuGet Version](https://img.shields.io/nuget/vpre/Devolutions.AvaloniaTheme.WinUI)](https://www.nuget.org/packages/Devolutions.AvaloniaTheme.WinUI)
![NuGet Downloads](https://img.shields.io/nuget/dt/Devolutions.AvaloniaTheme.WinUI)

## WinUI Theme [Work in Progress 🚧]

This theme is based on [Avalonia.Themes.Fluent](https://github.com/AvaloniaUI/Avalonia/tree/master/src/Avalonia.Themes.Fluent)
as fallback for controls not explicitly overridden yet.

## Installation

Install the Devolutions.AvaloniaTheme.WinUI package via [NuGet](https://www.nuget.org/packages/Devolutions.AvaloniaTheme.WinUI):

```bash
Install-Package Devolutions.AvaloniaTheme.WinUI
```

or .NET

```bash
dotnet add package Devolutions.AvaloniaTheme.WinUI
```

In your App.axaml, replace the existing theme (e.g. `<FluentTheme />`) with:

```xaml
<Application ...>
  <Application.Styles>
     <DevolutionsWinUiTheme />
  </Application.Styles>
</Application>
```

To opt out of global styles:

```xaml
<DevolutionsWinUiTheme GlobalStyles="False" />
```

## Expander

Expanders stretch horizontally by default to align stacked headers, matching the
other Devolutions themes. Set `HorizontalAlignment` explicitly to opt out.
Content slides into a clipped region when expanding down or up; left and right
expansion retain Fluent's behavior. The `borderless` class removes the card fill
and retains a content edge in the expansion direction.

## Classic and Mica validation

The page catalog tracks WinUI classic (solid backdrop) and WinUI Mica separately.
When a Mica control is ready, review the classic variant against WinUI with a solid
backdrop (for example, with Windows transparency effects disabled). Classic `⚠️`
may indicate that this validation is still pending, not necessarily a known defect.
Keep separate Light and Dark classic visual baselines when it differs from Mica;
use `↔️` only after confirming that both variants should render identically.
Once a basic set of controls is available, review the remaining classic `⚠️`
pages together and promote each to `✅` or `↔️` as appropriate.
