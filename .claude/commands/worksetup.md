---
description: "/worksetup [theme] [tabTitle?] [scale?] [projectPlan?] - Switch theme, set startup tab, set scale, optionally load a planning doc"
---

Configure SampleApp startup defaults for development.

Arguments:
1. **theme** (required): `MacOS`, `MacOSClassic`/`Classic`/`MacClassic`, `MacOSLiquidGlass`/`LiquidGlass`/`Liquid`/`Glass`, `DevExpress`, `Linux`, or `Default`
2. **tabTitle** (optional, default `Overview`): tab to select at startup (control tab or explicit top-level tab)
3. **scale** (optional, default `Default`): `Default`, `100%`, `125%`, `150%`, `175%`, `200%`, `225%`, `250%`, `275%`, `300%`, `400%`
4. **projectPlan** (optional): planning doc name/partial name under `.claude/docs/planning`

## 1) Parse and validate inputs
- Parse `/worksetup [theme] [tabTitle] [scale] [projectPlan]`
- Validate theme and scale against allowed values
- Theme matching is case-insensitive and accepts aliases listed above

## 2) Set startup theme in `samples/SampleApp/App.axaml`
- In `<Application.Styles>`, keep exactly one theme uncommented (or all commented for `Default`)
- Theme mapping:
  - `MacOS` -> `<DevolutionsMacOsTheme />`
  - `MacOSClassic` -> `<local:MacOsClassicThemeStyle />`
  - `MacOSLiquidGlass` -> `<local:MacOsLiquidGlassThemeStyle />`
  - `DevExpress` -> `<DevolutionsDevExpressTheme />`
  - `Linux` -> `<DevolutionsLinuxYaruTheme />`
  - `Default` -> all theme lines commented

## 3) Resolve tab title
Collect candidate tab titles from:
- `samples/SampleApp/ControlCatalog/control-catalog.jsonc` -> `controls[].name`
- explicit non-control tabs in `samples/SampleApp/MainWindow.axaml`:
  - `Overview`
  - `Control Alignment`
  - `Experiments`

Match rules:
- case-insensitive
- prefer exact match
- then partial match
- if ambiguous, ask user to clarify

## 4) Set startup tab in `samples/SampleApp/ViewModels/MainWindowViewModel.cs`
- Update:
  - `private string startupTabTitle = "...";`
- Do **not** set `IsSelected` flags in `MainWindow.axaml` for control tab selection

## 5) Set startup scale in `samples/SampleApp/ViewModels/MainWindowViewModel.cs` (if provided)
- Update only the index in:
  - `this.SelectedScale = this.AvailableScales[X]; // 0 = System Default, 10 = 400%`
- Preserve the existing comment exactly
- Scale index mapping:
  - `Default` -> 0
  - `100%` -> 1
  - `125%` -> 2
  - `150%` -> 3
  - `175%` -> 4
  - `200%` -> 5
  - `225%` -> 6
  - `250%` -> 7
  - `275%` -> 8
  - `300%` -> 9
  - `400%` -> 10

## 6) Error handling
- Invalid theme: report valid theme options
- Invalid scale: report valid scale options
- No tab match: list available tab titles from catalog + explicit non-control tabs
- Ambiguous tab match: ask user to be more specific

## 7) Completion message
Report:
- selected theme
- selected startup tab
- selected startup scale (if changed)
- files edited

Do not run the app unless explicitly asked.

## 8) Optional planning doc handling
If `projectPlan` is provided (or an open planning doc is detected):
- locate matching file in `.claude/docs/planning/**/*.md`
- load and summarize current phase + next steps
- ask whether to proceed with the plan work
