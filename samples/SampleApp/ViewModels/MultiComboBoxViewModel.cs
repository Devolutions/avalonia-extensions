namespace SampleApp.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

public partial class MultiComboBoxViewModel : ObservableObject
{
    [ObservableProperty]
    private string[] items = ["item 1", "item 2", "item 3"];

    [ObservableProperty]
    private ObservableCollection<string> lotOfItems = [];


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedText))]
    private ObservableCollection<string> selectedItems = ["item 2"];

    public MultiComboBoxViewModel()
    {
        this.selectedItems.CollectionChanged += (_, _) => this.OnPropertyChanged(nameof(this.SelectedText));

        for (var i = 1; i <= 1000; ++i)
        {
            this.lotOfItems.Add("Option " + i);
        }
    }

    public string SelectedText => this.SelectedItems.Count > 0 ? string.Join(", ", this.SelectedItems) : "None";
}