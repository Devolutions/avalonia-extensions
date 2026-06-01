namespace SampleApp.ViewModels;

using System.Collections.Generic;
using System.Collections.ObjectModel;

using Avalonia;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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

    [ObservableProperty]
    private DynamicFoodItem? selectedDynamicItem;

    [ObservableProperty]
    private string newItemName = "New item";

    [ObservableProperty]
    private string newItemCategory = "Fruits";

    [ObservableProperty]
    private string editItemName = string.Empty;

    [ObservableProperty]
    private string editItemCategory = string.Empty;

    private int newItemIndex = 1;

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

    public ObservableCollection<DynamicFoodItem> DynamicItems { get; } = new()
    {
        new DynamicFoodItem("Apple", "Fruits"),
        new DynamicFoodItem("Banana", "Fruits"),
        new DynamicFoodItem("Carrot", "Vegetables"),
        new DynamicFoodItem("Broccoli", "Vegetables"),
        new DynamicFoodItem("Chicken", "Meats"),
        new DynamicFoodItem("Beef", "Meats"),
    };

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

    partial void OnSelectedDynamicItemChanged(DynamicFoodItem? value)
    {
        this.EditItemName = value?.Name ?? string.Empty;
        this.EditItemCategory = value?.Category ?? string.Empty;
    }

    [RelayCommand]
    private void AddDynamicItem()
    {
        string name = string.IsNullOrWhiteSpace(this.NewItemName) ? $"New item {this.newItemIndex++}" : this.NewItemName;
        string category = this.NewItemCategory;
        this.DynamicItems.Add(new DynamicFoodItem(name, category));
        this.NewItemName = $"New item {this.newItemIndex++}";
    }

    [RelayCommand]
    private void RemoveSelectedDynamicItem()
    {
        if (this.SelectedDynamicItem is { } selected)
        {
            this.DynamicItems.Remove(selected);
            this.SelectedDynamicItem = null;
        }
    }

    [RelayCommand]
    private void ApplyDynamicItemEdit()
    {
        if (this.SelectedDynamicItem is not { } selected) return;

        selected.Name = string.IsNullOrWhiteSpace(this.EditItemName) ? selected.Name : this.EditItemName;
        selected.Category = string.IsNullOrWhiteSpace(this.EditItemCategory) ? selected.Category : this.EditItemCategory;
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

public partial class DynamicFoodItem : ObservableObject
{
    [ObservableProperty]
    private string name;

    [ObservableProperty]
    private string category;

    public DynamicFoodItem(string name, string category)
    {
        this.name = name;
        this.category = category;
    }

    public override string ToString() => this.Name;
}
