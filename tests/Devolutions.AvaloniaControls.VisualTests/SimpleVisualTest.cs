using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Xunit;
using System.IO;
using System.Threading.Tasks;
using SampleApp;

[assembly: AvaloniaTestApplication(typeof(Devolutions.AvaloniaControls.VisualTests.TestAppBuilder))]

namespace Devolutions.AvaloniaControls.VisualTests;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions
        {
            UseHeadlessDrawing = false
        });
}

public class SimpleVisualTest
{
    [AvaloniaFact]
    public void CaptureButton()
    {
        var window = new Window
        {
            Width = 200,
            Height = 100,
            Content = new Button { Content = "Hello World", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center }
        };

        window.Show();

        // Force a layout pass
        Dispatcher.UIThread.RunJobs();

        // Capture the window
        // Note: In Avalonia.Headless 11.x, we can use the extension method CaptureBitmap on TopLevel/Window
        // Ensure we have 'using Avalonia.Headless;'
        // using var bitmap = window.CaptureBitmap();
        
        var frame = window.CaptureRenderedFrame();
        if (frame == null) throw new System.Exception("CaptureRenderedFrame returned null");
        using var bitmap = frame;
        
        System.Console.WriteLine($"Bitmap size: {bitmap.PixelSize}");
        var path = "/tmp/button_test.png";
        if (File.Exists(path)) File.Delete(path);
        
        bitmap.Save(path);
        
        if (File.Exists(path))
        {
             System.Console.WriteLine($"File saved successfully to {path}");
             var info = new FileInfo(path);
             System.Console.WriteLine($"File size: {info.Length}");
        }
        else
        {
             System.Console.WriteLine($"File NOT found at {path}");
        }
        
        Assert.True(File.Exists(path), $"File should exist at {path}");
    }

    [AvaloniaFact]
    public void CaptureButtonDemo()
    {
        var demo = new SampleApp.DemoPages.ButtonDemo();
        var window = new Window
        {
            Width = 800,
            Height = 600,
            Content = demo
        };
        
        window.Show();
        
        // Wait for layout/rendering
        Dispatcher.UIThread.RunJobs();
        
        var frame = window.CaptureRenderedFrame();
        if (frame == null) throw new System.Exception("CaptureRenderedFrame returned null");
        using var bitmap = frame;
        
        var path = "/tmp/ButtonDemo.png";
        if (File.Exists(path)) File.Delete(path);
        bitmap.Save(path);
        
        Assert.True(File.Exists(path));
        System.Console.WriteLine($"Saved ButtonDemo to {path}");
    }
}
