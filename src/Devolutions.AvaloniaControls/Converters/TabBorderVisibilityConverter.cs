namespace Devolutions.AvaloniaControls.Converters;

using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;

/// <summary>
///   Takes a TabItem, its parent TabControl and the name of a Border element and returns a boolean to indicate if the
///   given Border element should be visible or not
/// </summary>
/// <param name="values">TabItem, TabControl, TabControl.SelectedIndex</param>
/// <param name="parameter">the name of the Border element whose visibility is to be set</param>
/// <returns>true if the Border element should be visible</returns>
/// <remarks>
///   The 3rd element in the parameter array, TabControl.SelectedIndex, is necessary as a separate input, even though the
///   whole TabControl is also passed in. Unless this dynamically changing property is passed in, the Converter will only
///   execute once, rather than each time the selected tab changes.
/// </remarks>
public class TabBorderVisibilityConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values[0] is not TabItem tabItem ||
            values[1] is not TabControl tabControl ||
            values[2] is not int selectedTabIndex)
        {
            return true;
        }

        if (tabControl.TabStripPlacement == Dock.Left && parameter as string == "BottomLeftCorner")
        {
            return true;
        }

        int currentTabIndex = GetTabIndex(tabControl, tabItem);

        bool isFirstTab = currentTabIndex == 0;
        bool isLastTab = currentTabIndex == tabControl.Items.Count - 1;
        bool isSelected = selectedTabIndex == currentTabIndex;
        bool previousTabIsSelected = selectedTabIndex == currentTabIndex - 1;
        bool nextTabIsSelected = selectedTabIndex == currentTabIndex + 1;
        bool isLeftOfSelectedTab = currentTabIndex < selectedTabIndex;
        bool isRightOfSelectedTab = currentTabIndex > selectedTabIndex;

        if (tabControl.TabStripPlacement == Dock.Left && parameter as string == "RightBorder")
        {
            return isSelected;
        }

        switch (parameter)
        {
            case "LeftBorder":
                return !isFirstTab && (isLeftOfSelectedTab || isSelected);
            case "BottomLeftCorner":
                return !isFirstTab && (isLeftOfSelectedTab || isSelected);
            case "RightBorder":
                return isLastTab || isRightOfSelectedTab || isSelected;
            case "BottomRightCorner":
                return isLastTab || isRightOfSelectedTab || isSelected;
            case "BottomLeftBorder":
                return !(previousTabIsSelected || isSelected);
            case "BottomRightBorder":
                return !(nextTabIsSelected || isSelected);
        }

        return true;
    }


    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();

    private static int GetTabIndex(TabControl tabControl, TabItem tabItem) =>
        tabControl.Items.Cast<object>().ToList().IndexOf(tabItem);
}