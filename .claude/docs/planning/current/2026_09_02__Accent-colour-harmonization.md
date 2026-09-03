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

## Part 1 — LiquidGlass selection colour — IMPLEMENTED

**Outcome: a deliberate approximation, not a match to Finder.** Decision taken by xfortin once the
model search came up empty. Hue preserved, chroma **capped** (not set, so a graphite accent stays
grey), lightness offset. Light `cap 0.162 / +0.055`, dark `cap 0.170 / -0.140`. The selection is now
opaque. Guarded by `MacOsAccentSelectionTests`, which pins both the exact literal the pack carries
and the dE tolerance against the recorded native colours.

**No new code at all in the end.** The change is: drop the brush opacity, and change the two
`LightnessAdjustment` values. Hue and chroma pass through untouched.

Getting there went through two dead ends worth recording. A separate converter was written first
(duplicating the whole class to change four lines of arithmetic), then collapsed into a `ChromaCap`
property on the existing converter, then dropped entirely once the models were scored against each
other: capping chroma buys ~0.01 mean dE over a lightness shift alone — about half a
just-noticeable difference — and is not worth new API. Scores against Finder's drop-down target:

| model | light mean/worst | dark mean/worst |
|---|---|---|
| raw accent, unchanged | 0.043 / 0.063 | **0.107** / 0.129 |
| **lightness offset only (shipped)** | 0.037 / 0.048 | 0.040 / 0.077 |
| chroma cap only | 0.036 / 0.053 | 0.105 / 0.123 |
| both | 0.026 / 0.042 | 0.031 / 0.077 |

The dark lightness offset is the one indispensable part: native dark selection is much darker than
the accent, so anything without it lands at dE ~0.107.

### What the measurements settled

The earlier data was wrong at the input: the accent column had been sampled from the **swatch circles
in System Settings**, which are artwork, not the value macOS reports to an app. Re-read from
Avalonia's resolved `SystemAccentColor` (via the hex readout added to the SampleApp SystemColours
page), the accents are:

| accent | light | dark | Finder menu light | Finder menu dark |
|---|---|---|---|---|
| purple | `#953D96` | `#A550A7` | `#B252B2` | `#A425A0` |
| red | `#E0383E` | `#FF5257` | `#E05D5C` | `#C03737` |
| green | `#62BA46` | `#62BA46` | `#65BA5C` | `#358B2A` |
| blue (default) | `#007AFF` | `#007AFF` | `#5D93FB` | `#254BB7` |

Note the default blue is `#007AFF` — the plan's original value was right; `#286AFA` was the swatch,
and the `#0a84ff` dark guess was never needed since blue reports the same value in both appearances.

**Every simple model is ruled out**, measured across four accents with correct inputs:

- constant Oklch offsets, and constant HSL offsets — the original "saturation −5.6pp" lead was an
  artefact of the swatch-sampled accents and disappears with correct ones
- compositing over any single surface, in sRGB **and** linear light — a joint fit of alpha and
  surface wants an implausible pink `#EDBACF` and still leaves RMS 15/255
- fixed-alpha Oklab blending — the chroma ratio varies 0.76–1.32
- sRGB gamut limiting — C/maxC spans 0.57–0.96
- Display P3 blending toward white/black — RMS 12–21/255

**What held:** hue is preserved (+0.3° to +7.7°) and target chroma is near-constant (light
0.164 ± 0.018; dark 0.169 ± 0.019 excluding purple). That is what the converter reproduces.

**There is no formula to find.** Apple ships hand-picked per-accent constants — `AppleHighlightColor`
stores explicit RGB per accent while `AppleAccentColor` is only an index (−1…7, absent =
Multicolor) — and exposes variants through opaque `NSColor` APIs. WWDC 2018 explicitly warns against
applying a constant darkening to a base colour because it only holds in one appearance, which is
exactly the failure mode of the offsets this replaced.

**Data caveat:** Finder's dark menus are translucent, so a measured dark target carries some of
whatever sat behind the menu. That is the likeliest reason dark fits worse and why purple dark
(chroma 0.208 against a 0.169 group) is the worst case.

**Decision revisited:** menu and drop-down selections *do* differ natively and both were measured,
but xfortin chose to keep a single colour — the menu value — for both surfaces, as originally
planned.

---

### Original analysis, kept for the record

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
**per appearance** — four hex values per accent.

#### Capture protocol (agreed 2026-09-02)

The two measurement traps that would silently invalidate a fit:

1. **Digital Color Meter must report sRGB, not display-native.** Set *View → Display Values →
   Hexadecimal* and the colour space to **sRGB**. "Display native values" returns numbers in the
   monitor's space, which will not match what the theme computes and will look like a bad model.
2. **Do not eyedrop the accent off a native AppKit control.** AppKit draws accent surfaces with
   gradients and overlays, so the sampled pixel is not the accent. Read the accent from the
   SampleApp's *SystemColours* experiment page instead (`SystemAccentColor` row): that swatch is a
   flat fill of Avalonia's resolved value, which is exactly the input the converter receives.

Per accent, with the accent set in System Settings → Appearance:

- Set Appearance to **Light**, then: sample the `SystemAccentColor` swatch on the SystemColours page,
  and sample the **middle** of a hovered Finder menu row (open any Finder menu, hover an item; stay
  away from text, glyphs and the rounded ends).
- Switch Appearance to **Dark** and repeat both. macOS reports a *different* accent per appearance,
  so both halves are required.

Accents worth having, in priority order:

| accent | why |
|---|---|
| **blue dark** | The one actually blocking dark. The existing blue-dark row is a **guess** (`#0a84ff`) and is the likeliest reason dark looks unmodellable. |
| **graphite** | The discriminating case: chroma ≈ 0, so any model that *scales* chroma is exposed immediately, and "reduce saturation ~6pp" clamps to 0 with lightness carrying the whole effect. |
| **green or red** | Furthest in hue from purple and blue, so it tests whether the light constants really are hue-independent. |

Template to fill in:

| accent | accent light | accent dark | Finder menu selection light | Finder menu selection dark |
|---|---|---|---|---|
| blue | `#007aff` | ? | `#528cfa` | `#274fbe` (from #644, re-check) |
| graphite | ? | ? | ? | ? |
| green/red | ? | ? | ? | ? |
| purple | `#8B3689` | `#9C479B` | `#B252B2` | `#A425A0` |

**Independently re-verified 2026-09-02** (all figures in the tables above reproduce to 4 dp): the
Oklch values, the identical −5.6pp HSL saturation delta on both light points, and the gamut result —
light needs a base with G = −11.9 at 0.64 opacity, so it is impossible, while dark needs
≈`#de24d5`, in gamut but extreme. The one figure that did not reproduce is blue *dark*
(recomputed dL −0.154 / dC −0.025 against the plan's −0.133 / −0.037), which is moot because that
row rests on the guessed accent.

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
- ~~The pack's parity check compares gradients **by type only**, so gradient parity is not
  verified.~~ Fixed in 2a: `DescribeToken` now compares stops and, for linear gradients, start/end
  point. Note the classic pack tokens follow `SystemAccentColor` through `DynamicResource`; only the
  **LiquidGlass** pack pins literals, because only it needs the converter.

---

## Part 2 — Classic

### 2a. Unify menu and drop-down hover — IMPLEMENTED

**Decision: gradient.** xfortin confirmed the drop-down gradient is the right classic look, so the
menus moved to it. Classic menu hover and drop-down row hover are now the same brush,
`ControlBackgroundAccentRaisedBrush`. What menus lost was `ControlBackgroundAccentMidBrush`
(`SystemAccentColor` @0.63, flat), which now has no consumers.

`MenuItemPointerOverBackgroundBrush` had to move from the dictionary root into both
`ThemeDictionaries`, because the gradient it points at is per variant — the same relocation 2b forced
on the ComboBox tokens.

**One premise in the original plan was wrong, and it was the premise blocking "gradient":** the
classic pack does *not* pin solid literals. `MenuResources.axaml` already declared
`MacOsMenuItemPointerOverBackgroundBrush` as `{DynamicResource SystemAccentColor}` @0.63 — only the
*LiquidGlass* pack pins literals, because it cannot run the `OklchAdjustmentConverter`. Classic can
express the gradient with `DynamicResource` stops directly, so there was no need to pin a gradient
literal and nothing to accept as unverified.

The other half of that constraint was real and is now closed: `DescribeToken` in
`MacOsMenuPackContractTests` compared gradients by type only, so any two gradients matched. It now
describes stops (offset + rgba) and, for linear gradients, start/end point — so
`Pack_menu_tokens_match_the_full_theme` genuinely verifies that the pack's gradient equals the
theme's. Without that, the pack could drift to different colours or a different direction and parity
would still pass.

**Still to do manually:** confirm the harmonized hover reads correctly in classic, light and dark.

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

## Known upstream issue: the accent input goes stale

**Avalonia captures `SystemAccentColor` at startup and never refreshes it when the macOS appearance
changes.** Confirmed by readout, symmetrically:

| | reported accent | correct for that appearance |
|---|---|---|
| dark on open | `#FF5257` | yes |
| light **on switch** | `#FF5257` | no - should be `#E0383E` |
| light on open | `#E0383E` | yes |
| dark **on switch** | `#E0383E` | no - should be `#FF5257` |

Located precisely: `ColorValuesChanged` **does** fire on the appearance change and carries the
**correct** `ThemeVariant`, but an **unchanged** `AccentColor1`; the `SystemAccentColor` resource
always equals the platform value. So `FluentTheme` re-derives correctly and is simply never handed a
new accent - the gap is in the macOS backend, upstream of anything here.

Only some accents expose it: macOS reports one value for both appearances for blue and green, and
different values for red (`#E0383E` / `#FF5257`) and purple (`#953D96` / `#A550A7`).

**Why it matters:** macOS's *Auto* appearance switches at sunrise/sunset, so a long-running app ends
up deriving from the wrong accent for part of the day with no user action. It affects every
per-variant accent derivation - selection, focus ring, accent wash - not just the selection colour.

Tracked at https://support.avaloniaui.net/support/tickets/1857. Minimal repro app: `~/Desktop/AvaloniaAccentStaleRepro` (shows Avalonia's value
against `NSColor.controlAccentColor` resolved per appearance via AppKit, read live).

**Workaround if upstream does not land:** resolve `controlAccentColor` ourselves for
`NSAppearanceNameAqua` and `NSAppearanceNameDarkAqua` and publish each into its own theme dictionary.
Then no refresh is needed at all - both values are correct up front, whichever appearance the app
launched in. `WallpaperTintApplier` already establishes the P/Invoke pattern in this assembly, and the
approach is proven to work (it is what the repro app uses). Note classic's gradient also consumes
`SystemAccentColorLight1`/`Dark1`, which Avalonia derives, so those would need deriving per variant
too.

---

## Sequencing

1. ~~**2b first.**~~ **Done** — see the status block in 2b. It did not change the gradient's
   *intended* colours (same accent steps), it changed whether they arrive at all, so 2a's flat-vs-
   gradient decision is unblocked and unchanged.
2. ~~**Part 1 next**, once the accent data is collected.~~ **Done** — four accents measured from
   Avalonia's resolved values, model search closed, approximation implemented. Graphite was never
   measured in the end; the chroma **cap** makes it degrade correctly by construction (a grey accent
   keeps its own near-zero chroma) and that is pinned by test rather than by measurement.
3. ~~**2a last**, as a taste decision informed by both.~~ **Done** — gradient chosen; see 2a. It
   turned out not to depend on Part 1 at all, so it did not need to wait for the accent data.

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
