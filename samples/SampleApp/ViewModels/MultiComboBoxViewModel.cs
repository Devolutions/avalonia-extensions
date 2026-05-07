namespace SampleApp.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using Devolutions.AvaloniaControls.Controls;

public enum MultiComboBoxDemoEnum
{
    EnumA,
    EnumValueB,
}

public partial class MultiComboBoxViewModel : ObservableValidator
{
    public class Item
    {
        public required int Value { get; init; }
        
        public string Text => $"Option {this.Value}";

        public override string ToString() => $"<{nameof(Item)} ({this.Text})>";
    }
    
    [ObservableProperty]
    private MultiComboBoxDemoEnum[] enumValues = Enum.GetValues<MultiComboBoxDemoEnum>();

    [ObservableProperty]
    private string[] items = ["item 1", "item 2", "item 3"];

    [ObservableProperty]
    [Required]
    [MinLength(1)]
    [NotifyDataErrorInfo]
    private AvaloniaList<string> requiredValues = [];

    public MultiComboBoxViewModel()
    {
        this.SelectedItems.CollectionChanged += (_, _) => this.OnPropertyChanged(nameof(this.SelectedText));
        this.SelectedEnumValues.CollectionChanged += (_, _) => this.OnPropertyChanged(nameof(this.SelectedEnumValuesText));
        this.LotOfItemsSelectedItems.CollectionChanged += (_, _) => this.OnPropertyChanged(nameof(this.LotOfItemsSelectedItemsText));
        this.InlineSelectedItems.CollectionChanged += (_, _) => this.OnPropertyChanged(nameof(this.InlineSelectedItemsText));

        // Trigger validation when collection changes
        this.RequiredValues.CollectionChanged += (_, _) =>
        {
            this.OnPropertyChanged(nameof(this.RequiredValues));
        };

        for (var i = 1; i <= 1000; ++i)
        {
            this.LotOfItems.Add(new Item { Value = i });
        }

        this.ValidateAllProperties();
    }

    public AvaloniaList<MultiComboBoxItem> InlineSelectedItems { get; } = [];
    
    public string InlineSelectedItemsText => this.InlineSelectedItems.Count > 0 ? string.Join(", ", this.InlineSelectedItems) : "None";
    
    public AvaloniaList<Item> LotOfItems { get; } = [];
    
    public AvaloniaList<Item> LotOfItemsSelectedItems { get; } = [];
    
    public string LotOfItemsSelectedItemsText => this.LotOfItemsSelectedItems.Count > 0 ? string.Join(", ", this.LotOfItemsSelectedItems) : "None";

    public AvaloniaList<MultiComboBoxDemoEnum> SelectedEnumValues { get; } = [MultiComboBoxDemoEnum.EnumA];

    public AvaloniaList<string> SelectedItems { get; } = ["item 2"];

    public string SelectedText => this.SelectedItems.Count > 0 ? string.Join(", ", this.SelectedItems) : "None";

    public string SelectedEnumValuesText => this.SelectedEnumValues.Count > 0 ? string.Join(", ", this.SelectedEnumValues) : "None";
}