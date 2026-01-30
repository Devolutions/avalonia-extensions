namespace SampleApp.ViewModels;

using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;

public partial class ComboBoxViewModel : ObservableValidator
{
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required]
    private string requiredTextValue = string.Empty;

    public ComboBoxViewModel()
    {
        this.ValidateAllProperties();
    }
}