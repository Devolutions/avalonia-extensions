namespace SampleApp;

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using SampleApp.PageCatalog;
using SampleApp.Controls;

internal static class MainWindowTabBuilder
{
  public static IReadOnlyList<TabItem> BuildControlTabs()
  {
    PageRegistry.EnsureValid();
    return [.. PageRegistry.ControlDemos.Select(CreateControlTab)];
  }

  private static TabItem CreateControlTab(PageCatalogEntry control) =>
    new()
    {
      Header = CreateHeader(control),
      Content = CreateContent(control),
    };

  public static SampleItemHeader CreateHeader(PageCatalogEntry control) =>
    new()
    {
      Title = control.Title,
      ApplicableTo = control.ApplicableToCsv,
    };

  public static Control CreateContent(PageCatalogEntry control)
  {
#if !ENABLE_ACCELERATE
    if (control.Source == ControlSource.AvaloniaPro)
    {
      return CreateAvaloniaProPlaceholder(control);
    }
#endif

    return CreateDemoContent(control);
  }

  private static Control CreateDemoContent(PageCatalogEntry control)
  {
    if (Activator.CreateInstance(control.PageType) is not Control content)
    {
      throw new InvalidOperationException($"Catalog page type '{control.PageType.FullName}' for '{control.Key}' must derive from Control.");
    }

    if (control.ViewModelType != null)
    {
      object? viewModel = Activator.CreateInstance(control.ViewModelType);
      if (viewModel == null)
      {
        throw new InvalidOperationException($"Could not create ViewModel '{control.ViewModelType.FullName}' for '{control.Key}'.");
      }

      content.DataContext = viewModel;
    }

    return content;
  }

  private static Control CreateAvaloniaProPlaceholder(PageCatalogEntry control) =>
    new TextBlock
    {
      Text = $"The {control.Title} demo requires Avalonia Pro.\nAdd AVALONIA_LICENSE_KEY to a .env file at the repo root and rebuild.",
      TextWrapping = Avalonia.Media.TextWrapping.Wrap,
      HorizontalAlignment = HorizontalAlignment.Center,
      VerticalAlignment = VerticalAlignment.Center,
      Margin = new Thickness(24),
    };
}
