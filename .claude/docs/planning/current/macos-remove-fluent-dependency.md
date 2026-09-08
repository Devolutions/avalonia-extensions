# macOS: Remove Fluent Theme Dependency

## Goal

Make `Devolutions.AvaloniaTheme.MacOS` self-contained. Remove runtime and package dependency on `Avalonia.Themes.Fluent`; preserve classic macOS and conditional Liquid Glass visuals, menu aliases, wallpaper tinting, and live system accent behavior.

For every Fluent 12.1.2 control loaded upstream:

- No MacOS `ControlTheme`: import upstream as-is.
- Existing MacOS `ControlTheme`: retain MacOS implementation and append only absent upstream semantic items in a marked final section.
- Vendor only upstream resources missing from MacOS resource scopes.
- Port Fluent system accent detection into MacOS-owned source. No Fluent classes or resource URIs remain.

## Fixed Source

Use only Avalonia `12.1.2`, commit `d3c867a9e2de379249b03dbeb3495bd7f076a81a`.

- Controls: <https://github.com/AvaloniaUI/Avalonia/tree/d3c867a9e2de379249b03dbeb3495bd7f076a81a/src/Avalonia.Themes.Fluent/Controls>
- Control manifest: <https://github.com/AvaloniaUI/Avalonia/blob/d3c867a9e2de379249b03dbeb3495bd7f076a81a/src/Avalonia.Themes.Fluent/Controls/FluentControls.xaml>
- Root order: <https://github.com/AvaloniaUI/Avalonia/blob/d3c867a9e2de379249b03dbeb3495bd7f076a81a/src/Avalonia.Themes.Fluent/FluentTheme.xaml>
- Base palette: <https://github.com/AvaloniaUI/Avalonia/blob/d3c867a9e2de379249b03dbeb3495bd7f076a81a/src/Avalonia.Themes.Fluent/Accents/BaseColorsPalette.xaml>
- Base resources: <https://github.com/AvaloniaUI/Avalonia/blob/d3c867a9e2de379249b03dbeb3495bd7f076a81a/src/Avalonia.Themes.Fluent/Accents/BaseResources.xaml>
- Control resources: <https://github.com/AvaloniaUI/Avalonia/blob/d3c867a9e2de379249b03dbeb3495bd7f076a81a/src/Avalonia.Themes.Fluent/Accents/FluentControlResources.xaml>
- Invariant strings: <https://github.com/AvaloniaUI/Avalonia/blob/d3c867a9e2de379249b03dbeb3495bd7f076a81a/src/Avalonia.Themes.Fluent/Strings/InvariantResources.xaml>
- Accent provider: <https://github.com/AvaloniaUI/Avalonia/blob/d3c867a9e2de379249b03dbeb3495bd7f076a81a/src/Avalonia.Themes.Fluent/Accents/SystemAccentColors.cs>

No `master`, floating tag, raw, or other-version provenance URL.

## Version Gate

Current Avalonia version is `12.0.5`; current package lower bound is `[12.0.2,)`. 12.1.2 source, notably TableView, requires matching package support.

Before implementation, coordinator must either raise supported Avalonia package/bounds to `12.1.2` or secure an approved older upstream source matching retained bounds. This plan assumes `12.1.2` support after that decision. No control agent edits dependency files.

## MacOS Invariants

- `ThemeRoot.axaml` currently hosts direct `<FluentTheme />`, then local controls. Remove it only after all local source/resource providers work.
- `MacOsTheme.axaml.cs` and `MacOsThemeWithGlobalStyles.axaml.cs` dynamically append base `ThemeResources.axaml`, create menu alias dictionary, conditionally append `ThemeResources_LiquidGlass.axaml`, run wallpaper tint hook, then rebuild menu aliases. Preserve this exact lifecycle and precedence.
- `ThemeRoot.axaml` intentionally uses `ResourceInclude` for classic `MenuResources.axaml`; changing to `MergeResourceInclude` breaks classic/Liquid Glass menu alias precedence. Keep this distinction.
- `MenuResourceAliasBuilder` converts active variant legacy theme keys to `MacOsMenu*` keys. Do not change mappings or rebuild timing while importing Fluent resources.
- `ThemeResources.axaml` has per-variant dynamic accent resources. `ThemeResources_LiquidGlass.axaml` builds accent-derived visuals with converters. Both must observe owned `SystemAccentColor*` keys.
- Keep custom controls, menu packs, DataGrid, TreeDataGrid, ColorPicker, Liquid Glass detection, wallpaper tinting, and optional global styles out of upstream control classification.

## Provenance And Comparison

For each new imported AXAML file:

```xml
<!-- Retrieved from Avalonia Fluent 12.1.2:
     https://github.com/AvaloniaUI/Avalonia/blob/d3c867a9e2de379249b03dbeb3495bd7f076a81a/src/Avalonia.Themes.Fluent/Controls/<Control>.xaml -->
```

For each changed MacOS counterpart, append inside affected theme after all existing content:

```xml
<!-- Properties below imported from Avalonia Fluent 12.1.2:
     https://github.com/AvaloniaUI/Avalonia/blob/d3c867a9e2de379249b03dbeb3495bd7f076a81a/src/Avalonia.Themes.Fluent/Controls/<Control>.xaml -->
<!-- Only source items absent from matching MacOS scope. -->
```

Semantic comparison includes all themes, keys/targets, setters, templates, nested styles/selectors, pseudo-classes, transitions, animations, resources, `BasedOn`, template parts, source URIs, converters, and types. Existing MacOS choice wins even when Fluent differs. No duplicate default-key theme or Fluent `BasedOn` shortcut.

Replace stale `master` or old Fluent source comments in touched files with permanent-SHA form.

## Phase 1: Authoritative Manifest

Coordinator writes `MacOsFluentImportManifest.md` beside this plan before modifications. For all entries in upstream `FluentControls.xaml`:

1. Record upstream target/key themes, local resources, namespaces, source includes, and all direct resource references.
2. Map source to current MacOS control by actual `ControlTheme` target/key, not file name.
3. Classify import, append, resource-only, or excluded-not-loaded.
4. List exact missing semantic units and insertion scope for every append assignment.
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

## Phase 3: Compatibility Resources

Create separate source-owned layers:

- `Accents/Fluent/BaseColorsPalette.axaml`
- `Accents/Fluent/BaseResources.axaml`
- `Accents/Fluent/FluentControlResources.axaml`
- `Accents/Fluent/InvariantResources.axaml`

Each receives exact fixed-SHA source header. Retain only source keys unavailable from MacOS at equivalent resource scope and theme variant. Preserve upstream source values/forms for retained keys.

Rules:

- Classic `ThemeResources.axaml` and Liquid Glass override resources remain visual authority. Do not overwrite values merely to match Fluent.
- Retain matching Default/Light/Dark dictionaries for every dynamic key used by a vendored control.
- Add only missing Cut/Copy/Paste text flyout strings. Existing MacOS Undo/Delete/Select All strings remain.
- Keep `MenuResources.axaml`, `MenuResources_LiquidGlass.axaml`, and `MenuResourceAliasBuilder` isolated. Do not inject general compatibility tokens there or rebind their aliases to Fluent names.
- Do not import compact density or Fluent palette collection/public palette API. MacOS has no equivalent public feature.

Resource agent runs lookup tests for classic Light/Dark and Liquid Glass Light/Dark. Verify compatibility dictionaries do not shadow variant resources loaded dynamically from C# or computed menu aliases.

## Phase 4: Own System Accent Provider

Add `Accents/SystemAccentColors.cs`, based on upstream 12.1.2 exact source with fixed-SHA C# provenance. Adapt namespace/visibility only.

Required behavior:

- Provide `SystemAccentColor`, `SystemAccentColorDark1` through `Dark3`, and `SystemAccentColorLight1` through `Light3`.
- Use current platform `AccentColor1`; fall back to `#0078D7` if unavailable.
- Resolve platform settings from `Application` and visuals.
- Subscribe/unsubscribe `ColorValuesChanged` with owner lifecycle.
- Invalidate cache, calculate HSL shades using identical upstream deltas, and notify resource host on changes.

Instantiate provider in `ThemeRoot.axaml` before classic/compatibility resources so current `DynamicResource` lookups resolve it. Keep provider instance under theme resource ownership; no app-global static cache.

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

1. Adds all imported files to `Controls/_index.axaml` once, preserving required source dependencies.
2. Wires local base palette, accent provider, compatibility resource layers, existing menu resources, and control index in a precedence-tested order.
3. Retains `ResourceInclude` classic menu semantics and C# variant/alias loading order.
4. Removes direct `<FluentTheme />` and fallback comments from `ThemeRoot.axaml`.
5. Removes `Avalonia.Themes.Fluent` package reference from MacOS csproj.
6. Searches project source for Fluent assembly/type/resource references and stale source URLs. Expected code/AXAML result: no `Avalonia.Themes.Fluent`, `<FluentTheme`, `avares://Avalonia.Themes.Fluent`, `using:Avalonia.Themes.Fluent`, raw URL, or `blob/master`. Review README separately; update only statements that say Fluent is still required.

## Validation

Static gates: complete upstream manifest; imports indexed once; resource/type/URI resolution without Fluent; no default theme duplicates; source comments fixed SHA; existing MacOS content changed only through manifest append sections/integration files.

Run:

```bash
dotnet build src/Devolutions.AvaloniaTheme.MacOS/Devolutions.AvaloniaTheme.MacOS.csproj
dotnet build src/Devolutions.AvaloniaTheme.MacOS/Devolutions.AvaloniaTheme.MacOS.csproj -c Release
dotnet test tests/Devolutions.AvaloniaControls.Tests/Devolutions.AvaloniaControls.Tests.csproj
dotnet test tests/Devolutions.AvaloniaControls.VisualTests/Devolutions.AvaloniaControls.VisualTests.csproj
```

SampleApp smoke test from binary directory. Exercise classic + Liquid Glass when host supports it; Light/Dark; accent change/override; menus/menu packs and aliases; wallpaper tint; new Fluent-derived controls; date/time picker; file chooser; focus/navigation; `GlobalStyles` true/false; TreeDataGrid availability. Review visual diffs before any baseline update. No baseline update without user approval.

## Agent Contract And Risks

Agent response: files changed, exact source permalink, manifest items done, discovered dependencies, commands/result, no commit/staging/baseline/dependency work.

Main risks: Avalonia version mismatch, compatibility resource precedence breaking Liquid Glass or menu aliases, stale explicit source references, HSL shade parity, and broad visual regressions from missing template scopes. Coordinator final-review compares upstream 12.1.2, MacOS baseline, manifest in both visual variants.

Confidence: 95%. Existing dynamic resource, Liquid Glass, wallpaper tint, menu alias, accent test boundaries identified. Version gate required before implementation.
