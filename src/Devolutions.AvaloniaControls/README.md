[![image](https://github.com/user-attachments/assets/6a7bca22-bd0c-45cc-b847-8ea0b7776a6f)](https://devolutions.net/)

Custom Avalonia Controls developed by [Devolutions](https://devolutions.net/)

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Build Status](https://github.com/Devolutions/avalonia-extensions/actions/workflows/build-package.yml/badge.svg?branch=master)](https://github.com/Devolutions/avalonia-extensions/actions/workflows/build-package.yml)
[![NuGet Version](https://img.shields.io/nuget/vpre/Devolutions.AvaloniaControls)](https://www.nuget.org/packages/Devolutions.AvaloniaControls)
![NuGet Downloads](https://img.shields.io/nuget/dt/Devolutions.AvaloniaControls)

## Custom Controls [Work in Progress]

In this package we publish various custom controls as well as converters, markup extensions and other helper
utilities used in our [themes](https://github.com/Devolutions/avalonia-extensions) and  [Devolutions Remote
Desktop Manager](https://devolutions.net/remote-desktop-manager/). The more generically useful ones are listed here
(full documentation tba ...).

- [Installation](#installation)
- [Controls](#controls)
- [Converters](#converters)
- [MarkupExtensions](#markupextensions)

## Installation

Install the Devolutions.AvaloniaControls package
via [NuGet](https://www.nuget.org/packages/Devolutions.AvaloniaControls):

``` bash
Install-Package Devolutions.AvaloniaControls
```

or .NET

```bash
dotnet add package Devolutions.AvaloniaControls
```

### Controls

- `EditableComboBox`
- `SearchHighlightTextBlock`
- `TabPane`
  (Extends `TabControl` for different styling only)

### Converters
Usage: `Converter={x:Static DevoConverters.<ConverterName>}`

- `ColorToCssFillConverter` <br />ConverterParameter: class(es) of the path(s) to apply colour to <br />
  Converts a Brush and CSS class(es) into a CSS string for SVG path styling.
- `CornerRadiusExtractor` <br />ConverterParameter: predefined `CornerRadiusSubset` (enum) <br />
  Returns a new CornerRadius, to selectively apply a given CornerRadius to a subset of the four corners (0 to the others)
- `HasClass` / `HasNotClass` <br />ConverterParameter: class name to check for <br />
  Returns a boolean depending on whether the given element has the given class
- `ThicknessExtractor` <br />ConverterParameter: predefined `ThicknessSubset` (enum) <br />
  Returns a new Thickness, to selectively apply a given Thickness to a subset of the four sides (0 to the others)

### MultiConverters
Usage: `<MultiBinding Converter="{x:Static DevoMultiConverters.<ConverterName>}">`

- `BooleanToChoiceConverter`
- `ClassToChoiceConverter`
- `FirstNonNullValueMultiConverter`
- `IsExplicitlyTrueConverter`
- `IsUnsetConverter`

### MarkupExtensions

- `AddBinding`
- `MultiplyBinding`
- `AndBinding`
- `OrBinding`
- `BindingToggler`
- `DynamicResourceToggler`
- `WindowActiveBindingToggler`
- `WindowActiveResourceToggler`
- `WindowIsActiveBinding`
- `ChangeColorOpacity`
