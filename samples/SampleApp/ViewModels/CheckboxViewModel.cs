namespace SampleApp.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;

public partial class CheckboxViewModel : ObservableObject
{
    [ObservableProperty]
    private bool isChecked;
}