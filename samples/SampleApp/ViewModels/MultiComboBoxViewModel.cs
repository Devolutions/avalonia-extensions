namespace SampleApp.ViewModels;

using System;
using System.Collections.ObjectModel;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;

public enum MultiComboBoxDemoEnum
{
    EnumA,
    EnumValueB,
}

public partial class MultiComboBoxViewModel : ObservableObject
{
    [ObservableProperty]
    private MultiComboBoxDemoEnum[] enumValues = Enum.GetValues<MultiComboBoxDemoEnum>();

    [ObservableProperty]
    private string[] items = ["item 1", "item 2", "item 3"];

    [ObservableProperty]
    private ObservableCollection<string> lotOfItems = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedEnumValuesText))]
    private AvaloniaList<MultiComboBoxDemoEnum> selectedEnumValues = [MultiComboBoxDemoEnum.EnumA];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedText))]
    private AvaloniaList<string> selectedItems = ["item 2"];

    public MultiComboBoxViewModel()
    {
        this.SelectedItems.CollectionChanged += (_, _) => this.OnPropertyChanged(nameof(this.SelectedText));
        this.SelectedEnumValues.CollectionChanged += (_, _) => this.OnPropertyChanged(nameof(this.SelectedEnumValuesText));

        for (var i = 1; i <= 1000; ++i)
        {
            this.lotOfItems.Add("Option " + i);
        }
    }

    public string SelectedText => this.SelectedItems.Count > 0 ? string.Join(", ", this.SelectedItems) : "None";

    public string SelectedEnumValuesText => this.SelectedEnumValues.Count > 0 ? string.Join(", ", this.SelectedEnumValues) : "None";
}