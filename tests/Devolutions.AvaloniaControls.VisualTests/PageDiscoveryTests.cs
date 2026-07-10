using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using SampleApp.PageCatalog;
using Xunit;

namespace Devolutions.AvaloniaControls.VisualTests;

public class PageDiscoveryTests
{
    [Fact]
    public void CanDiscoverDemoPages()
    {
        var demoPages = PageRegistry.All
            .Select(control => control.PageType)
            .Where(t => t.IsSubclassOf(typeof(UserControl)) &&
                        !t.IsAbstract &&
                        t.Name.EndsWith("Demo"))
            .Distinct()
            .OrderBy(t => t.Name)
            .ToList();

        Assert.NotEmpty(demoPages);
        
        // Print discovered pages for verification
        foreach (var page in demoPages)
        {
            System.Console.WriteLine($"Discovered: {page.Name}");
        }
        
        Assert.All(demoPages, page => Assert.EndsWith("Demo", page.Name, StringComparison.Ordinal));
    }
}
