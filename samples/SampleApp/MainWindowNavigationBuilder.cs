namespace SampleApp;

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using SampleApp.PageCatalog;
using SampleApp.Controls;

internal static class MainWindowNavigationBuilder
{
  private static readonly IBrush AvaloniaProBadgeBackground = new SolidColorBrush(Color.Parse("#7C3AED"));
  private static readonly IBrush DevolutionsBadgeBackground = new SolidColorBrush(Color.Parse("#0068C3"));

  public static SampleItemHeader CreateHeader(PageCatalogEntry control)
  {
    if (string.IsNullOrWhiteSpace(App.EffectiveCatalogThemeName))
    {
      return CreateHeader(control, string.Empty, null);
    }

    ThemeId themeId = ThemeIds.Parse(App.EffectiveCatalogThemeName);
    ThemeId? inheritedFrom = control.IsSameAsReference(themeId) ? themeId.GetSameAsReference() : null;
    return CreateHeader(control, control.GetEffectiveStatusSymbol(themeId), inheritedFrom);
  }

  private static SampleItemHeader CreateHeader(PageCatalogEntry control, string statusSymbol, ThemeId? inheritedFrom)
  {
    string? statusTooltip = string.IsNullOrWhiteSpace(statusSymbol)
      ? null
      : PageRegistry.GetStatusDescription(statusSymbol);

    if (statusTooltip != null && inheritedFrom is { } referenceTheme)
    {
      statusTooltip = $"↔️ Identical to {GetCatalogColumnName(referenceTheme)}. {statusTooltip}";
    }

    (string? badgeText, IBrush? badgeBackground) = GetSourceBadge(control.Source);

    return new SampleItemHeader
    {
      Title = control.Title,
      StatusSymbol = statusSymbol,
      StatusTooltip = statusTooltip,
      IsStatusInherited = inheritedFrom != null,
      SourceBadgeText = badgeText,
      SourceBadgeBackground = badgeBackground,
    };
  }

  // Spelled as in page-catalog.jsonc's status columns, which differ in casing from the theme names.
  private static string GetCatalogColumnName(ThemeId themeId) =>
    themeId switch
    {
      ThemeId.WinUiClassic => "WinUIClassic",
      ThemeId.WinUiMica => "WinUIMica",
      _ => themeId.ToThemeName(),
    };

  private static (string? badgeText, IBrush? badgeBackground) GetSourceBadge(ControlSource? source) =>
    source switch
    {
      ControlSource.AvaloniaPro => ("Pro", AvaloniaProBadgeBackground),
      ControlSource.Devolutions => ("Devo", DevolutionsBadgeBackground),
      _ => (null, null),
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
