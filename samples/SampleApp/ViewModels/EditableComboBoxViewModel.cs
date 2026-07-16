namespace SampleApp.ViewModels;

using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public partial class EditableComboBoxViewModel : ObservableValidator
{
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required]
    private string requiredTextValue = string.Empty;

    [ObservableProperty]
    private Country? selectedCountry;
    
    [ObservableProperty]
    private Country? selectedCountry2;

    [ObservableProperty]
    private string? selectedLargeItem;

    public IReadOnlyList<string> LargeItemsList { get; } =
        Enumerable.Range(1, 1000).Select(i => $"Item {i}").ToList();

    // --- Grouping demo (mirrors the GroupedComboBox demo set, adapted for EditableComboBox) ---

    [ObservableProperty]
    private FoodItem? selectedGroupedItem;

    [ObservableProperty]
    private FoodItem? selectedEmptyGroupItem;

    [ObservableProperty]
    private FoodItem? selectedEmptyGroupItemWithName;

    [ObservableProperty]
    private FoodItem? selectedLargeGroupedItem;

    [ObservableProperty]
    private DynamicFoodItem? selectedDynamicItem;

    [ObservableProperty]
    private Thickness headerMargin = new(4, 6, 8, 4);

    [ObservableProperty]
    private string headerMarginText = "4 6 8 4";

    [ObservableProperty]
    private string newItemName = "New item";

    [ObservableProperty]
    private string newItemCategory = "Fruits";

    [ObservableProperty]
    private string editItemName = string.Empty;

    [ObservableProperty]
    private string editItemCategory = string.Empty;

    private int newItemIndex = 1;

    public IReadOnlyList<FoodItem> GroupedItems { get; } =
    [
        new("Apple", "Fruits"),
        new("Banana", "Fruits"),
        new("Orange", "Fruits"),
        new("Strawberry", "Fruits"),
        new("Carrot", "Vegetables"),
        new("Broccoli", "Vegetables"),
        new("Lettuce", "Vegetables"),
        new("Chicken", "Meats"),
        new("Beef", "Meats"),
        new("Pork", "Meats"),
    ];

    // Empty-category items render with no header at the top of the drop-down — this is exactly the
    // WinForms ConnectionPicker "current vault at the top, System vault under a header" case.
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

    public IReadOnlyList<FoodItem> LargeGroupedItems { get; } = GenerateLargeGroupedItems();

    public ObservableCollection<DynamicFoodItem> DynamicItems { get; } =
    [
        new("Apple", "Fruits"),
        new("Banana", "Fruits"),
        new("Carrot", "Vegetables"),
        new("Broccoli", "Vegetables"),
        new("Chicken", "Meats"),
        new("Beef", "Meats"),
    ];

    public Func<string, int> CategoryGroupOrderSelector { get; } = group => group switch
    {
        "Meats" => 0,
        "Fruits" => 1,
        "Vegetables" => 2,
        _ => 999,
    };

    public Func<string, int> EmptyGroupFirstSelector { get; } = group => string.IsNullOrEmpty(group) ? 0 : 1;

    public EditableComboBoxViewModel()
    {
        this.SelectedLargeItem = this.LargeItemsList[0];
        this.SelectedCountry = this.Countries[0];
        this.selectedCountry2 = this.Countries2[^1];

        this.ValidateAllProperties();
    }

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

    private static List<FoodItem> GenerateLargeGroupedItems()
    {
        List<FoodItem> items = new(1500);
        for (int i = 1; i <= 600; ++i) items.Add(new FoodItem($"Alpha Item {i:D3}", "Category Alpha"));
        for (int i = 1; i <= 600; ++i) items.Add(new FoodItem($"Beta Item {i:D3}", "Category Beta"));
        for (int i = 1; i <= 300; ++i) items.Add(new FoodItem($"Gamma Item {i:D3}", "Category Gamma"));
        return items;
    }

    public IReadOnlyList<Country> Countries { get; } =
    [
        new("Canada", "CA"),
        new("France", "FR"),
        new("Germany", "DE"),
        new("Japan", "JP"),
        new("United Kingdom", "GB"),
        new("United States", "US"),
    ];

    public IReadOnlyList<Country> Countries2 { get; } =
    [
        new("Canada", "CA", false),
        new("France", "FR", false),
        new("Germany", "DE", false),
        new("Japan", "JP", false),
        new("United Kingdom", "GB", false),
        new("United States", "US", false),
    ];

    // Dynamic ItemsSource toggle demo
    private static readonly IReadOnlyList<Country> CountrySetA =
    [
        new("Canada", "CA"),
        new("France", "FR"),      // common
        new("Germany", "DE"),     // common
        new("United States", "US"),
    ];

    private static readonly IReadOnlyList<Country> CountrySetB =
    [
        new("Australia", "AU"),
        new("France", "FR"),      // common
        new("Germany", "DE"),     // common
        new("Japan", "JP"),
    ];

    [ObservableProperty]
    private IReadOnlyList<Country> dynamicCountries = CountrySetA;

    [ObservableProperty]
    private Country? dynamicSelectedCountry;

    [ObservableProperty]
    private string dynamicSetLabel = "Set A";

    [RelayCommand]
    private void ToggleCountrySet()
    {
        bool isSetA = ReferenceEquals(this.DynamicCountries, CountrySetA);
        this.DynamicCountries = isSetA ? CountrySetB : CountrySetA;
        this.DynamicSetLabel = isSetA ? "Set B" : "Set A";
    }

    // Programmatic SelectedItem demo
    [ObservableProperty]
    private Country? programmaticSelectedCountry;

    [RelayCommand]
    private void SelectFirstCountry() => this.ProgrammaticSelectedCountry = this.Countries[0];

    [RelayCommand]
    private void SelectLastCountry() => this.ProgrammaticSelectedCountry = this.Countries[^1];

    [RelayCommand]
    private void ClearCountry() => this.ProgrammaticSelectedCountry = null;

    [ObservableProperty]
    private IBrush selectedBrush = Brushes.Transparent;
    
    [RelayCommand]
    private void Green()
    {
        this.SelectedBrush = Brushes.Green;
    }

    [RelayCommand]
    private void Orange()
    {
        this.SelectedBrush = Brushes.Orange;
    }

    [RelayCommand]
    private void Red()
    {
        this.SelectedBrush = Brushes.Red;
    }
}

public record Country(string Name, string Code, bool OverrideToString = true)
{
    public override string? ToString() => this.OverrideToString ? this.Name : base.ToString();
}