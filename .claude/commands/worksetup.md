---
description: "/worksetup [theme] [tabTitle?] [scale?] [projectPlan?] - Switch theme, navigate to tab, set scale, and optionally load project plan"
---

You are being asked to switch the active theme, navigate to a specific tab in the SampleApp, and optionally set the UI scale.

The user will provide one to four arguments:
1. **theme**: One of 'MacOS', 'MacOSClassic' (or 'Classic'), 'MacOSLiquidGlass' (or 'LiquidGlass'), 'DevExpress', 'Linux', or 'Default'
2. **tabTitle**: (optional) The title of a TabItem (e.g., 'Button', 'TextBox', 'DataGrid', 'Control Alignment'),
   Default: 'Overview'
3. **scale**: (optional) UI scale percentage (e.g., '100%', '150%', '200%', '300%', '400%')
   Default: 'Default' (system default)
4. **projectPlan**: (optional) Name or partial name of a document in `.claude/docs/planning` to load and work on

## Execution Instructions:

### Step 1: Read and parse arguments
Extract the theme, tabTitle, scale, and projectPlan from the user's command. The format is: `/worksetup [theme] [tabTitle] [scale] [projectPlan]`

### Step 2: Validate theme argument
Theme must be one of:
- 'MacOS' → Maps to `<DevolutionsMacOsTheme />` (automatic sub-theme selection based on OS version)
- 'MacOSClassic' (or 'Classic') → Maps to `<local:MacOsClassicThemeStyle />` (force Classic theme)
- 'MacOSLiquidGlass' (or 'LiquidGlass' or 'Liquid') → Maps to `<local:MacOsLiquidGlassThemeStyle />` (force LiquidGlass theme)
- 'DevExpress' → Maps to `<DevolutionsDevExpressTheme />`
- 'Linux' → Maps to `<DevolutionsLinuxYaruTheme />`
- 'Default' → All themes commented out (platform-appropriate theme auto-selected)

Case-insensitive matching is acceptable. Accept variations:
- 'Classic' or 'MacOSClassic' or 'MacClassic' → MacOSClassic
- 'LiquidGlass' or 'Liquid' or 'MacOSLiquidGlass' or 'Glass' → MacOSLiquidGlass

### Step 3: Update App.axaml theme
File: `samples/SampleApp/App.axaml`

In the `<Application.Styles>` block (around lines 41-49):
- Comment out ALL theme lines with `<!-- ... -->`
- If theme is 'Default', leave all commented
- Otherwise, uncomment ONLY the selected theme

**Theme line locations:**
- Line ~44: `<DevolutionsMacOsTheme />` (MacOS automatic)
- Line ~45: `<local:MacOsClassicThemeStyle />` (MacOS Classic forced)
- Line ~46: `<local:MacOsLiquidGlassThemeStyle />` (MacOS LiquidGlass forced)
- Line ~47: `<DevolutionsDevExpressTheme />` (DevExpress)
- Line ~48: `<DevolutionsLinuxYaruTheme />` (Linux)

Default state (master branch should always have this):
```xml
  <Application.Styles>
    <!-- <DevolutionsMacOsTheme /> -->
    <!-- <local:MacOsClassicThemeStyle /> -->
    <!-- <local:MacOsLiquidGlassThemeStyle /> -->
    <!-- <DevolutionsDevExpressTheme /> -->
    <!-- <DevolutionsLinuxYaruTheme /> -->

    <!-- Dummie style to prevent Rider from deleting the Application.Styles block -->
    <Style />
  </Application.Styles>
```

Target state for MacOS (automatic):
```xml
  <Application.Styles>
    <DevolutionsMacOsTheme />
    <!-- <local:MacOsClassicThemeStyle /> -->
    <!-- <local:MacOsLiquidGlassThemeStyle /> -->
    <!-- <DevolutionsDevExpressTheme /> -->
    <!-- <DevolutionsLinuxYaruTheme /> -->

    <!-- Dummie style to prevent Rider from deleting the Application.Styles block -->
    <Style />
  </Application.Styles>
```

Target state for MacOS Classic (forced):
```xml
  <Application.Styles>
    <!-- <DevolutionsMacOsTheme /> -->
    <local:MacOsClassicThemeStyle />
    <!-- <local:MacOsLiquidGlassThemeStyle /> -->
    <!-- <DevolutionsDevExpressTheme /> -->
    <!-- <DevolutionsLinuxYaruTheme /> -->

    <!-- Dummie style to prevent Rider from deleting the Application.Styles block -->
    <Style />
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

### Step 6: Update UI Scale (if provided)
File: `samples/SampleApp/ViewModels/MainWindowViewModel.cs`

If a scale argument was provided:
1. Parse the scale value (e.g., "100%", "150%", "200%", "300%", "400%")
2. Strip the '%' character and convert to a decimal (e.g., "150%" → 1.5)
3. Find the matching entry in AvailableScales array based on the Scale property
4. Update line 42: Change ONLY the array index in `this.SelectedScale = this.AvailableScales[X];` where X is the matching index

**IMPORTANT**: Preserve the existing comment on that line exactly as-is. Do NOT modify the comment - it's a human reference for the array indices.

**Available scale mappings:**
- "Default" or "0%" → index 0 → `new("Default", 0)`
- "100%" → index 1 → `new("100%", 1.0)`
- "125%" → index 2 → `new("125%", 1.25)`
- "150%" → index 3 → `new("150%", 1.5)`
- "175%" → index 4 → `new("175%", 1.75)`
- "200%" → index 5 → `new("200%", 2.0)`
- "225%" → index 6 → `new("225%", 2.25)`
- "250%" → index 7 → `new("250%", 2.5)`
- "275%" → index 8 → `new("275%", 2.75)`
- "300%" → index 9 → `new("300%", 3.0)`
- "400%" → index 10 → `new("400%", 4.0)`

**Default state** (master branch should always have this):
```csharp
this.SelectedScale = this.AvailableScales[0]; // 0 = System Default, 10 = 400%
```

**Example change for 400%:**
```csharp
this.SelectedScale = this.AvailableScales[10]; // 0 = System Default, 10 = 400%
```

**Example change for 150%:**
```csharp
this.SelectedScale = this.AvailableScales[3]; // 0 = System Default, 10 = 400%
```

Use the Edit tool to make this change. Be careful to preserve exact formatting and the comment.

### Step 7: Error handling
- If theme invalid: Report valid options
- If no matching tab: List all available tab titles from MainWindow.axaml
- If multiple ambiguous matches: Ask user to be more specific
- If scale invalid: Report valid options (100%, 125%, 150%, 175%, 200%, 225%, 250%, 275%, 300%, 400%, or Default)

### Step 8: Report completion
After successful changes, inform the user:
- Which theme was activated
- Which tab was selected
- Which scale was set (if provided)
- Remind them they can run: `dotnet run --project samples/SampleApp/SampleApp.csproj`

DO NOT automatically run the app unless explicitly asked.

### Step 9: Process project plan (if provided or detected)

#### 9.1: Find planning document
If a `projectPlan` argument was provided:
1. Use Glob to search for matching files in `.claude/docs/planning/**/*.md`
2. Match case-insensitively using partial matching (e.g., "TagInput" matches "2025_10_21__Ursa_TagInput_theming.md")
3. If multiple matches found, prefer exact match first, then shortest match

If NO `projectPlan` argument was provided:
1. Use `mcp__ide__getDiagnostics` tool (without uri parameter) to check for open files
2. Look for any open files with paths matching `.claude/docs/planning/**/*.md`
3. If found, use that as the planning document

#### 9.2: Handle planning document discovery
- If no planning doc found and none was requested: Skip to completion (no error)
- If projectPlan argument provided but no match found: List available planning docs and report error
- If multiple ambiguous matches: Ask user to be more specific

#### 9.3: Read and process planning document
If a planning document was found:
1. Read the entire document using the Read tool
2. Parse and understand:
   - Current phase/status
   - Next steps or tasks to complete
   - Any blockers or notes
3. Inform the user which planning doc was loaded
4. Begin working on the next steps in the plan

#### 9.4: Project plan completion report
After loading the planning doc, inform the user:
- Which planning document was loaded
- What phase/section is currently active
- Brief summary of next steps you'll be working on
- Ask if they want you to proceed or if they want to modify the plan first
