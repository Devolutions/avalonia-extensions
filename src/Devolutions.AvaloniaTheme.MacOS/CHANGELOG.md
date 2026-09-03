# Devolutions.AvaloniaTheme.MacOS

**NOTE:** This theme is still in active development and we are currently not maintaining a detailed change log.
Please see commits if you're curious. However we will do our best to call out key changes in this log.

## Unreleased

- Fixed: in the classic variant, accent-derived surfaces did not follow the system accent. Accent
  buttons, checkbox/radio fills and borders, the ComboBox button, drop-down row hover, the
  DropDownButton, ListBoxItem selection and the Expander chevron were pinned to Avalonia's fallback
  accent (`#0078d7`) instead of the platform accent.
- Fixed: the LiquidGlass selection highlight read washed out, most visibly on drop-down rows in the
  light variant. The brush was translucent, and at 0.64 opacity over a near-white popup surface an
  accent's chroma is diluted toward the surface - measured across four accents the result was both
  lighter and less chromatic than native every time (blue chroma 0.155 against native 0.177). The
  brush is now **opaque**, and preserves the accent's hue while shifting lightness and capping
  chroma, fitted against Finder's drop-down selection for four accents. Menus and drop-down rows
  share the one colour. A grey (graphite) accent now stays grey, where the previous chroma offset
  gave it a violet cast.
- Changed: the standalone LiquidGlass menu pack now derives its selection colour from
  `SystemAccentColor` instead of carrying literals pinned to the default accent, so pack-only
  consumers follow a custom system accent. It needs `SystemAccentColor` from the host, as the
  classic pack already did.
- Added: `OklchAdjustmentConverter.ChromaCap`, an optional chroma ceiling. Unbounded by default, so
  existing callers are unaffected.
- Fixed: in the classic light variant, a pressed checked `RadioButton` kept its unpressed fill. The
  light dictionary spelled the key `RadioButtonPressedCheckedBackgroundBrush` while the control and
  the dark dictionary both use `RadioButtonCheckedPressedBackgroundBrush`, so nothing resolved.
- Changed: in the classic variant, menu hover now uses the accent gradient
  (`ControlBackgroundAccentRaisedBrush`), the same brush as drop-down rows. It was a flat accent at
  0.63 opacity, which made menu selection and ComboBox popup selection two different colours. The
  standalone menu pack mirrors the change, so `MacOsMenuItemPointerOverBackgroundBrush` is now a
  gradient rather than a solid brush.
- Known issue (Avalonia, not this theme): `SystemAccentColor` is captured when the app starts and
  is not refreshed when the macOS appearance changes, so after a light/dark switch accent-derived
  colours are computed from the previous appearance's accent until restart. macOS reports a different
  accent per appearance for some accents (red `#E0383E` light / `#FF5257` dark), so it is visible on
  those and not on blue or green. Tracked at https://support.avaloniaui.net/support/tickets/1857
- BREAKING (resource keys): the intermediate colour tokens `AccentButtonTopColor`,
  `AccentButtonBottomColor`, `ControlBackgroundAccentRecessedTopColor`,
  `ControlBackgroundAccentRecessedBottomColor` and `ControlBorderAccentColor` were removed. They
  could only ever hold a stale accent, which was the cause of the bug above. Consume the brushes
  instead - `ControlBackgroundAccentRaisedBrush`, `ButtonBackgroundAccentRecessedBrush`,
  `ControlBorderAccentBrush`, `ButtonBorderAccentBrush` - or `SystemAccentColor` and its
  `Light1`/`Dark1` steps directly, always through `DynamicResource`.

## v2026.6.23

> **BREAKING:** This release line requires Avalonia 12. `2026.6.16` is the last stable release compatible with Avalonia 11.
> See the [Avalonia 11 -> 12 breaking changes](https://docs.avaloniaui.net/docs/avalonia12-breaking-changes) for upstream migration guidance.

- First stable release on the Avalonia 12 line.

## v2026.6.17-avalonia12

- Preview release for validating the Avalonia 12 migration before the stable rollout.

## v2026.6.16

- Last stable release compatible with Avalonia 11.

## v2025.07.28

- Minor fixes to CalendarDatePicker to accommodate long date formats

## v2025.07.21

- Added CalendarDatePicker styling

## v2025.07.15

- Themed `TabPane`, which is an alternately-themed `TabControl`

## v2025.07.14

- BREAKING: default vertical alignment of `TextBlock` changed to centred. You may see unwanted changes in
  your layouts.
-

## v2026.01.08

- BREAKING: Changes to how controls respond to custom height settings. This makes the controls behave more consistently.
  But if you had your own work-arounds, you may see unwanted changes in your layouts.