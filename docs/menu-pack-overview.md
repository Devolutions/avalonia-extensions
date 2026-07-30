# The DevExpress Menu Pack — Purpose and Structure

*Status: implemented for DevExpress only. Other platform themes deferred until this one is confirmed.*

---

## 1. The problem it solves

Our platform themes (DevExpress, MacClassic, Linux, WinUI, LiquidGlass) are designed to make an app
look native to the platform it runs on. But a consuming app often has **branded sections** that
deliberately opt out of the platform look — internally these are called *"Fluent islands"*: plain
`FluentTheme` plus local style rules, rather than the platform-specific theme.

That opt-out creates an inconsistency. A branded section still has menus — context menus, menu bars,
flyouts — and those menus suddenly look different from every other menu in the app. Menus are
chrome, not branding; users read them as part of the application shell, so they should stay
consistent even where the surrounding content does not.

**The menu pack is the fix:** an individually addressable style pack that applies one theme's menu
styling to a section, without dragging in that theme's full control set.

So the pack is best understood not as a standalone product, but as a **companion to the
global-styles opt-out** — the piece that lets a section drop the platform theme for its content
while keeping the platform theme for its menus.

## 2. What it covers

`ContextMenu`, `MenuFlyoutPresenter`, `Menu`, `MenuItem`, `Separator`, plus the menu-specific helper
styles (`Menu.styles.axaml`, `Separator.styles.axaml`, and the `MenuItem` SVG icon CSS).

Consumed with a single line:

```xaml
<Application.Styles>
  <FluentTheme />   <!-- prerequisite; must be application-wide -->
  <StyleInclude Source="avares://Devolutions.AvaloniaTheme.DevExpress/Controls/MenuPack.styles.axaml" />
</Application.Styles>
```

## 3. The central design problem

The pack gets layered over a **host theme we do not control**. That creates a two-way hazard:

- **Leak in** — the host redefines a generic key our menus depend on, and our menus change shape.
  This was observed for real: loading the pack over the MacOS theme made menu outlines disappear.
- **Leak out** — our resources override host keys and restyle the host's non-menu controls. Also
  observed: a `ComboBox` placed inside a pack-styled context menu rendered with the wrong chevron.

The root cause was the same in both directions: menu templates were resolving through **generic,
widely-owned keys** (`MenuFlyoutPresenterBorderThemeThickness`, `FlyoutThemeMaxWidth`,
`MenuFlyoutSeparatorThemeHeight`, …). Those keys mean "whoever won the resource lookup", which is
exactly the wrong semantics when two themes are in the same visual tree.

An earlier iteration also duplicated the token definitions — one copy for the full theme, one for
the pack. That dual source of truth caused two separate bugs before it was removed.

## 4. The architecture

Three rules, in order of importance:

### Rule 1 — One source of truth

All menu tokens live in **`Accents/MenuResources.axaml`**, included by *both* the full theme
(via `ThemeRoot.axaml`) and the pack (via `MenuPack.styles.axaml`).

```
                  Accents/MenuResources.axaml        <-- 36 DevExMenu* tokens
                     /                    \
        ThemeRoot.axaml              MenuPack.styles.axaml
        (full theme)                 (external consumers)
```

Drift between the pack and the full theme is now **structurally impossible**, rather than something
a reviewer has to remember to check. This directly addresses the maintenance concern that motivated
the redesign.

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
every value is a literal. Getting this right required probing the real Fluent values at runtime
rather than reading them off — three of the initial guesses were wrong (padding `0` not `1`,
max width `456` not `816`, horizontal min width `32` not `128`).

### Rule 4 — The pack styles menu content only, never the host's own controls

Leakage runs both ways, and the outward direction is easy to miss. A `ControlTheme` keyed
`x:Key="{x:Type Foo}"` is an *implicit* theme: merging it makes it the default for **every** `Foo` in
the consuming app, with no selector involved.

The pack originally merged `Separator.axaml` for exactly this reason and it caused a real
leak-out — a plain `<Separator/>` outside any menu rendered opaque black and full-bleed
(`0,14,400,1`) instead of the host's subtle inset line (`12,18,376,1`), because the theme's
unprefixed `SeparatorBrush` / `SeparatorMargin` values ship only with the full theme.

The fix is not to duplicate the template but to **rely on the host's and pin what it binds**.
Fluent's `Separator` template turns out to be copied verbatim by our DevExpress, MacOS and Linux
themes — all four are character-for-character identical, differing only in which resource keys the
setters reference. So the pack drops the implicit theme entirely and instead pins every
template-bound property (`Background`, `Margin`, `Height`, `HorizontalAlignment`,
`VerticalAlignment`, `BorderThickness`, `CornerRadius`, `Focusable`) inside the existing
menu-scoped selector in `Separator.styles.axaml`.

Measured result — menu separators are byte-identical over Fluent, MacOS, Linux, and a deliberately
hostile host that sets magenta at 9px with a 5px lime border and 12px corner radius; meanwhile plain
separators over each host are byte-identical to that host *without* the pack. This also removes a
duplicated Fluent template, satisfying the project's "no template duplication" requirement.

### What is *deliberately* inherited from the host

Only one dependency is intentionally inherited:

| Key | Rationale |
|-----|-----------|
| `FluentMenuScrollViewer` | Technical dependency only: reused rather than vendored (for now). See the companion decision document. |

This list is the explicit contract. Anything *not* on it and not `DevExMenu*`-prefixed is a bug.

**Typography was originally on this list, and was moved off it.** Inheriting `DefaultFontFamily` /
`ControlFontSize` from the host directly contradicted §1: DevExpress sets size 12 and its own family,
MacOS 13, Linux Open Sans, and plain Fluent defines neither — so a branded section rendered its menus
in the *branded* font, visibly different from the platform-themed menus they were meant to match.
`DevExMenuFontFamily` / `DevExMenuFontSize` / `DevExMenuFontWeight` are now pinned like everything
else.

Pinning them also revealed a second gap: the `{x:Type MenuItem}` theme pinned `FontSize` but not
`FontFamily`, and the popup Border pinned all three — so *menu-bar* items still inherited the host
font while popup items did not. With both closed, a top-level menu item now measures **37×23 over
Fluent, MacClassic and Linux alike** (it was 38×20 / 37×23 / 38×20 before). Menu geometry is
host-invariant, which is what makes the cheap test strategy in §5 possible.

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

### Visual coverage — planned approach

The demo pages are not yet in the visual regression matrix. The obvious approach — a baseline per
page per host — would cost 3 pages × 4 hosts per pack, and triple again once the MacOS and Linux
packs land.

Because menu geometry is now host-invariant (§4), that matrix is avoidable. Two **differential**
assertions cover the real invariants and need *no stored baselines at all*:

- **leak-out:** render a row of ordinary controls with and without the pack over the same host;
  assert the two captures are pixel-identical. Scales to N packs × N hosts for free. The
  `Separator` case of this is **already implemented** (test 3 above) as a property-level assertion;
  extending it to a pixel comparison over a control row is the natural next step.
- **leak-in:** render each menu page over two different hosts; assert identical. This is only
  meaningful because typography is pinned.

`ImageComparer.CompareImages` already operates on file paths, so both are cheap to build.

Note that these two invariants pull in opposite directions — menus must look the *same* across hosts,
host controls must keep looking *different*. They therefore belong on separate demo pages; mixing
menu content and a host-control row on one page makes neither assertion expressible.

## 6. Proving it in the SampleApp

Four dedicated demo pages under a `MenuPack` heading (`MenuPackAbout`, `MenuPackMenuDemo`,
`MenuPackContextMenuDemo`, `MenuPackMenuFlyoutDemo`). These load the pack **by avares URI**, not via
the theme object — so the sample exercises the same path an external consumer would.

Separate pages, rather than additions to the existing demo pages, because several of those are
already full and extra content risked pushing controls off-screen and disturbing visual baselines.

## 7. Current status

- Build clean; `dotnet test` **164/164**; all 145 visual baselines unchanged.
- Two real bugs fixed along the way: a `KeyNotFoundException` crash when the pack was used over a
  bare Fluent host (i.e. the documented consumer scenario), and a separator-height leak-in.

**Known gaps:**

- The MenuPack demo pages have no `status` entry in the page catalog, so they are **not** covered by
  visual regression testing. Adding coverage means 4 pages × N themes of new baselines.
- Packs for MacOS / Linux / WinUI are not built yet.
- The `FluentMenuScrollViewer` dependency — see the companion decision document.

## 8. Files

**Added**
- `src/Devolutions.AvaloniaTheme.DevExpress/Accents/MenuResources.axaml`
- `src/Devolutions.AvaloniaTheme.DevExpress/Controls/MenuPack.styles.axaml`
- `tests/Devolutions.AvaloniaControls.VisualTests/MenuPackContractTests.cs`
- `samples/SampleApp/DemoPages/MenuPack*.axaml` (+ `.axaml.cs`) — 4 pages

**Modified**
- `Accents/ThemeResources.axaml`, `ThemeRoot.axaml`
- `Controls/MenuItem.axaml`, `Menu.axaml`, `MenuSvg.styles.axaml`, `Separator.styles.axaml`
- `README.md`

**Removed**
- `Accents/MenuPackThemeResources.axaml` (the duplicate token set)

**URI:** `avares://Devolutions.AvaloniaTheme.DevExpress/Controls/MenuPack.styles.axaml`
