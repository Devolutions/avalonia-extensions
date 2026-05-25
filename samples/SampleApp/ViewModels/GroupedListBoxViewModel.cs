namespace SampleApp.ViewModels;

using System.Collections.Generic;

using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// View model for the <see cref="SampleApp.DemoPages.GroupedListBoxDemo"/> page.
/// Mirrors <see cref="GroupedTileListBoxViewModel"/> but is intentionally separate so the two
/// demos can evolve independently and so we exercise the new binding-based grouping API
/// (<c>GroupBinding</c> / <c>GroupOrderBinding</c>) end-to-end.
/// </summary>
public partial class GroupedListBoxViewModel : ObservableObject
{
    // Selected items per scenario.
    [ObservableProperty]
    private FoodItem? selectedGroupedItem;

    [ObservableProperty]
    private FoodItem? selectedEmptyGroupItem;

    [ObservableProperty]
    private FoodItem? selectedLargeItem;

    // Double-clicked items per scenario.
    [ObservableProperty]
    private FoodItem? doubleClickedGroupedItem;

    [ObservableProperty]
    private FoodItem? doubleClickedEmptyGroupItem;

    [ObservableProperty]
    private FoodItem? doubleClickedLargeItem;

    /// <summary>Scenario 1: items partitioned into named groups.</summary>
    public List<FoodItem> GroupedItems { get; } = new()
    {
        new FoodItem("Apple", "Fruits", "Folder"),
        new FoodItem("Banana", "Fruits", "Folder"),
        new FoodItem("Orange", "Fruits", "Folder"),
        new FoodItem("Strawberry", "Fruits", "Folder"),
        new FoodItem("Grapes", "Fruits", "Folder"),
        new FoodItem("Mango", "Fruits", "Folder"),

        new FoodItem("Carrot", "Vegetables", "User"),
        new FoodItem("Broccoli", "Vegetables", "User"),
        new FoodItem("Lettuce", "Vegetables", "User"),
        new FoodItem("Tomato", "Vegetables", "User"),
        new FoodItem("Cucumber", "Vegetables", "User"),
        new FoodItem("Pepper", "Vegetables", "User"),

        new FoodItem("Chicken", "Meats", "Computer"),
        new FoodItem("Beef", "Meats", "Computer"),
        new FoodItem("Pork", "Meats", "Computer"),
        new FoodItem("Fish", "Meats", "Computer"),
        new FoodItem("Lamb", "Meats", "Computer"),
    };

    /// <summary>Scenario 2: items where the empty-string group should sort first.</summary>
    public List<FoodItem> EmptyGroupItems { get; } = new()
    {
        new FoodItem("Water", ""),
        new FoodItem("Salt", ""),
        new FoodItem("Sugar", ""),

        new FoodItem("Coffee", "Beverages"),
        new FoodItem("Tea", "Beverages"),
        new FoodItem("Juice", "Beverages"),
        new FoodItem("Soda", "Beverages"),

        new FoodItem("Chips", "Snacks"),
        new FoodItem("Cookies", "Snacks"),
        new FoodItem("Crackers", "Snacks"),
        new FoodItem("Nuts", "Snacks"),
        new FoodItem("Pretzels", "Snacks"),
    };

    /// <summary>Scenario 3: large dataset (500 items, 3 categories).</summary>
    public List<FoodItem> LargeItems { get; } = GenerateLargeItems();

    /// <summary>Sort selector: "Meats &gt; Fruits &gt; Vegetables" used in Scenario 1.</summary>
    public System.Func<string, int> CategoryGroupOrderSelector { get; } = group => group switch
    {
        "Meats" => 0,
        "Fruits" => 1,
        "Vegetables" => 2,
        _ => 999,
    };

    /// <summary>Sort selector: empty groups first, then alphabetical, used in Scenario 2.</summary>
    public System.Func<string, int> EmptyGroupFirstSelector { get; } = group => string.IsNullOrEmpty(group) ? 0 : 1;

    private static List<FoodItem> GenerateLargeItems()
    {
        List<FoodItem> items = new(500);
        for (int i = 1; i <= 200; i++)
        {
            items.Add(new FoodItem($"Alpha Item {i:D3}", "Category Alpha"));
        }

        for (int i = 1; i <= 200; i++)
        {
            items.Add(new FoodItem($"Beta Item {i:D3}", "Category Beta"));
        }

        for (int i = 1; i <= 100; i++)
        {
            items.Add(new FoodItem($"Gamma Item {i:D3}", "Category Gamma"));
        }

        return items;
    }
}
