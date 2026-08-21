# Headless: `InvalidCastException` when two themes are loaded in one process

> Status: **Upcoming** — pre-existing issue found while building the MacOS menu pack
> (PR #624). Not caused by that work; parked for a dedicated session.

## Symptom

A headless test that shows a control styled by the **MacOS menu pack** on top of a
**non-MacOS host theme** throws while building a menu template:

```
System.InvalidCastException : Specified cast is not valid.
   at CompiledAvaloniaXaml.XamlDynamicSetters.<>XamlDynamicSetter_8(StyledElement, BindingPriority, Object)
   at CompiledAvaloniaXaml.!AvaloniaResources.XamlClosure_40.Build_2(IServiceProvider)
      src/Devolutions.AvaloniaTheme.MacOS/Controls/MenuPack.styles.axaml
   at Avalonia.Markup.Xaml.Templates.TemplateContent.Load[T](Object)
   at Avalonia.Controls.Primitives.TemplatedControl.ApplyTemplate()
   at Avalonia.Layout.Layoutable.Measure(Size)
```

## Confirmed pre-existing

Reproduced on **unmodified `HEAD`** (before the menu-pack typography/geometry fixes) by
stashing all local changes and re-running the probe. It is **not** a regression from the
menu-pack work.

## Notable characteristics

- Only occurs when **two themes are initialised in the same process** (host theme +
  the MacOS pack, which carries its own resources).
- **Which** host fails is order-dependent: whichever theme is initialised *second* in the
  process is the one that throws. Running each host in a separate test process makes both
  pass, so it looks like shared/static XAML state rather than markup that is simply wrong.
- The reported line number tracks edits to the menu files, i.e. it points at "whichever
  setter is being built" rather than one specific culprit. Candidates seen include the
  `ScrollViewer Theme="{StaticResource FluentMenuScrollViewer}"` setter — a plausible
  suspect, since `FluentMenuScrollViewer` is the one resource the pack deliberately
  inherits from the host, and its type could differ per host.

## Why it matters

- It blocks writing **render-based** cross-host tests for the menu packs. The current
  regression tests in `MacOsMenuPackContractTests` therefore assert the *style contract*
  structurally rather than measuring a live popup under two hosts, which is a weaker check.
- If it can happen outside headless, an app that switches themes at runtime (like the
  SampleApp) could hit the same cast.

## Suggested starting points

1. Reproduce minimally: headless test, add `FluentTheme` + `DevolutionsDevExpressTheme` +
   `MacOsMenuPack`, measure a `MenuItem`. Vary initialisation order.
2. Identify `XamlDynamicSetter_8` — decompile the generated XAML or bisect by commenting
   setters in `MenuItem.axaml` / `MenuFlyoutPresenter.axaml` to find which value has an
   unexpected runtime type.
3. Check `FluentMenuScrollViewer` specifically: confirm it resolves to a `ControlTheme` in
   every supported host, and consider whether the packs should own it instead of inheriting
   it (currently documented as the single inherited prerequisite).
4. Decide whether it is an Avalonia bug (worth an upstream repro) or a resource-typing
   problem on our side.

## Related

- PR #624 — MacOS menu pack (where this surfaced)
- `tests/Devolutions.AvaloniaControls.VisualTests/MacOsMenuPackContractTests.cs`
  — see `Pack_applies_menu_row_geometry_at_style_level`, which is structural *because* of
  this issue
