namespace Devolutions.AvaloniaControls.Tests;

using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Devolutions.AvaloniaControls.Controls;
using Devolutions.AvaloniaTheme.WinUI.Internal;
using SampleApp;
using SampleApp.ViewModels;

[Collection("StylesTest")]
public class MainWindowThemeSelectorTests
{
  [AvaloniaFact]
  public void ThemeSelector_ShowsRequestedGroupsAndLabels()
  {
    App.SetTheme(new MacOsClassicTheme());
    MainWindowViewModel viewModel = new();
    MainWindow window = new() { DataContext = viewModel };

    try
    {
      window.Show();
      Dispatcher.UIThread.RunJobs();

      GroupedComboBox selector = window.FindControl<GroupedComboBox>("Themes")!;
      Assert.NotNull(selector);
      Assert.Null(selector.EmptyGroupName);
      Assert.Equal(
        [
          "Avalonia Fluent",
          "Avalonia Simple",
          "Linux",
          "Yaru",
          "MacOS",
          "Mac (automatic)",
          "Mac Classic",
          "Liquid Glass",
          "Windows",
          "DevExpress",
          "WinUI (automatic)",
          "WinUI (Win10)",
          "WinUI (Win11 Mica)",
        ],
        selector.Items.Select(item => item is Theme theme ? theme.DisplayName : item!.ToString()));
      Assert.Equal(3, selector.Items.OfType<ComboBoxGroupHeader>().Count());
      Assert.Equal(viewModel.AvailableThemes, selector.Items.OfType<Theme>());
      Assert.Same(viewModel.CurrentTheme, selector.SelectedItem);
    }
    finally
    {
      window.Close();
      Dispatcher.UIThread.RunJobs();
    }
  }

  [AvaloniaTheory]
  [InlineData("MacClassic")]
  [InlineData("LiquidGlass")]
  [InlineData("Linux")]
  [InlineData("DevExpress")]
  [InlineData("WinUiClassic")]
  [InlineData("WinUiMica")]
  [InlineData("Fluent")]
  [InlineData("Simple")]
  public void MainWindow_PopupFitsThemesAndNavigationScrollbarClearsBadges(string themeName)
  {
    MainWindowViewModel viewModel = new();
    App.SetTheme(viewModel.AvailableThemes.Single(theme => theme.Name == themeName));
    viewModel = new MainWindowViewModel();
    MainWindow window = new() { DataContext = viewModel, WindowState = WindowState.Normal, Width = 1200, Height = 800 };

    try
    {
      window.Show();
      Assert.True(window.TrySelectPageByTitle("GroupedComboBox"));
      Dispatcher.UIThread.RunJobs();

      GroupedComboBox selector = window.FindControl<GroupedComboBox>("Themes")!;
      Assert.True(double.IsPositiveInfinity(selector.MaxDropDownHeight));
      selector.IsDropDownOpen = true;
      Dispatcher.UIThread.RunJobs();

      Popup popup = selector.GetVisualDescendants().OfType<Popup>().Single();
      ScrollViewer popupScroll = popup.Child!.GetVisualDescendants().OfType<ScrollViewer>().Single();
      Assert.True(popupScroll.Extent.Height > 200);
      Assert.True(popupScroll.Extent.Height <= popupScroll.Viewport.Height + 1,
        $"Theme {themeName}: popup extent {popupScroll.Extent.Height} exceeds viewport {popupScroll.Viewport.Height}.");
      Assert.Equal(selector.Items.Count, selector.Items.Select((_, index) => selector.ContainerFromIndex(index)).Count(container => container != null));
      selector.IsDropDownOpen = false;

      window.Height = 450;
      Dispatcher.UIThread.RunJobs();

      TreeView tree = window.FindControl<TreeView>("MainNavigationTree")!;
      ScrollViewer treeScroll = tree.GetVisualDescendants().OfType<ScrollViewer>().Single(scroll => scroll.TemplatedParent == tree);
      ScrollBar scrollbar = treeScroll.GetVisualDescendants().OfType<ScrollBar>().Single(bar => bar.Name == "PART_VerticalScrollBar");
      ScrollContentPresenter presenter = treeScroll.GetVisualDescendants().OfType<ScrollContentPresenter>().Single();
      Assert.True(scrollbar.IsVisible);
      Assert.True(treeScroll.AllowAutoHide);
      Assert.Equal(1, Grid.GetColumnSpan(presenter));

      double scrollbarLeft = scrollbar.TranslatePoint(new Point(), tree)!.Value.X;
      double scrollbarRight = scrollbar.TranslatePoint(new Point(scrollbar.Bounds.Width, 0), tree)!.Value.X;
      Assert.InRange(tree.Bounds.Width - scrollbarRight, 0, 2);

      var badges = tree.GetVisualDescendants().OfType<SampleApp.Controls.SampleItemHeader>()
        .Where(header => header.SourceBadgeText == "Devo")
        .SelectMany(header => header.GetVisualDescendants().OfType<Border>())
        .Where(badge => badge.IsVisible)
        .ToList();
      Assert.NotEmpty(badges);
      foreach (Border badge in badges)
      {
        double badgeRight = badge.TranslatePoint(new Point(badge.Bounds.Width, 0), tree)!.Value.X;
        Assert.True(badgeRight <= scrollbarLeft - 4,
          $"Theme {themeName}: badge right {badgeRight} is too close to scrollbar left {scrollbarLeft}.");
      }
    }
    finally
    {
      window.Close();
      Dispatcher.UIThread.RunJobs();
      Windows11MicaDetector.SetTestOverride(null);
      App.SetTheme(new MacOsClassicTheme());
    }
  }

  [AvaloniaTheory]
  [InlineData("Fluent")]
  [InlineData("Simple")]
  [InlineData("Linux")]
  [InlineData("MacOS")]
  [InlineData("MacClassic")]
  [InlineData("LiquidGlass")]
  [InlineData("DevExpress")]
  [InlineData("WinUI")]
  [InlineData("WinUiClassic")]
  [InlineData("WinUiMica")]
  public void ThemeSelector_PreservesStartupSelectionAndSwitchesTheme(string themeName)
  {
    Theme startupTheme = new MainWindowViewModel().AvailableThemes.Single(theme => theme.Name == themeName);
    App.SetTheme(startupTheme);
    MainWindowViewModel viewModel = new();
    MainWindow window = new() { DataContext = viewModel };

    try
    {
      window.Show();
      Dispatcher.UIThread.RunJobs();

      GroupedComboBox selector = window.FindControl<GroupedComboBox>("Themes")!;
      Assert.Equal(themeName, Assert.IsAssignableFrom<Theme>(selector.SelectedItem).Name);
      Assert.Same(viewModel.CurrentTheme, selector.SelectedItem);

      Theme nextTheme = viewModel.AvailableThemes.Single(theme => theme.Name == (themeName == "Fluent" ? "Simple" : "Fluent"));
      selector.SelectedItem = nextTheme;
      Dispatcher.UIThread.RunJobs();

      Assert.Same(nextTheme, selector.SelectedItem);
      Assert.Same(nextTheme, viewModel.CurrentTheme);
      Assert.Same(nextTheme, App.CurrentTheme);
    }
    finally
    {
      window.Close();
      Dispatcher.UIThread.RunJobs();
      Windows11MicaDetector.SetTestOverride(null);
      App.SetTheme(new MacOsClassicTheme());
    }
  }
}
