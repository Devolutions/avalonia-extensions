# DevExpress: Remove Fluent Theme Dependency

## Goal

Make `Devolutions.AvaloniaTheme.DevExpress` self-contained. Remove runtime and package dependency on `Avalonia.Themes.Fluent` without changing existing DevExpress-owned visual decisions.

Import every Fluent control entry currently listed on GitHub `main`:

- No existing DevExpress `ControlTheme` for upstream target: vendor upstream theme as-is.
- Existing DevExpress `ControlTheme`: retain DevExpress content. Append only upstream properties/styles/resources absent from corresponding DevExpress scope.
- Import every upstream resource needed by vendored controls when no DevExpress-owned resource already provides same key and scope.
- Replace Fluent's system accent resource provider with DevExpress-owned equivalent.

## Fixed Upstream Source

Inspect source directly from `main` at https://github.com/AvaloniaUI/Avalonia/tree/main/src/Avalonia.Themes.Fluent/Controls. For every import, resolve the source file's current GitHub commit SHA immediately before editing and use its permanent permalink in the AXAML/C# provenance comment. Do not read local NuGet assets or use a cloned checkout as source authority.

- Controls directory: <https://github.com/AvaloniaUI/Avalonia/tree/main/src/Avalonia.Themes.Fluent/Controls>
- Fluent root/load order: <https://github.com/AvaloniaUI/Avalonia/blob/main/src/Avalonia.Themes.Fluent/FluentTheme.xaml>
- Control manifest/load order: <https://github.com/AvaloniaUI/Avalonia/blob/main/src/Avalonia.Themes.Fluent/Controls/FluentControls.xaml>
- Palette: <https://github.com/AvaloniaUI/Avalonia/blob/main/src/Avalonia.Themes.Fluent/Accents/BaseColorsPalette.xaml>
- Base resources: <https://github.com/AvaloniaUI/Avalonia/blob/main/src/Avalonia.Themes.Fluent/Accents/BaseResources.xaml>
- Fluent control resources: <https://github.com/AvaloniaUI/Avalonia/blob/main/src/Avalonia.Themes.Fluent/Accents/FluentControlResources.xaml>
- Invariant strings: <https://github.com/AvaloniaUI/Avalonia/blob/main/src/Avalonia.Themes.Fluent/Strings/InvariantResources.xaml>
- Accent detector: <https://github.com/AvaloniaUI/Avalonia/blob/main/src/Avalonia.Themes.Fluent/Accents/SystemAccentColors.cs>

Current repository baseline: `3eb4763180dce7d1b2febdd4422caaaea724f5ea`. Current configured development Avalonia version: `12.0.5` in `Common.props`.

## Current Architecture

- `ThemeRoot.axaml` currently instantiates `<FluentTheme />` before DevExpress resources. Remove it only after all source imports and resource dependency audit pass.
- `ThemeRoot.axaml` merges DevExpress icons, menu resources, theme resources, then `Controls/_index.axaml`.
- `Controls/_index.axaml` currently loads 59 DevExpress/local control files. Add vendored Fluent control files there in upstream `FluentControls.xaml` order, while preserving current DevExpress load order where possible.
- `DevolutionsDevExpressTheme` loads `DevExpressTheme` or `DevExpressThemeWithGlobalStyles`. Accent provider must be hosted by this theme resource tree so it tracks application and visual platform settings.
- `Devolutions.AvaloniaTheme.DevExpress.csproj` directly references `Avalonia.Themes.Fluent`; remove this reference when source no longer references any Fluent type, namespace, resource URI, or package asset.
- Keep `Avalonia.Controls.ColorPicker`, `Avalonia.Controls.DataGrid`, SVG, Accelerate, and `Devolutions.AvaloniaControls` dependencies. They are outside this scope.

## Non-Goals

- No migration of MacOS, Linux, WinUI, sample app, or test projects off Fluent.
- No DevExpress restyling toward Fluent. Existing DevExpress values win.
- No compact-density feature unless current public DevExpress API explicitly supports it.
- No unrelated cleanup, formatting churn, changelog entry, package version change, or sample default change.

## Required Provenance Format

Every imported control file must have top-of-file XML comment before its root element:

```xml
<!-- Retrieved from Avalonia Fluent main at <resolved-commit-sha>:
     https://github.com/AvaloniaUI/Avalonia/blob/<resolved-commit-sha>/src/Avalonia.Themes.Fluent/Controls/<Control>.xaml -->
```

Every existing DevExpress control file changed for upstream parity must retain DevExpress theme first. Append one final section inside each affected `ControlTheme`, after existing setters/styles and before closing tag:

```xml
<!-- Properties below imported from Avalonia Fluent main at <resolved-commit-sha>:
     https://github.com/AvaloniaUI/Avalonia/blob/<resolved-commit-sha>/src/Avalonia.Themes.Fluent/Controls/<Control>.xaml -->
<!-- Only source properties/styles absent from DevExpress theme scope. -->
```

Use permanent SHA URLs above. Do not leave existing `master` provenance comments in touched files; replace them with fixed-SHA form. Preserve credit and existing non-Fluent comments.

"Property" means every upstream item that affects target control behavior: direct `Setter`, nested style setter, `Style`, resource, `ControlTheme`, template part, transition, animation, selector, and type/namespace dependency. Compare by target/theme key and selector scope, not source-line position or matching text alone.

## Phase 1: Produce Authoritative Manifest

One coordinator agent creates `FluentImportManifest.md` beside this plan before any agent edits AXAML.

For every upstream file listed in `Controls/FluentControls.xaml`:

1. Record source file, primary `TargetType`, named supporting `ControlTheme` keys, local resources, namespace requirements, and direct upstream resource keys referenced.
2. Locate current DevExpress theme by actual `ControlTheme` target/key, not matching filename. Existing custom control files do not establish coverage for unrelated upstream controls.
3. Classify entry as `import-as-is`, `append-missing`, `resource-only dependency`, or `not loaded by FluentControls`.
4. For `append-missing`, list exact missing items plus exact insertion scope. Do not ask implementation agents to infer this independently.
5. Record dependencies between controls. Keep source ordering for themes that depend on a theme/resource defined by another imported control.
6. Build resource-key inventory from all vendored control references, including `StaticResource`, `DynamicResource`, `{StaticResource ...}`, `BasedOn`, template references, converters, and source URIs.
7. Diff inventory against current DevExpress `Accents/ThemeResources.axaml`, `Accents/MenuResources.axaml`, `Accents/Icons.axaml`, `Controls/*.axaml`, global styles, and framework/control-package resources. Mark exact provider and lookup scope for each key.

Manifest is implementation contract. Coordinator resolves ambiguities before splitting work. If existing DevExpress content has changed while agents run, update manifest and reassign affected file; never overwrite user/other-agent content.

## Phase 2: Vendor Missing Fluent Controls

Create DevExpress `Controls/<Control>.axaml` for each following upstream source with no current DevExpress equivalent. Preserve upstream content and ordering; only make mechanical changes required by local ownership:

- `Carousel.xaml`
- `CarouselPage.xaml`
- `CommandBar.xaml`
- `ContentPage.xaml`
- `DatePicker.xaml`
- `DateTimePickerShared.xaml`
- `DrawerPage.xaml`
- `FluentControls.xaml` is manifest-only. Do not vendor nested aggregate index; DevExpress `_index.axaml` is aggregate.
- `GridSplitter.xaml`
- `GroupBox.xaml`
- `HeaderedContentControl.xaml`
- `ItemsControl.xaml`
- `ManagedFileChooser.xaml`
- `MenuScrollViewer.xaml`
- `NavigationPage.xaml`
- `NotificationCard.xaml`
- `OverlayPopupHost.xaml`
- `PathIcon.xaml`
- `PipsPager.xaml`
- `PopupRoot.xaml`
- `ProgressBar.xaml`
- `RefreshContainer.xaml`
- `RefreshVisualizer.xaml`
- `RepeatButton.xaml`
- `SelectableTextBlock.xaml`
- `Slider.xaml`
- `SplitView.xaml`
- `TabbedPage.xaml`
- `TabStrip.xaml`
- `TabStripItem.xaml`
- `TextSelectionHandle.xaml`
- `ThemeVariantScope.xaml`
- `TimePicker.xaml`
- `ToggleButton.xaml`
- `ToggleSwitch.xaml`
- `ToolTip.xaml`
- `TransitioningContentControl.xaml`
- `WindowDrawnDecorations.xaml`
- `WindowNotificationManager.xaml`

Mechanical local changes allowed:

- Rename `.xaml` to `.axaml` and update corresponding DevExpress `_index.axaml` URI.
- Replace only `avares://Avalonia.Themes.Fluent/...` source references with checked-in DevExpress resource URIs/files.
- Remove upstream internal-only `x:ClassModifier="internal"` when no class is declared, if Avalonia XAML compilation requires it.
- Add provenance comment.
- Add namespaces/packages only when compiler proves required. Never retain `using:Avalonia.Themes.Fluent` or Fluent assembly reference.

No visual substitutions. Do not replace Fluent resources with similarly named DevExpress resources in imported files. Resource-layer task supplies exact missing source keys so imported AXAML retains behavior.

## Phase 3: Extend Existing DevExpress Control Themes

Existing control files corresponding to upstream Fluent controls:

- `AdornerLayer.axaml`
- `AutoCompleteBox.axaml`
- `Button.axaml`
- `ButtonSpinner.axaml`
- `Calendar.axaml`
- `CalendarButton.axaml`
- `CalendarDatePicker.axaml`
- `CalendarDayButton.axaml`
- `CalendarItem.axaml`
- `CheckBox.axaml`
- `ComboBox.axaml`
- `ComboBoxItem.axaml`
- `ContextMenu.axaml`
- `DataValidationErrors.axaml`
- `DropDownButton.axaml`
- `EmbeddableControlRoot.axaml`
- `Expander.axaml`
- `FlyoutPresenter.axaml`
- `HyperlinkButton.axaml`
- `Label.axaml`
- `ListBox.axaml`
- `ListBoxItem.axaml`
- `Menu.axaml`
- `MenuFlyoutPresenter.axaml`
- `MenuItem.axaml`
- `NumericUpDown.axaml`
- `RadioButton.axaml`
- `ScrollBar.axaml`
- `ScrollViewer.axaml`
- `Separator.axaml`
- `SplitButton.axaml`
- `TabControl.axaml`
- `TabItem.axaml`
- `TextBox.axaml`
- `TreeView.axaml`
- `TreeViewItem.axaml`
- `Window.axaml`

Rules for each agent:

1. Work only files explicitly assigned by manifest.
2. Preserve existing DevExpress declarations unchanged.
3. Find upstream matching `ControlTheme` by `x:Key` and `TargetType`. Existing file can hold multiple themes; compare each independently.
4. Append manifest-listed missing source items at bottom of matching theme. Do not use `BasedOn` Fluent theme, `StyleInclude` Fluent file, or a new duplicate default-key `ControlTheme` as shortcut.
5. Preserve local supporting themes. Import upstream supporting theme only when current scope has no equivalent and manifest marks it required.
6. For existing source files with top-level resources, add upstream missing resources as clearly marked bottom section of dictionary, unless resource task owns shared resource file.
7. Do not append an upstream setter/style that DevExpress already defines at same target/selector scope, even if value differs. DevExpress visual choice wins.
8. Resolve source resource references only through resources vendored by resource task. Do not inline values to silence missing-resource errors.

### Parallel Work Batches

Dispatch file-disjoint agents. Coordinator owns `ThemeRoot.axaml`, `Controls/_index.axaml`, csproj, manifest, and final conflict resolution.

| Batch | Existing files | Primary concern |
|---|---|---|
| A | `AdornerLayer`, `AutoCompleteBox`, `Button`, `ButtonSpinner`, `CheckBox`, `RadioButton` | Input/button state coverage |
| B | `Calendar`, `CalendarButton`, `CalendarDatePicker`, `CalendarDayButton`, `CalendarItem`, `DataValidationErrors` | Calendar supporting themes and bindings |
| C | `ComboBox`, `ComboBoxItem`, `ListBox`, `ListBoxItem`, `ScrollViewer`, `ScrollBar` | Popup/list/scroll dependencies |
| D | `ContextMenu`, `Menu`, `MenuItem`, `MenuFlyoutPresenter`, `FlyoutPresenter`, `Separator` | Keep isolated DevEx menu-pack contract intact |
| E | `DropDownButton`, `EmbeddableControlRoot`, `Expander`, `HyperlinkButton`, `Label`, `NumericUpDown`, `SplitButton` | Shared button resources |
| F | `TabControl`, `TabItem`, `TextBox`, `TreeView`, `TreeViewItem`, `Window` | Template and platform-window dependencies |

Resource agent runs before or alongside batches but publishes completed key manifest before batches compile. No batch edits `ThemeResources.axaml`, `MenuResources.axaml`, accent provider, project file, root, or index.

## Phase 4: Vendor Fluent Resource Layers

Create separate DevExpress AXAML files under `Accents/Fluent/`:

- `BaseColorsPalette.axaml`
- `BaseResources.axaml`
- `FluentControlResources.axaml`
- `InvariantResources.axaml`

Each file includes top-level fixed-SHA provenance comment pointing to its exact upstream source.

Resource agent responsibilities:

1. Fetch matching source directly from GitHub `main`; record resolved source commit SHA in its provenance comment.
2. Retain only resource keys unavailable from DevExpress at same resource lookup scope, including both `Default` and `Dark` dictionaries where applicable. Do not copy a whole resource merely because value differs.
3. Keep upstream value/reference form for every included key. Do not translate Fluent brushes/colors to DevExpress look.
4. Include all theme dictionary variants required by a retained dynamic resource. A key needed in default and dark needs both variants.
5. Preserve resource declaration order and any converter/type namespace needed by retained entries.
6. `InvariantResources.axaml` must provide missing text strings, including `StringTextFlyoutCutText`, `StringTextFlyoutCopyText`, and `StringTextFlyoutPasteText`; existing DevExpress Undo/Delete/Select All strings remain unchanged.
7. Do not modify `MenuResources.axaml` because it deliberately pins menu-pack tokens against host themes. New general Fluent compatibility keys belong in `Accents/Fluent/*`, not its isolated menu resource contract.

Coordinator merges files in `ThemeRoot.axaml` in Fluent-compatible dependency order before `Controls/_index.axaml`:

1. `Accents/Fluent/BaseColorsPalette.axaml`
2. DevExpress system accent provider
3. DevExpress native resources and existing DevExpress `ThemeResources.axaml`
4. `Accents/Fluent/BaseResources.axaml`
5. `Accents/Fluent/FluentControlResources.axaml`
6. `Accents/Fluent/InvariantResources.axaml`
7. `Controls/_index.axaml`

Exact placement must preserve existing DevExpress key precedence. Verify merged dictionary lookup direction with a focused resource-resolution test; do not assume declaration order equals priority. If Fluent fallback keys would override DevExpress keys, reorder or split resource files so DevExpress values remain selected.

Do not import `DensityStyles/Compact.xaml` or Fluent `ColorPaletteResources*` classes unless manifest finds direct DevExpress public behavior/API requirement. Current DevExpress theme has neither Fluent's `DensityStyle` API nor palette collection property.

## Phase 5: Own Accent Detection

Add DevExpress-owned `Accents/SystemAccentColors.cs`, based on upstream `SystemAccentColors.cs` fetched directly from GitHub `main`; resolve and record fixed source SHA before editing. Preserve behavior:

- Publish seven keys: `SystemAccentColor`, dark shades `Dark1` through `Dark3`, light shades `Light1` through `Light3`.
- Default main color `#0078D7` (`0, 120, 215`) when platform settings/accent unavailable.
- Retrieve platform settings from `Application` or visual owner's platform settings.
- Subscribe while attached; unsubscribe while detached.
- Invalidate cached values and call `NotifyHostedResourcesChanged` when `PlatformColorValues` changes.
- Calculate shades through identical HSL lightness deltas.

Required adaptations:

- Namespace/type becomes DevExpress-owned, with visibility only as broad as XAML loading requires.
- Add fixed-SHA provenance header to source file.
- Instantiate provider from `ThemeRoot.axaml` in resource merge order equivalent to Fluent's `FluentTheme.xaml`.
- Do not copy Fluent `FluentTheme`, `ColorPaletteResources`, or density API into `DevolutionsDevExpressTheme`; only accent detection belongs here.
- Confirm existing DevExpress `DynamicResource SystemAccentColor*` bindings resolve and react to runtime OS accent changes.

## Phase 6: Wire-Up and Dependency Removal

Coordinator makes integration-only changes after agents finish:

1. Add all new control files to `Controls/_index.axaml`, one include per file, stable alphabetical grouping or documented upstream order. Ensure `DateTimePickerShared.axaml` loads before `DatePicker.axaml`/`TimePicker.axaml` when source dependency requires it.
2. Add resource layer includes and DevExpress accent provider to `ThemeRoot.axaml`.
3. Remove `<FluentTheme />` and fallback comment from `ThemeRoot.axaml`.
4. Remove `PackageReference Include="Avalonia.Themes.Fluent"` from DevExpress csproj.
5. Search DevExpress project for remaining `Avalonia.Themes.Fluent`, `<FluentTheme`, `avares://Avalonia.Themes.Fluent`, `using:Avalonia.Themes.Fluent`, and `blob/master` provenance. Expected result: none.
6. Update project trimming configuration only if build/publish analysis shows new XAML/code requires it. Existing descriptor preserves DevExpress assembly wholesale, so no change expected.

## Validation Gates

### Static Audit

- Every entry in upstream `FluentControls.xaml` classified in manifest.
- Every imported control listed in DevExpress `_index.axaml` exactly once.
- No duplicate default `ControlTheme` key/target introduced.
- Every `StaticResource`, `DynamicResource`, `BasedOn`, source URI, converter, control type, and compiled XAML namespace resolves without Fluent package.
- Existing DevExpress default setters/selectors unchanged outside explicitly appended provenance sections.
- All imported and appended source comments use their resolved source SHA, never `master`.

### Build and Tests

Run from repository root:

```bash
dotnet build src/Devolutions.AvaloniaTheme.DevExpress/Devolutions.AvaloniaTheme.DevExpress.csproj
dotnet test tests/Devolutions.AvaloniaControls.Tests/Devolutions.AvaloniaControls.Tests.csproj
```

Run a Release/trimming build because theme declares `IsTrimmable` and `IsAotCompatible`:

```bash
dotnet build src/Devolutions.AvaloniaTheme.DevExpress/Devolutions.AvaloniaTheme.DevExpress.csproj -c Release
```

Visual baselines may change because fallback controls now render from source-owned equivalent AXAML. Do not update baselines automatically. Review every diff. Update only intentional, explained diffs after user approval.

### Runtime Smoke Test

Build and launch SampleApp using DevExpress selection from binary directory:

```bash
dotnet build samples/SampleApp/SampleApp.csproj
cd samples/SampleApp/bin/Debug/net10.0 && dotnet SampleApp.dll
```

Verify light and dark variants:

- Existing DevExpress controls retain current appearance and keyboard/focus behavior.
- Newly owned Fluent controls instantiate without missing-resource warnings/errors.
- Flyouts, menus, date/time pickers, scrollbars, window decorations, file chooser, selection handles, refresh controls, tab strip, slider, split view, and notification manager load on platforms/features where available.
- Change OS accent while app runs, or simulate `PlatformColorValues` change in focused test. Verify all seven resource keys invalidate and DevExpress accent-bound brushes update.
- Open with `GlobalStyles="False"` and default `GlobalStyles="True"`; imported controls must not rely on opt-in global styles unless Fluent source explicitly did.

## Agent Completion Contract

Each Luna agent returns:

- Files changed and source files compared.
- Exact upstream permalink(s).
- Manifest items implemented and items intentionally left for another owner.
- New resource/type dependencies discovered.
- Command run plus result. If no build due parallel conflicts, state that precisely.
- No commit, staging, dependency upgrade, baseline update, or edits outside assigned files.

Coordinator performs final three-way review against GitHub `main` files at recorded source SHAs, current DevExpress baseline, and manifest before claiming completion.

## Risks and Decisions

- **Merge precedence risk:** Fluent compatibility keys can silently override DevExpress values. Gate resource order with runtime key-resolution test.
- **Template completeness risk:** Setter-only comparison misses template and pseudo-class behavior. Manifest records full semantic scopes.
- **Resource scope risk:** `StaticResource` resolves at XAML load; `DynamicResource` resolves at runtime. Preserve source form and test both variants.
- **Platform feature risk:** File chooser, window decorations, selection handles, refresh, and notification controls may be platform conditional. Compile all; smoke test available paths.
- **Parallel edit risk:** File-disjoint agents only. Coordinator alone integrates root/index/project changes.
- **Upstream drift risk:** Resolve one source SHA before each edit batch, record it in every changed file, and do not mix revisions within a batch.

## Confidence

95%. Fixed upstream revision, explicit control inventory, source architecture, resource loading path, and accent implementation identified. Main execution risk: resource precedence and semantic comparison for existing complex templates. Manifest-first workflow and build/runtime gates address it.
