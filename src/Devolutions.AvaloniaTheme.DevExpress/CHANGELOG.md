# Devolutions.AvaloniaTheme.DevExpress

**NOTE:** This theme is still in active development and we are currently not maintaining a detailed change log.
Please see commits if you're curious. However we will do our best to call out key changes in this log.

## v2025.08.13

- Add ListBox styling (incl. custom multi-column ListBox)
- Make TabControl transparent to be more flexible on different backgrounds

## v2025.08.08

- Fix transparent scrollbar junction in DataGrid

## v2025.07.28

- Add CalendarDatePicker styling

## v2025.07.21

- Fix TextBox text alignment issue introduced in v2025.07.15
- Fix layout jumps when switching TabPane tabs

## v2025.07.15

- **BREAKING**: Under the light theme, the font now default to Segoe UI Demi-Bold, instead of Normal(Regular).
  - This is exposed through two new ThemeDictionaries resources, bound as `DynamicResource`:
    - `DefaultFontWeight` (defaults to `Normal` in dark theme, and `DemiBold` in light theme)
    - `DemiBoldWeight` (defaults to `DemiBold` in dark theme, and `Bold` in light theme)


- Themed `TabPane`, which is an alternately-themed `TabControl`


- Support `.search-highlight` & `.secondary-search-highlight` classes on some controls:
  - ComboBox
  - EditableComboBox
  - Label
  - Button
  - TextBox
  - RadioButton
  - TabControl

## v2025.07.14

- **BREAKING**: Input controls and TextBlock are now all designed to play nicely (and consistently) when dropped into
  layouts with only their default properties. Depending on your usage or the work-arounds you may have applied to fix
  previous alignment issues, you may now see unwanted changes in your layouts.
  <br /><br />Controls now:
  - don't stretch to fill the height of their container
  - have no default margins of their own
  - align vertically centred
