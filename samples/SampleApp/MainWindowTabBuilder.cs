namespace SampleApp;

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using SampleApp.ControlCatalog;
using SampleApp.Controls;

internal static class MainWindowTabBuilder
{
  public static IReadOnlyList<TabItem> BuildControlTabs()
  {
    ControlRegistry.EnsureValid();
    return [.. ControlRegistry.All.Select(CreateControlTab)];
  }

  private static TabItem CreateControlTab(ControlCatalogEntry control) =>
    new()
    {
      Header = CreateHeader(control),
      Content = CreateContent(control),
    };

  private static SampleItemHeader CreateHeader(ControlCatalogEntry control) =>
    new()
    {
      Title = control.Title,
      ApplicableTo = control.ApplicableToCsv,
    };

  private static Control CreateContent(ControlCatalogEntry control)
  {
#if !ENABLE_ACCELERATE
    if (control.Source == ControlSource.AvaloniaPro)
    {
      return CreateAvaloniaProPlaceholder(control);
    }
#endif

    return CreateDemoContent(control);
  }

  private static Control CreateDemoContent(ControlCatalogEntry control)
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

  private static Control CreateAvaloniaProPlaceholder(ControlCatalogEntry control) =>
    new TextBlock
    {
      Text = $"{control.ControlTypeName} requires Avalonia Pro.\nAdd AVALONIA_LICENSE_KEY to a .env file at the repo root and rebuild.",
      TextWrapping = Avalonia.Media.TextWrapping.Wrap,
      HorizontalAlignment = HorizontalAlignment.Center,
      VerticalAlignment = VerticalAlignment.Center,
      Margin = new Thickness(24),
    };
}
