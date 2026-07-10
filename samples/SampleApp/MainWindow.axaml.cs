namespace SampleApp;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using SampleApp.PageCatalog;
using ViewModels;

public partial class MainWindow : Window
{
  private static readonly string LaunchTime = DateTime.Now.ToString("MMM d, HH:mm");
  private static readonly Lazy<Task<string?>> LaunchBranch = new(GetGitBranchAsync);
  private readonly IBrush tieDyeBrush;
  private MainWindowViewModel? currentViewModel;
  private IReadOnlyList<NavigationNode> navigationNodes = [];
  private string? pendingStartupPageTitle;
  private bool suppressThemeChangeEvents;

  public MainWindow()
  {
    this.InitializeComponent();
    this.InitializeNavigationTree();
    this.tieDyeBrush = this.GenerateTieDyeBrush();

    this.ApplyWindowsMicaBackdrop();

    // Once the window is fully loaded, update background, detect scale, and size containers
    this.Loaded += async (s, e) =>
    {
      this.ApplyStartupPageSelection();
      this.UpdatePreviewBackground();
      this.DetectSystemScale();
      this.InitializeContainerSizes();
      this.ApplyCurrentScale(); // Apply any pre-selected scale
      try
      {
        this.Title = await BuildWindowTitleAsync(this.Title);
      }
      catch
      {
        // Title stays as-is from XAML if anything goes wrong
      }
    };

#if DEBUG
    // Enable Accelerate dev tools (AvaloniaUI.DiagnosticsSupport)
    (Application.Current as App)?.AttachDevToolsOnce();
#endif
  }

  public void SuppressThemeChangeEvents(bool suppress)
  {
    this.suppressThemeChangeEvents = suppress;
  }

  private void Themes_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
  {
    if (this.suppressThemeChangeEvents) return;

    SelectingItemsControl? cb = sender as SelectingItemsControl;
    if (cb?.SelectedItem is Theme newTheme) App.SetTheme(newTheme);
  }

  private void Scale_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
  {
    SelectingItemsControl? cb = sender as SelectingItemsControl;
    if (cb?.SelectedItem is not ScaleOption scaleOption) return;

    Border? container = this.FindControl<Border>("ScaledContainer");
    Canvas? wrapper = this.FindControl<Canvas>("ScaledWrapper");
    if (container == null || wrapper == null) return;

    // Use window's client area dimensions as base size
    double baseWidth = this.ClientSize.Width;
    double baseHeight = this.ClientSize.Height;

    if (Math.Abs(scaleOption.Scale) < 1e-9) // System Default
    {
      container.RenderTransform = null;
      container.Width = baseWidth;
      container.Height = baseHeight;
      wrapper.Width = baseWidth;
      wrapper.Height = baseHeight;
    }
    else
    {
      // Calculate scale relative to system scale (unless its 0/null)
      if (this.RenderScaling <= 0)
      {
        return;
      }

      // e.g., if system is 200% and user selects 100%, scale = 100/200 = 0.5
      double relativeScale = scaleOption.Scale / this.RenderScaling;

      // Apply transform to container
      container.RenderTransform = new ScaleTransform(relativeScale, relativeScale);
      container.RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Relative);
      container.Width = baseWidth;
      container.Height = baseHeight;

      // Size the wrapper Canvas so ScrollViewer knows the scaled bounds
      wrapper.Width = baseWidth * relativeScale;
      wrapper.Height = baseHeight * relativeScale;
    }
  }

  private void InitializeContainerSizes()
  {
    Border? container = this.FindControl<Border>("ScaledContainer");
    Canvas? wrapper = this.FindControl<Canvas>("ScaledWrapper");
    if (container == null || wrapper == null) return;

    // Set initial sizes to match window
    double baseWidth = this.ClientSize.Width;
    double baseHeight = this.ClientSize.Height;

    container.Width = baseWidth;
    container.Height = baseHeight;
    wrapper.Width = baseWidth;
    wrapper.Height = baseHeight;
  }

  private void DetectSystemScale()
  {
    if (this.DataContext is not MainWindowViewModel vm) return;

    // RenderScaling reflects the actual system/display scaling
    vm.SystemScale = this.RenderScaling;
  }

  private void ApplyCurrentScale()
  {
    if (this.DataContext is not MainWindowViewModel vm) return;
    if (vm.SelectedScale == null || vm.SelectedScale.Scale == 0) return;

    Border? container = this.FindControl<Border>("ScaledContainer");
    Canvas? wrapper = this.FindControl<Canvas>("ScaledWrapper");
    if (container == null || wrapper == null) return;

    double baseWidth = this.ClientSize.Width;
    double baseHeight = this.ClientSize.Height;
    double relativeScale = vm.SelectedScale.Scale / this.RenderScaling;

    container.RenderTransform = new ScaleTransform(relativeScale, relativeScale);
    container.RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Relative);
    container.Width = baseWidth;
    container.Height = baseHeight;

    wrapper.Width = baseWidth * relativeScale;
    wrapper.Height = baseHeight * relativeScale;
  }

  private void ApplyWindowsMicaBackdrop()
  {
    // Native Windows 11 Mica backdrop. Avalonia drives the DWM system backdrop when the
    // Mica transparency level is requested; it is a no-op on Windows 10 and non-Windows
    // platforms (the level simply does not apply). This is opt-in for the WinUI Mica
    // variant only -- App.IsWinUiMicaTheme is already resolved from real Win11 detection
    // or the dev override, so the backdrop lights up automatically on Windows 11 with no
    // extra UI. On the Mac the visual approximation comes from the wallpaper preview layer.
    if (!App.IsWinUiMicaTheme) return;

    this.TransparencyLevelHint = new[] { WindowTransparencyLevel.Mica };
  }

  private void UpdatePreviewBackground()
  {
    if (this.currentViewModel == null) return;
    if (this.currentViewModel.SelectedWallpaper == null) return;
    Panel? wallpaperPanel = this.FindControl<Panel>("PreviewWallpaper");
    Panel? contentPanel = this.FindControl<Panel>("MainContentPanel");
    if (wallpaperPanel == null || contentPanel == null) return;

    // Only the translucent preview themes (LiquidGlass, WinUI Mica) render over a wallpaper.
    // For any other theme, keep the wallpaper hidden and the content surface unaffected so a
    // previously selected custom wallpaper does not leak into an opaque theme.
    if (!App.IsWallpaperPreviewTheme)
    {
      wallpaperPanel.Background = Brushes.Transparent;
      contentPanel.SetValue(Panel.BackgroundProperty, AvaloniaProperty.UnsetValue);
      return;
    }

    switch (this.currentViewModel.SelectedWallpaper.Option)
    {
      case WallpaperOption.Psychodelic:
        wallpaperPanel.Background = this.tieDyeBrush;
        contentPanel.SetValue(Panel.BackgroundProperty, AvaloniaProperty.UnsetValue);
        break;
      case WallpaperOption.CustomLight:
        wallpaperPanel.Background = Brushes.Transparent;
        if (this.TryGetResource("PreviewCustomWallpaperLight", this.ActualThemeVariant, out object? lightResource) &&
            lightResource is IBrush lightBrush)
        {
          contentPanel.Background = lightBrush;
        }
        else if (Application.Current!.TryGetResource("PreviewCustomWallpaperLight", this.ActualThemeVariant, out object? lightAppResource) &&
                 lightAppResource is IBrush lightAppBrush)
        {
          contentPanel.Background = lightAppBrush;
        }

        break;
      case WallpaperOption.CustomDark:
        wallpaperPanel.Background = Brushes.Transparent;
        if (this.TryGetResource("PreviewCustomWallpaperDark", this.ActualThemeVariant, out object? darkResource) &&
            darkResource is IBrush darkBrush)
        {
          contentPanel.Background = darkBrush;
        }
        else if (Application.Current!.TryGetResource("PreviewCustomWallpaperDark", this.ActualThemeVariant, out object? darkAppResource) &&
                 darkAppResource is IBrush darkAppBrush)
        {
          contentPanel.Background = darkAppBrush;
        }

        break;
      case WallpaperOption.None:
      default:
        wallpaperPanel.Background = Brushes.Transparent;
        contentPanel.SetValue(Panel.BackgroundProperty, AvaloniaProperty.UnsetValue);
        break;
    }
  }

  // When switching Themes, the ViewModel survives across window recreation, but the window itself is brand new.
  // That's why OnDataContextChanged fires - the new window is receiving the ViewModel for the first time,
  // even though the ViewModel already existed (and remembers things like wallpaper setting).
  protected override void OnDataContextChanged(EventArgs e)
  {
    base.OnDataContextChanged(e);

    MainWindowViewModel? vm = this.DataContext as MainWindowViewModel;

    // Only unsubscribe if we're switching to a different ViewModel
    if (this.currentViewModel != null && this.currentViewModel != vm)
    {
      this.currentViewModel.PropertyChanged -= this.OnViewModelPropertyChanged;
    }

    // Only subscribe if this is a new ViewModel
    if (vm != null && this.currentViewModel != vm)
    {
      vm.PropertyChanged += this.OnViewModelPropertyChanged;
    }

    this.currentViewModel = vm;
  }

  protected override void OnClosed(EventArgs e)
  {
    // Unsubscribe from ViewModel events to prevent memory leaks
    if (this.currentViewModel != null)
    {
      this.currentViewModel.PropertyChanged -= this.OnViewModelPropertyChanged;
      this.currentViewModel = null;
    }

    base.OnClosed(e);
  }

  private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
  {
    if (e.PropertyName == nameof(MainWindowViewModel.SelectedWallpaper))
    {
      this.UpdatePreviewBackground();
    }
  }

  private static async Task<string> BuildWindowTitleAsync(string? baseTitle)
  {
    string prefix = string.IsNullOrWhiteSpace(baseTitle) ? "SampleApp" : baseTitle;
    string? branch = await LaunchBranch.Value;
    return string.IsNullOrEmpty(branch)
      ? $"{prefix} — {LaunchTime}"
      : $"{prefix} — {branch} — {LaunchTime}";
  }

  private static async Task<string?> GetGitBranchAsync()
  {
    try
    {
      using Process process = new();
      process.StartInfo = new ProcessStartInfo("git", "rev-parse --abbrev-ref HEAD")
      {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true,
        WorkingDirectory = AppContext.BaseDirectory
      };
      process.Start();

      // Drain stderr in the background so a chatty git can't fill the pipe buffer and block.
      Task<string> stderrTask = process.StandardError.ReadToEndAsync();

      using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));
      string branch;
      try
      {
        branch = await process.StandardOutput.ReadToEndAsync(cts.Token);
        await process.WaitForExitAsync(cts.Token);
        await stderrTask;
      }
      catch (OperationCanceledException)
      {
        try
        {
          if (!process.HasExited)
          {
            process.Kill(entireProcessTree: true);
            process.WaitForExit(500);
          }
        }
        catch
        {
          // best effort — process may already be exited or unkillable
        }

        return null;
      }

      if (process.ExitCode != 0) return null;

      branch = branch.Trim();
      return string.IsNullOrEmpty(branch) || branch == "HEAD" ? null : branch;
    }
    catch
    {
      return null;
    }
  }

  private IBrush GenerateTieDyeBrush() =>
    // Simple vibrant radial gradient for demo purposes
    new RadialGradientBrush
    {
      GradientOrigin = new RelativePoint(0.4, 0.4, RelativeUnit.Relative),
      Center = new RelativePoint(0.3, 0.3, RelativeUnit.Relative),
      RadiusX = new RelativeScalar(0.8, RelativeUnit.Relative),
      RadiusY = new RelativeScalar(0.8, RelativeUnit.Relative),
      GradientStops = new GradientStops
      {
        new GradientStop(Color.Parse("#FF00FF"), 0),
        new GradientStop(Color.Parse("#00FFFF"), 0.3),
        new GradientStop(Color.Parse("#FFFF00"), 0.6),
        new GradientStop(Color.Parse("#FF0000"), 1)
      }
    };

  public string? GetSelectedPageTitle()
  {
    if (this.FindControl<TreeView>("MainNavigationTree")?.SelectedItem is NavigationNode node && node.Page != null)
    {
      return node.Title;
    }

    return null;
  }

  public void SetPendingStartupPageTitle(string? pageTitle)
  {
    this.pendingStartupPageTitle = pageTitle;
  }

  public bool TrySelectPageByTitle(string title)
  {
    TreeView? treeView = this.FindControl<TreeView>("MainNavigationTree");
    if (treeView == null)
    {
      return false;
    }

    NavigationNode? targetNode = FindMatchingPageNode(this.navigationNodes, title);
    if (targetNode?.Page == null)
    {
      return false;
    }

    ExpandAncestorSections(targetNode);
    treeView.SelectedItem = targetNode;
    this.ShowPage(targetNode);
    return true;
  }

  private void InitializeNavigationTree()
  {
    PageRegistry.EnsureValid();

    this.navigationNodes = BuildNavigationNodes();
    TreeView? treeView = this.FindControl<TreeView>("MainNavigationTree");
    if (treeView == null)
    {
      throw new InvalidOperationException("Main navigation tree was not found.");
    }

    treeView.ItemsSource = this.navigationNodes;
    treeView.Width = CalculateNavigationWidth(this.navigationNodes);

    NavigationNode? initialNode = this.navigationNodes.FirstOrDefault(static node => node.Page != null)
                              ?? this.navigationNodes.SelectMany(static node => node.Children).FirstOrDefault(static node => node.Page != null);
    if (initialNode != null)
    {
      treeView.SelectedItem = initialNode;
      this.ShowPage(initialNode);
    }
  }

  private void ApplyStartupPageSelection()
  {
    string? startupPageTitle = this.pendingStartupPageTitle;
    this.pendingStartupPageTitle = null;

    if (string.IsNullOrWhiteSpace(startupPageTitle) &&
        this.DataContext is MainWindowViewModel viewModel &&
        viewModel.TryConsumeStartupTabTitle(out string configuredTitle) &&
        !string.IsNullOrWhiteSpace(configuredTitle))
    {
      startupPageTitle = configuredTitle;
    }

    if (string.IsNullOrWhiteSpace(startupPageTitle))
    {
      return;
    }

    this.TrySelectPageByTitle(startupPageTitle);
  }

  private static IReadOnlyList<NavigationNode> BuildNavigationNodes()
  {
    var roots = new List<NavigationNode>();
    var sectionLookup = new Dictionary<string, NavigationNode>(StringComparer.OrdinalIgnoreCase);

    foreach (PageCatalogEntry page in PageRegistry.All)
    {
      if (!sectionLookup.TryGetValue(page.Section, out NavigationNode? sectionNode))
      {
        bool expandByDefault = string.Equals(page.Section, PageRegistry.ControlDemosSection, StringComparison.OrdinalIgnoreCase);
        sectionNode = new NavigationNode(
          title: page.Section,
          header: new TextBlock { Text = page.Section, FontWeight = FontWeight.SemiBold },
          page: null,
          depth: 0,
          isExpanded: expandByDefault);
        sectionLookup.Add(page.Section, sectionNode);
        roots.Add(sectionNode);
      }

      object pageHeader = MainWindowNavigationBuilder.CreateHeader(page);

      sectionNode.Children.Add(new NavigationNode(
        title: page.Title,
        header: pageHeader,
        page: page,
        depth: sectionNode.Depth + 1,
        isExpanded: false,
        parent: sectionNode));
    }

    var flattenedRoots = new List<NavigationNode>();
    foreach (NavigationNode root in roots)
    {
      if (root.Children.Count == 1 && root.Children[0].Page != null &&
          string.Equals(root.Title, root.Children[0].Title, StringComparison.OrdinalIgnoreCase))
      {
        flattenedRoots.Add(root.Children[0]);
      }
      else
      {
        flattenedRoots.Add(root);
      }
    }

    return flattenedRoots;
  }

  private static double CalculateNavigationWidth(IEnumerable<NavigationNode> roots)
  {
    int maxTitleScore = roots
      .SelectMany(static root => root.Flatten())
      .Select(static node => node.Title.Length + (node.Depth * 2))
      .DefaultIfEmpty(20)
      .Max();

    return Math.Clamp(maxTitleScore * 11, 280, 700);
  }

  private static NavigationNode? FindMatchingPageNode(IEnumerable<NavigationNode> nodes, string targetTitle)
  {
    string normalizedTarget = targetTitle.Trim();

    List<NavigationNode> pageNodes = nodes
      .SelectMany(static node => node.Flatten())
      .Where(static node => node.Page != null)
      .ToList();

    return pageNodes.FirstOrDefault(node => string.Equals(node.Title, normalizedTarget, StringComparison.OrdinalIgnoreCase)) ??
           pageNodes.FirstOrDefault(node => node.Title.Contains(normalizedTarget, StringComparison.OrdinalIgnoreCase)) ??
           pageNodes.FirstOrDefault(node => normalizedTarget.Contains(node.Title, StringComparison.OrdinalIgnoreCase));
  }

  private static void ExpandAncestorSections(NavigationNode targetNode)
  {
    NavigationNode? current = targetNode.Parent;
    while (current != null)
    {
      current.IsExpanded = true;
      current = current.Parent;
    }
  }

  private void NavigationTree_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
  {
    if (sender is not TreeView treeView || treeView.SelectedItem is not NavigationNode selectedNode)
    {
      return;
    }

    if (selectedNode.Page == null && selectedNode.Children.Count > 0)
    {
      NavigationNode? firstPage = selectedNode.Flatten().FirstOrDefault(static node => node.Page != null);
      if (firstPage != null)
      {
        treeView.SelectedItem = firstPage;
        this.ShowPage(firstPage);
      }

      return;
    }

    this.ShowPage(selectedNode);
  }

  private void ShowPage(NavigationNode selectedNode)
  {
    if (selectedNode.Page == null)
    {
      return;
    }

    ContentControl? pageHost = this.FindControl<ContentControl>("MainPageHost");
    if (pageHost == null)
    {
      throw new InvalidOperationException("Main page host was not found.");
    }

    pageHost.Content = MainWindowNavigationBuilder.CreateContent(selectedNode.Page);
  }

  private sealed class NavigationNode : INotifyPropertyChanged
  {
    private bool isExpanded;

    public NavigationNode(
      string title,
      object header,
      PageCatalogEntry? page,
      int depth,
      bool isExpanded,
      NavigationNode? parent = null)
    {
      this.Title = title;
      this.Header = header;
      this.Page = page;
      this.Depth = depth;
      this.Parent = parent;
      this.isExpanded = isExpanded;
    }

    public string Title { get; }

    public object Header { get; }

    public PageCatalogEntry? Page { get; }

    public int Depth { get; }

    public NavigationNode? Parent { get; }

    public bool IsExpanded
    {
      get => this.isExpanded;
      set
      {
        if (this.isExpanded == value)
        {
          return;
        }

        this.isExpanded = value;
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsExpanded)));
      }
    }

    public List<NavigationNode> Children { get; } = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    public IEnumerable<NavigationNode> Flatten()
    {
      yield return this;

      foreach (NavigationNode child in this.Children.SelectMany(static child => child.Flatten()))
      {
        yield return child;
      }
    }
  }
}