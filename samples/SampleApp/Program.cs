namespace SampleApp;

using System;
using Avalonia;
using Avalonia.Dialogs;

internal class Program
{
  // Initialization code. Don't use any Avalonia, third-party APIs or any
  // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
  // yet and stuff might break.
  [STAThread]
  public static void Main(string[] args) => BuildAvaloniaApp()
    .StartWithClassicDesktopLifetime(args);

  // Avalonia configuration, don't remove; also used by visual designer.
  public static AppBuilder BuildAvaloniaApp()
  {
    AppBuilder builder = AppBuilder.Configure<App>().UsePlatformDetect();

    // Routes file pickers through Avalonia's managed dialogs, which are the ones our themes style via
    // ManagedFileChooser.axaml. Without this, Windows and macOS would use their native dialogs and that
    // styling could never be inspected here (see the "File Chooser" page under Experiments).
    // Note this is also what loads Avalonia.Dialogs during SampleApp startup; a host that keeps the
    // native dialogs loads it only on demand, via DeferredResourceInclude in each theme.
    // The guard is for CA1416: the extension is declared desktop-only and this TFM carries no platform.
    if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
    {
      builder = builder.UseManagedSystemDialogs();
    }

    return builder
      .WithInterFont()
      .LogToTrace();
  }
}