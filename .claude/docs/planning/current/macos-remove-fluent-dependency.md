# macOS: Remove Fluent Theme Dependency

## Goal

Make `Devolutions.AvaloniaTheme.MacOS` self-contained. Remove runtime and package dependency on `Avalonia.Themes.Fluent`; preserve classic macOS and conditional Liquid Glass visuals, menu aliases, wallpaper tinting, and live system accent behavior.

For every Fluent control currently loaded upstream GitHub `main`:

- No MacOS `ControlTheme`: import upstream as-is.
- Existing MacOS `ControlTheme`: retain MacOS implementation and append only absent upstream semantic items in a marked final section.
- Vendor only upstream resources missing from MacOS resource scopes.
- Port Fluent system accent detection into MacOS-owned source. No Fluent classes or resource URIs remain.

## Upstream Source

Inspect sources directly from <https://github.com/AvaloniaUI/Avalonia/tree/main/src/Avalonia.Themes.Fluent/Controls>. Resolve `main` to one commit SHA immediately before each implementation batch. Fetch source directly from GitHub at that SHA; do not inspect local NuGet assets or use a cloned upstream checkout as source authority.

- Controls: <https://github.com/AvaloniaUI/Avalonia/tree/main/src/Avalonia.Themes.Fluent/Controls>
- Control manifest: <https://github.com/AvaloniaUI/Avalonia/blob/main/src/Avalonia.Themes.Fluent/Controls/FluentControls.xaml>
- Root order: <https://github.com/AvaloniaUI/Avalonia/blob/main/src/Avalonia.Themes.Fluent/FluentTheme.xaml>
- Base palette: <https://github.com/AvaloniaUI/Avalonia/blob/main/src/Avalonia.Themes.Fluent/Accents/BaseColorsPalette.xaml>
- Base resources: <https://github.com/AvaloniaUI/Avalonia/blob/main/src/Avalonia.Themes.Fluent/Accents/BaseResources.xaml>
- Control resources: <https://github.com/AvaloniaUI/Avalonia/blob/main/src/Avalonia.Themes.Fluent/Accents/FluentControlResources.xaml>
- Invariant strings: <https://github.com/AvaloniaUI/Avalonia/blob/main/src/Avalonia.Themes.Fluent/Strings/InvariantResources.xaml>
- Accent provider: <https://github.com/AvaloniaUI/Avalonia/blob/main/src/Avalonia.Themes.Fluent/Accents/SystemAccentColors.cs>

Each provenance comment must use permanent `blob/<resolved-sha>/...` URL. Never use `master`, `main`, a floating tag, raw URL, or a SHA different from that batch's resolved revision. Current package support remains Avalonia `12.x`; do not create a version gate or change package bounds as part of this removal.

## Mandatory Control Headers

Every `Controls/*.axaml` file mapped to an Avalonia Fluent control, including existing MacOS themes and newly imported fallback controls, must begin with exactly one header comment in this form:

```xml
<!-- Based off Avalonia Fluent 12.1.2:
https://github.com/AvaloniaUI/Avalonia/blob/d3c867a9e2de379249b03dbeb3495bd7f076a81a/src/Avalonia.Themes.Fluent/Controls/<Control>.xaml -->
```

Rules:

- Header is first file content, before root element and other comments.
- Use matching Fluent filename. `FluentFallbackControls.axaml` points to `FluentControls.xaml`.
- Replace any prior top-level `Fluent source`, `Based on`, `Retrieved from`, `Properties below imported`, raw GitHub, `main`, or `master` provenance comment. Do not leave duplicate source headers.
- Preserve interior provenance comments describing a specific appended section; they are not file headers.
- Do not add this header to MacOS-only/custom control files, `*.styles.axaml`, or `_index.axaml`; false Fluent attribution is prohibited.
- Header uses the fixed Avalonia `12.1.2` source SHA above even if implementation analysis consults GitHub `main` for current control inventory.

## MacOS Invariants

- `ThemeRoot.axaml` currently hosts direct `<FluentTheme />`, then local controls. Remove it only after all local source/resource providers work.
- `MacOsTheme.axaml.cs` and `MacOsThemeWithGlobalStyles.axaml.cs` dynamically append base `ThemeResources.axaml`, create menu alias dictionary, conditionally append `ThemeResources_LiquidGlass.axaml`, run wallpaper tint hook, then rebuild menu aliases. Preserve this exact lifecycle and precedence.
- `ThemeRoot.axaml` intentionally uses `ResourceInclude` for classic `MenuResources.axaml`; changing to `MergeResourceInclude` breaks classic/Liquid Glass menu alias precedence. Keep this distinction.
- `MenuResourceAliasBuilder` converts active variant legacy theme keys to `MacOsMenu*` keys. Do not change mappings or rebuild timing while importing Fluent resources.
- `ThemeResources.axaml` has per-variant dynamic accent resources. `ThemeResources_LiquidGlass.axaml` builds accent-derived visuals with converters. Both must observe owned `SystemAccentColor*` keys.
- Keep custom controls, menu packs, DataGrid, TreeDataGrid, ColorPicker, Liquid Glass detection, wallpaper tinting, and optional global styles out of upstream control classification.

## Provenance And Comparison

For each changed MacOS counterpart, append inside affected theme after all existing content:

```xml
<!-- Properties below imported from Avalonia Fluent main at <resolved-commit-sha>:
     https://github.com/AvaloniaUI/Avalonia/blob/<resolved-commit-sha>/src/Avalonia.Themes.Fluent/Controls/<Control>.xaml -->
<!-- Only source items absent from matching MacOS scope. -->
```

Semantic comparison includes all themes, keys/targets, setters, templates, nested styles/selectors, pseudo-classes, transitions, animations, resources, `BasedOn`, template parts, source URIs, converters, and types. Existing MacOS choice wins even when Fluent differs. No duplicate default-key theme or Fluent `BasedOn` shortcut. File-level provenance always follows mandatory header form above; appended-section provenance uses its own fixed-SHA comment at insertion point.

Replace stale `master` or old Fluent source comments in touched files with permanent-SHA form.

## Phase 1: Authoritative Manifest

Coordinator writes `MacOsFluentImportManifest.md` beside this plan before modifications. For all entries in upstream `FluentControls.xaml`:

1. Record upstream target/key themes, local resources, namespaces, source includes, and all direct resource references.
2. Map source to current MacOS control by actual `ControlTheme` target/key, not file name.
3. Classify import, append, resource-only, or excluded-not-loaded.
4. List exact missing semantic units and insertion scope for every append assignment, marking whether each changes existing classic or Liquid Glass visuals, layout, focus order, or interaction state.
5. Map resource provider and variant/scope for every source reference.
6. Preserve dependency order, especially date/time shared resources and supporting control themes.
7. Flag every resource potentially consumed by `MenuResourceAliasBuilder` or changed by Liquid Glass overlay. These require coordinator review before addition.

## Phase 2: Controls

### Existing MacOS Candidate Counterparts

`AutoCompleteBox`, `ButtonSpinner`, `Button`, `CalendarButton`, `CalendarDatePicker`, `CalendarDayButton`, `CalendarItem`, `Calendar`, `CheckBox`, `ComboBoxItem`, `ComboBox`, `ContextMenu`, `DataValidationErrors`, `DropDownButton`, `EmbeddableControlRoot`, `Expander`, `FlyoutPresenter`, `GridSplitter`, `HyperlinkButton`, `ListBoxItem`, `ListBox`, `MenuFlyoutPresenter`, `MenuItem`, `Menu`, `NumericUpDown`, `RadioButton`, `ScrollBar`, `ScrollViewer`, `Separator`, `SplitButton`, `TabControl`, `TabItem`, `TextBox`, `ToggleSwitch`, `ToolTip`, `TreeViewItem`, `TreeView`, and `Window`.

Manifest checks actual themes in each file plus supporting named themes such as `FluentCalendarButton`, `FluentTextBoxButton`, scroll bar components, `PopUpToggleButton`, and MacOS-specific menu/split-button themes.

### Preliminary Import-As-Is Files

No matching MacOS filename currently exists for:

`AdornerLayer`, `CarouselPage`, `Carousel`, `CommandBar`, `ContentPage`, `DatePicker`, `DateTimePickerShared`, `DrawerPage`, `GroupBox`, `HeaderedContentControl`, `ItemsControl`, `Label`, `ManagedFileChooser`, `MenuScrollViewer`, `NavigationPage`, `NotificationCard`, `OverlayPopupHost`, `PathIcon`, `PipsPager`, `PopupRoot`, `ProgressBar`, `RefreshContainer`, `RefreshVisualizer`, `RepeatButton`, `SelectableTextBlock`, `Slider`, `SplitView`, `TabbedPage`, `TableView`, `TableViewCell`, `TableViewColumnHeader`, `TableViewRow`, `TabStrip`, `TabStripItem`, `TextSelectionHandle`, `ThemeVariantScope`, `TimePicker`, `ToggleButton`, `TransitioningContentControl`, `WindowDrawnDecorations`, and `WindowNotificationManager`.

`FluentControls.xaml` is not imported. Convert extensions and local source URIs only. No visual/value substitution, inlining, Fluent namespace, or Fluent assembly URI.

## Phase 3: Fallback Resource Architecture

Create separate source-owned layers:

- `Accents/Fluent/BaseColorsPalette.axaml`
- `Accents/Fluent/BaseResources.axaml`
- `Accents/Fluent/FluentControlResources.axaml`
- `Accents/Fluent/InvariantResources.axaml`

Each receives exact source-SHA header. Copy complete upstream resource content, preserving declaration form/order and every theme variant. Do not prune keys manually: source `StaticResource` aliases have delayed transitive dependencies and a partial layer causes runtime `KeyNotFoundException` failures.

Mirror Fluent's two-scope topology. Never flatten Fluent-compatible resources into MacOS `ThemeRoot` resource dictionary, where they can conflict with classic/Liquid Glass values and aliases.

```xml
<Styles>
  <!-- Child fallback scope: complete upstream-compatible resources and only controls MacOS does not own. -->
  <Styles>
    <Styles.Resources>
      <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
          <ResourceInclude Source="/Accents/Fluent/BaseColorsPalette.axaml" />
          <accents:SystemAccentColors />
          <MergeResourceInclude Source="/Accents/Fluent/BaseResources.axaml" />
          <MergeResourceInclude Source="/Accents/Fluent/FluentControlResources.axaml" />
          <MergeResourceInclude Source="/Accents/Fluent/InvariantResources.axaml" />
        </ResourceDictionary.MergedDictionaries>
      </ResourceDictionary>
    </Styles.Resources>
    <StyleInclude Source="/Controls/FluentFallbackControls.axaml" />
  </Styles>

  <!-- Outer MacOS scope: existing menu/icon resources and MacOS control index. -->
  <!-- Preserve ResourceInclude and C# variant overlay lifecycle. -->
</Styles>
```

`FluentFallbackControls.axaml` includes only imported controls without a MacOS default `ControlTheme`; existing MacOS controls stay in `_index.axaml`. Outer MacOS resources remain authoritative while fallback still resolves unowned controls and source-static resources.

Rules:

- Classic `ThemeResources.axaml` and Liquid Glass override resources remain visual authority. Do not overwrite values merely to match Fluent.
- Complete source fallback resources exist only inside child fallback scope.
- Retain matching Default/Light/Dark dictionaries for every dynamic key used by a vendored control.
- Add only missing Cut/Copy/Paste text flyout strings. Existing MacOS Undo/Delete/Select All strings remain.
- Keep `MenuResources.axaml`, `MenuResources_LiquidGlass.axaml`, and `MenuResourceAliasBuilder` isolated. Do not inject general compatibility tokens there or rebind their aliases to Fluent names.
- Do not import compact density or Fluent palette collection/public palette API. MacOS has no equivalent public feature.

Coordinator audits every fallback `StaticResource`, `DynamicResource`, `BasedOn`, converter, and source URI recursively for closure. Verify classic Light/Dark and Liquid Glass Light/Dark outer resources retain precedence over fallback resources and computed menu aliases.

## Existing Theme Protection

Existing classic and Liquid Glass templates are visual authority. Do not append upstream defaults merely because no equivalent MacOS setter exists.

- Never append source padding, margin, minimum size, alignment, font, border, background, foreground, transform, transition, focus, or state setter when it changes rendered MacOS behavior.
- Never append source selectors for template parts missing from current MacOS template. Dead selectors are not parity.
- Never replace existing MacOS template with upstream template.
- Add source units to existing MacOS files only when required to resolve an actual missing key/supporting theme/template part or preserve non-visual functional contract. Record omitted visual source items in manifest.
- Imported controls remain as-is in fallback scope. Existing MacOS controls must not inherit imported default themes except for genuinely unowned child controls.

This protects MacOS composition from fallback primitive themes, especially `RepeatButton`, `ToggleButton`, `PathIcon`, `ItemsControl`, `PopupRoot`, and `OverlayPopupHost`.

## Phase 4: Own System Accent Provider

Add `Accents/SystemAccentColors.cs`, based on upstream GitHub source at resolved SHA with fixed-SHA C# provenance. Adapt namespace/visibility only.

Required behavior:

- Provide `SystemAccentColor`, `SystemAccentColorDark1` through `Dark3`, and `SystemAccentColorLight1` through `Light3`.
- Use current platform `AccentColor1`; fall back to `#0078D7` if unavailable.
- Resolve platform settings from `Application` and visuals.
- Subscribe/unsubscribe `ColorValuesChanged` with owner lifecycle.
- Invalidate cache, calculate HSL shades using identical upstream deltas, and notify resource host on changes.

Instantiate provider in child fallback scope before complete fallback resources. Keep outer classic/Liquid Glass resource lifecycle and C# loaded overlays unchanged; provider remains under theme resource ownership with no app-global static cache.

Regression requirements:

- Existing `MacOsClassicAccentTests` and `MacOsAccentSelectionTests` continue to pass.
- Higher-scope explicit Window/Application `SystemAccentColor*` resources still override provider as current tests expect.
- Classic and Liquid Glass brushes update from local seven-key provider when platform colors change.
- Do not alter `WallpaperTintApplier`, which handles wallpaper tint rather than system accent detection.

## Phase 5: Parallel Dispatch

Coordinator owns manifest, `ThemeRoot.axaml`, `_index.axaml`, project/version work, dynamic resource integration review, and final merge. One resource/accent agent owns `Accents/Fluent/*`, `Accents/SystemAccentColors.cs`, and tests. No other agent edits those files.

| Batch | Existing MacOS files |
|---|---|
| A | `AutoCompleteBox`, `Button`, `ButtonSpinner`, `CheckBox`, `RadioButton`, `HyperlinkButton` |
| B | `Calendar`, `CalendarButton`, `CalendarDatePicker`, `CalendarDayButton`, `CalendarItem`, `DataValidationErrors` |
| C | `ComboBox`, `ComboBoxItem`, `ListBox`, `ListBoxItem`, `ScrollBar`, `ScrollViewer` |
| D | `ContextMenu`, `Menu`, `MenuItem`, `MenuFlyoutPresenter`, `FlyoutPresenter`, `Separator`, `ToolTip` |
| E | `DropDownButton`, `EmbeddableControlRoot`, `Expander`, `GridSplitter`, `NumericUpDown`, `SplitButton` |
| F | `TabControl`, `TabItem`, `TextBox`, `ToggleSwitch`, `TreeView`, `TreeViewItem`, `Window` |
| G | Imported controls A-M, source order |
| H | Remaining imported controls N-Z, source order |

Each agent only changes assigned files from manifest. Existing custom MacOS controls excluded. Coordinator indexes imported controls once in dependency-safe upstream order.

## Phase 6: Integrate

Coordinator:

1. Adds imported files only to `Controls/FluentFallbackControls.axaml` once, preserving required source dependencies. Existing MacOS controls remain in `_index.axaml`.
2. Wires complete local fallback resource files and accent provider inside child fallback `Styles`; retains existing outer menu resources, control index, ResourceInclude semantics, and C# variant overlay lifecycle.
3. Retains `ResourceInclude` classic menu semantics and C# variant/alias loading order.
4. Removes direct `<FluentTheme />` and fallback comments from `ThemeRoot.axaml`.
5. Removes `Avalonia.Themes.Fluent` package reference from MacOS csproj.
6. Searches project source for Fluent assembly/type/resource references and stale source URLs. Expected code/AXAML result: no `Avalonia.Themes.Fluent`, `<FluentTheme`, `avares://Avalonia.Themes.Fluent`, `using:Avalonia.Themes.Fluent`, raw URL, or `blob/master`. Review README separately; update only statements that say Fluent is still required.

## Validation

Static gates: complete upstream manifest; imports indexed once; resource/type/URI resolution without Fluent; no default theme duplicates; source comments fixed SHA; existing MacOS content changed only through manifest append sections/integration files.

Use C# LSP diagnostics only. Do not run build, restore, tests, SampleApp, or executables.

User performs visual/runtime validation. Review visual differences before baseline changes. Do not update baselines without user approval.

Keep Avalonia framework/package version upgrades separate from Fluent-removal visual triage. Framework changes can alter text fallback, selection, scrolling, popups, composition, and scale across every theme. A difference reproduced under classic, Liquid Glass, Linux, and DevExpress is framework/runtime evidence, not MacOS migration evidence.

## Agent Contract And Risks

Agent response: files changed, exact source permalink, manifest items done, discovered dependencies, commands/result, no commit/staging/baseline/dependency work.

Main risks: fallback resource precedence breaking Liquid Glass/menu aliases, partial-resource alias closure, stale explicit source references, HSL shade parity, fallback primitive themes affecting MacOS children, and broad visual regressions from missing template scopes. Coordinator final-review compares upstream source SHA, MacOS baseline, manifest in both visual variants.

Confidence: 95%. Existing dynamic resource, fallback-scope boundary, Liquid Glass, wallpaper tint, menu alias, and accent test boundaries identified.
