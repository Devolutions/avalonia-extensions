namespace SampleApp.ViewModels;

using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;

public partial class EditableComboBoxViewModel : ObservableValidator
{
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required]
    private string requiredTextValue = string.Empty;

    public EditableComboBoxViewModel()
    {
        this.ValidateAllProperties();
    }
}