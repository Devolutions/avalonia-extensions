namespace SampleApp.DemoPages;

using Avalonia.Controls;
using Avalonia.Input;

using SampleApp.ViewModels;

public partial class GroupedListBoxDemo : UserControl
{
    public GroupedListBoxDemo()
    {
        this.InitializeComponent();
    }

    private void GroupedListBox_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (this.DataContext is GroupedListBoxViewModel vm)
        {
            vm.DoubleClickedGroupedItem = vm.SelectedGroupedItem;
        }
    }

    private void EmptyGroupListBox_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (this.DataContext is GroupedListBoxViewModel vm)
        {
            vm.DoubleClickedEmptyGroupItem = vm.SelectedEmptyGroupItem;
        }
    }

    private void LargeListBox_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (this.DataContext is GroupedListBoxViewModel vm)
        {
            vm.DoubleClickedLargeItem = vm.SelectedLargeItem;
        }
    }
}
