namespace SampleApp.ViewModels;

using System.Linq;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;

public partial class CheckBoxListBoxViewModel : ObservableObject
{
    [ObservableProperty]
    private string[] items = ["item 1", "item 2", "item 3"];
    
    [ObservableProperty]
    private CheckBoxListBoxDemoItem[] items2 =
    [
        new("Obj 1"),
        new("Obj 2"),
        new("Obj 3"),
    ];

    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedText))]
    private AvaloniaList<string> selectedItems = ["item 2"];
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedText2))]
    private AvaloniaList<CheckBoxListBoxDemoItem> selectedItems2;

    
    public CheckBoxListBoxViewModel()
    {
        this.selectedItems2 = [this.items2[2]];
        
        this.selectedItems.CollectionChanged += (_, _) => this.OnPropertyChanged(nameof(this.SelectedText));
        this.selectedItems2.CollectionChanged += (_, _) => this.OnPropertyChanged(nameof(this.SelectedText2));
    }

    public string SelectedText => this.SelectedItems.Count > 0 ? string.Join(", ", this.SelectedItems) : "None";
    public string SelectedText2 => this.SelectedItems2.Count > 0 ? string.Join(", ", this.SelectedItems2.Select(static i => i.Header)) : "None";
}

public record CheckBoxListBoxDemoItem(string Header);