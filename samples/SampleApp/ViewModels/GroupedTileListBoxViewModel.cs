namespace SampleApp.ViewModels;

using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

// Data model for the demo
public record FoodItem(string Name, string Category);

public partial class GroupedTileListBoxViewModel : ObservableObject
{
    // Selected items for each scenario
    [ObservableProperty]
    private FoodItem? _selectedGroupedItem;

    [ObservableProperty]
    private FoodItem? _selectedEmptyGroupItem;

    [ObservableProperty]
    private FoodItem? _selectedFlatItem;

    // Scenario 1: Named groups
    public List<FoodItem> GroupedItems { get; } = new()
    {
        // Fruits
        new FoodItem("Apple", "Fruits"),
        new FoodItem("Banana", "Fruits"),
        new FoodItem("Orange", "Fruits"),
        new FoodItem("Strawberry", "Fruits"),
        new FoodItem("Grapes", "Fruits"),
        new FoodItem("Mango", "Fruits"),

        // Vegetables
        new FoodItem("Carrot", "Vegetables"),
        new FoodItem("Broccoli", "Vegetables"),
        new FoodItem("Lettuce", "Vegetables"),
        new FoodItem("Tomato", "Vegetables"),
        new FoodItem("Cucumber", "Vegetables"),
        new FoodItem("Pepper", "Vegetables"),

        // Meats
        new FoodItem("Chicken", "Meats"),
        new FoodItem("Beef", "Meats"),
        new FoodItem("Pork", "Meats"),
        new FoodItem("Fish", "Meats"),
        new FoodItem("Lamb", "Meats"),
    };

    // Scenario 2: Empty group first
    public List<FoodItem> EmptyGroupItems { get; } = new()
    {
        // Empty group (Uncategorized)
        new FoodItem("Water", ""),
        new FoodItem("Salt", ""),
        new FoodItem("Sugar", ""),

        // Beverages
        new FoodItem("Coffee", "Beverages"),
        new FoodItem("Tea", "Beverages"),
        new FoodItem("Juice", "Beverages"),
        new FoodItem("Soda", "Beverages"),

        // Snacks
        new FoodItem("Chips", "Snacks"),
        new FoodItem("Cookies", "Snacks"),
        new FoodItem("Crackers", "Snacks"),
        new FoodItem("Nuts", "Snacks"),
        new FoodItem("Pretzels", "Snacks"),
    };

    // Scenario 3: Flat list (no grouping)
    public List<FoodItem> FlatItems { get; } = new()
    {
        new FoodItem("Pizza", "Fast Food"),
        new FoodItem("Burger", "Fast Food"),
        new FoodItem("Pasta", "Italian"),
        new FoodItem("Sushi", "Japanese"),
        new FoodItem("Tacos", "Mexican"),
        new FoodItem("Salad", "Healthy"),
        new FoodItem("Soup", "Comfort"),
        new FoodItem("Sandwich", "Quick"),
        new FoodItem("Steak", "Premium"),
        new FoodItem("Rice", "Staple"),
        new FoodItem("Bread", "Bakery"),
        new FoodItem("Cheese", "Dairy"),
    };

    // Group selector function - extracts Category from FoodItem
    public Func<object, object> CategoryGroupSelector { get; } = item => ((FoodItem)item).Category;

    // Group order selector - empty groups first, then alphabetical
    public Func<object, int> EmptyGroupFirstSelector { get; } = group => string.IsNullOrEmpty((string)group) ? 0 : 1;
}
