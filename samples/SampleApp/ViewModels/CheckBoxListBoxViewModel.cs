namespace SampleApp.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

public partial class CheckBoxListBoxViewModel : ObservableObject
{
    [ObservableProperty]
    private string[] items = ["item 1", "item 2", "item 3"];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedText))]
    private ObservableCollection<string> selectedItems = [];

    public CheckBoxListBoxViewModel()
    {
        this.selectedItems.CollectionChanged += (_, _) => this.OnPropertyChanged(nameof(this.SelectedText));
    }

    public string SelectedText => this.SelectedItems.Count > 0 ? string.Join(", ", this.SelectedItems) : "None";
}