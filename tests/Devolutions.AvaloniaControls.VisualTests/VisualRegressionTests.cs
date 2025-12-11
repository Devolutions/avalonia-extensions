using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using SampleApp;
using Xunit;

namespace Devolutions.AvaloniaControls.VisualTests;

[Collection("VisualTests")]
public class VisualRegressionTests
{
    private const string BaselinesDirectory = "../../../Screenshots/Baseline";
    private const string TestResultsDirectory = "../../../Screenshots/Test";
    private const string TestDiffsDirectory = "../../../Screenshots/Test-Diffs";

    private static Dictionary<string, List<string>>? _pageThemes;
    private static Dictionary<string, string>? _pageViewModels;

    // Finds applicable themes and ViewModel for each control by parsing MainWindow.axaml
    private static void EnsureMetadataLoaded()
    {
        if (_pageThemes != null && _pageViewModels != null) return;

        _pageThemes = new Dictionary<string, List<string>>();
        _pageViewModels = new Dictionary<string, string>();
        
        // Locate MainWindow.axaml relative to the test execution directory
        // Assuming running from bin/Debug/net9.0/ or similar
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var mainWindowPath = Path.GetFullPath(Path.Combine(baseDir, "../../../../../samples/SampleApp/MainWindow.axaml"));

        if (!File.Exists(mainWindowPath))
        {
             throw new FileNotFoundException($"Could not find MainWindow.axaml at {mainWindowPath}");
        }

        var content = File.ReadAllText(mainWindowPath);
        
        // We can split by TabItem to be safer
        var tabItems = Regex.Split(content, "<TabItem");
        
        foreach (var item in tabItems)
        {
            var applicableMatch = Regex.Match(item, "ApplicableTo=\"([^\"]+)\"");
            var pageMatch = Regex.Match(item, "<demoPages:([a-zA-Z0-9]+)");
            
            if (applicableMatch.Success && pageMatch.Success)
            {
                var applicableTo = applicableMatch.Groups[1].Value;
                var pageName = pageMatch.Groups[1].Value;
                
                var themes = applicableTo.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                         .Select(t => t.Trim())
                                         .Where(t => !string.Equals(t, "Fluent", StringComparison.OrdinalIgnoreCase) && 
                                                     !string.Equals(t, "Simple", StringComparison.OrdinalIgnoreCase))
                                         .ToList();
                
                _pageThemes[pageName] = themes;

                // Check for ViewModel
                var viewModelMatch = Regex.Match(item, @"<vm:(\w+)");
                if (viewModelMatch.Success)
                {
                    _pageViewModels[pageName] = viewModelMatch.Groups[1].Value;
                }
            }
        }
    }

    public static IEnumerable<object?[]> GetDemoPages()
    {
        var assembly = typeof(SampleApp.DemoPages.ButtonDemo).Assembly;
        var pages = assembly.GetTypes()
            .Where(t => t.Namespace == "SampleApp.DemoPages" && 
                        t.IsSubclassOf(typeof(UserControl)) && 
                        !t.IsAbstract && 
                        t.Name.EndsWith("Demo"))
            .OrderBy(t => t.Name)
            .ToList();

        EnsureMetadataLoaded();
        var result = new List<object?[]>();

        foreach (var page in pages)
        {
            if (!_pageThemes!.ContainsKey(page.Name))
            {
                // If a theme is not among the applicable themes for this control (as per SampleItemHeader in MainWindow.axaml), skip it
                continue;
            }

            var applicableThemes = _pageThemes[page.Name];
            
            // Resolve ViewModel type if any
            Type? viewModelType = null;
            if (_pageViewModels!.TryGetValue(page.Name, out var viewModelName))
            {
                viewModelType = assembly.GetTypes().FirstOrDefault(t => t.Name == viewModelName);
            }

            foreach (var applicableTheme in applicableThemes)
            {
                // Only run tests for themes we explicitly support in the test runner
                if (applicableTheme is "MacClassic" or "LiquidGlass" or "Linux" or "DevExpress")
                {
                    result.Add(new object?[] { page, applicableTheme, viewModelType });
                }
            }
        }
            
        return result;
    }

    // TestPage() is called automatically by the xUnit Test Runner for each entry 
    //   returned by GetDemoPages() when you run the tests.
    [AvaloniaTheory]
    [MemberData(nameof(GetDemoPages))]
    public void TestPage(Type pageType, string themeName, Type? viewModelType)
    {
        var pageName = pageType.Name;
        // Ensure directories exist
        Directory.CreateDirectory(TestResultsDirectory);

        // 1. Set Theme FIRST (Before creating any UI controls)
        // Force theme reload to ensure fresh styles for every test
        App.CurrentTheme = null;

        Theme theme = themeName switch
        {
            "MacClassic" => new MacOsClassicTheme(),
            "LiquidGlass" => new MacOsLiquidGlassTheme(),
            "Linux" => new LinuxYaruTheme(),
            "DevExpress" => new DevExpressTheme(),
            _ => throw new ArgumentException($"Unknown theme: {themeName}")
        };
        App.SetTheme(theme);
        
        // 2. Instantiate the page
        var content = (Control)Activator.CreateInstance(pageType)!;

        // Assign DataContext for pages that require it
        if (viewModelType != null)
        {
            content.DataContext = Activator.CreateInstance(viewModelType);
        }
        
        // 3. Create a window to host it
        var window = new Window
        {
            Width = 1024,
            Height = 768,
            Content = content
        };

        window.Show();

        // 4. Test Light Mode
        CaptureAndCompare("", ThemeVariant.Light);

        // 5. Test Dark Mode
        CaptureAndCompare("_dark", ThemeVariant.Dark);
        

        void CaptureAndCompare(string suffix, ThemeVariant variant)
        {
            if (Application.Current != null)
            {
                Application.Current.RequestedThemeVariant = variant;
            }
            
            // Wait for layout and theme application
            Dispatcher.UIThread.RunJobs();
            
            // Capture
            var frame = window.CaptureRenderedFrame();
            if (frame == null) throw new Exception($"Failed to capture frame for {variant}");
            using var bitmap = frame;
            
            // Save and Compare
            var fileName = $"{pageName}{suffix}.png";
            var baselinePath = Path.Combine(BaselinesDirectory, themeName, fileName);
            var testPath = Path.Combine(TestResultsDirectory, themeName, fileName);
            var diffPath = Path.Combine(TestDiffsDirectory, themeName, $"{pageName}{suffix}_diff.png");
            
            // Ensure subdirectories exist
            Directory.CreateDirectory(Path.GetDirectoryName(baselinePath)!);
            Directory.CreateDirectory(Path.GetDirectoryName(testPath)!);
            Directory.CreateDirectory(Path.GetDirectoryName(diffPath)!);
            
            bitmap.Save(testPath);
            
            if (File.Exists(baselinePath))
            {
                bool result = ImageComparer.CompareImages(baselinePath, testPath, diffPath);
                Assert.True(result, $"\u001b[1mVisual regression detected for {pageName} ({themeName}) [{variant}]\u001b[0m. Diff saved to {diffPath}");
            }
            else
            {
                Assert.Fail($"\u001b[1mNo baseline found for {pageName} ({themeName}) [{variant}]\u001b[0m. Saved screenshot to {testPath}");
            }
        }

        
        window.Close();
    }
}
