namespace SampleApp.ViewModels;

using System.ComponentModel.DataAnnotations;
using System.Linq;
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

    public IReadOnlyList<string> LargeItemsList { get; } =
        Enumerable.Range(1, 1000).Select(i => $"Item {i}").ToList();

    public EditableComboBoxViewModel()
    {
        this.ValidateAllProperties();
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