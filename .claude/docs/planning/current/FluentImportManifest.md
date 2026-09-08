# DevExpress Fluent Import Manifest

GitHub source resolved from `main` before audit: `27c1ece36cbe17de3b8f95ae88223ba68702ae47`.

Source authority: https://github.com/AvaloniaUI/Avalonia/tree/main/src/Avalonia.Themes.Fluent/Controls

Every implementation agent must fetch raw source directly from GitHub at this SHA. Do not read a local NuGet package or cloned upstream checkout. Use this SHA in every provenance permalink.

## Import As-Is

Import these upstream files as new `Controls/*.axaml` files. Preserve contents except extension/include URI localization and top-of-file provenance comment. Do not add them to `_index.axaml`; integration owner handles index ordering.

```text
CommandBar             CarouselPage           ContentPage
DrawerPage             NavigationPage         TabbedPage
ProgressBar            Carousel               GroupBox
GridSplitter           ItemsControl           MenuScrollViewer
NotificationCard       OverlayPopupHost       PopupRoot
PathIcon               PipsPager              RepeatButton
SplitView              TableView              TableViewCell
TableViewColumnHeader  TableViewRow           TabStrip
TabStripItem           ToggleButton           ToggleSwitch
ToolTip                TransitioningContentControl
WindowNotificationManager  WindowDrawnDecorations
DateTimePickerShared   DatePicker             TimePicker
Slider                 ManagedFileChooser     SelectableTextBlock
RefreshContainer       RefreshVisualizer      ThemeVariantScope
HeaderedContentControl TextSelectionHandle
```

Keep GitHub upstream include order during later index integration. `DateTimePickerShared` precedes `DatePicker` and `TimePicker`; `MenuScrollViewer` precedes `Menu`; `TableViewCell` precedes `TableViewColumnHeader`.

Potential package/type gate: current project uses Avalonia `12.0.5` but GitHub `main` contains newer `TableView`, dialog, window-decoration, and other APIs. Import agent reports build-required packages/types; coordinator owns any project/version decision.

## Existing Themes

Existing DevExpress content wins. Add only listed append-safe source semantics. Do not add selectors for source parts absent from DevExpress template. All source permalink paths use `https://github.com/AvaloniaUI/Avalonia/blob/27c1ece36cbe17de3b8f95ae88223ba68702ae47/src/Avalonia.Themes.Fluent/Controls/<Control>.xaml`.

### Batch A

- `AdornerLayer.axaml`: append top-level focus metrics `SystemControlFocusVisualMargin`, `SystemControlFocusVisualPrimaryThickness`, `SystemControlFocusVisualSecondaryThickness`. Keep `DefaultFocusAdorner`.
- `AutoCompleteBox.axaml`: append `PlaceholderForeground`, plus `TextBox#PART_TextBox` placeholder and `ClearSelectionOnLostFocus` bindings. Keep local template/popup.
- `Button.axaml`: append `HorizontalAlignment=Left`, `RenderTransform=none`, render-transform transition, pressed `scale(0.98)`, access-key presenter setter. Keep DevExpress state layers.
- `ButtonSpinner.axaml`: append source missing main defaults and error border only after compatibility resource layer lands; append repeat-button min-width/pressed/disabled source states. Keep vertical template.
- `CheckBox.axaml`: no semantic append. Replace stale provenance only if agent changes file.
- `RadioButton.axaml`: no semantic append. Replace stale provenance only if agent changes file.

### Batch B

- `Calendar.axaml`: append `BorderBrush=CalendarViewBorderBrush`, `BorderThickness=CalendarBorderThickness`.
- `CalendarButton.axaml`: append `Margin=1`. Do not append missing `Border#Border` source selectors.
- `CalendarDatePicker.axaml`: append root placeholder/alignment setters only; do not add source `#Background` selectors or template.
- `CalendarDayButton.axaml`: append `Margin=1`; optional default source border thickness only if local template will consume it. No `#Border` source selectors.
- `CalendarItem.axaml`: append `FluentCalendarButton` navigation background and pointer/pressed presenter states. Keep main template.
- `DataValidationErrors.axaml`: add upstream named `TooltipDataValidationErrors` theme. Required by local DataGrid. Add `collections` namespace.

### Batch C

- `ComboBox.axaml`: append only upstream local resource keys currently absent: `ComboBoxTopHeaderMargin`, `ComboBoxPopupMaxNumberOfItems`, `ComboBoxPopupMaxNumberOfItemsThatCanBeShownOnOneSide`, `ComboBoxEditableTextPadding`, `ComboBoxMinHeight`. No source state/template selectors.
- `ComboBoxItem.axaml`: append pressed `ContentPresenter` source state. Pointer foreground only if no DevExpress conflict proven. Do not append selection/pointer backgrounds.
- `ListBox.axaml`: no effective append. Source scrollbar setters are template-inert.
- `ListBoxItem.axaml`: append pressed `ContentPresenter` state. Pointer foreground only if no DevExpress conflict proven. No selected/pointer backgrounds.
- `ScrollViewer.axaml`: append auto-hide content presenter `Grid.ColumnSpan=2`, `Grid.RowSpan=2`. Do not alter default separator choices. Report gesture binding insertion separately.
- `ScrollBar.axaml`: append source vertical/horizontal context-flyout resources and orientation `ContextFlyout` setters. Append foreground/border only after compatibility resources. Keep current supporting themes/templates.

### Batch D

- `ContextMenu.axaml`: set local `ScrollViewer Theme="{StaticResource DevExMenuScrollViewer}"` inside existing template. No generic Fluent resource.
- `Menu.axaml`: no source parity append expected.
- `MenuItem.axaml`: add `HorizontalMenuItem` based on `MenuTopLevelMenuItem`, using menu-pack-pinned resource keys. Do not base on `FluentTopLevelMenuItem`. Adapt only chevron open state to existing `PathIcon` if owned DevEx token exists.
- `MenuFlyoutPresenter.axaml`: no source parity append expected.
- `FlyoutPresenter.axaml`: replace self-referential Fluent `BasedOn` with source-owned template; retain DevExpress outer setters/styles.
- `Separator.axaml`: no source parity append expected.

### Batch E

- `DropDownButton.axaml`: append `HorizontalAlignment=Left`, pressed `RenderTransform=scale(0.98)`.
- `EmbeddableControlRoot.axaml`: name existing `VisualLayerManager` `PART_VisualLayerManager` after verifying source part contract.
- `Expander.axaml`: append `IsTabStop=False`, default `BorderThickness=ExpanderContentDownBorderThickness`. Keep DevExpress header template/animations.
- `HyperlinkButton.axaml`: append render-transform transition and pressed `scale(0.98)`.
- `Label.axaml`: no append.
- `NumericUpDown.axaml`: no safe append without template rewrite.
- `SplitButton.axaml`: append `HorizontalAlignment=Left`; append component access-key/state styles only after resources. Keep two-column DevExpress template.

### Batch F

- `TabControl.axaml`: append top-placement item presenter margin selector. Define `TabControlTopPlacementItemMargin` in local source dictionary if not owned by compatibility resource layer.
- `TabItem.axaml`: append direct source font/padding/min-height and non-pipe state semantics only after resource layer; do not add `PART_SelectedPipe` selectors.
- `TextBox.axaml`: add source mobile `ShowMode` values, `:touch-mode` flyout selector, toggle-button checked/indeterminate source states, optional placeholder foreground. Keep custom template/state logic.
- `TreeView.axaml`: no semantic append.
- `TreeViewItem.axaml`: append pointer/pressed/selected border/header foreground states only after resources. Retain DevExpress indent and chevron theme.
- `Window.axaml`: no semantic append.

## Compatibility Resources

Resource owner creates source-derived, minimal-key files under `Accents/Fluent/` from GitHub SHA:

- `BaseColorsPalette.axaml`
- `BaseResources.axaml`
- `FluentControlResources.axaml`
- `InvariantResources.axaml`

Do not copy full Fluent control resources. Existing DevExpress key/value wins. Required closure includes all source keys currently used by retained DevExpress templates, imported controls, calendar/control tokens, text flyout strings, scrollbar menu strings, tab/tree state tokens, and FlyoutPresenter defaults.

Do not change `MenuResources.axaml`, except coordinator-approved addition of `DevExMenu*` pinned tokens needed by `HorizontalMenuItem`.

## Accent Provider

Resource owner adds DevExpress-owned `Accents/SystemAccentColors.cs`, adapted from GitHub source SHA. Required seven system accent keys, HSL shade calculation, owner lifecycle, platform-change invalidation/notification.

## Integration Ownership

Coordinator alone edits `ThemeRoot.axaml`, `Controls/_index.axaml`, DevExpress `.csproj`, and manifest. Remove `<FluentTheme />` and package reference only after resource/control integration compiles.
