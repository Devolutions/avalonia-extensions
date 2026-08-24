Menu-pack-overview - my shortened version


# The DevExpress Menu Pack — Purpose and Structure

*Status: implemented for DevExpress only. Other platform themes deferred until this one is confirmed.*

---

## 1. The problem it solves

Our platform themes (DevExpress, MacClassic, Linux, LiquidGlass) are designed to make an app
look native to the platform it runs on. But a consuming app often has **branded sections** that
deliberately opt out of the platform look — e.g. RDM *"Fluent islands"*: plain
`FluentTheme` plus local style rules, rather than the platform-specific theme.

That opt-out creates an inconsistency. A branded section still has menus — context menus, menu bars,
flyouts — and those menus suddenly look different from every other menu in the app. Menus are
chrome, not branding; users read them as part of the application shell, so they should stay
consistent even where the surrounding content does not.

**The menu pack is the fix:** an individually addressable style pack that applies one theme's menu
styling to a section, without bringing in that theme's full control set.

So the pack is not meant as a standalone product, but as a **companion to the
global-styles opt-out** — the piece that lets a section drop the platform theme for its content
while keeping the platform theme for its menus.

## 2. What it covers

`ContextMenu`, `MenuFlyoutPresenter`, `Menu`, `MenuItem`, `Separator`, plus the menu-specific helper
styles (`Menu.styles.axaml`, `Separator.styles.axaml`, and the `MenuItem` SVG icon CSS).

Consumed with a single line:

```xaml
<Application.Styles>
  <FluentTheme />   <!-- Fluent or Fluent-based theme prerequisite; must be application-wide -->
  <StyleInclude Source="avares://Devolutions.AvaloniaTheme.DevExpress/Controls/MenuPack.styles.axaml" />
</Application.Styles>
```

## 3. The central design problem

The pack gets layered over a **host theme we do not control**. That creates a two-way hazard:

- **Leak in** — the host redefines a generic key that our menus depend on, and our menus change shape.
- **Leak out** — our resources override host keys and restyle the host's non-menu controls. 

The root cause is the same in both directions: menu templates are resolving through **generic,
widely-owned keys** (`MenuFlyoutPresenterBorderThemeThickness`, `FlyoutThemeMaxWidth`,
`MenuFlyoutSeparatorThemeHeight`, …). Those keys mean "whoever won the resource lookup", which is
exactly the wrong semantics when two themes are in the same visual tree. We solve this by using a unique prefix.

## 4. The architecture

Three rules, in order of importance:

### Rule 1 — One source of truth

All menu tokens live in **`Accents/MenuResources.axaml`**, included by *both* the full theme
(via `ThemeRoot.axaml`) and the pack (via `MenuPack.styles.axaml`), so there will be no drift between 
the pack and the full theme

```
                  Accents/MenuResources.axaml        <-- 36 DevExMenu* tokens
                     /                    \
        ThemeRoot.axaml              MenuPack.styles.axaml
        (full theme)                 (external consumers)
```

### Rule 2 — Every key the pack owns is prefixed `DevExMenu`

A prefixed key cannot collide with, or be silently overridden by, the host's keys. This includes
keys that happen to be unique today (`SvgMenuItem*Css`, `MenuItemChevronPathData`,
`KeyGestureConverter`) — uniqueness by coincidence is not a contract, and a future host theme could
introduce any of them.

Prefix choice: `DevExMenu` rather than `MenuPack`. When the other platform themes get packs, a
shared `MenuPack*` prefix would collide between them — most visibly in our own SampleApp, which
loads several themes in one process. The prefix names the *vendor*, not the *contract*.

### Rule 3 — Values are pinned literals, never aliases

A token defined as `{StaticResource FlyoutThemeMaxWidth}` re-opens the leak Rule 2 just closed. So
every value is a literal. 


### What is *deliberately* inherited from the host

None. The pack owns all menu template dependencies, including the vendored
`DevExMenuScrollViewer` and the arrow resources it pins locally.
Anything not `DevExMenu*`-prefixed is a bug.


## 5. How it is guarded

`MenuPackContractTests` (visual test project), 21 cases across four tests:

1. **Token parity** — every `DevExMenu*` token resolves *identically* under the full theme and under
   "pack over a foreign host", in both Light and Dark. This is what enforces Rule 1.
2. **Hostile-host leak guard** — a host theme that deliberately redefines the generic keys our menus
   used to depend on cannot move any menu token. This enforces Rules 2 and 3.
3. **Leak-out guard** — adding the pack over a host must not change a `Separator` that is outside any
   menu. This enforces Rule 4, and is *differential*: it compares host-only against host-plus-pack
   and so needs no stored baseline.
4. **Page-construction smoke** — the MenuPack demo pages build over DevExpress, MacClassic,
   LiquidGlass, and Linux hosts.

The `MenuTokens` array in that file is the machine-readable contract; new tokens must be added to it.

Tests 1–3 were each **verified to have teeth** by temporarily reintroducing the bug they guard and
confirming a loud failure with a readable diff, rather than trusting that green meant covered.

## 6. Proving it in the SampleApp

Four dedicated demo pages under a `MenuPack` heading (`MenuPackAbout`, `MenuPackMenuDemo`,
`MenuPackContextMenuDemo`, `MenuPackMenuFlyoutDemo`). These load the pack **by avares URI**, not via
the theme object — so the sample exercises the same path an external consumer would.

## 7. Current status

- Build clean; `dotnet test` **164/164**; all 145 visual baselines unchanged.
- Two real bugs fixed along the way: a `KeyNotFoundException` crash when the pack was used over a
  bare Fluent host (i.e. the documented consumer scenario), and a separator-height leak-in.

**Known gaps:**

- Packs for MacOS & Linux are not built yet.
- Historical discussion of the removed Fluent dependency — see the companion decision document.


**URI:** `avares://Devolutions.AvaloniaTheme.DevExpress/Controls/MenuPack.styles.axaml`
