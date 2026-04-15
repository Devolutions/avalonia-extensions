namespace SampleApp.ViewModels;

using System.ComponentModel.DataAnnotations;
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

    public IReadOnlyList<Country> Countries { get; } =
    [
        new("Canada", "CA"),
        new("France", "FR"),
        new("Germany", "DE"),
        new("Japan", "JP"),
        new("United Kingdom", "GB"),
        new("United States", "US"),
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

    public EditableComboBoxViewModel()
    {
        this.ValidateAllProperties();
    }
}

public record Country(string Name, string Code)
{
    public override string ToString() => this.Name;
}