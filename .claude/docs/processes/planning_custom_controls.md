# Planning Guide: Custom Control Development

This specialized guide is for planning work on creating new custom controls in the `Devolutions.AvaloniaControls` project.

For general planning guidance, see `docs/processes/planning_docs.md`.
For styling existing controls in themes, see `docs/processes/planning_theme_controls.md`.

## When to Use This Guide

Use this guide when planning to:
- Create a new reusable custom control in `Devolutions.AvaloniaControls`
- Add new functionality that extends Avalonia's built-in controls
- Build composite controls from multiple existing controls
- Create controls with special behavior or interactivity patterns

## Goal & Context Template

When starting a custom control planning doc, clearly state:
- **Control name**: What you're building (e.g., EditableComboBox, SearchHighlightTextBlock)
- **Purpose**: What problem does this solve that built-in controls don't?
- **Usage context**: Where/how will this be used in applications?
- **Reusability**: Will this be used across all three themes (MacOS, DevExpress, Linux)?
- **Base class**: What Avalonia control class will you extend (Control, UserControl, ItemsControl, etc.)?

## Control Behavior Specification

Define the control's behavior in detail before writing code:

### Public API Design

```markdown
## Properties

### MyProperty (StyledProperty<string>)
- **Type**: string
- **Default**: null
- **Binding Mode**: TwoWay
- **Description**: What this property controls
- **Validation**: Any validation rules or coercion
- **Example usage**: `<MyControl MyProperty="value" />`

### IsEnabled (inherited)
- **Overridden behavior**: Custom disabled state handling
```

```markdown
## Events

### ValueChanged
- **Type**: EventHandler<ValueChangedEventArgs>
- **When raised**: When user changes the value through interaction
- **Event args**: Old value, new value
- **Cancellable**: Yes/No

### DropDownOpened
- **Type**: EventHandler
- **When raised**: When dropdown portion opens
```

```markdown
## Methods

### Focus()
- **Overridden from**: Control
- **Behavior**: Focuses inner text box portion
- **Returns**: bool indicating success

### SelectAll()
- **New method**
- **Behavior**: Selects all text in the editable portion
- **Returns**: void
```

### Interaction Patterns

Document how users interact with the control:

```markdown
## User Interaction Flows

### Keyboard Navigation
1. Tab → Focuses the control (inner text box)
2. Arrow Down → Opens dropdown (if closed) or navigates to next item
3. Arrow Up → Opens dropdown (if closed) or navigates to previous item
4. Enter → Accepts highlighted item, closes dropdown
5. Escape → Closes dropdown without selection
6. Tab (when dropdown open) → Accepts selection, moves to next control

### Mouse/Touch Interaction
1. Click on control → Opens dropdown
2. Click on dropdown item → Selects item, closes dropdown
3. Click outside dropdown → Closes without selection (light dismiss)
4. Hover over item → Highlights item

### Edge Cases
- What happens if user types while dropdown open?
- How does filtering work in real-time?
- What if no items match the filter?
```

## Control Architecture

### Template Parts Planning

Define required and optional template parts:

```markdown
## Template Parts

### PART_InnerTextBox (required)
- **Type**: TextBox (or InnerTextBox custom variant)
- **Purpose**: Editable text input portion
- **Properties bound to control**:
  - Text → TwoWay to Value property
  - CaretIndex → TwoWay
  - SelectionStart/End → TwoWay
- **Events subscribed**: TextChanged, GotFocus

### PART_InnerComboBox (required)
- **Type**: ComboBox (or InnerComboBox custom variant)
- **Purpose**: Dropdown portion with items
- **Properties bound to control**:
  - ItemsSource → filteredItems internal collection
  - IsDropDownOpen → TwoWay
  - SelectedItem → internal handling
- **Events subscribed**: SelectionChanged

### PART_Popup (optional)
- **Type**: Popup
- **Purpose**: Custom popup if not using ComboBox's built-in
- **Properties**: Placement, offset, shadow
```

### Internal Components

Document internal helper classes/controls:

```markdown
## Internal Components

### InnerTextBox : TextBox
- **Purpose**: Custom TextBox with specific behavior for this control
- **Customizations**:
  - Watermark support
  - Focus handling
  - Validation binding

### InnerComboBox : ComboBox
- **Purpose**: Custom ComboBox with filter support
- **Customizations**:
  - Dynamic ItemsSource filtering
  - Selected item alignment in popup
  - Custom item rendering

### EditableComboBoxItem
- **Purpose**: Specialized item wrapper
- **Properties**:
  - Value (string)
  - DataContext (original item)
- **Methods**: Clone() for filtering workaround
```

### State Management

Plan how control state is maintained:

```markdown
## State Management

### Private Fields
- `compositeDisposable`: Manages subscriptions lifecycle
- `filteredItems`: Observable collection of filtered items
- `realizedItems`: Cached array of all items as EditableComboBoxItems
- `innerTextBox`: Reference to template part
- `innerComboBox`: Reference to template part

### Property Change Handling
- `ItemsSourceProperty` changed → Call FillItems()
- `ValueProperty` changed → Call FilterItems() and SelectItemFromText()
- `IsDropDownOpenProperty` changed → Handle OnOpenMenu() / OnCloseMenu()
- `ModeProperty` changed → Update filtering behavior

### Lifecycle Events
- OnAttachedToVisualTree → Subscribe to observables, wire event handlers
- OnDetachedFromVisualTree → Dispose subscriptions, unwire handlers
- OnInitialized → Set initial watermark, fill items
```

## Pseudo-Class Definition

Define custom pseudo-classes for control states:

```markdown
## Pseudo-Classes

### :dropdownopen
- **Constant**: PC_DropdownOpen = ":dropdownopen"
- **When active**: IsDropDownOpen is true
- **Visual effect**: Could change border color, add glow
- **Usage in theme**: `<Style Selector="^:dropdownopen">`

### :pressed
- **Constant**: PC_Pressed = ":pressed"
- **When active**: Between PointerPressed and PointerReleased
- **Visual effect**: Pressed appearance before dropdown opens
- **Usage in theme**: `<Style Selector="^:pressed">`

### :disabled
- **Built-in pseudo-class**
- **Override behavior**: Ensure inner parts also show disabled state
- **Visual effect**: Reduced opacity, different colors
```

## Data Binding Strategy

Plan complex binding scenarios:

```markdown
## Property Bindings

### Two-Way Bindings
```csharp
// Value property binds two-way to inner TextBox
[!!TextBox.TextProperty] = this[!!ValueProperty]
```
**Rationale**: User edits text OR code sets Value, both update the other

### One-Way Bindings
```csharp
// CaretBrush flows one-way from control to inner text box
[!TextBox.CaretBrushProperty] = this[!CaretBrushProperty]
```
**Rationale**: Control provides caret brush, text box uses it

### Observable Subscriptions
```csharp
this.GetObservable(WatermarkProperty).Subscribe(watermark =>
    this.innerTextBox.Watermark = watermark);
```
**Rationale**: Need custom logic when property changes, not just binding
```

## Theme Support Planning

Plan how control will be styled in each theme:

```markdown
## Theme Integration

### Default ControlTheme
- Provide default theme in `Devolutions.AvaloniaControls/DefaultControlTemplates.axaml`
- Should work reasonably with any theme
- Uses generic Avalonia resources

### Per-Theme Customization
- Each theme (MacOS, DevExpress, Linux) provides custom ControlTheme
- Location: `src/Devolutions.AvaloniaTheme.{Theme}/Controls/EditableComboBox.axaml`
- Themes can override:
  - Colors and brushes
  - Dimensions and spacing
  - Corner radius and borders
  - Template if needed (rare)

### Required Resources for Theming
Document what resources themes should define:
- `EditableComboBoxItemHoverBackground` - Item hover color
- Standard control resources (ControlBackgroundHighBrush, etc.)
- Font sizes, padding values specific to this control
```

## Converter and Helper Planning

If control needs custom converters or helpers:

```markdown
## Converters Needed

### SelectedIndexToPopupOffsetConverter
- **Purpose**: Calculate vertical offset to align selected item with control
- **Input parameters**:
  1. SelectedIndex (int)
  2. ItemHeight (double)
  3. InitialDistance (double)
  4. MaxDropDownHeight (double)
  5. PopupTrimHeight (double)
- **Logic**:
  - If no selection: Open at top
  - If selection: Offset = -(SelectedIndex * ItemHeight + InitialDistance)
  - Clamp to ensure popup doesn't exceed MaxDropDownHeight
- **Returns**: double (offset in pixels)

### TotalWidthConverter
- **Purpose**: Add PopupSideMarginWidth to control width for popup min width
- **Input**: Control width + margin width
- **Returns**: Combined total
```

## Testing Strategy

Plan how to verify control functionality:

```markdown
## Manual Testing Checklist

### Basic Functionality
- [ ] Control renders correctly in SampleApp
- [ ] Typing in text box updates Value property
- [ ] Setting Value property updates text box
- [ ] Dropdown opens on click, arrow down, or programmatically
- [ ] Items appear in dropdown correctly
- [ ] Selecting item updates Value
- [ ] Keyboard navigation works (arrows, enter, escape, tab)

### Filtering Mode (if applicable)
- [ ] Typing filters items in real-time
- [ ] Filter is case-insensitive
- [ ] Clearing text shows all items again
- [ ] No items match shows empty dropdown

### Edge Cases
- [ ] Empty ItemsSource shows empty dropdown
- [ ] Null ItemsSource doesn't crash
- [ ] Very long item text doesn't break layout
- [ ] Special characters in text work correctly
- [ ] Rapid clicking/typing doesn't cause issues
- [ ] Changing ItemsSource while open updates dropdown

### Theme Integration
- [ ] Works in MacOS theme (light and dark)
- [ ] Works in DevExpress theme (light and dark)
- [ ] Works in Linux theme (light and dark)
- [ ] Focus indicators visible
- [ ] Disabled state renders correctly
- [ ] Hover states work on all parts

### Accessibility
- [ ] Keyboard-only navigation works completely
- [ ] Tab order is logical
- [ ] Focus indicators clearly visible
- [ ] Screen reader support (if applicable)
```

## Demo Page Planning

Plan comprehensive demo content:

```markdown
## Demo Page: EditableComboBoxDemo.axaml

### Basic Examples
```xml
<EditableComboBox ItemsSource="{Binding Items}" />
<EditableComboBox Value="Preset Value" ItemsSource="{Binding Items}" />
<EditableComboBox IsEnabled="False" Value="Disabled" />
```

### Feature Demonstrations
- Mode="Filter" vs Mode="Immediate" comparison
- ClearOnOpen property toggle
- Custom ItemTemplate example
- Binding to Value property with display
- MaxDropDownHeight customization
- Watermark text example

### Integration Examples
- In a form with other controls
- In a DataGrid
- In a dialog
- With validation
```

## Common Pitfalls to Document

```markdown
## Known Challenges

### ItemsSource Collection Changes
**Issue**: Avalonia doesn't always re-attach items when same instance removed and re-added
**Workaround**: Clone items when filtering instead of reusing instances
**Code**: `this.filteredItems.AddRange(this.realizedItems.Select(i => i.Clone()))`

### Template Part Initialization Timing
**Issue**: Template parts not available until OnApplyTemplate
**Solution**: Use compositeDisposable pattern, subscribe in OnAttachedToVisualTree

### Focus Management
**Issue**: Focus needs to target inner TextBox, not outer control
**Solution**: Override Focus() method to delegate to innerTextBox.Focus()

### Binding Cycles
**Issue**: Two-way bindings can cause infinite loops if not careful
**Solution**: Check for actual value change before updating properties
```

## Code Organization

Plan file structure for complex controls:

```markdown
## File Structure

### EditableComboBox/
- `EditableComboBox.axaml` - Default XAML template (if any)
- `EditableComboBox.axaml.cs` - Main control class
- `EditableComboBoxItem.cs` - Item wrapper class
- `EditableComboBoxMode.cs` - Enum for control modes
- `InnerComboBox.cs` - Internal custom ComboBox (if needed)
- `InnerTextBox.cs` - Internal custom TextBox (if needed)

### Naming Conventions
- Main control: `EditableComboBox`
- Internal parts: `InnerXxx` prefix
- Item wrappers: `[Control]Item` suffix
- Enums/options: `[Control]Mode`, `[Control]Option`
```

## Example Planning Doc Structure

```markdown
# Custom Control: EditableComboBox

## Goal
Create a reusable combo box control that combines editable text input with dropdown selection, supporting both immediate selection and filtering modes.

## Context
Applications need a control that allows users to either:
1. Type free-form text directly
2. Select from a predefined list
3. Filter the list by typing (optional mode)

Built-in ComboBox doesn't support real-time filtering, and TextBox with AutoComplete doesn't provide the same UX.

## References
- Similar concept: WPF editable ComboBox
- Base code: Avalonia ComboBox and TextBox
- Usage: Will be used in forms throughout client applications

## Control Behavior Specification

### Properties
[... detailed property specifications ...]

### Interaction Patterns
[... detailed interaction documentation ...]

## Architecture

### Template Parts
[... template part specifications ...]

### State Management
[... state management strategy ...]

## Actions

### Phase 1: Core Control Structure
- [ ] Create EditableComboBox folder in Devolutions.AvaloniaControls
- [ ] Create main EditableComboBox class extending ItemsControl
- [ ] Define all StyledProperty declarations
- [ ] Create EditableComboBoxItem wrapper class
- [ ] Create EditableComboBoxMode enum

### Phase 2: Internal Components
- [ ] Implement InnerTextBox with custom behavior
- [ ] Implement InnerComboBox with filtering support
- [ ] Wire up property bindings between inner and outer control
- [ ] Set up FuncControlTemplate in constructor

### Phase 3: Interaction Logic
- [ ] Implement FillItems() and FilterItems()
- [ ] Wire up keyboard handling (OnKeyDown)
- [ ] Wire up pointer handling (OnPointerPressed/Released)
- [ ] Implement Focus() delegation
- [ ] Add pseudo-class management

### Phase 4: Theme Integration
- [ ] Create default ControlTheme in DefaultControlTemplates.axaml
- [ ] Create MacOS theme variant
- [ ] Create DevExpress theme variant
- [ ] Create Linux theme variant

### Phase 5: Demo and Testing
- [ ] Create EditableComboBoxDemo.axaml page
- [ ] Add various usage examples
- [ ] Manual testing checklist execution
- [ ] Document known issues and workarounds

### Phase 6: Documentation
- [ ] XML doc comments on public API
- [ ] Update README with control description
- [ ] Add to control status tracking
- [ ] Update evergreen docs if needed

## Appendix

### Design Research
[Links to similar controls, discussions, examples]

### Technical Decisions
[Rationale for architectural choices]

### Performance Considerations
[Any performance implications, optimization strategies]
```
