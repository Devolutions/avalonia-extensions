# IsOneOfConverter for Multi-Theme Visibility

**Status:** Planned
**Priority:** Medium
**Created:** 2025-10-24

## Overview

Create a new `IsOneOfConverter` value converter that accepts a comma-separated list of theme names and returns `true` if the current theme matches any of them. This will simplify theme-specific visibility logic in demo pages and throughout the application.

## Problem Statement

Currently, when we need to show content for multiple (but not all) themes, we have limited options:

1. **Use MultiBinding with Or** - Very verbose and repetitive
2. **Use NotEqualTo with single exclusion** - Implicit logic that breaks when adding new themes
3. **Multiple separate conditional TextBlocks** - Duplicates content

Example of the current verbose approach:
```xml
<TextBlock.IsVisible>
  <MultiBinding Converter="{x:Static BoolConverters.Or}">
    <Binding Source={x:Static sampleApp:App.CurrentTheme} Path=Name
             Converter={StaticResource EqualToComparisonConverter} ConverterParameter=MacOS />
    <Binding Source={x:Static sampleApp:App.CurrentTheme} Path=Name
             Converter={StaticResource EqualToComparisonConverter} ConverterParameter=Linux />
  </MultiBinding>
</TextBlock.IsVisible>
```

## Proposed Solution

Create a new `IsOneOfConverter` that accepts comma-separated values:

```xml
<TextBlock IsVisible="{Binding Source={x:Static sampleApp:App.CurrentTheme},
                       Path=Name,
                       Converter={StaticResource IsOneOfConverter},
                       ConverterParameter='MacOS,Linux'}">
  (results in minor positioning bug)
</TextBlock>
```

### Converter Implementation

**Location:** `samples/SampleApp/Converters/IsOneOfConverter.cs` (new file)

```csharp
using System;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace SampleApp.Converters;

/// <summary>
/// Converter that returns true if the value matches any of the comma-separated options in the parameter.
/// </summary>
public class IsOneOfConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter is not string paramString)
            return false;

        var valueString = value.ToString();
        if (string.IsNullOrEmpty(valueString))
            return false;

        var options = paramString
            .Split(',')
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s));

        return options.Contains(valueString, StringComparer.OrdinalIgnoreCase);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("IsOneOfConverter does not support two-way binding.");
    }
}
```

### Key Features

1. **Case-insensitive matching** - `"MacOS"` matches `"macos"`, `"MACOS"`, etc.
2. **Whitespace tolerant** - `"MacOS, Linux"` and `"MacOS,Linux"` both work
3. **Empty string handling** - Gracefully handles null/empty values and parameters
4. **Clear error handling** - Throws NotSupportedException for ConvertBack (one-way only)

## Implementation Steps

### Step 1: Create Converter Class
- Create `samples/SampleApp/Converters/IsOneOfConverter.cs`
- Implement IValueConverter with the logic above
- Add XML documentation comments

### Step 2: Register Converter as Resource
**File:** `samples/SampleApp/App.axaml`

Add to `Application.Resources`:
```xml
<converters:IsOneOfConverter x:Key="IsOneOfConverter" />
```

Make sure namespace is declared:
```xml
xmlns:converters="clr-namespace:SampleApp.Converters"
```

### Step 3: Update ComboBoxDemo.axaml
Replace the current conditional visibility:

**Before:**
```xml
<TextBlock IsVisible="{Binding Source={x:Static sampleApp:App.CurrentTheme},
                       Path=Name,
                       Converter={StaticResource EqualToComparisonConverter},
                       ConverterParameter=MacOS}">
  (results in minor positioning bug)
</TextBlock>
```

**After:**
```xml
<TextBlock IsVisible="{Binding Source={x:Static sampleApp:App.CurrentTheme},
                       Path=Name,
                       Converter={StaticResource IsOneOfConverter},
                       ConverterParameter='MacOS,Linux'}">
  (results in minor positioning bug)
</TextBlock>
```

### Step 4: Test Thoroughly
- Test with MacOS theme - text should be visible
- Test with Linux theme - text should be visible
- Test with DevExpress theme - text should be hidden
- Test edge cases:
  - Empty parameter
  - Single value in parameter
  - Extra whitespace in parameter
  - Mixed case theme names

### Step 5: Document Usage
Add a comment in `ComboBoxDemo.axaml` explaining the pattern for future reference:

```xml
<!-- Use IsOneOfConverter to show content for multiple themes -->
<!-- ConverterParameter accepts comma-separated theme names: 'MacOS,Linux' -->
```

## Alternative Approaches Considered

### Alternative 1: Extend EqualToComparisonConverter
**Why rejected:** Modifying existing converters is risky and could break existing usage. String parsing in an existing converter feels like overloading its purpose.

### Alternative 2: Use NotEqualTo with DevExpress
**Why rejected:** Implicit logic that becomes fragile when adding new themes. Not self-documenting.

### Alternative 3: MultiBinding with Or
**Why rejected:** Too verbose for common use cases. Harder to maintain.

## Future Enhancements

1. **Negative matching:** Consider adding an `IsNotOneOfConverter` for exclusion lists
2. **Regex support:** Could support regex patterns if needed (e.g., `"Mac.*"`)
3. **Reusability:** This pattern could be useful in other Avalonia projects

## Files to Create/Modify

### New Files
- `samples/SampleApp/Converters/IsOneOfConverter.cs`

### Modified Files
- `samples/SampleApp/App.axaml` - Register converter resource
- `samples/SampleApp/DemoPages/ComboBoxDemo.axaml` - Use new converter

## Testing Checklist

- [ ] Converter returns true when value matches one option
- [ ] Converter returns true when value matches multiple options
- [ ] Converter returns false when value doesn't match any option
- [ ] Case-insensitive matching works
- [ ] Whitespace is handled correctly
- [ ] Null/empty values handled gracefully
- [ ] Works in ComboBoxDemo with all three themes
- [ ] ConvertBack throws NotSupportedException

## Commit Message Template

```
[SampleApp] Add IsOneOfConverter for multi-theme visibility

- Create IsOneOfConverter for checking if value matches any comma-separated option
- Add case-insensitive, whitespace-tolerant string matching
- Register converter in App.axaml resources
- Update ComboBoxDemo to use new converter for MacOS/Linux conditional text
- Add documentation comments and usage examples

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>
```

## Related Issues/PRs

- MacOS ComboBox IsEditable implementation (depends on this being applied to demo page)

## Notes

This is a general-purpose converter that could be used anywhere in the SampleApp (or moved to a shared library) for theme-specific or any multi-option conditional visibility logic.
