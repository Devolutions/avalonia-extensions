# Linux Yaru: Remove Fluent Theme Dependency

> Status: Completed 2026-09-11.
>
> Implementation: `5184449 [Linux] Remove Fluent theme dependency` and `252ac28 Cleanup`.
> User completed manual runtime and visual validation. Result accepted.

## Goal

Make `Devolutions.AvaloniaTheme.Linux` self-contained. Remove runtime and package dependency on `Avalonia.Themes.Fluent`; retain existing GTK Yaru visual decisions and fixed Yaru orange accent.

For every Avalonia Fluent control currently included by upstream GitHub `main`:

- No existing Linux `ControlTheme`: import source as-is.
- Existing Linux `ControlTheme`: keep Linux/Yaru theme first; append only missing upstream semantic items in a final marked section.
- Vendor resource keys needed by imported source without replacing Linux-owned values.
- Recreate upstream accent-provider mechanics locally, while preserving Linux's current explicit `#D85E33` palette override.

## Upstream Source

Inspect sources directly from <https://github.com/AvaloniaUI/Avalonia/tree/main/src/Avalonia.Themes.Fluent/Controls>. Resolve `main` to one commit SHA immediately before each implementation batch. Fetch source directly from GitHub at that SHA; do not inspect local NuGet assets or use a cloned upstream checkout as source authority.

- Controls: <https://github.com/AvaloniaUI/Avalonia/tree/main/src/Avalonia.Themes.Fluent/Controls>
- Upstream load manifest: <https://github.com/AvaloniaUI/Avalonia/blob/main/src/Avalonia.Themes.Fluent/Controls/FluentControls.xaml>
- Theme resource order: <https://github.com/AvaloniaUI/Avalonia/blob/main/src/Avalonia.Themes.Fluent/FluentTheme.xaml>
- Base palette: <https://github.com/AvaloniaUI/Avalonia/blob/main/src/Avalonia.Themes.Fluent/Accents/BaseColorsPalette.xaml>
- Base resources: <https://github.com/AvaloniaUI/Avalonia/blob/main/src/Avalonia.Themes.Fluent/Accents/BaseResources.xaml>
- Control resources: <https://github.com/AvaloniaUI/Avalonia/blob/main/src/Avalonia.Themes.Fluent/Accents/FluentControlResources.xaml>
- Invariant strings: <https://github.com/AvaloniaUI/Avalonia/blob/main/src/Avalonia.Themes.Fluent/Strings/InvariantResources.xaml>
- System accent provider: <https://github.com/AvaloniaUI/Avalonia/blob/main/src/Avalonia.Themes.Fluent/Accents/SystemAccentColors.cs>
- Palette provider: <https://github.com/AvaloniaUI/Avalonia/blob/main/src/Avalonia.Themes.Fluent/ColorPaletteResources.cs>

Each provenance comment must use permanent `blob/<resolved-sha>/...` URL. Never use `master`, `main`, a floating tag, raw URL, or a SHA different from that batch's resolved revision.

Current package support remains Avalonia `12.x`. Do not create a version gate or change package bounds as part of theme removal.

## Current Architecture And Invariants

- `ThemeRoot.axaml` currently hosts `<FluentTheme>` and nested `ColorPaletteResources` for Light/Dark accent `#D85E33`. Remove both Fluent types only after local replacement resolves identical seven `SystemAccentColor*` keys.
- `ThemeRoot.axaml` loads `Icons.axaml`, `ThemeResources.axaml`, and `Controls/_index.axaml`. `MenuResources.axaml` is loaded by `_index.axaml`.
- `GlobalStyles.axaml` is optional. Imported controls must work with `GlobalStyles="False"`; no imported control may silently depend on menu SVG, separator, or menu global styles.
- `Accents/ThemeResources.axaml` owns Yaru colors, inputs, popup surfaces, many Fluent-compatible keys, ColorPicker resources, and Undo/Delete/Select All strings. Keep its Yaru values unchanged.
- `MenuResources.axaml` and menu-pack AXAML are a separate contract. Do not use general Fluent compatibility resources to restyle pack-only menus.
- Keep DataGrid, ColorPicker, TreeDataGrid/Accelerate, and Devolutions custom control files. They are not upstream Fluent controls.

## Provenance And Comparison Rules

New vendored file header:

```xml
<!-- Retrieved from Avalonia Fluent main at <resolved-commit-sha>:
     https://github.com/AvaloniaUI/Avalonia/blob/<resolved-commit-sha>/src/Avalonia.Themes.Fluent/Controls/<Control>.xaml -->
```

Existing control final-section header:

```xml
<!-- Properties below imported from Avalonia Fluent main at <resolved-commit-sha>:
     https://github.com/AvaloniaUI/Avalonia/blob/<resolved-commit-sha>/src/Avalonia.Themes.Fluent/Controls/<Control>.xaml -->
<!-- Only source items absent from matching Linux/Yaru scope. -->
```

Compare complete semantic scope: default and named `ControlTheme`, target type/key, direct setters, templates, nested styles/setters, pseudo-class selectors, transitions, animations, local resources, `BasedOn`, template parts, converters, and resource URIs. Same value is not required; existing Yaru declaration wins. Do not add duplicate default theme as shortcut.

Replace stale `master`, raw, and prior-SHA Fluent comments in files touched by this work with the fixed-SHA comment above.

## Phase 1: Manifest Before Editing

Coordinator creates `LinuxYaruFluentImportManifest.md` beside this plan. For every entry in upstream `FluentControls.xaml`:

1. Record upstream file, all `ControlTheme` targets/keys, local resources, type namespaces, source includes, and direct resource references.
2. Locate Linux coverage by actual target/key, not filename. A local custom theme does not count as an upstream control counterpart.
3. Classify `import-as-is`, `append-missing`, `resource-only`, or `not-loaded-by-upstream`.
4. For append work, list exact missing units, exact destination scope, and whether it changes an existing Yaru default visual, layout, focus order, or interaction state.
5. Inventory every retained `StaticResource`, `DynamicResource`, `BasedOn`, converter, type, and URI. Identify provider, theme variant, and lookup scope.
6. Record control ordering dependencies, notably `DateTimePickerShared` before `DatePicker` and `TimePicker`.

Manifest controls all agent work. Coordinator resolves ambiguity and merges integration-only files. Agents never infer ownership or edit unassigned resource/root/index/project files.

## Phase 2: Control Inventory

### Current Linux Counterparts

Preliminary existing-file counterparts requiring semantic comparison:

`ButtonSpinner`, `Button`, `CalendarButton`, `CalendarDatePicker`, `CalendarDayButton`, `CalendarItem`, `Calendar`, `CheckBox`, `ComboBoxItem`, `ComboBox`, `ContextMenu`, `DataValidationErrors`, `DropDownButton`, `EmbeddableControlRoot`, `Expander`, `FlyoutPresenter`, `HyperlinkButton`, `ListBox`, `MenuFlyoutPresenter`, `MenuItem`, `Menu`, `NumericUpDown`, `ScrollBar`, `Separator`, `SplitButton`, `TabControl`, `TabItem`, `TextBox`, and `Window`.

Manifest must additionally inspect supporting themes in these files, including `FluentScrollBar*`, `FluentCalendarButton`, `FluentTextBoxButton`, and menu themes. `ListBox` uses `BasedOn`; check source default theme availability before retaining that reference.

### Preliminary Import-As-Is Files

No same-named Linux file currently exists for:

`AdornerLayer`, `AutoCompleteBox`, `CarouselPage`, `Carousel`, `CommandBar`, `ContentPage`, `DatePicker`, `DateTimePickerShared`, `DrawerPage`, `GridSplitter`, `GroupBox`, `HeaderedContentControl`, `ItemsControl`, `Label`, `ListBoxItem`, `ManagedFileChooser`, `MenuScrollViewer`, `NavigationPage`, `NotificationCard`, `OverlayPopupHost`, `PathIcon`, `PipsPager`, `PopupRoot`, `ProgressBar`, `RadioButton`, `RefreshContainer`, `RefreshVisualizer`, `RepeatButton`, `ScrollViewer`, `SelectableTextBlock`, `Slider`, `SplitView`, `TabbedPage`, `TableView`, `TableViewCell`, `TableViewColumnHeader`, `TableViewRow`, `TabStrip`, `TabStripItem`, `TextSelectionHandle`, `ThemeVariantScope`, `TimePicker`, `ToggleButton`, `ToggleSwitch`, `ToolTip`, `TransitioningContentControl`, `TreeView`, `TreeViewItem`, `WindowDrawnDecorations`, and `WindowNotificationManager`.

`FluentControls.xaml` is aggregate metadata only. Do not vendor it. DevExpress-style local `_index.axaml` remains the aggregate.

Import source without visual substitutions. Mechanical changes only: `.xaml` to `.axaml`, local include URI conversion, removal of internal-only class modifier when compiler requires it, and provenance header. No `Avalonia.Themes.Fluent` namespace/type/resource URI remains.

## Phase 3: Fallback Resource Architecture

Create separate compatibility layers under `Accents/Fluent/`:

- `BaseColorsPalette.axaml`
- `BaseResources.axaml`
- `FluentControlResources.axaml`
- `InvariantResources.axaml`

Each file gets source header with exact source permalink. Copy each complete upstream resource file, preserving declaration form/order and all theme variants. Do not prune resources key-by-key: retained `StaticResource` aliases can require delayed transitive resources and otherwise cause runtime `KeyNotFoundException` failures.

Mirror Fluent's two-scope topology. Do not merge Fluent-compatible resources into the same `ThemeRoot` resource dictionary as Yaru resources.

```xml
<Styles>
  <!-- Child fallback scope: complete upstream-compatible resources and only controls Yaru does not own. -->
  <Styles>
    <Styles.Resources>
      <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
          <ResourceInclude Source="/Accents/Fluent/BaseColorsPalette.axaml" />
          <accents:SystemAccentColors />
          <accents:YaruAccentColors />
          <MergeResourceInclude Source="/Accents/Fluent/BaseResources.axaml" />
          <MergeResourceInclude Source="/Accents/Fluent/FluentControlResources.axaml" />
          <MergeResourceInclude Source="/Accents/Fluent/InvariantResources.axaml" />
        </ResourceDictionary.MergedDictionaries>
      </ResourceDictionary>
    </Styles.Resources>
    <StyleInclude Source="/Controls/FluentFallbackControls.axaml" />
  </Styles>

  <!-- Outer Yaru scope: existing resources and owned control themes. -->
  <!-- Existing ThemeRoot resource includes remain here. -->
</Styles>
```

`FluentFallbackControls.axaml` includes only imported controls with no Yaru default `ControlTheme`; do not include controls Yaru already owns. This preserves Yaru resource precedence without flattening or duplicate-key collisions.

Rules:

- Existing Yaru keys in outer `ThemeResources.axaml` win, even if upstream has different value.
- Complete source fallback resources belong only in child fallback scope.
- Add missing Cut/Copy/Paste strings in `Accents/Fluent/InvariantResources.axaml`; retain existing Yaru Undo/Delete/Select All strings.
- Do not place compatibility entries in `MenuResources.axaml`.
- Do not import `DensityStyles/Compact.xaml`; no Linux public density API exists.
- Do not import Fluent `ColorPaletteResources*` as general public API. Only implement minimal local provider required to preserve current root palette behavior.

Coordinator verifies resource precedence and resource closure before integration. Audit every `StaticResource`, `DynamicResource`, `BasedOn`, converter, and source URI in fallback files recursively. `StaticResource` resolves at load time; `DynamicResource` resolves later. Both require Light and Dark audit.

## Existing Theme Protection

Existing Yaru templates are visual authority. Do not append upstream defaults merely because no same-property Yaru setter exists.

- Never append source `Padding`, `Margin`, `MinWidth`, `MinHeight`, alignment, font, border, background, foreground, transform, transition, focus, or state setter when it changes rendered Yaru behavior.
- Never append source selectors for template parts absent from Yaru template. They are dead styles, not parity.
- Never replace an existing Yaru template with upstream template.
- Add to existing Yaru files only source units necessary to resolve an actual missing key/supporting theme/template part or preserve non-visual functional contract. Record omitted visual source units in manifest.
- Imported controls remain as-is in fallback scope. Existing Yaru controls must not inherit their default themes unless source template explicitly needs an unowned child control.

This rule prevents broad regressions through shared primitives such as `ScrollViewer`, `RepeatButton`, `ToggleButton`, `ItemsControl`, `PathIcon`, `PopupRoot`, and `OverlayPopupHost`.

## Phase 4: Accent Ownership

Current Linux behavior must remain: both Light and Dark resolve `SystemAccentColor` and six HSL-derived shades from fixed Yaru orange `#D85E33`, not upstream blue and not host OS accent.

Implement locally:

1. `Accents/SystemAccentColors.cs`, sourced from upstream provider. Keep owner attach/detach, platform settings lookup, cache invalidation, `PlatformColorValues` event, notification, default accent, seven names, and identical `CalculateAccentShades` algorithm.
2. `Accents/YaruAccentColors.cs`, source-compatible minimal palette/override provider, returning `#D85E33` and all six calculated shades for Light and Dark. It must have precedence over system-detected provider, precisely replacing current nested Fluent `ColorPaletteResources` behavior.
3. Fixed-SHA C# provenance comments on both local implementations, stating any adaptation.
4. Instantiate providers in child fallback scope: base palette, system detection, Yaru fixed-accent override, complete fallback resources, fallback controls. Keep outer Yaru resources and controls outside this child scope.

Tests must prove default key resolves `#D85E33` in Light and Dark, expected shade calculations match upstream, a higher-scope app/window resource can still override each key, and no Fluent assembly is loaded. Do not change Yaru to live OS accent detection merely because generic provider is now owned locally.

## Phase 5: Parallel Dispatch

Coordinator owns manifest, `ThemeRoot.axaml`, `_index.axaml`, project/dependency changes, and final review. Resource/accent agent owns only `Accents/Fluent/*`, `Accents/SystemAccentColors.cs`, `Accents/YaruAccentColors.cs`, and focused tests.

| Batch | Existing Linux files |
|---|---|
| A | `Button`, `ButtonSpinner`, `CheckBox`, `HyperlinkButton`, `SplitButton` |
| B | `Calendar`, `CalendarButton`, `CalendarDatePicker`, `CalendarDayButton`, `CalendarItem`, `DataValidationErrors` |
| C | `ComboBox`, `ComboBoxItem`, `ListBox`, `ScrollBar`, `TextBox` |
| D | `ContextMenu`, `Menu`, `MenuItem`, `MenuFlyoutPresenter`, `FlyoutPresenter`, `Separator` |
| E | `DropDownButton`, `EmbeddableControlRoot`, `Expander`, `NumericUpDown`, `TabControl`, `TabItem`, `Window` |
| F | Unstyled import files A-M by upstream order |
| G | Remaining unstyled import files N-Z by upstream order |

Imported control agents receive manifest entries and modify only assigned new files. Preserve upstream ordering in coordinator's `_index.axaml`; no file gets included twice.

## Phase 6: Integration And Dependency Removal

Coordinator:

1. Adds imported controls only to `Controls/FluentFallbackControls.axaml` in dependency-safe upstream order; existing Yaru controls remain in `_index.axaml`.
2. Adds local accent providers and complete compatibility resource layers to child fallback `Styles` in `ThemeRoot.axaml`; outer Yaru resource order remains unchanged.
3. Removes `<FluentTheme>` plus nested `<FluentTheme.Palettes>`/`<ColorPaletteResources>` markup and fallback wording.
4. Removes `Avalonia.Themes.Fluent` package reference from Linux csproj.
5. Searches Linux project for `Avalonia.Themes.Fluent`, `<FluentTheme`, `<ColorPaletteResources`, `avares://Avalonia.Themes.Fluent`, `using:Avalonia.Themes.Fluent`, raw upstream URL, and `blob/master`. Expected source result: none, excluding user-facing README examples deliberately kept until separately updated.
6. Update README install/scoping text only if it states Fluent remains required. Do not alter examples describing replacement of Fluent with Yaru.

## Validation

Static: upstream 12.1.2 manifest complete; all imports indexed once; no unresolved source resource/type/URI; no duplicate default theme; Yaru source content unchanged except final provenance sections; all source links fixed SHA.

Use C# LSP diagnostics only. Do not run build, restore, tests, SampleApp, or executables.

User performs visual/runtime validation. Capture and triage visual differences before baseline changes. Do not update baselines without user approval.

Keep Avalonia framework/package version changes separate from Fluent-removal visual triage. An Avalonia upgrade can alter every theme through text fallback, selection, scrolling, popup, composition, and scale behavior. A visual diff shared by Linux, MacClassic, LiquidGlass, and DevExpress is framework/runtime evidence, not Linux migration evidence.

## Agent Contract And Risks

Each Luna agent returns changed files, upstream sources/SHAs, manifest entries done, unresolved dependencies, commands/results, and no commit/staging/baseline/dependency changes.

Main risks: resource precedence, partial-resource alias closure, `BasedOn` cycles, static versus dynamic lookup, fallback primitive themes affecting Yaru children, and accidentally replacing fixed Yaru orange with system accent. Coordinator handles conflicts and validates three-way diff: upstream source SHA, Linux baseline, manifest.

Confidence: 95%. Root palette behavior, fallback-scope boundary, and Yaru visual authority identified.
