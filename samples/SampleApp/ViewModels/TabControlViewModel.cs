namespace SampleApp.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

public record TabItemModel(string Header, string Content, bool IsEnabled = true);

public partial class TabControlViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<TabItemModel> tabItems =
    [
        new("Home", "Welcome to the Home tab! This content is bound from a ViewModel."),
        new("Settings", "Settings tab content goes here. You can configure various options."),
        new("About", "About tab - This demonstrates using ItemsSource with a TabControl."),
        new("Disabled", "This tab is disabled", false),
    ];

    [ObservableProperty]
    private int selectedIndex = 0;
}
