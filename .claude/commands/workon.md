---
description: Switch theme and navigate to a specific tab in the SampleApp
---

You are being asked to switch the active theme and navigate to a specific tab in the SampleApp.

The user will provide two arguments:
1. **theme**: One of 'MacOS', 'DevExpress', or 'Linux'
2. **tabTitle**: The title of a TabItem (e.g., 'Button', 'TextBox', 'DataGrid', 'Control Alignment')

## Execution Instructions:

### Step 1: Read and parse arguments
Extract the theme and tabTitle from the user's command. The format is: `/workon [theme] [tabTitle]`

### Step 2: Validate theme argument
Theme must be one of:
- 'MacOS' → Maps to `<DevolutionsMacOsTheme />`
- 'DevExpress' → Maps to `<DevolutionsDevExpressTheme />`
- 'Linux' → Maps to `<DevolutionsLinuxYaruTheme />`

Case-insensitive matching is acceptable.

### Step 3: Update App.axaml theme
File: `samples/SampleApp/App.axaml`

In the `<Application.Styles>` block (around lines 31-35):
- Comment out ALL three theme lines with `<!-- ... -->`
- Uncomment ONLY the selected theme

Current state example:
```xml
  <Application.Styles>
    <!-- <DevolutionsMacOsTheme /> -->
    <!-- <DevolutionsDevExpressTheme /> -->
    <DevolutionsLinuxYaruTheme />
  </Application.Styles>
```

Target state for MacOS:
```xml
  <Application.Styles>
    <DevolutionsMacOsTheme />
    <!-- <DevolutionsDevExpressTheme /> -->
    <!-- <DevolutionsLinuxYaruTheme /> -->
  </Application.Styles>
```

Use the Edit tool to make these changes. Be careful to preserve exact formatting and indentation.

### Step 4: Find matching TabItem in MainWindow.axaml
File: `samples/SampleApp/MainWindow.axaml`

Search for TabItems with matching titles:
- Look for `<controls:SampleItemHeader Title="..." />` elements
- Also check simple `Header="..."` attributes
- Match case-insensitively
- Support partial matching (e.g., "combo" matches "ComboBox", "Editable" matches "EditableComboBox")

If multiple matches found, prefer exact match first, then shortest match.

### Step 5: Update TabItem IsSelected attributes
In MainWindow.axaml:
- Remove ALL existing `IsSelected="True"` attributes from all TabItems
- Add `IsSelected="True"` ONLY to the matched TabItem

IMPORTANT: There may be multiple TabItems with `IsSelected="True"` initially - you must remove ALL of them.

Example change for Button tab:
```xml
<TabItem IsSelected="True">
  <TabItem.Header>
    <controls:SampleItemHeader Title="Button" ApplicableTo="MacOS, Windows - DevExpress, Linux - Yaru" />
  </TabItem.Header>
  <demoPages:ButtonDemo />
</TabItem>
```

### Step 6: Error handling
- If theme invalid: Report valid options
- If no matching tab: List all available tab titles from MainWindow.axaml
- If multiple ambiguous matches: Ask user to be more specific

### Step 7: Report completion
After successful changes, inform the user:
- Which theme was activated
- Which tab was selected
- Remind them they can run: `dotnet run --project samples/SampleApp/SampleApp.csproj`

DO NOT automatically run the app unless explicitly asked.
