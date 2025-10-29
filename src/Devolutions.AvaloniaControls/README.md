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
- `TagInput`
  (Wrapper for [Ursa](https://github.com/irihitech/Ursa.Avalonia)'s TagInput control - manages tags/keywords/labels with add/remove functionality)

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

- `BooleanToChoiceConverter` <br />
  Takes a boolean and two value choices. I checks for the presence of the class given as `ConverterParameter` and returns the first choice value if found, or the second one otherwise. <br />
  ➡️ Use the `BindingToggler` MarkupExtension instead for a more streamlined syntax.
- `ClassToChoiceConverter` <br />
  Takes a control's classes and two value choices. I checks for the presence of the class given as `ConverterParameter` and returns the first choice value if found, or the second one otherwise. 
- `FirstNonEmptyStringMultiConverter` <br />
  Returns the first non-empty string in the multi-value binding, ignores the rest
- `FirstNonNullValueMultiConverter` <br />
  Returns the first non-null value in the multi-value binding, ignores the rest
- `IsExplicitlyTrueConverter` <br />
  Takes a _single_ input and returns a boolean based on whether the input is a boolean and true (useful to convert a possible `AvaloniaProperty.UnsetValue` to 'False' for use in other boolean operations)
- `IsUnsetConverter` <br />
  Takes a _single_ input and returns `True` if it is `AvaloniaProperty.UnsetValue`.

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
