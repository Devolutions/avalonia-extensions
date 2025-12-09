using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Xunit;

namespace Devolutions.AvaloniaControls.VisualTests;

public class PageDiscoveryTests
{
    [Fact]
    public void CanDiscoverDemoPages()
    {
        var assembly = typeof(SampleApp.DemoPages.ButtonDemo).Assembly;
        var demoPages = assembly.GetTypes()
            .Where(t => t.Namespace == "SampleApp.DemoPages" && 
                        t.IsSubclassOf(typeof(UserControl)) && 
                        !t.IsAbstract && 
                        t.Name.EndsWith("Demo"))
            .OrderBy(t => t.Name)
            .ToList();

        Assert.NotEmpty(demoPages);
        
        // Print discovered pages for verification
        foreach (var page in demoPages)
        {
            System.Console.WriteLine($"Discovered: {page.Name}");
        }
        
        // Basic check to ensure we found some expected ones
        Assert.Contains(demoPages, t => t.Name == "ButtonDemo");
        Assert.Contains(demoPages, t => t.Name == "TextBoxDemo");
        Assert.Contains(demoPages, t => t.Name == "ComboBoxDemo");
    }
}
