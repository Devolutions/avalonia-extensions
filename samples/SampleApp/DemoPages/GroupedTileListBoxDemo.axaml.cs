namespace SampleApp.DemoPages;

using Avalonia.Controls;
using Avalonia.Input;
using SampleApp.ViewModels;

public partial class GroupedTileListBoxDemo : UserControl
{
    public GroupedTileListBoxDemo()
    {
        this.InitializeComponent();
    }

    private void GroupedListBox_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (this.DataContext is GroupedTileListBoxViewModel vm)
        {
            vm.DoubleClickedGroupedItem = vm.SelectedGroupedItem;
        }
    }

    private void EmptyGroupListBox_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (this.DataContext is GroupedTileListBoxViewModel vm)
        {
            vm.DoubleClickedEmptyGroupItem = vm.SelectedEmptyGroupItem;
        }
    }

    private void FlatListBox_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (this.DataContext is GroupedTileListBoxViewModel vm)
        {
            vm.DoubleClickedFlatItem = vm.SelectedFlatItem;
        }
    }
}
