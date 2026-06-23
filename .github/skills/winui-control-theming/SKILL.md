---
name: winui-control-theming
description: "Create or update Devolutions.Avalonia.WinUI control templates, ControlThemes, and ThemeResources. Use when adding a new WinUI-styled control, porting Fluent overrides from another app, choosing WinUI brush/resource names, or deciding whether a resource belongs in ThemeResources.axaml or a control file."
---

# WinUI Control Theming

Use this skill when working on `src/Devolutions.Avalonia.WinUI/` and the goal is to add or refine WinUI-styled control themes while staying consistent with the resource strategy already used for this theme.

## Core Principle

Prefer **official WinUI resource names** over made-up local names whenever they can be identified from WinUI source or docs.

This theme should follow the WinUI model as closely as practical:

- **Shared WinUI semantic tokens** go in `Accents/ThemeResources.axaml`
- **Control-specific WinUI resources** go in the relevant file under `Controls/`
- **New local resource names** should be introduced only when no real WinUI equivalent exists

## Why

Avalonia's Fluent theme is a Fluent-looking base theme, but its resource surface does not always match the newer WinUI token vocabulary. For this theme, the goal is not just to make controls "look good"; it is to make them look WinUI-like **using WinUI concepts and names where possible**.

That keeps future control work consistent:

- later controls can reuse the same token set
- resource names stay recognizable to people familiar with WinUI
- ad-hoc local naming does not spread across the theme

## Required Workflow

When adding or updating a WinUI control theme:

1. **Start from the WinUI control**
   - Find the WinUI default template and the theme resources it uses.
   - Extract the actual resource names referenced by the template.

2. **Separate shared tokens from control-specific resources**
   - Examples of shared tokens: `TextFillColorPrimaryBrush`, `AccentFillColorDefaultBrush`, `ControlFillColorDefaultBrush`
   - Examples of control resources: `ButtonBackground`, `ButtonForegroundPointerOver`, `ButtonBorderBrushPressed`

3. **Compare against Avalonia Fluent**
   - Check the existing Avalonia Fluent control template and resource model.
   - Decide whether the control can be adapted with targeted overrides or needs a fuller template port.

4. **Port only what is needed**
   - Do not dump the full WinUI token set into `ThemeResources.axaml`.
   - Add only the resources that are actually referenced by the new or updated control theme, unless there is a clear near-term reason to add a small shared set together.

5. **Place resources in the right layer**
   - Put reusable cross-control WinUI tokens in `src/Devolutions.Avalonia.WinUI/Accents/ThemeResources.axaml`
   - Put control-only resources in the relevant `src/Devolutions.Avalonia.WinUI/Controls/*.axaml`
   - Keep `SampleAppBackground` and similar development-only resources clearly marked

6. **Prefer ControlTheme isolation**
   - Prefer `ControlTheme`-based overrides over broad global styles
   - Keep `GlobalStyles.axaml` empty or minimal unless a true bleed-out/global case is required

7. **Preserve fallbacks**
   - Keep Fluent as the fallback for anything not explicitly overridden
   - Reuse Avalonia/Fluent built-in resources when they already represent the right concept

## Naming Rules

Use these rules in order:

1. **Use the real WinUI name** if it exists and is identifiable
2. **Reuse an existing WinUI-named resource already present in this theme** if it matches the concept
3. **Use a control-scoped WinUI-style name** if the concept is specific to one control
4. **Invent a local name only as a last resort**

Avoid generic names that hide intent, such as:

- `PrimaryBrush`
- `HoverBrush`
- `WinUiButtonBrush`
- `CustomAccentBrush`

Prefer names that encode the WinUI concept, such as:

- `TextFillColorPrimaryBrush`
- `ButtonBackgroundPointerOver`
- `ControlStrokeColorSecondaryBrush`

## Porting From Other Avalonia Apps

When the source is another Avalonia app with Fluent overrides, such as UniGetUI:

1. Find the exact override location
2. Identify whether it is:
   - a selector style
   - a local resource token
   - a template replacement
3. Preserve the **visual intent** and **resource naming strategy**
4. Convert selector-based app styles into isolated `ControlTheme` definitions where appropriate for this repository
5. Audit the imported resources afterward and remove any that are not actually referenced

Do not assume the app's entire resource file belongs in this repository. Import the minimum coherent subset.

## Resource Audit Checklist

After adding or changing a control:

- List all resources referenced by the new control theme
- Confirm each new resource is actually used
- Confirm each shared resource belongs in `ThemeResources.axaml` rather than the control file
- Remove speculative tokens that were added "for later"
- Keep comments short and factual, especially around development-only resources

## Repository-Specific Notes

- The WinUI theme lives in `src/Devolutions.Avalonia.WinUI/`
- `ThemeRoot.axaml` should continue loading Fluent as fallback
- `Controls/_index.axaml` should include each committed control resource file
- `Accents/ThemeResources.axaml` should stay intentionally small and grow only as new control themes require it
- `GlobalStyles.axaml` should remain empty unless a genuine global styling requirement appears

## Good Outcome

A good change in this theme does all of the following:

- makes the control more WinUI-like
- uses WinUI resource names where possible
- keeps shared and control-local resources separated correctly
- avoids unnecessary token bloat
- preserves Fluent fallback behavior
