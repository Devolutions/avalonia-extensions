namespace SampleApp.DemoPages;

using Avalonia.Controls;
using Avalonia.Threading;
using SampleApp.ViewModels;

public partial class EnumPickerDemo : UserControl
{
    private bool visitedThrowingTextProviderTab;

    public EnumPickerDemo()
    {
        this.InitializeComponent();
    }

    private void EnumPickerDemoTabs_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.Source != sender || sender is not TabControl { SelectedIndex: var selectedIndex } || this.DataContext is not EnumPickerViewModel viewModel)
        {
            return;
        }

        if (selectedIndex == 1)
        {
            this.visitedThrowingTextProviderTab = true;
            viewModel.ResetThrowingTextProviderExcludedValues();
            return;
        }

        if (this.visitedThrowingTextProviderTab)
        {
            Dispatcher.UIThread.Post(() => viewModel.ThrowingTextProviderExcludedValues = null);
        }
    }
}
