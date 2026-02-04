using System.IO;
using SkiaSharp;
using Xunit;

namespace Devolutions.AvaloniaControls.VisualTests;

public class ComparisonTests
{
    [Fact]
    public void IdenticalImagesAreEqual()
    {
        var path1 = Path.Combine(Path.GetTempPath(), "img1.png");
        var path2 = Path.Combine(Path.GetTempPath(), "img2.png");
        var diff = Path.Combine(Path.GetTempPath(), "diff.png");

        CreateTestImage(path1, SKColors.Blue);
        CreateTestImage(path2, SKColors.Blue);

        bool result = ImageComparer.CompareImages(path1, path2, diff);
        Assert.True(result);
    }

    [Fact]
    public void DifferentImagesAreNotEqual()
    {
        var path1 = Path.Combine(Path.GetTempPath(), "img1_diff.png");
        var path2 = Path.Combine(Path.GetTempPath(), "img2_diff.png");
        var diff = Path.Combine(Path.GetTempPath(), "diff_out.png");

        CreateTestImage(path1, SKColors.Blue);
        CreateTestImage(path2, SKColors.Red);

        bool result = ImageComparer.CompareImages(path1, path2, diff);
        Assert.False(result);
        Assert.True(File.Exists(diff));
    }

    private void CreateTestImage(string path, SKColor color)
    {
        using var bitmap = new SKBitmap(10, 10);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(color);
        
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(path);
        data.SaveTo(stream);
    }
}
