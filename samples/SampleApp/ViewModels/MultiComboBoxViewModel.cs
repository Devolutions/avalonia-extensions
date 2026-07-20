namespace SampleApp.ViewModels;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using Devolutions.AvaloniaControls.Controls;

public enum MultiComboBoxDemoEnum
{
    EnumA,
    EnumValueB,
}

public partial class MultiComboBoxViewModel : ObservableValidator
{
    public class Item
    {
        public required int Value { get; init; }
        
        public string Text => $"Option {this.Value}";

        public override string ToString() => $"<{nameof(Item)} ({this.Text})>";
    }
    
    [ObservableProperty]
    private MultiComboBoxDemoEnum[] enumValues = Enum.GetValues<MultiComboBoxDemoEnum>();

    [ObservableProperty]
    private string[] items = ["item 1", "item 2", "item 3"];

    [ObservableProperty]
    [Required]
    [MinLength(1)]
    [NotifyDataErrorInfo]
    private AvaloniaList<string> requiredValues = [];

    public MultiComboBoxViewModel()
    {
        // Pre-select part of the "Fruits" group so its header starts in the mixed (indeterminate) state.
        this.GroupedSelectedItems = [this.GroupedItems[0], this.GroupedItems[1]];

        this.SelectedItems.CollectionChanged += (_, _) => this.OnPropertyChanged(nameof(this.SelectedText));
        this.GroupedSelectedItems.CollectionChanged += (_, _) => this.OnPropertyChanged(nameof(this.GroupedSelectedText));
        this.SelectedEnumValues.CollectionChanged += (_, _) => this.OnPropertyChanged(nameof(this.SelectedEnumValuesText));
        this.LotOfItemsSelectedItems.CollectionChanged += (_, _) => this.OnPropertyChanged(nameof(this.LotOfItemsSelectedItemsText));
        this.InlineSelectedItems.CollectionChanged += (_, _) => this.OnPropertyChanged(nameof(this.InlineSelectedItemsText));

        // Trigger validation when collection changes
        this.RequiredValues.CollectionChanged += (_, _) =>
        {
            this.OnPropertyChanged(nameof(this.RequiredValues));
        };

        for (var i = 1; i <= 1000; ++i)
        {
            this.LotOfItems.Add(new Item { Value = i });
        }

        this.ValidateAllProperties();
    }

    public AvaloniaList<MultiComboBoxItem> InlineSelectedItems { get; } = [];
    
    public string InlineSelectedItemsText => this.InlineSelectedItems.Count > 0 ? string.Join(", ", this.InlineSelectedItems) : "None";
    
    public AvaloniaList<Item> LotOfItems { get; } = [];
    
    public AvaloniaList<Item> LotOfItemsSelectedItems { get; } = [];
    
    public string LotOfItemsSelectedItemsText => this.LotOfItemsSelectedItems.Count > 0 ? string.Join(", ", this.LotOfItemsSelectedItems) : "None";

    public AvaloniaList<MultiComboBoxDemoEnum> SelectedEnumValues { get; } = [MultiComboBoxDemoEnum.EnumA];

    public AvaloniaList<string> SelectedItems { get; } = ["item 2"];

    public string SelectedText => this.SelectedItems.Count > 0 ? string.Join(", ", this.SelectedItems) : "None";

    public string SelectedEnumValuesText => this.SelectedEnumValues.Count > 0 ? string.Join(", ", this.SelectedEnumValues) : "None";

    // --- Grouping demo (mirrors the EditableComboBox / GroupedComboBox demo set, for MultiComboBox) ---

    // Shared group objects (name + sort order), reused across the items that belong to them.
    private static readonly FoodGroup Meats = new("Meats", 0);
    private static readonly FoodGroup Fruits = new("Fruits", 1);
    private static readonly FoodGroup Vegetables = new("Vegetables", 2);

    public IReadOnlyList<FoodItem> GroupedItems { get; } =
    [
        new("Apple", Fruits),
        new("Banana", Fruits),
        new("Orange", Fruits),
        new("Strawberry", Fruits),
        new("Carrot", Vegetables),
        new("Broccoli", Vegetables),
        new("Lettuce", Vegetables),
        new("Chicken", Meats),
        new("Beef", Meats),
        new("Pork", Meats),
    ];

    // Start with a partial selection in one group so the group header shows the mixed (indeterminate) state.
    public AvaloniaList<FoodItem> GroupedSelectedItems { get; }

    public AvaloniaList<FoodItem> GroupedOrderBindingSelectedItems { get; } = [];

    // Empty-category items render with no header at the top of the drop-down (when EmptyGroupName is unset);
    // the remaining categories follow under their own headers.
    public IReadOnlyList<FoodItem> EmptyGroupItems { get; } =
    [
        new("Water", ""),
        new("Salt", ""),
        new("Sugar", ""),
        new("Coffee", "Beverages"),
        new("Tea", "Beverages"),
        new("Juice", "Beverages"),
        new("Chips", "Snacks"),
        new("Cookies", "Snacks"),
        new("Crackers", "Snacks"),
    ];

    public AvaloniaList<FoodItem> EmptyGroupSelectedItems { get; } = [];

    public AvaloniaList<FoodItem> EmptyGroupNamedSelectedItems { get; } = [];

    public IReadOnlyList<FoodItem> LargeGroupedItems { get; } = GenerateLargeGroupedItems();

    public AvaloniaList<FoodItem> LargeGroupedSelectedItems { get; } = [];

    public AvaloniaList<FoodItem> HeaderTemplateSelectedItems { get; } = [];

    public Func<string, int> CategoryGroupOrderSelector { get; } = group => group switch
    {
        "Meats" => 0,
        "Fruits" => 1,
        "Vegetables" => 2,
        _ => 999,
    };

    public Func<string, int> EmptyGroupFirstSelector { get; } = group => string.IsNullOrEmpty(group) ? 0 : 1;

    public string GroupedSelectedText =>
        this.GroupedSelectedItems.Count > 0 ? string.Join(", ", this.GroupedSelectedItems.Select(i => i.Name)) : "None";

    private static List<FoodItem> GenerateLargeGroupedItems()
    {
        List<FoodItem> items = new(1500);
        for (int i = 1; i <= 600; ++i) items.Add(new FoodItem($"Alpha Item {i:D3}", "Category Alpha"));
        for (int i = 1; i <= 600; ++i) items.Add(new FoodItem($"Beta Item {i:D3}", "Category Beta"));
        for (int i = 1; i <= 300; ++i) items.Add(new FoodItem($"Gamma Item {i:D3}", "Category Gamma"));
        return items;
    }
}