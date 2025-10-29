---
description: "/workon [theme] [tabTitle?] [projectPlan?] - Switch theme, navigate to tab, and optionally load project plan"
---

You are being asked to switch the active theme and navigate to a specific tab in the SampleApp.

The user will provide one to three arguments:
1. **theme**: One of 'MacOS', 'DevExpress', or 'Linux'
2. **tabTitle**: (optional) The title of a TabItem (e.g., 'Button', 'TextBox', 'DataGrid', 'Control Alignment'),
   Default: 'Overview'
3. **projectPlan**: (optional) Name or partial name of a document in `.claude/docs/planning` to load and work on

## Execution Instructions:

### Step 1: Read and parse arguments
Extract the theme, tabTitle, and projectPlan from the user's command. The format is: `/workon [theme] [tabTitle] [projectPlan]`

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

### Step 8: Process project plan (if provided or detected)

#### 8.1: Find planning document
If a `projectPlan` argument was provided:
1. Use Glob to search for matching files in `.claude/docs/planning/**/*.md`
2. Match case-insensitively using partial matching (e.g., "TagInput" matches "2025_10_21__Ursa_TagInput_theming.md")
3. If multiple matches found, prefer exact match first, then shortest match

If NO `projectPlan` argument was provided:
1. Use `mcp__ide__getDiagnostics` tool (without uri parameter) to check for open files
2. Look for any open files with paths matching `.claude/docs/planning/**/*.md`
3. If found, use that as the planning document

#### 8.2: Handle planning document discovery
- If no planning doc found and none was requested: Skip to completion (no error)
- If projectPlan argument provided but no match found: List available planning docs and report error
- If multiple ambiguous matches: Ask user to be more specific

#### 8.3: Read and process planning document
If a planning document was found:
1. Read the entire document using the Read tool
2. Parse and understand:
   - Current phase/status
   - Next steps or tasks to complete
   - Any blockers or notes
3. Inform the user which planning doc was loaded
4. Begin working on the next steps in the plan

#### 8.4: Project plan completion report
After loading the planning doc, inform the user:
- Which planning document was loaded
- What phase/section is currently active
- Brief summary of next steps you'll be working on
- Ask if they want you to proceed or if they want to modify the plan first
