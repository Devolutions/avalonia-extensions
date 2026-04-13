namespace SampleApp.ViewModels;

using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;

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

    public EditableComboBoxViewModel()
    {
        this.ValidateAllProperties();
    }
}

public record Country(string Name, string Code)
{
    public override string ToString() => this.Name;
}