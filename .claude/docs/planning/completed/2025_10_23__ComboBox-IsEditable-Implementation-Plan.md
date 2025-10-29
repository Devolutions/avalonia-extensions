# ComboBox IsEditable Property Implementation Plan

This document describes the changes needed to support the `IsEditable` property on ComboBox controls across all three themes (DevExpress, Linux, MacOS).

## Overview

The `IsEditable` property allows ComboBox to function as a hybrid control where users can either:
- Select an item from the dropdown list, OR
- Type custom text directly into the control

This is implemented by embedding a TextBox (`PART_EditableTextBox`) into the ComboBox template that becomes visible when `IsEditable=true`.

---

## DevExpress Theme - COMPLETED ✅

### Changes Made

#### 1. Template Changes (ComboBox.axaml)

**Location:** Lines 130-176 in the ComboBox template

**Placeholder Text Visibility Logic:**
```xml
<TextBlock Name="PlaceholderTextBlock"
           ...
           Text="{TemplateBinding PlaceholderText}">
  <TextBlock.IsVisible>
    <MultiBinding Converter="{x:Static BoolConverters.And}">
      <Binding Path="SelectionBoxItem" RelativeSource="{RelativeSource TemplatedParent}"
               Converter="{x:Static ObjectConverters.IsNull}" />
      <Binding Path="!IsEditable" RelativeSource="{RelativeSource TemplatedParent}" />
    </MultiBinding>
  </TextBlock.IsVisible>
</TextBlock>
```
- **Purpose:** Hide placeholder when IsEditable is true (TextBox shows its own Watermark)
- **Logic:** Show placeholder only when SelectionBoxItem is null AND IsEditable is false

**ContentPresenter Visibility:**
```xml
<ContentPresenter Name="ContentPresenter"
                  Content="{TemplateBinding SelectionBoxItem}"
                  ...
                  IsVisible="{TemplateBinding IsEditable, Converter={x:Static BoolConverters.Not}}"/>
```
- **Purpose:** Hide the normal content presenter when in editable mode
- **Logic:** Show only when IsEditable is false

**Editable TextBox Addition:**
```xml
<TextBox Name="PART_EditableTextBox"
         Grid.Column="0"
         Padding="{TemplateBinding Padding}"
         HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}"
         VerticalAlignment="{TemplateBinding VerticalContentAlignment}"
         Foreground="{TemplateBinding Foreground}"
         Background="Transparent"
         Text="{TemplateBinding Text, Mode=TwoWay}"
         Watermark="{TemplateBinding PlaceholderText}"
         BorderThickness="0"
         FocusAdorner="{x:Null}"
         IsVisible="{TemplateBinding IsEditable}">
  <TextBox.Styles>
    <Style Selector="TextBox /template/ Border#BottomBorderElement">
      <Setter Property="IsVisible" Value="False" />
    </Style>
  </TextBox.Styles>
  <TextBox.Resources>
    <SolidColorBrush x:Key="TextControlBackgroundFocused">Transparent</SolidColorBrush>
    <SolidColorBrush x:Key="TextControlBackgroundPointerOver">Transparent</SolidColorBrush>
    <Thickness x:Key="TextControlBorderThemeThicknessFocused">0</Thickness>
  </TextBox.Resources>
</TextBox>
```

**Key Properties:**
- `Name="PART_EditableTextBox"` - Required by Avalonia's ComboBox implementation
- `Background="Transparent"` - Lets ComboBox background show through
- `BorderThickness="0"` - No border on the internal TextBox
- `FocusAdorner="{x:Null}"` - Suppress default focus adorner
- `IsVisible="{TemplateBinding IsEditable}"` - Show only when editable

**Focus Suppression:**
- Hide `BottomBorderElement` completely (DevExpress TextBox uses this for focus visualization)
- Override background resources to stay transparent when focused/hovered
- This ensures focus visualization appears on the ComboBox container, not the internal TextBox

#### 2. Style Changes (ComboBox.axaml)

**Location:** Lines 276-279

```xml
<Style Selector="^[IsEditable=true]">
  <Setter Property="IsTabStop" Value="False" />
  <Setter Property="KeyboardNavigation.TabNavigation" Value="Local" />
</Style>
```

**Purpose:**
- `IsTabStop="False"` - ComboBox itself shouldn't receive tab focus (TextBox will)
- `TabNavigation="Local"` - Tab navigation stays within the ComboBox (focuses the TextBox)

#### 3. Focus Selector Update (ComboBox.axaml)

**Location:** Line 223

```xml
<Style Selector="^:focus, ^:focus-within, ^:focus-visible">
  <Style Selector="^ /template/ Border#BottomBorderElement">
    <Setter Property="BorderBrush" Value="{DynamicResource ControlBorderSelectedBrush}" />
    <Setter Property="BorderThickness" Value="0 0 0 1.6" />
  </Style>
</Style>
```

**Change:** Added `:focus-within` selector
- **Purpose:** Show ComboBox focus styling even when the internal TextBox has focus
- **Effect:** The thicker bottom border appears whether ComboBox or its child TextBox is focused

#### 4. Demo Page Updates (ComboBoxDemo.axaml)

Added two examples:
1. **Editable with selection:** `IsEditable="True" SelectedIndex="0"`
2. **Editable without selection:** `IsEditable="True" PlaceholderText="Pick or type"`

---

## Linux Theme - TO BE IMPLEMENTED

**Branch:** `Linux/add-editable-support-to-combo`
**Target PR:** Separate PR to master

### Current Structure Analysis

**File:** `src/Devolutions.AvaloniaTheme.Linux/Controls/ComboBox.axaml`

**Current Content Display (Lines 124-140):**
```xml
<TextBlock Name="PlaceholderTextBlock"
           Grid.Column="0"
           Margin="{TemplateBinding Padding}"
           IsVisible="{TemplateBinding SelectionBoxItem, Converter={x:Static ObjectConverters.IsNull}}"
           Text="{TemplateBinding PlaceholderText}" />
<ContentControl Grid.Column="0"
                Margin="{TemplateBinding Padding}"
                Content="{TemplateBinding SelectionBoxItem}"
                ContentTemplate="{TemplateBinding SelectionBoxItemTemplate}" />
```

**Current Focus Handling (Lines 202-206):**
```xml
<Style Selector="^:focus-visible">
  <Style Selector="^ /template/ Border#FocusBorderElement">
    <Setter Property="IsVisible" Value="True" />
  </Style>
</Style>
```

**Focus Border Element (Lines 153-157):**
```xml
<Border Name="FocusBorderElement"
        IsVisible="False"
        BorderThickness="{StaticResource FocusBorderThickness}"
        BorderBrush="{WindowActiveResourceToggler InputFocusedBorder, InputFocusedInactiveBorder}"
        CornerRadius="{TemplateBinding CornerRadius}" />
```

### Implementation Steps

#### Step 1: Update PlaceholderTextBlock Visibility
```xml
<TextBlock Name="PlaceholderTextBlock"
           Grid.Column="0"
           Margin="{TemplateBinding Padding}"
           Text="{TemplateBinding PlaceholderText}">
  <TextBlock.IsVisible>
    <MultiBinding Converter="{x:Static BoolConverters.And}">
      <Binding Path="SelectionBoxItem" RelativeSource="{RelativeSource TemplatedParent}"
               Converter="{x:Static ObjectConverters.IsNull}" />
      <Binding Path="!IsEditable" RelativeSource="{RelativeSource TemplatedParent}" />
    </MultiBinding>
  </TextBlock.IsVisible>
</TextBlock>
```

#### Step 2: Update ContentControl Visibility
```xml
<ContentControl Grid.Column="0"
                Margin="{TemplateBinding Padding}"
                Content="{TemplateBinding SelectionBoxItem}"
                ContentTemplate="{TemplateBinding SelectionBoxItemTemplate}"
                IsVisible="{TemplateBinding IsEditable, Converter={x:Static BoolConverters.Not}}" />
```

#### Step 3: Add PART_EditableTextBox

**Insert after ContentControl (around line 140):**
```xml
<TextBox Name="PART_EditableTextBox"
         Grid.Column="0"
         Padding="{TemplateBinding Padding}"
         HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}"
         VerticalAlignment="{TemplateBinding VerticalContentAlignment}"
         Foreground="{TemplateBinding Foreground}"
         Background="Transparent"
         Text="{TemplateBinding Text, Mode=TwoWay}"
         Watermark="{TemplateBinding PlaceholderText}"
         BorderThickness="0"
         FocusAdorner="{x:Null}"
         IsVisible="{TemplateBinding IsEditable}">
  <TextBox.Styles>
    <Style Selector="TextBox /template/ Border#FocusBorderElement">
      <Setter Property="IsVisible" Value="False" />
    </Style>
  </TextBox.Styles>
  <TextBox.Resources>
    <SolidColorBrush x:Key="TextControlBackgroundFocused">Transparent</SolidColorBrush>
    <SolidColorBrush x:Key="TextControlBackgroundPointerOver">Transparent</SolidColorBrush>
  </TextBox.Resources>
</TextBox>
```

**Note:** Linux TextBox also uses `Border#FocusBorderElement` for focus visualization (similar pattern to DevExpress's `BottomBorderElement`), so we hide it the same way.

#### Step 4: Update Focus Selector

**Change line 202 from:**
```xml
<Style Selector="^:focus-visible">
```

**To:**
```xml
<Style Selector="^:focus-visible, ^:focus-within">
```

#### Step 5: Add IsEditable Style

**Add at end of ControlTheme (after line 214):**
```xml
<Style Selector="^[IsEditable=true]">
  <Setter Property="IsTabStop" Value="False" />
  <Setter Property="KeyboardNavigation.TabNavigation" Value="Local" />
</Style>
```

#### Step 6: Update Demo Page

**File:** `samples/SampleApp/DemoPages/ComboBoxDemo.axaml`

The existing DevExpress examples should work for all themes. When running with Linux theme, verify:
1. Editable ComboBox with selection works
2. Editable ComboBox with "Pick or type" placeholder works
3. Focus visualization appears on ComboBox border, not internal TextBox

### Testing Checklist for Linux

- [ ] Editable ComboBox displays correctly
- [ ] Can type text into editable ComboBox
- [ ] Can select item from dropdown
- [ ] Selected item text appears in editable TextBox
- [ ] Placeholder/Watermark shows when empty
- [ ] Focus border appears on ComboBox (not internal TextBox)
- [ ] Tab navigation works correctly
- [ ] Disabled state works
- [ ] Works on both light and dark backgrounds

---

## MacOS Theme - TO BE IMPLEMENTED

**Branch:** `MacOS/add-editable-support-to-combo`
**Target PR:** Separate PR to master (after Linux is merged)

### Current Structure Analysis

**File:** `src/Devolutions.AvaloniaTheme.MacOS/Controls/ComboBox.axaml`

**Current Content Display (Lines 150-165):**
```xml
<TextBlock Name="PlaceholderTextBlock"
           Grid.Column="0"
           Margin="{TemplateBinding Padding}"
           IsVisible="{TemplateBinding SelectionBoxItem, Converter={x:Static ObjectConverters.IsNull}}"
           Text="{TemplateBinding PlaceholderText}" />
<ContentControl Grid.Column="0"
                Margin="{TemplateBinding Padding}"
                Content="{TemplateBinding SelectionBoxItem}"
                ContentTemplate="{TemplateBinding SelectionBoxItemTemplate}" />
```

**Current Focus Handling (Lines 229-233):**
```xml
<Style Selector="^:focus-visible">
  <Style Selector="^ /template/ Border#FocusBorderElement">
    <Setter Property="BorderBrush" Value="{DynamicResource FocusBorderBrush}" />
  </Style>
</Style>
```

**Focus Border Element (Lines 178-181):**
```xml
<Border Name="FocusBorderElement"
        Margin="{StaticResource FocusBorderMargin}"
        BorderThickness="{StaticResource FocusBorderThickness}"
        CornerRadius="7" />
```

**Popup Positioning Logic (Lines 199-207):**
```xml
<Popup.VerticalOffset>
  <MultiBinding Converter="{x:Static DevoMultiConverters.SelectedIndexToPopupOffsetConverter}">
    <Binding RelativeSource="{RelativeSource TemplatedParent}" Path="SelectedIndex" />
    <Binding Source="{StaticResource ComboBoxItemHeight}" />
    <Binding Source="{StaticResource InitialFirstItemDistance}" />
    <Binding RelativeSource="{RelativeSource TemplatedParent}" Path="MaxDropDownHeight" />
    <Binding Source="{StaticResource PopupTrimHeight}" />
  </MultiBinding>
</Popup.VerticalOffset>
```

### Special Considerations for MacOS

#### Popup Positioning Challenge

**Current Behavior:**
- MacOS ComboBox uses `SelectedIndexToPopupOffsetConverter` to position the popup
- The popup aligns so the selected item appears directly over the ComboBox
- This creates the illusion that the selected item "stays in place" when the dropdown opens

**Potential Issue with IsEditable:**
- When `IsEditable=true` and user has typed custom text (no selection), `SelectedIndex=-1`
- The converter needs to handle this gracefully
- May need to update `SelectedIndexToPopupOffsetConverter` to treat `-1` as "no offset" or similar

**⚠️ WATCH OUT:** Test popup positioning thoroughly when:
1. ComboBox is editable with no selection (`SelectedIndex=-1`)
2. ComboBox is editable with custom typed text
3. ComboBox is editable with a selected item

The converter should probably position the popup at the "natural" position (like Linux does) when `SelectedIndex=-1` in editable mode.

### Implementation Steps

#### Step 1: Update PlaceholderTextBlock Visibility
```xml
<TextBlock Name="PlaceholderTextBlock"
           Grid.Column="0"
           Margin="{TemplateBinding Padding}"
           Text="{TemplateBinding PlaceholderText}">
  <TextBlock.IsVisible>
    <MultiBinding Converter="{x:Static BoolConverters.And}">
      <Binding Path="SelectionBoxItem" RelativeSource="{RelativeSource TemplatedParent}"
               Converter="{x:Static ObjectConverters.IsNull}" />
      <Binding Path="!IsEditable" RelativeSource="{RelativeSource TemplatedParent}" />
    </MultiBinding>
  </TextBlock.IsVisible>
</TextBlock>
```

#### Step 2: Update ContentControl Visibility
```xml
<ContentControl Grid.Column="0"
                Margin="{TemplateBinding Padding}"
                Content="{TemplateBinding SelectionBoxItem}"
                ContentTemplate="{TemplateBinding SelectionBoxItemTemplate}"
                IsVisible="{TemplateBinding IsEditable, Converter={x:Static BoolConverters.Not}}" />
```

#### Step 3: Add PART_EditableTextBox

**Insert after ContentControl (around line 165):**
```xml
<TextBox Name="PART_EditableTextBox"
         Grid.Column="0"
         Padding="{TemplateBinding Padding}"
         HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}"
         VerticalAlignment="{TemplateBinding VerticalContentAlignment}"
         Foreground="{TemplateBinding Foreground}"
         Background="Transparent"
         Text="{TemplateBinding Text, Mode=TwoWay}"
         Watermark="{TemplateBinding PlaceholderText}"
         BorderThickness="0"
         FocusAdorner="{x:Null}"
         IsVisible="{TemplateBinding IsEditable}">
  <TextBox.Styles>
    <Style Selector="TextBox /template/ Border#FocusBorderElement">
      <Setter Property="BorderBrush" Value="Transparent" />
    </Style>
  </TextBox.Styles>
  <TextBox.Resources>
    <SolidColorBrush x:Key="TextControlBackgroundFocused">Transparent</SolidColorBrush>
    <SolidColorBrush x:Key="TextControlBackgroundPointerOver">Transparent</SolidColorBrush>
  </TextBox.Resources>
</TextBox>
```

**Note:** MacOS TextBox uses `Border#FocusBorderElement` but sets `BorderBrush` instead of `IsVisible`. Setting it to `Transparent` hides the focus border.

#### Step 4: Update Focus Selector

**Change line 229 from:**
```xml
<Style Selector="^:focus-visible">
```

**To:**
```xml
<Style Selector="^:focus-visible, ^:focus-within">
```

#### Step 5: Add IsEditable Style

**Add at end of ControlTheme (after line 241):**
```xml
<Style Selector="^[IsEditable=true]">
  <Setter Property="IsTabStop" Value="False" />
  <Setter Property="KeyboardNavigation.TabNavigation" Value="Local" />
</Style>
```

#### Step 6: (Optional) Update SelectedIndexToPopupOffsetConverter

**File:** `src/Devolutions.AvaloniaTheme.MacOS/Converters/...` (find the converter)

**Potential Change Needed:**
If the converter doesn't already handle `SelectedIndex=-1` gracefully, update it to:
- Return 0 or a default offset when `SelectedIndex=-1`
- This makes the popup appear in the natural position (like non-editable) when no item is selected

**Testing Required:**
- Open dropdown when SelectedIndex=-1 (no selection)
- Verify popup doesn't jump to unexpected position
- Verify popup positioning still works correctly when item is selected

#### Step 7: Update Demo Page

**File:** `samples/SampleApp/DemoPages/ComboBoxDemo.axaml`

The existing DevExpress examples should work for all themes. When running with MacOS theme, verify:
1. Editable ComboBox with selection works
2. Editable ComboBox with "Pick or type" placeholder works
3. Focus visualization appears on ComboBox border
4. **CRITICAL:** Popup positioning works correctly in all scenarios:
   - No selection (SelectedIndex=-1)
   - With selection (SelectedIndex >= 0)
   - After typing custom text
   - After selecting item then typing

### Testing Checklist for MacOS

- [ ] Editable ComboBox displays correctly
- [ ] Can type text into editable ComboBox
- [ ] Can select item from dropdown
- [ ] Selected item text appears in editable TextBox
- [ ] Placeholder/Watermark shows when empty
- [ ] Focus border appears on ComboBox (not internal TextBox)
- [ ] Tab navigation works correctly
- [ ] Disabled state works
- [ ] **CRITICAL:** Popup positions correctly when SelectedIndex=-1
- [ ] **CRITICAL:** Popup positions correctly when item is selected
- [ ] **CRITICAL:** Popup positions correctly after typing custom text
- [ ] Works on both light and dark backgrounds
- [ ] Works with active/inactive window states (MacOS specific)

---

## Implementation Order

1. ✅ **DevExpress** (COMPLETED)
   - Branch: `DevExpress/add-editable-support-to-combo`
   - Commits: Already pushed

2. **Linux** (NEXT)
   - Branch: `Linux/add-editable-support-to-combo`
   - Estimated complexity: Low
   - Similar to DevExpress, straightforward implementation

3. **MacOS** (LAST)
   - Branch: `MacOS/add-editable-support-to-combo`
   - Estimated complexity: Medium
   - Requires careful testing of popup positioning logic
   - May require converter updates

---

## Key Patterns & Learnings

### Pattern 1: Placeholder Visibility
Always use MultiBinding with And converter to show placeholder only when:
- SelectionBoxItem is null AND
- IsEditable is false (because TextBox has its own Watermark)

### Pattern 2: Content Toggle
Use `BoolConverters.Not` to hide ContentPresenter when IsEditable is true:
```xml
IsVisible="{TemplateBinding IsEditable, Converter={x:Static BoolConverters.Not}}"
```

### Pattern 3: TextBox Focus Suppression
Each theme has its own focus visualization element in TextBox template:
- **DevExpress:** `Border#BottomBorderElement` → Hide with `IsVisible="False"`
- **Linux:** `Border#FocusBorderElement` → Hide with `IsVisible="False"`
- **MacOS:** `Border#FocusBorderElement` → Hide with `BorderBrush="Transparent"`

### Pattern 4: Focus-Within Selector
Always add `:focus-within` to ComboBox focus selector so focus styling appears when internal TextBox is focused.

### Pattern 5: Tab Navigation
Always set these properties when IsEditable=true:
- `IsTabStop="False"` on ComboBox
- `KeyboardNavigation.TabNavigation="Local"` on ComboBox

This ensures tab focuses the internal TextBox, not the ComboBox container.

---

## Common Pitfalls to Avoid

1. **Forgetting `:focus-within`**
   - Without this, ComboBox loses focus styling when TextBox is focused

2. **Not suppressing TextBox focus visual**
   - Results in double focus indicators (ComboBox + TextBox)

3. **Wrong placeholder visibility logic**
   - Must check BOTH SelectionBoxItem and IsEditable

4. **Not using `Background="Transparent"` on TextBox**
   - TextBox would have white/default background over ComboBox background

5. **Missing `Mode=TwoWay` on Text binding**
   - User text changes wouldn't propagate to ComboBox.Text property

6. **MacOS-specific: Not testing popup positioning**
   - SelectedIndex=-1 case must be handled by popup offset converter

---

## Files to Modify Per Theme

### Linux
1. `src/Devolutions.AvaloniaTheme.Linux/Controls/ComboBox.axaml`
   - Template changes
   - Style additions
2. `samples/SampleApp/DemoPages/ComboBoxDemo.axaml`
   - Already has examples from DevExpress work

### MacOS
1. `src/Devolutions.AvaloniaTheme.MacOS/Controls/ComboBox.axaml`
   - Template changes
   - Style additions
2. Potentially: `src/Devolutions.AvaloniaTheme.MacOS/Converters/...` (converter file)
   - Handle SelectedIndex=-1 case
3. `samples/SampleApp/DemoPages/ComboBoxDemo.axaml`
   - Already has examples from DevExpress work

---

## Commit Message Templates

### Linux Theme
```
[Linux] Add IsEditable support to ComboBox

- Add PART_EditableTextBox to ComboBox template
- Update placeholder and content visibility logic for editable mode
- Suppress TextBox focus border (FocusBorderElement)
- Add :focus-within selector for proper focus styling
- Configure tab navigation for editable mode

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>
```

### MacOS Theme
```
[MacOS] Add IsEditable support to ComboBox

- Add PART_EditableTextBox to ComboBox template
- Update placeholder and content visibility logic for editable mode
- Suppress TextBox focus border styling
- Add :focus-within selector for proper focus styling
- Configure tab navigation for editable mode
- Handle popup positioning when SelectedIndex=-1

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>
```

---

## Summary

The IsEditable implementation follows a consistent pattern across all themes with minor variations in focus suppression techniques. The key is understanding each theme's TextBox focus visualization approach and suppressing it appropriately. MacOS requires extra attention to popup positioning logic due to its selected-item-alignment feature.
