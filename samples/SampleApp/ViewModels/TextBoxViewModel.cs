namespace SampleApp.ViewModels;

using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;

public partial class TextBoxViewModel : ObservableValidator
{
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "This Input is required.")]
    private string requiredInput = string.Empty;

    public TextBoxViewModel()
    {
        this.ValidateAllProperties();
    }
}