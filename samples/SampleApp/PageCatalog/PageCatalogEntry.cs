namespace SampleApp.PageCatalog;

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;

public enum ControlSource
{
  Avalonia,
  AvaloniaPro,
  Devolutions,
}

public sealed class PageCatalogEntry
{
  public PageCatalogEntry(
    string key,
    string section,
    string title,
    Type pageType,
    ControlSource? source,
    IReadOnlyList<string> categoryPath,
    IReadOnlyDictionary<ThemeId, string> statusByTheme,
    IReadOnlyCollection<ThemeId>? excludeFromTests = null,
    Type? viewModelType = null)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(key);
    ArgumentException.ThrowIfNullOrWhiteSpace(section);
    ArgumentException.ThrowIfNullOrWhiteSpace(title);
    ArgumentNullException.ThrowIfNull(pageType);
    ArgumentNullException.ThrowIfNull(categoryPath);
    ArgumentNullException.ThrowIfNull(statusByTheme);

    this.Key = key;
    this.Section = section;
    this.Title = title;
    this.PageType = pageType;
    this.Source = source;
    this.CategoryPath = categoryPath.ToArray();
    this.StatusByTheme = new Dictionary<ThemeId, string>(statusByTheme);
    this.ExcludeFromTests = excludeFromTests == null
      ? new HashSet<ThemeId>()
      : new HashSet<ThemeId>(excludeFromTests);
    this.ViewModelType = viewModelType;
  }

  public string Key { get; }

  public string Section { get; }

  public string Title { get; }

  public Type PageType { get; }

  public Type? ViewModelType { get; }

  public ControlSource? Source { get; }

  public IReadOnlyList<string> CategoryPath { get; }

  public IReadOnlyDictionary<ThemeId, string> StatusByTheme { get; }

  public IReadOnlySet<ThemeId> ExcludeFromTests { get; }

  public string CategoryPathText => string.Join("/", this.CategoryPath);

  public string ApplicableToCsv =>
    ThemeIds.ToCsv(
      this.StatusByTheme
        .Where(pair => !PageRegistry.IsNotSupportedSymbol(pair.Value))
        .Select(static pair => pair.Key));

  public string GetStatusSymbol(ThemeId themeId)
  {
    if (this.StatusByTheme.TryGetValue(themeId, out string? symbol))
    {
      return symbol;
    }

    throw new InvalidOperationException($"No status symbol defined for '{this.Key}' and theme '{themeId}'.");
  }

  public string GetStatusSymbol(string themeName)
  {
    ThemeId themeId = ThemeIds.Parse(themeName);
    return this.GetStatusSymbol(themeId);
  }

  public bool ShouldTest(ThemeId themeId) =>
    !PageRegistry.IsNotSupportedSymbol(this.GetStatusSymbol(themeId)) &&
    !this.ExcludeFromTests.Contains(themeId);

  public static IReadOnlyList<string> Validate(IReadOnlyList<PageCatalogEntry> controls)
  {
    ArgumentNullException.ThrowIfNull(controls);

    var errors = new List<string>();

    foreach (IGrouping<string, PageCatalogEntry> duplicateKeyGroup in controls
      .GroupBy(static control => control.Key, StringComparer.OrdinalIgnoreCase)
      .Where(static group => group.Count() > 1))
    {
      errors.Add($"Duplicate page key '{duplicateKeyGroup.Key}'.");
    }

    foreach (PageCatalogEntry control in controls)
    {
      if (control.CategoryPath.Count > 0 && control.CategoryPath.Any(string.IsNullOrWhiteSpace))
      {
        errors.Add($"'{control.Key}' has an invalid category path segment.");
      }

      foreach (ThemeId themeId in ThemeIds.All)
      {
        if (!control.StatusByTheme.ContainsKey(themeId))
        {
          errors.Add($"'{control.Key}' is missing a status symbol for theme '{themeId}'.");
        }
      }

      foreach (ThemeId unexpectedTheme in control.StatusByTheme.Keys.Except(ThemeIds.All))
      {
        errors.Add($"'{control.Key}' defines an unexpected theme '{unexpectedTheme}'.");
      }

      foreach ((ThemeId themeId, string symbol) in control.StatusByTheme)
      {
        if (!PageRegistry.StatusDescriptions.ContainsKey(symbol))
        {
          errors.Add($"'{control.Key}' has unknown status symbol '{symbol}' for theme '{themeId}'.");
        }
      }

      foreach (ThemeId excludedTheme in control.ExcludeFromTests)
      {
        if (!ThemeIds.All.Contains(excludedTheme))
        {
          errors.Add($"'{control.Key}' excludes unknown theme '{excludedTheme}' from tests.");
        }
      }

      if (!typeof(Control).IsAssignableFrom(control.PageType))
      {
        errors.Add($"Page type '{control.PageType.FullName}' for '{control.Key}' must derive from {nameof(Control)}.");
      }
      else
      {
        if (control.PageType.IsAbstract)
        {
          errors.Add($"Page type '{control.PageType.FullName}' for '{control.Key}' cannot be abstract.");
        }

        if (control.PageType.GetConstructor(Type.EmptyTypes) == null)
        {
          errors.Add($"Page type '{control.PageType.FullName}' for '{control.Key}' must have a public parameterless constructor.");
        }
      }

      if (control.ViewModelType != null)
      {
        if (control.ViewModelType.IsAbstract)
        {
          errors.Add($"ViewModel type '{control.ViewModelType.FullName}' for '{control.Key}' cannot be abstract.");
        }

        if (control.ViewModelType.GetConstructor(Type.EmptyTypes) == null)
        {
          errors.Add($"ViewModel type '{control.ViewModelType.FullName}' for '{control.Key}' must have a public parameterless constructor.");
        }
      }
    }

    return errors;
  }
}
