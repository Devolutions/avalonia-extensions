namespace SampleApp.ViewModels;

using System.ComponentModel.DataAnnotations;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

public partial class ComboBoxViewModel : ObservableValidator
{
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required]
    private string requiredTextValue = string.Empty;

    public IReadOnlyList<string> LargeItemsList { get; } =
        Enumerable.Range(1, 1000).Select(i => $"Item {i}").ToList();

    public ComboBoxViewModel()
    {
        this.ValidateAllProperties();
    }
}