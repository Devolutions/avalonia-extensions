using System;
using System.IO;
using SkiaSharp;

namespace Devolutions.AvaloniaControls.VisualTests;

public static class ImageComparer
{
    public static bool CompareImages(string baselinePath, string testPath, string diffPath)
    {
        if (!File.Exists(baselinePath))
        {
            throw new FileNotFoundException("Baseline image not found", baselinePath);
        }

        if (!File.Exists(testPath))
        {
            throw new FileNotFoundException("Test screenshot not found", testPath);
        }

        using var baseline = SKBitmap.Decode(baselinePath);
        using var screenshot = SKBitmap.Decode(testPath);

        bool hasDimensionMismatch = baseline.Width != screenshot.Width || baseline.Height != screenshot.Height;
        if (hasDimensionMismatch)
        {
            Console.WriteLine($"Dimension mismatch: Baseline {baseline.Width}x{baseline.Height} vs Screenshot {screenshot.Width}x{screenshot.Height}");
        }

        bool areEqual = true;
        int diffWidth = Math.Max(baseline.Width, screenshot.Width);
        int diffHeight = Math.Max(baseline.Height, screenshot.Height);
        using var diff = new SKBitmap(diffWidth, diffHeight);
        
        // Compare all pixels, treating missing regions as transparent.
        for (int y = 0; y < diffHeight; y++)
        {
            for (int x = 0; x < diffWidth; x++)
            {
                bool baselineHasPixel = x < baseline.Width && y < baseline.Height;
                bool screenshotHasPixel = x < screenshot.Width && y < screenshot.Height;
                SKColor p1 = baselineHasPixel ? baseline.GetPixel(x, y) : SKColors.Transparent;
                SKColor p2 = screenshotHasPixel ? screenshot.GetPixel(x, y) : SKColors.Transparent;

                if (p1 != p2)
                {
                    areEqual = false;
                    // Highlight difference in red
                    diff.SetPixel(x, y, SKColors.Red);
                }
                else
                {
                    // Fade out matching pixels slightly in the diff
                    var faded = new SKColor(p1.Red, p1.Green, p1.Blue, 50);
                    diff.SetPixel(x, y, faded);
                }
            }
        }

        if (hasDimensionMismatch)
        {
            areEqual = false;
        }

        if (!areEqual)
        {
            var dir = Path.GetDirectoryName(diffPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using var image = SKImage.FromBitmap(diff);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.Create(diffPath);
            data.SaveTo(stream);
            Console.WriteLine($"Diff saved to {diffPath}");
        }

        return areEqual;
    }
}
