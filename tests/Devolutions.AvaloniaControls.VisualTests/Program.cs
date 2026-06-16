using Avalonia;
using SampleApp;

namespace Devolutions.AvaloniaControls.VisualTests;

public class Program
{
    // Required for the Avalonia Designer to work in Rider when it picks this project as the host.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    public static void Main(string[] args)
    {
    }
}
