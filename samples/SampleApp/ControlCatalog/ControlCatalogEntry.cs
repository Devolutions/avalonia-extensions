namespace SampleApp.ControlCatalog;

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

public sealed class ControlCatalogEntry
{
  public ControlCatalogEntry(
    string key,
    string controlTypeName,
    string title,
    Type pageType,
    ControlSource source,
    IReadOnlyList<string> categoryPath,
    IReadOnlyDictionary<ControlThemeId, string> statusByTheme,
    IReadOnlyCollection<ControlThemeId>? excludeFromTests = null,
    Type? viewModelType = null)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(key);
    ArgumentException.ThrowIfNullOrWhiteSpace(controlTypeName);
    ArgumentException.ThrowIfNullOrWhiteSpace(title);
    ArgumentNullException.ThrowIfNull(pageType);
    ArgumentNullException.ThrowIfNull(categoryPath);
    ArgumentNullException.ThrowIfNull(statusByTheme);

    this.Key = key;
    this.ControlTypeName = controlTypeName;
    this.Title = title;
    this.PageType = pageType;
    this.Source = source;
    this.CategoryPath = categoryPath.ToArray();
    this.StatusByTheme = new Dictionary<ControlThemeId, string>(statusByTheme);
    this.ExcludeFromTests = excludeFromTests == null
      ? new HashSet<ControlThemeId>()
      : new HashSet<ControlThemeId>(excludeFromTests);
    this.ViewModelType = viewModelType;
  }

  public string Key { get; }

  public string ControlTypeName { get; }

  public string Title { get; }

  public Type PageType { get; }

  public Type? ViewModelType { get; }

  public ControlSource Source { get; }

  public IReadOnlyList<string> CategoryPath { get; }

  public IReadOnlyDictionary<ControlThemeId, string> StatusByTheme { get; }

  public IReadOnlySet<ControlThemeId> ExcludeFromTests { get; }

  public string CategoryPathText => string.Join("/", this.CategoryPath);

  public string ApplicableToCsv =>
    ControlThemeIds.ToCsv(
      this.StatusByTheme
        .Where(pair => !ControlRegistry.IsNotSupportedSymbol(pair.Value))
        .Select(static pair => pair.Key));

  public string GetStatusSymbol(ControlThemeId themeId)
  {
    if (this.StatusByTheme.TryGetValue(themeId, out string? symbol))
    {
      return symbol;
    }

    throw new InvalidOperationException($"No status symbol defined for '{this.Key}' and theme '{themeId}'.");
  }

  public string GetStatusSymbol(string themeName)
  {
    ControlThemeId themeId = ControlThemeIds.Parse(themeName);
    return this.GetStatusSymbol(themeId);
  }

  public bool ShouldTest(ControlThemeId themeId) =>
    !ControlRegistry.IsNotSupportedSymbol(this.GetStatusSymbol(themeId)) &&
    !this.ExcludeFromTests.Contains(themeId);

  public static IReadOnlyList<string> Validate(IReadOnlyList<ControlCatalogEntry> controls)
  {
    ArgumentNullException.ThrowIfNull(controls);

    var errors = new List<string>();

    foreach (IGrouping<string, ControlCatalogEntry> duplicateKeyGroup in controls
      .GroupBy(static control => control.Key, StringComparer.OrdinalIgnoreCase)
      .Where(static group => group.Count() > 1))
    {
      errors.Add($"Duplicate control key '{duplicateKeyGroup.Key}'.");
    }

    foreach (ControlCatalogEntry control in controls)
    {
      if (string.IsNullOrWhiteSpace(control.ControlTypeName))
      {
        errors.Add($"'{control.Key}' must define a control type name.");
      }

      if (control.CategoryPath.Count == 0 || control.CategoryPath.Any(string.IsNullOrWhiteSpace))
      {
        errors.Add($"'{control.Key}' must define a non-empty category path.");
      }

      foreach (ControlThemeId themeId in ControlThemeIds.All)
      {
        if (!control.StatusByTheme.ContainsKey(themeId))
        {
          errors.Add($"'{control.Key}' is missing a status symbol for theme '{themeId}'.");
        }
      }

      foreach (ControlThemeId unexpectedTheme in control.StatusByTheme.Keys.Except(ControlThemeIds.All))
      {
        errors.Add($"'{control.Key}' defines an unexpected theme '{unexpectedTheme}'.");
      }

      foreach ((ControlThemeId themeId, string symbol) in control.StatusByTheme)
      {
        if (string.IsNullOrWhiteSpace(symbol))
        {
          errors.Add($"'{control.Key}' has an empty status symbol for theme '{themeId}'.");
        }
        else if (!ControlRegistry.StatusDescriptions.ContainsKey(symbol))
        {
          errors.Add($"'{control.Key}' has unknown status symbol '{symbol}' for theme '{themeId}'.");
        }
      }

      foreach (ControlThemeId excludedTheme in control.ExcludeFromTests)
      {
        if (!ControlThemeIds.All.Contains(excludedTheme))
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
