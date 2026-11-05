namespace SampleApp.Experiments;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

/// <summary>
/// Exercises Avalonia's managed file dialogs, which are what each theme's ManagedFileChooser.axaml styles.
/// SampleApp enables them in <see cref="Program.BuildAvaloniaApp"/>; without that, platforms with native
/// file dialogs (Windows, macOS) would never show these styles at all.
/// </summary>
public partial class FileChooserDemo : UserControl
{
  private static readonly FilePickerFileType[] FileTypes =
  [
    new("Text documents") { Patterns = ["*.txt", "*.md", "*.log"] },
    new("Images") { Patterns = ["*.png", "*.jpg", "*.jpeg", "*.gif", "*.svg"] },
    FilePickerFileTypes.All,
  ];

  public FileChooserDemo() => this.InitializeComponent();

  private IStorageProvider? Storage => TopLevel.GetTopLevel(this)?.StorageProvider;

  private async void OnOpenFile(object? sender, RoutedEventArgs e) =>
    await this.RunAsync(async storage =>
    {
      IReadOnlyList<IStorageFile> files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
      {
        Title = "Open a file",
        AllowMultiple = false,
        FileTypeFilter = FileTypes,
      });

      return Describe("Opened", files);
    });

  private async void OnOpenFiles(object? sender, RoutedEventArgs e) =>
    await this.RunAsync(async storage =>
    {
      IReadOnlyList<IStorageFile> files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
      {
        Title = "Open several files",
        AllowMultiple = true,
        FileTypeFilter = FileTypes,
      });

      return Describe("Opened", files);
    });

  private async void OnSaveFile(object? sender, RoutedEventArgs e) =>
    await this.RunAsync(async storage =>
    {
      IStorageFile? file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
      {
        Title = "Save a file",
        SuggestedFileName = "devolutions-theme-test.txt",
        DefaultExtension = "txt",
        ShowOverwritePrompt = true,
        FileTypeChoices = FileTypes,
      });

      return Describe("Saving to", file is null ? [] : new IStorageItem[] { file });
    });

  private async void OnOpenFolder(object? sender, RoutedEventArgs e) =>
    await this.RunAsync(async storage =>
    {
      IReadOnlyList<IStorageFolder> folders = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
      {
        Title = "Select a folder",
        AllowMultiple = false,
      });

      return Describe("Selected", folders);
    });

  private static string Describe(string verb, IReadOnlyList<IStorageItem> items)
  {
    if (items.Count == 0)
    {
      return "Cancelled — nothing was picked.";
    }

    IEnumerable<string> lines = items.Select(static item =>
      item.TryGetLocalPath() is { Length: > 0 } local ? local : item.Path.ToString());

    return $"{verb} {items.Count} item(s):{Environment.NewLine}{string.Join(Environment.NewLine, lines)}";
  }

  /// <summary>
  /// Event handlers here are <c>async void</c>, so an exception would otherwise tear the app down. Showing
  /// it in the result pane keeps a failed pick debuggable instead of fatal.
  /// </summary>
  private async Task RunAsync(Func<IStorageProvider, Task<string>> pick)
  {
    if (this.Storage is not { } storage)
    {
      this.ResultText.Text = "No StorageProvider on this TopLevel.";
      return;
    }

    try
    {
      this.ResultText.Text = await pick(storage);
    }
    catch (Exception ex)
    {
      this.ResultText.Text = $"{ex.GetType().Name}: {ex.Message}";
    }
  }
}
