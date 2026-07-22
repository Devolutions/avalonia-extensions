namespace SampleApp.ViewModels;

using System;
using System.Collections.Generic;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

// Data model for the demo
public record FoodGroup(string Name, int SortOrder);

public record FoodItem(string Name, FoodGroup Group, string? IconName = null)
{
    // Convenience constructor for the many demos that group by name and don't care about group order;
    // each gets its own (value-equal) FoodGroup with a default sort order.
    public FoodItem(string name, string category, string? iconName = null)
        : this(name, new FoodGroup(category, 0), iconName)
    {
    }

    // Back-compat for the existing {Binding Category} usages across the demos.
    public string Category => this.Group.Name;
}

public partial class GroupedTileListBoxViewModel : ObservableObject
{
    // Selected items for each scenario
    [ObservableProperty]
    private FoodItem? selectedGroupedItem;

    [ObservableProperty]
    private FoodItem? selectedEmptyGroupItem;

    [ObservableProperty]
    private FoodItem? selectedFlatItem;

    // Double-clicked items for each scenario
    [ObservableProperty]
    private FoodItem? doubleClickedGroupedItem;

    [ObservableProperty]
    private FoodItem? doubleClickedEmptyGroupItem;

    [ObservableProperty]
    private FoodItem? doubleClickedFlatItem;

    [ObservableProperty]
    private FoodItem? selectedLargeItem;

    [ObservableProperty]
    private FoodItem? doubleClickedLargeItem;

    // Scenario 4 auto-scroll test: target index for the "Select & scroll" button
    [ObservableProperty]
    private int largeScrollTargetIndex = 350;

    [ObservableProperty]
    private bool largeAutoScrollToSelectedItem = true;

    public GroupedTileListBoxViewModel()
    {
        // Pre-select a deep item so the "selected on open" auto-scroll path
        this.SelectedLargeItem = this.LargeItems[this.LargeScrollTargetIndex];
    }

    [RelayCommand]
    private void SelectLargeAtTarget()
    {
        if (this.LargeScrollTargetIndex >= 0 && this.LargeScrollTargetIndex < this.LargeItems.Count)
        {
            // Setting SelectedLargeItem from the VM exercises the external
            // selection-change path the AutoScrollToSelectedItem fix targets.
            this.SelectedLargeItem = this.LargeItems[this.LargeScrollTargetIndex];
        }
    }

    // Scenario 5: Multiple selection. Two-way bound to GroupedTileListBox.SelectedItems;
    // the control mutates this collection directly and the demo observes it live.
    public AvaloniaList<FoodItem> MultiSelectedItems { get; } = new();

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
    public Func<object, string> CategoryGroupSelector { get; } = item => ((FoodItem)item).Category;
    
    public Func<string, int> CategoryGroupOrderSelector { get; } = group => group switch
    {
        "Meats" => 0,
        "Fruits" => 1,
        "Vegetables" => 2,
        _ => 999,
    };

    // Group order selector - empty groups first, then alphabetical
    public Func<string, int> EmptyGroupFirstSelector { get; } = group => string.IsNullOrEmpty((string)group) ? 0 : 1;

    // Scenario 4: Large dataset for virtualization testing
    public List<FoodItem> LargeItems { get; } = GenerateLargeItems();

    private static List<FoodItem> GenerateLargeItems()
    {
        var items = new List<FoodItem>(500);
        for (int i = 1; i <= 200; i++)
            items.Add(new FoodItem($"Alpha Item {i:D3}", "Category Alpha"));
        for (int i = 1; i <= 200; i++)
            items.Add(new FoodItem($"Beta Item {i:D3}", "Category Beta"));
        for (int i = 1; i <= 100; i++)
            items.Add(new FoodItem($"Gamma Item {i:D3}", "Category Gamma"));
        return items;
    }
}
