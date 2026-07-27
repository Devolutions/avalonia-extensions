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

        this.CreateTestImage(path1, SKColors.Blue);
        this.CreateTestImage(path2, SKColors.Blue);

        bool result = ImageComparer.CompareImages(path1, path2, diff);
        Assert.True(result);
    }

    [Fact]
    public void DifferentImagesAreNotEqual()
    {
        var path1 = Path.Combine(Path.GetTempPath(), "img1_diff.png");
        var path2 = Path.Combine(Path.GetTempPath(), "img2_diff.png");
        var diff = Path.Combine(Path.GetTempPath(), "diff_out.png");

        this.CreateTestImage(path1, SKColors.Blue);
        this.CreateTestImage(path2, SKColors.Red);

        bool result = ImageComparer.CompareImages(path1, path2, diff);
        Assert.False(result);
        Assert.True(File.Exists(diff));
    }

    [Fact]
    public void DifferentSizedImagesAreNotEqualAndProduceDiff()
    {
        var path1 = Path.Combine(Path.GetTempPath(), "img1_size_diff.png");
        var path2 = Path.Combine(Path.GetTempPath(), "img2_size_diff.png");
        var diff = Path.Combine(Path.GetTempPath(), "diff_size_out.png");

        this.CreateTestImage(path1, SKColors.Blue, 10, 10);
        this.CreateTestImage(path2, SKColors.Blue, 10, 12);

        bool result = ImageComparer.CompareImages(path1, path2, diff);
        Assert.False(result);
        Assert.True(File.Exists(diff));

        using var diffBitmap = SKBitmap.Decode(diff);
        Assert.Equal(10, diffBitmap.Width);
        Assert.Equal(12, diffBitmap.Height);
    }

    private void CreateTestImage(string path, SKColor color)
    {
        this.CreateTestImage(path, color, 10, 10);
    }

    private void CreateTestImage(string path, SKColor color, int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(color);
        
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(path);
        data.SaveTo(stream);
    }
}
