using Avalonia;
using Avalonia.Headless;
using SampleApp;

[assembly: AvaloniaTestApplication(typeof(Devolutions.AvaloniaControls.Tests.Program))]

namespace Devolutions.AvaloniaControls.Tests;

public class Program
{
    // Required for the Avalonia Designer to work in Rider when it picks this project as the host.
    public static AppBuilder BuildAvaloniaApp()
    {
        Environment.SetEnvironmentVariable("DEVOLUTIONS_SKIP_WALLPAPER_TINT_SAMPLING", "true");

        return AppBuilder.Configure<App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions
            {
                UseHeadlessDrawing = false
            });
    }

}
