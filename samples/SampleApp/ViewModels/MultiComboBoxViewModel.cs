namespace SampleApp.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;

public enum MultiComboBoxDemoEnum
{
    EnumA,
    EnumValueB,
}

public partial class MultiComboBoxViewModel : ObservableValidator
{
    [ObservableProperty]
    private MultiComboBoxDemoEnum[] enumValues = Enum.GetValues<MultiComboBoxDemoEnum>();

    [ObservableProperty]
    private string[] items = ["item 1", "item 2", "item 3"];

    [ObservableProperty]
    private ObservableCollection<string> lotOfItems = [];

    [ObservableProperty]
    [Required]
    [MinLength(1)]
    [NotifyDataErrorInfo]
    private AvaloniaList<string> requiredValues = [];

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

        // Trigger validation when collection changes
        this.RequiredValues.CollectionChanged += (_, _) =>
        {
            this.OnPropertyChanged(nameof(this.RequiredValues));
        };

        for (var i = 1; i <= 1000; ++i)
        {
            this.lotOfItems.Add("Option " + i);
        }

        this.ValidateAllProperties();
    }

    public string SelectedText => this.SelectedItems.Count > 0 ? string.Join(", ", this.SelectedItems) : "None";

    public string SelectedEnumValuesText => this.SelectedEnumValues.Count > 0 ? string.Join(", ", this.SelectedEnumValues) : "None";
}