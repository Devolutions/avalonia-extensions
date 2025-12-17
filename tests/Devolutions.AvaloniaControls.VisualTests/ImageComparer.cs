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

        if (baseline.Width != screenshot.Width || baseline.Height != screenshot.Height)
        {
            Console.WriteLine($"Dimension mismatch: Baseline {baseline.Width}x{baseline.Height} vs Screenshot {screenshot.Width}x{screenshot.Height}");
            return false;
        }

        bool areEqual = true;
        using var diff = new SKBitmap(baseline.Width, baseline.Height);
        
        // Simple pixel comparison
        for (int y = 0; y < baseline.Height; y++)
        {
            for (int x = 0; x < baseline.Width; x++)
            {
                var p1 = baseline.GetPixel(x, y);
                var p2 = screenshot.GetPixel(x, y);

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

        if (!areEqual)
        {
            var dir = Path.GetDirectoryName(diffPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using var image = SKImage.FromBitmap(diff);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.OpenWrite(diffPath);
            data.SaveTo(stream);
            Console.WriteLine($"Diff saved to {diffPath}");
        }

        return areEqual;
    }
}
