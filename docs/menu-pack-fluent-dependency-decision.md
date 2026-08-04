# Open decision: should the Menu Pack be fully independent of Fluent?

*Companion to "The DevExpress Menu Pack — Purpose and Structure".*
*Current status: **deferred**. README prerequisite sharpened in the meantime.*

---

## 1. The dependency

The menu pack is self-contained except for **one** foreign resource reference:
Fluent's `FluentMenuScrollViewer` control theme, referenced from `MenuItem.axaml` (the scroll
container for menus long enough to overflow).

`Separator` is no longer part of this dependency discussion. The pack previously merged
`Separator.axaml` (implicit `{x:Type Separator}` theme), but that leaked styling out to non-menu
separators in the host app. The pack now does **not** merge `Separator.axaml`; instead it relies on
the host separator template and seals menu separators through scoped setters in
`Separator.styles.axaml`.

This was confirmed by audit, not assumption: it is the **only** `StaticResource` in the entire pack
that is neither `DevExMenu*`-owned nor defined locally. Everything else is already pinned.

## 2. Why it matters — the failure mode

If `FluentTheme` is not present, the pack does not degrade gracefully. It throws
**`InvalidCastException` during template construction**. Three properties make this unusually
hard to diagnose:

- It fires on **any** menu, including a single-item menu that can never scroll. So nothing about
  the symptom points at a *scroll viewer*.
- The exception message does not mention scrolling, Fluent, or resources.
- **Scoping `FluentTheme` to a subtree does not fix it.** Verified: `TryFindResource` from a
  descendant *does* return the `ControlTheme`, and the template build *still* throws. The
  requirement is strictly application-wide Fluent.

  (Best interpretation: the `StaticResource` inside our merged dictionary resolves in the
  dictionary's own scope, which cannot see a subtree-scoped theme. Stated as reading of the
  behaviour, not a verified mechanism.)

The practical risk is therefore not "menus look slightly off" but "an unfamiliar consumer hits an
undiagnosable exception and concludes the pack is broken."

## 3. Argument for removing it

Makes the pack genuinely self-contained — matching the stated goal of an *externally consumable*
pack — and works over Avalonia's Simple theme, or a standalone custom theme. Since the themes are
open source, we do not control what consumers base their branded sections on.

**A leak argument was initially advanced here and should be discounted.** The concern was that a
host theme restyling the scroll viewer would bleed into our menus. On checking: **0 of our 4 themes**
override `ScrollBarButtonArrowForeground`, `...PointerOver`, or `...IconFontSize`, despite heavily
customising ~20 other `ScrollBar*` keys. Modern Fluent scrollbars do not draw arrow buttons at all.
This is a dead corner and not a reason to act.

## 4. Argument for keeping it

**The packs are not distributed in isolation.** They ship as part of the Fluent-based platform
themes, as a companion to the global-styles opt-out for branded sections. In the primary consuming
app those sections are *Fluent islands* — Fluent is already loaded application-wide by construction.

So for the actual known consumer, removing the dependency is worth **exactly zero**. The benefit is
entirely hypothetical, accruing only to an external open-source consumer who bases their branded
sections on something other than Fluent.

App-wide Fluent is a reasonable, cheap, documentable prerequisite for a pack that is conceptually a
Fluent add-on in the first place.

## 5. What removal would involve

Vendor the control theme into our own theme as `DevExMenuScrollViewer`.

**Cost — verified, not estimated:**

| Aspect | Finding |
|---|---|
| Size | ~92 lines, one new file |
| Converter | `MenuScrollingVisibilityConverter` is **public in `Avalonia.Controls`**, not the Fluent assembly — no blocker, no reimplementation |
| Template stability | md5-identical across Avalonia 11.0.0, 11.2.0, 11.3.2. 12.0.2 adds a single setter (`IsScrollChainingEnabled="False"`) |
| Wiring | 3 reference sites in `MenuItem.axaml` |
| Proven? | Yes — a spike was built and confirmed: *"pack over SimpleTheme (no Fluent anywhere): OK"* |

Because the upstream template has been effectively frozen across four releases, ongoing maintenance
is close to zero.

**The spike is saved and ready to re-apply** (`MenuScrollViewer.axaml` + `wiring.diff`).

**Cost that does not scale for free:** MacOS references `FluentMenuScrollViewer` at 4 sites and Linux
at 3. Vendoring per-theme means N near-identical copies. The natural shared home would be
`Devolutions.AvaloniaControls`, but the pack currently has **zero** dependency on that assembly, and
introducing one to save ~92 duplicated lines is probably the worse trade. Recommendation would be to
accept the duplication.

## 6. Decision taken for now

**Keep the dependency; sharpen the documentation.**

The README prerequisite has been upgraded from "requires Fluent" to an explicit statement that
`FluentTheme` must be loaded **application-wide in `Application.Styles`, not scoped to a subtree**,
including a description of the `InvalidCastException` symptom so anyone who hits it can identify the
cause immediately. This is free and removes most of the practical risk regardless of what is decided
later.

## 7. Question for reviewers

Does anyone anticipate a consumer basing branded sections on something **other than** Fluent —
Simple, or a fully standalone theme?

- **No** → keep as is. The dependency costs nothing and the docs now cover the failure mode.
- **Yes / uncertain, and we want the packs to stand alone** → vendoring is cheap, proven, and low
  maintenance; the spike can be re-applied quickly.
