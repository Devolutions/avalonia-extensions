# Accent colour harmonization — LiquidGlass selection + classic accent freeze

*Written 2026-09-02, after PR #644 (LiquidGlass popup surface unification) merged.*
*Self-contained on purpose: written to survive a context reset. Everything needed is here.*

## Why

Two related problems, both about how the system accent reaches a selected row.

1. **LiquidGlass selection colour does not match Finder.** It reads washed out. Measured with the
   macOS accent set to Purple, it is wrong in both variants and wrong in *different directions*,
   which rules out a simple tweak.
2. **Classic never really followed the accent at all**, and the classic menu selection is a
   different colour from the ComboBox family.

---

## Part 1 — LiquidGlass selection colour

### Measured data (macOS accent = Purple)

System accent as reported per appearance: **light `#8B3689`**, **dark `#9C479B`**.

| | Light | Dark |
|---|---|---|
| Finder — menu selection | **`#B252B2`** | **`#A425A0`** |
| Finder — ComboBox popup selection | `#AA44AA` | `#A3259F` |
| Ours — menu *and* ComboBox popup | `#B979B8` | `#7C3073` |

Ours is identical for menus and drop-downs, which is expected and correct — both resolve
`AccentHighlight`. Finder's two differ slightly; **decision taken: target Finder's *menu* selection
for both.** One colour, simpler, and the difference is not worth two derivations.

### Oklch analysis (this is the important part)

Converted to Oklch (L, C, H):

| | L | C | H |
|---|---|---|---|
| accent light `#8B3689` | 0.4831 | 0.1551 | 328.58 |
| accent dark `#9C479B` | 0.5382 | 0.1553 | 327.91 |
| **target** light `#B252B2` | 0.5938 | 0.1722 | 327.47 |
| **target** dark `#A425A0` | 0.5175 | 0.2080 | 329.49 |
| ours light `#B979B8` | 0.6639 | 0.1158 | 327.32 |
| ours dark `#7C3073` | 0.4418 | 0.1354 | 332.46 |

Implied accent→target deltas, against the same figures for the default blue accent (`#007aff`,
targets `#528cfa` light / `#274fbe` dark, from PR #644):

| accent | light dL | light dC | dark dL | dark dC |
|---|---|---|---|---|
| purple | **+0.1107** | **+0.0170** | −0.0208 | +0.0527 |
| blue | **+0.0523** | **−0.0428** | −0.1327 | −0.0373 |

### Three conclusions

1. **A constant Oklch delta cannot work.** The light deltas alone differ by 0.058 in lightness and
   0.060 in chroma between the two hues. The current design — `OklchAdjustmentConverter` with fixed
   `LightnessAdjustment`/`ChromaAdjustment`/`HueAdjustment` per variant — was fitted against blue
   and cannot generalise. This is not a tuning problem; the model is wrong.

2. **The selection must become opaque.** Reproducing the light target through the current
   `0.64` opacity over the `#f8f9f9` surface requires a base colour **outside sRGB gamut** (one
   channel goes negative). It is arithmetically impossible. Dark at `0.69` needs `#de24d5`, which is
   in gamut but extreme. Going opaque makes the rendered colour *be* the brush colour, so the target
   is hit exactly and the surface stops diluting it. This also matches what PR #644 already did to
   the popup surface itself.

3. **In HSL, light mode fits a simple transformation.** This is the strongest lead and it is
   almost certainly what AppKit does:

   | | hue | saturation | lightness |
   |---|---|---|---|
   | purple light | −1.4° | **−5.6pp** | **+13.1pp** |
   | blue light | +8.0° | **−5.6pp** | **+15.1pp** |

   The saturation drop is *identical* and the lightness rise within 2pp, across a ~90° hue
   difference. Working model for light: **reduce saturation ~6pp, raise lightness ~14pp, keep hue**.
   The hue deltas disagree (−1.4 vs +8.0), which is either measurement noise from screenshots or a
   sign the assumed blue accent value is slightly off — worth resolving but small.

   Also consistent with this: the light Oklch *chroma* of both targets is nearly hue-independent
   (`C≈0.172` purple, `C≈0.175` blue), which is what a fixed saturation reduction looks like in
   Oklch.

   **Ruled out by the same data:**
   - *HSB/HSV* — saturation delta is −7.2 (purple) vs −32.8 (blue). Not consistent.
   - *Blending toward white* — solving per-channel alpha gives spreads of 0.19–0.21, so no single
     alpha reproduces it.
   - *A per-accent lookup table.* Considered, because macOS ships eight fixed accents and Apple
     could hand-tune each. The identical HSL saturation delta across two very different hues argues
     against it: a table would not land on the same number twice by chance.

4. **Dark mode is unresolved, and probably because of a bad input.** purple dark is S **+25.7** /
   L −5.1 while blue dark is S **−34.1** / L −7.1 — opposite saturation signs, which no single
   transformation produces. The blue *dark accent* used for that row was **guessed** (`#0a84ff`),
   since only the light value was ever recorded. Get the real per-appearance values before
   concluding anything about dark.

### Data gap to close before implementing

- **The single blocking unknown: the real per-appearance accent values.** The blue *dark* rows above
  use a guessed `#0a84ff`, and that guess is very likely why dark looks unmodellable. macOS reports
  different values per appearance (purple is `#8B3689` light vs `#9C479B` dark). Until dark accents
  are measured, do not attempt to fit dark.
- Two hues cannot distinguish "constant absolute L and C" from "L,C as a function of the accent".
  **Gather a third and ideally fourth accent** (green and red are furthest from purple/blue in hue;
  graphite is a useful degenerate case since its chroma is near zero and will expose any model that
  multiplies chroma).

For each accent, record: the system accent value **per appearance**, and Finder's **menu** selection
**per appearance**. Six numbers per accent.

### Steps

1. **Collect** the data above for 3–4 accents including graphite.
2. **Fit**, starting from the HSL lead in conclusion 3 rather than from scratch:
   - *(a)* **HSL: saturation −~6pp, lightness +~14pp, hue preserved.** Already fits both light data
     points. Confirm against a third accent, then pin the exact constants. Establish whether dark
     uses the same form with different constants once the real dark accents are known.
   - *(b)* If hue turns out to shift systematically, add a hue term — but only if a third accent
     supports it; two points cannot distinguish a hue rule from measurement noise.
   - *(c)* Oklch equivalents (`C` set absolutely, `L` offset) if HSL proves awkward to express — the
     two are near-equivalent here, and Oklch is better behaved for extreme accents.
   - Graphite is the discriminating case: its saturation is ~0, so "reduce saturation by 6pp" clamps
     to 0 and lightness alone carries the effect. Verify that still reads as a selection, and decide
     whether it needs a floor.
3. **Implement.** If *(a)* or *(b)* wins, `OklchAdjustmentConverter` is the wrong tool — it only
   offsets. Add a converter that *sets* L/C (e.g. `OklchTargetConverter`) rather than extending the
   existing one, so the offset behaviour stays available for other callers.
4. **Make the selection opaque** — drop the `0.64`/`0.69` opacities on `AccentHighlight`.
5. **Update the standalone pack.** It cannot run a converter, so its accent tokens are pinned
   literals; re-pin them to the new derivation's output for macOS's default accent (`#007aff`).
6. **Guard it.** `MacOsMenuPackContractTests` already pins `SystemAccentColor`; extend that pattern
   into a test that pins two or three accents and asserts the derived colour against the recorded
   Finder values. That is the only way this stays correct.

### Constraints — established in PR #644, do not re-litigate

- Popup surface is **opaque**: light `#f8f9f9`, dark `#22272a`. Sampled from native.
- `:selected` and `:focus-visible` are **deliberately not accented**. They use a neutral,
  `PopupRowSelectedBackgroundBrush` (`#d7d9d9` light / `#3d4243` dark), shared by menus and
  drop-down rows. Accenting them was tried and reverted: it makes a keyboard-focused row
  indistinguishable from a hovered one, and a submenu-open parent look as though the mouse were
  still on it. **Only the pointer-over state is accented.**
- Menus and drop-downs already share the hover route (`MenuItemPointerOverBackgroundBrush` and
  `ComboBoxItemPointerOverBackgroundBrush` both resolve `AccentHighlight`, declared **per variant**),
  so one fix covers every popup. Keep them per variant — a root-level `StaticResource` binds the
  Default (light) entry and cannot see `ThemeDictionaries`.
- The pack's parity check compares gradients **by type only**, so gradient parity is not verified.

---

## Part 2 — Classic

### 2a. Unify menu and drop-down hover

Two different classic-native brushes, both pre-existing, both accent-derived in intent:

| | brush | appearance |
|---|---|---|
| menus | `ControlBackgroundAccentMidBrush` = `SystemAccentColor` @0.63 | flat |
| drop-downs | `ControlBackgroundAccentRaisedBrush` = gradient of `AccentButtonTopColor`→`BottomColor` | gradient |

**Decision required: flat or gradient.**
- *Gradient* matches the classic look and was the initial preference, but the standalone menu pack
  pins solid literals and cannot express a gradient that parity would actually verify (see
  constraint above). Choosing gradient means accepting an unverified pack, or teaching the pack to
  pin a gradient literal and strengthening `DescribeToken` to compare stops.
- *Flat* is trivially expressible in the pack and already verified.

Recommendation: decide this **after** 2b, because 2b may change what the gradient even looks like.

### 2b. The accent freeze — IMPLEMENTED (branch `harmonize-menu-selection-colors`)

**Status: done.** Guarded by `tests/Devolutions.AvaloniaControls.VisualTests/MacOsClassicAccentTests.cs`
(12 tests: 5 brush families × both variants, plus a LiquidGlass-precedence guard). Non-visual suite
229/229. Classic visual baselines will move broadly — CI is the only signal, as expected.

**The mechanism was worse than described below.** The probe did not show a frozen colour; all five
aliases threw `KeyNotFoundException: Static resource 'SystemAccentColor…' not found`. `StaticResource`
resolves against the **parse-time dictionary scope**, which never contains a runtime-supplied platform
colour — so the alias cannot see the platform accent *at all*. In the running app it resolves only
because the Application-level chain happens to hold Avalonia's fallback accent at first build, which
is exactly why it captures `#0078d7` and can never move. Timing is a symptom; **scope is the cause**.
So the rule to carry forward is sharper than "resolves once": a `StaticResource` alias to a
platform-provided **`Color`** is unusable, while an alias to a **brush** whose own `Color` is a
`DynamicResource` is fine (the captured brush stays live) — that is the shape LiquidGlass's
`SystemAccent` family already uses.

**What shipped** (all in `Accents/ThemeResources.axaml` unless noted): five colour aliases per variant
replaced by per-variant brushes bound straight to `SystemAccentColor` / `…Light1` / `…Dark1` via
`DynamicResource` — `ControlBackgroundAccentRaisedBrush`, `ButtonBackgroundAccentRecessedBrush`,
`ControlBorderAccentBrush`, `ButtonBorderAccentBrush`, `CheckBoxCheckedPressedBackgroundBrush` (the
last also stops being a `Color` under a `…Brush` key). The matching root definitions were deleted, so
root-vs-theme-dictionary precedence never arises. Three root aliases had to move into both
dictionaries too — `ComboBoxButtonBackgroundBrush`, `ComboBoxButtonBorderBrush`,
`ComboBoxItemPointerOverBackgroundBrush` — or they would have bound the Default (light) brush for
dark: the recurring trap, one level up from the bug being fixed. `Controls/Expander.axaml` now strokes
the chevron with `ControlBorderAccentBrush`. The five intermediate colour tokens
(`AccentButtonTopColor`, `AccentButtonBottomColor`, `ControlBackgroundAccentRecessedTopColor`,
`ControlBackgroundAccentRecessedBottomColor`, `ControlBorderAccentColor`) were **removed** — noted as
breaking in the MacOS `CHANGELOG.md`.

**Blast radius, as built:** accent buttons, checkbox/radio fills *and* borders, the ComboBox button,
drop-down row hover, `DropDownButton`, `ListBoxItem` selection, and the Expander chevron. That is the
"most controls in classic do not follow" report, accounted for. **The menu pack is untouched** — none
of these are `MacOsMenu*` tokens, so 2b carries no pinned-literal churn and no pack-parity risk.

**Still to do manually:** live accent switch in classic, starting once in light and once in dark
(`/worksetup macosclassic`; no theme is active in `App.axaml` on master).

---

#### Original analysis, kept for the record

**Symptom (as observed):** whichever variant is active at startup is the broken one. Start in light →
light is frozen to the wrong accent and dark is correct; start in dark → dark is frozen and light is
correct. An earlier "light only" reading was just an artefact of always starting in light.

**Evidence:** with the accent forced to red, classic menus went red while the drop-down gradient
stayed `#269fff → #0078d7` — Avalonia's *fallback* accent (`#0078d7` and its `Light1` `#269fff`).

**Mechanism:** `AccentButtonTopColor` / `AccentButtonBottomColor` are declared inside the
`ThemeDictionaries` as `<StaticResource ResourceKey="SystemAccentColorLight1" />` **aliases**. A
`StaticResource` alias resolves **once**, when its dictionary is first materialised. Theme
dictionaries appear to be materialised lazily per variant: the variant active at startup is
materialised before the platform accent has arrived, so it captures the fallback permanently; the
other variant is materialised later, after the real accent is known, so it captures the right one.
That is exactly the reported light/dark symmetry.

Menus escape it because `ControlBackgroundAccentMidBrush` reads `{DynamicResource SystemAccentColor}`
directly — a live lookup, re-resolved on change.

**Fix:** take the `StaticResource` colour aliases out of the path. Declare
`ControlBackgroundAccentRaisedBrush` **per variant inside the `ThemeDictionaries`**, with its stops
bound via `DynamicResource` straight to `SystemAccentColorLight1` / `SystemAccentColor` /
`SystemAccentColorDark1`. Per variant is required because light and dark use different stops and a
root-level brush cannot vary by theme.

**Also audit** for the same pattern elsewhere: any `StaticResource` alias to a platform-provided
colour (`SystemAccentColor*`, `SystemRegionColor`, …) is frozen the same way. The user reports "most
controls in classic do not follow" the accent, so expect more than this one brush.

**Verification:**
- A test that changes `SystemAccentColor` *after* the theme has loaded and asserts the brush
  follows — reproducing the late-arriving-accent timing rather than pinning the accent up front.
- Manually: change the system accent while the app runs, starting once in light and once in dark,
  confirming both variants now follow.

**Blast radius:** every `ControlBackgroundAccentRaisedBrush` consumer in classic, which is many
controls, not just these popups. Classic visual baselines will move broadly. This is the reason it
was deliberately kept out of PR #644.

---

## Sequencing

1. ~~**2b first.**~~ **Done** — see the status block in 2b. It did not change the gradient's
   *intended* colours (same accent steps), it changed whether they arrive at all, so 2a's flat-vs-
   gradient decision is unblocked and unchanged.
2. **Part 1 next**, once the accent data is collected. Still blocked on the manual measurements: the
   real per-appearance accent values for 3–4 accents including graphite. Nothing in 2b closes that gap.
3. **2a last**, as a taste decision informed by both.

## Risks

- **Visual baselines.** Part 1 moves LiquidGlass popup baselines; 2b moves classic baselines widely.
  Both are expected and correct. Note visual regression cannot be run on this machine (baselines
  fail wholesale), so CI is the only signal.
- **Pack drift.** Every accent change needs the pack's pinned literals updated in the same commit,
  or `Pack_menu_tokens_match_the_full_theme` fails. That test is the safety net — keep it strict.
- **The recurring trap.** Across #643 and #644, every substantive bug in this area was *a variant- or
  platform-dependent value resolved in a scope that cannot see it*: `StaticResource` at a dictionary
  root, `StaticResource` as a `MultiBinding` source, `MergeResourceInclude` turning defaults into own
  entries, and now `StaticResource` aliases freezing a late-arriving platform value. **Suspect it
  first.**
- **Still outstanding from #644:** several menu tokens are consumed as
  `<Binding Source="{StaticResource MacOsMenu…}"/>` inside MultiBindings in `Menu.axaml`
  (`MacOsMenuItemPadding`, popup vertical offsets). `Binding.Source` cannot take a
  `DynamicResource`, so toolbar-style menus still use classic padding and offsets. Independent of
  this work, but the same family.
