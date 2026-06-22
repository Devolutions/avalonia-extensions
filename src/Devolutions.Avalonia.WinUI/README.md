[![image](https://github.com/user-attachments/assets/6a7bca22-bd0c-45cc-b847-8ea0b7776a6f)](https://devolutions.net/)

Custom Avalonia Themes developed by [Devolutions](https://devolutions.net/)

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Build Status](https://github.com/Devolutions/avalonia-extensions/actions/workflows/build-package.yml/badge.svg?branch=master)](https://github.com/Devolutions/avalonia-extensions/actions/workflows/build-package.yml)
[![NuGet Version](https://img.shields.io/nuget/vpre/Devolutions.Avalonia.WinUI)](https://www.nuget.org/packages/Devolutions.Avalonia.WinUI)
![NuGet Downloads](https://img.shields.io/nuget/dt/Devolutions.Avalonia.WinUI)

## WinUI Theme [Work in Progress]

This theme is based on [Avalonia.Themes.Fluent](https://github.com/AvaloniaUI/Avalonia/tree/master/src/Avalonia.Themes.Fluent)
as fallback for controls not explicitly overridden yet.

## Installation

Install the Devolutions.Avalonia.WinUI package via [NuGet](https://www.nuget.org/packages/Devolutions.Avalonia.WinUI):

```bash
Install-Package Devolutions.Avalonia.WinUI
```

or .NET

```bash
dotnet add package Devolutions.Avalonia.WinUI
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
