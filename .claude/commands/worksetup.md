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

  ## 2) Update startup settings in `samples/SampleApp/PageCatalog/page-catalog.jsonc`
  Edit only the `sampleAppStartUpSettings` block:

  ```jsonc
  "sampleAppStartUpSettings": {
    "theme": "DevExpress",
    "selectedPage": "ComboBox",
    "scale": "default"
  }
  ```

  - `theme`: set from the validated theme argument
  - `selectedPage`: set from resolved page title (see Step 3)
  - `scale`: set from validated scale argument (if provided), otherwise preserve existing value

  The app applies these settings at runtime; do not edit `App.axaml` or `MainWindowViewModel.cs` for normal `/worksetup` usage.

  ## 3) Resolve tab title
Collect candidate tab titles from:
- `samples/SampleApp/PageCatalog/page-catalog.jsonc` -> `pages.*[].uniqueTitle`
- explicit non-control tabs in `samples/SampleApp/MainWindow.axaml`:
  - `Overview`
  - `Control Alignment`
  - `Experiments`

Match rules:
- case-insensitive
- prefer exact match
- then partial match
- if ambiguous, ask user to clarify

  ## 4) Scale value format
  In `sampleAppStartUpSettings.scale`, write one of:
  - `default`
  - `100%`, `125%`, `150%`, `175%`, `200%`, `225%`, `250%`, `275%`, `300%`, `400%`

  ## 5) Error handling
  - Invalid theme: report valid theme options
  - Invalid scale: report valid scale options
  - No tab match: list available tab titles from catalog + explicit non-control tabs
  - Ambiguous tab match: ask user to be more specific

  ## 6) Completion message
  Report:
  - selected theme
  - selected startup tab
  - selected startup scale (if changed)
  - files edited

  Do not run the app unless explicitly asked.

  ## 7) Optional planning doc handling
If `projectPlan` is provided (or an open planning doc is detected):
- locate matching file in `.claude/docs/planning/**/*.md`
- load and summarize current phase + next steps
- ask whether to proceed with the plan work
