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
            // Fallback or throw? Let's try to find it if the relative path is wrong
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

        // Define priority for themes to detect leaking (DevExpress/Linux first)
        var themePriority = new Dictionary<string, int>
        {
            { "DevExpress", 0 },
            { "Yaru", 1 },
            { "MacClassic", 2 },
            { "LiquidGlass", 3 }
        };

        foreach (var page in pages)
        {
            if (!_pageThemes!.ContainsKey(page.Name))
            {
                // If not in our map, skip it (as per user instruction to only test what's in SampleItemHeader)
                continue;
            }

            var applicableThemes = _pageThemes[page.Name];
            
            // Resolve ViewModel type if any
            Type? viewModelType = null;
            if (_pageViewModels!.TryGetValue(page.Name, out var viewModelName))
            {
                viewModelType = assembly.GetTypes().FirstOrDefault(t => t.Name == viewModelName);
            }

            // Sort themes by priority
            applicableThemes.Sort((a, b) => 
            {
                int pA = themePriority.ContainsKey(a) ? themePriority[a] : 99;
                int pB = themePriority.ContainsKey(b) ? themePriority[b] : 99;
                return pA.CompareTo(pB);
            });

            foreach (var applicableTheme in applicableThemes)
            {
                // Map ApplicableTo names to our Test Theme names
                string? themeName = applicableTheme switch
                {
                    "MacClassic" => "MacClassic",
                    "LiquidGlass" => "LiquidGlass",
                    "Yaru" => "Linux",
                    "DevExpress" => "DevExpress",
                    _ => null
                };

                if (themeName != null)
                {
                    result.Add(new object?[] { page.Name, page, themeName, viewModelType });
                }
            }
        }
            
        return result;
    }

    [AvaloniaTheory]
    [MemberData(nameof(GetDemoPages))]
    public void TestPage(string pageName, Type pageType, string themeName, Type? viewModelType)
    {
        // Ensure directories exist
        Directory.CreateDirectory(TestResultsDirectory);

        // 1. Set Theme FIRST (Before creating any UI controls)
        // Force theme reload to ensure fresh styles for every test
        App.CurrentTheme = null;

        SampleApp.Theme theme = themeName switch
        {
            "MacClassic" => new SampleApp.MacOsClassicTheme(),
            "LiquidGlass" => new SampleApp.MacOsLiquidGlassTheme(),
            "Linux" => new SampleApp.LinuxYaruTheme(),
            "DevExpress" => new SampleApp.DevExpressTheme(),
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
        
        // 4. Wait for layout
        Dispatcher.UIThread.RunJobs();
        
        // 5. Capture
        var frame = window.CaptureRenderedFrame();
        if (frame == null) throw new Exception("Failed to capture frame");
        using var bitmap = frame;
        
        // 6. Save and Compare
        var baselinePath = Path.Combine(BaselinesDirectory, themeName, $"{pageName}.png");
        var actualPath = Path.Combine(TestResultsDirectory, themeName, $"{pageName}.png");
        var diffPath = Path.Combine(TestDiffsDirectory, themeName, $"{pageName}_diff.png");
        
        // Ensure subdirectories exist
        Directory.CreateDirectory(Path.GetDirectoryName(baselinePath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(actualPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(diffPath)!);
        
        bitmap.Save(actualPath);
        
        if (File.Exists(baselinePath))
        {
            bool result = ImageComparer.CompareImages(baselinePath, actualPath, diffPath);
            Assert.True(result, $"\u001b[1mVisual regression detected for {pageName} ({themeName})\u001b[0m. Diff saved to {diffPath}");
        }
        else
        {
            Assert.Fail($"\u001b[1mNo baseline found for {pageName} ({themeName})\u001b[0m. Saved actual to {actualPath}");
        }
        
        window.Close();
    }
}
