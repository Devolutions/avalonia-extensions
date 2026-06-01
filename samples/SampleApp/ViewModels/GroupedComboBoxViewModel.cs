namespace SampleApp.ViewModels;

using System.Collections.Generic;

using Avalonia;

using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// View model for the <see cref="SampleApp.DemoPages.GroupedComboBoxDemo"/> page.
/// </summary>
public partial class GroupedComboBoxViewModel : ObservableObject
{
    [ObservableProperty]
    private FoodItem? selectedGroupedItem;

    [ObservableProperty]
    private FoodItem? selectedEmptyGroupItem;

    [ObservableProperty]
    private FoodItem? selectedEmptyGroupItemWithName;

    [ObservableProperty]
    private FoodItem? selectedLargeItem;

    [ObservableProperty]
    private Thickness headerMargin = new(4, 6, 8, 4);

    [ObservableProperty]
    private string headerMarginText = "4 6 8 4";

    public List<FoodItem> GroupedItems { get; } = new()
    {
        new FoodItem("Apple", "Fruits", "Folder"),
        new FoodItem("Banana", "Fruits", "Folder"),
        new FoodItem("Orange", "Fruits", "Folder"),
        new FoodItem("Strawberry", "Fruits", "Folder"),
        new FoodItem("Carrot", "Vegetables", "User"),
        new FoodItem("Broccoli", "Vegetables", "User"),
        new FoodItem("Lettuce", "Vegetables", "User"),
        new FoodItem("Chicken", "Meats", "Computer"),
        new FoodItem("Beef", "Meats", "Computer"),
        new FoodItem("Pork", "Meats", "Computer"),
    };

    public List<FoodItem> EmptyGroupItems { get; } = new()
    {
        new FoodItem("Water", ""),
        new FoodItem("Salt", ""),
        new FoodItem("Sugar", ""),
        new FoodItem("Coffee", "Beverages"),
        new FoodItem("Tea", "Beverages"),
        new FoodItem("Juice", "Beverages"),
        new FoodItem("Chips", "Snacks"),
        new FoodItem("Cookies", "Snacks"),
        new FoodItem("Crackers", "Snacks"),
    };

    public List<FoodItem> LargeItems { get; } = GenerateLargeItems();

    public System.Func<string, int> CategoryGroupOrderSelector { get; } = group => group switch
    {
        "Meats" => 0,
        "Fruits" => 1,
        "Vegetables" => 2,
        _ => 999,
    };

    public System.Func<string, int> EmptyGroupFirstSelector { get; } = group => string.IsNullOrEmpty(group) ? 0 : 1;

    partial void OnHeaderMarginTextChanged(string value)
    {
        try
        {
            this.HeaderMargin = Thickness.Parse(value);
        }
        catch (FormatException)
        {
        }
    }

    private static List<FoodItem> GenerateLargeItems()
    {
        List<FoodItem> items = new(1500);

        for (int i = 1; i <= 600; ++i)
        {
            items.Add(new FoodItem($"Alpha Item {i:D3}", "Category Alpha"));
        }

        for (int i = 1; i <= 600; ++i)
        {
            items.Add(new FoodItem($"Beta Item {i:D3}", "Category Beta"));
        }

        for (int i = 1; i <= 300; ++i)
        {
            items.Add(new FoodItem($"Gamma Item {i:D3}", "Category Gamma"));
        }

        return items;
    }
}
