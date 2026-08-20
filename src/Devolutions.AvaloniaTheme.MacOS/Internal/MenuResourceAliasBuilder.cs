namespace Devolutions.AvaloniaTheme.MacOS.Internal;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;

internal static class MenuResourceAliasBuilder
{
  private static readonly (string MenuToken, string LegacyToken)[] AliasMappings =
  {
    ("MacOsMenuPopupBackgroundBrush", "PopupBackgroundBrush"),
    ("MacOsMenuPopupInnerBorderHighlightBrush", "PopupInnerBorderHighlightBrush"),
    ("MacOsMenuPopupBorderBrush", "PopupBorderBrush"),
    ("MacOsMenuItemForegroundBrush", "MenuItemForegroundBrush"),
    ("MacOsMenuForegroundHighBrush", "ForegroundHighBrush"),
    ("MacOsMenuForegroundMidLowBrush", "ForegroundMidLowBrush"),
    ("MacOsMenuForegroundLowBrush", "ForegroundLowBrush"),
    ("MacOsMenuAccentForegroundBrush", "ControlForegroundAccentHighBrush"),
    ("MacOsMenuSelectedBackgroundBrush", "LayoutBackgroundMidBrush"),
    ("MacOsMenuPressedBackgroundBrush", "LayoutBackgroundHighBrush"),
    ("MacOsMenuItemPointerOverBackgroundBrush", "MenuItemPointerOverBackgroundBrush"),
    ("MacOsMenuSeparatorBrush", "SeparatorBrush"),
    ("MacOsMenuPopupBorderThickness", "MenuFlyoutPresenterBorderThickness"),
    ("MacOsMenuFontSize", "ControlFontSize"),
    ("MacOsMenuHeaderFontSizeSmall", "MenuHeaderFontSizeSmall"),
    ("MacOsMenuChevronSize", "TreeViewItemChevronSize"),
    ("MacOsMenuSelectionCornerRadius", "SelectionCornerRadius"),
    ("MacOsMenuPopupMargin", "PopupMargin"),
    ("MacOsMenuPopupInnerBorderThickness", "PopupInnerBorderThickness"),
    ("MacOsMenuPopupCornerRadius", "PopupCornerRadius"),
    ("MacOsMenuPopupShadow", "PopupShadow"),
    ("MacOsMenuPopupMaxWidth", "FlyoutThemeMaxWidth"),
    ("MacOsMenuPopupMinHeight", "MenuFlyoutThemeMinHeight"),
    ("MacOsMenuPopupPadding", "MenuFlyoutPadding"),
    ("MacOsMenuFlyoutPresenterPadding", "MenuFlyoutPresenterThemePadding"),
    ("MacOsMenuFlyoutScrollerMargin", "MenuFlyoutScrollerMargin"),
    ("MacOsMenuHorizontalFlyoutMinWidth", "HorizontalMenuFlyoutThemeMinWidth"),
    ("MacOsMenuSubMenuPopupHorizontalOffset", "SubMenuPopupHorizontalOffset"),
    ("MacOsMenuSubMenuPopupVerticalOffset", "SubMenuPopupVerticalOffset"),
    ("MacOsMenuPopupHorizontalOffset", "MenuPopupHorizontalOffset"),
    ("MacOsMenuPopupVerticalOffset", "MenuPopupVerticalOffset"),
    ("MacOsMenuToolBarPopupVerticalOffset", "MenuToolBarPopupVerticalOffset"),
    ("MacOsMenuBarPadding", "MenuBarPadding"),
    ("MacOsMenuItemPadding", "MenuItemPadding"),
    ("MacOsMenuItemMinHeight", "MenuItemMinHeight"),
    ("MacOsMenuIconPresenterMargin", "MenuIconPresenterMargin"),
    ("MacOsMenuInputGestureTextMargin", "MenuInputGestureTextMargin"),
    ("MacOsMenuItemIconPadding", "MenuItemIconPadding"),
    ("MacOsMenuToolBarItemPadding", "MenuToolBarItemPadding"),
    ("MacOsMenuToolBarItemIconPadding", "MenuToolBarItemIconPadding"),
    ("MacOsMenuToolBarItemActiveBackgroundMargin", "MenuToolBarItemActiveBackgroundMargin"),
    ("MacOsMenuItemActiveBackgroundMargin", "MenuItemActiveBackgroundMargin"),
    ("MacOsMenuSeparatorHeight", "MenuFlyoutSeparatorThemeHeight"),
    ("MacOsMenuSeparatorPadding", "MenuFlyoutSeparatorThemePadding"),
    ("MacOsMenuChevronPath", "ChevronPath"),
    ("MacOsMenuCheckMarkPath", "CheckMarkPath"),
  };

  public static void Rebuild(Styles styles, ResourceDictionary aliases)
  {
    aliases.ThemeDictionaries.Clear();
    aliases.ThemeDictionaries[ThemeVariant.Light] = BuildVariantDictionary(styles, ThemeVariant.Light);
    aliases.ThemeDictionaries[ThemeVariant.Dark] = BuildVariantDictionary(styles, ThemeVariant.Dark);
  }

  private static ResourceDictionary BuildVariantDictionary(Styles styles, ThemeVariant variant)
  {
    ResourceDictionary variantDictionary = new();

    foreach ((string menuToken, string legacyToken) in AliasMappings)
    {
      if (TryResolveMenuValue(styles, menuToken, legacyToken, variant, out object? value))
      {
        variantDictionary[menuToken] = value;
      }
    }

    return variantDictionary;
  }

  private static bool TryResolveMenuValue(
    Styles styles,
    string menuToken,
    string legacyToken,
    ThemeVariant variant,
    out object? value)
  {
    // The legacy theme tokens are the source of truth: they already reflect the
    // active MacOS variant (classic vs LiquidGlass). The MacOsMenu* defaults are
    // only a fallback for standalone menu-pack consumers that load no theme tokens.
    if (styles.TryGetResource(legacyToken, variant, out value) ||
        styles.TryGetResource(menuToken, variant, out value) ||
        styles.TryGetResource(legacyToken, null, out value) ||
        styles.TryGetResource(menuToken, null, out value))
    {
      return true;
    }

    value = null;
    return false;
  }
}
