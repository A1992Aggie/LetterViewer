using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using LetterViewer.Models;

namespace LetterViewer.Utilities;

[SupportedOSPlatform("windows")]
public static class ImageCombiner
{
    public static void CombineImages(string[] imagePaths, string outputPath,
        CombineDirection direction, int spacing = 30, bool normalizeSize = true)
    {
        if (direction == CombineDirection.Right)
            CombineHorizontally(imagePaths, outputPath, spacing, normalizeSize);
        else
            CombineVertically(imagePaths, outputPath, spacing, normalizeSize);
    }

    public static void CombineHorizontally(string[] imagePaths, string outputPath, int spacing = 30, bool normalizeSize = true)
    {
        var images = imagePaths
            .Select(p => Services.ImageProcessingService.ApplyExifOrientation(Image.FromFile(p)))
            .ToList();

        try
        {
            if (normalizeSize)
            {
                int targetHeight = (int)images.Average(img => img.Height);
                for (int i = 0; i < images.Count; i++)
                {
                    if (images[i].Height != targetHeight)
                        images[i] = ResizeToHeight(images[i], targetHeight);
                }
            }

            int totalWidth = images.Sum(img => img.Width) + Math.Max(0, images.Count - 1) * spacing;
            int maxHeight = images.Max(img => img.Height);

            using var combined = new Bitmap(totalWidth, maxHeight);
            using var g = Graphics.FromImage(combined);
            g.Clear(Color.Black);
            Services.ImageProcessingService.SetHighQualityGraphics(g);

            int xOffset = 0;
            foreach (var img in images)
            {
                int yOffset = (maxHeight - img.Height) / 2;
                g.DrawImage(img, xOffset, yOffset, img.Width, img.Height);
                xOffset += img.Width + spacing;
            }

            Services.ImageProcessingService.SaveWithMaxQuality(combined, outputPath);
        }
        finally
        {
            foreach (var img in images) img.Dispose();
        }
    }

    public static void CombineVertically(string[] imagePaths, string outputPath, int spacing = 30, bool normalizeSize = true)
    {
        var images = imagePaths
            .Select(p => Services.ImageProcessingService.ApplyExifOrientation(Image.FromFile(p)))
            .ToList();

        try
        {
            if (normalizeSize)
            {
                int targetWidth = (int)images.Average(img => img.Width);
                for (int i = 0; i < images.Count; i++)
                {
                    if (images[i].Width != targetWidth)
                        images[i] = ResizeToWidth(images[i], targetWidth);
                }
            }

            int maxWidth = images.Max(img => img.Width);
            int totalHeight = images.Sum(img => img.Height) + Math.Max(0, images.Count - 1) * spacing;

            using var combined = new Bitmap(maxWidth, totalHeight);
            using var g = Graphics.FromImage(combined);
            g.Clear(Color.Black);
            Services.ImageProcessingService.SetHighQualityGraphics(g);

            int yOffset = 0;
            foreach (var img in images)
            {
                int xOffset = (maxWidth - img.Width) / 2;
                g.DrawImage(img, xOffset, yOffset, img.Width, img.Height);
                yOffset += img.Height + spacing;
            }

            Services.ImageProcessingService.SaveWithMaxQuality(combined, outputPath);
        }
        finally
        {
            foreach (var img in images) img.Dispose();
        }
    }

    private static Image ResizeToHeight(Image image, int targetHeight)
    {
        double ratio = (double)image.Width / image.Height;
        int targetWidth = (int)(targetHeight * ratio);
        var resized = new Bitmap(targetWidth, targetHeight);
        using var g = Graphics.FromImage(resized);
        Services.ImageProcessingService.SetHighQualityGraphics(g);
        g.DrawImage(image, 0, 0, targetWidth, targetHeight);
        image.Dispose();
        return resized;
    }

    private static Image ResizeToWidth(Image image, int targetWidth)
    {
        double ratio = (double)image.Height / image.Width;
        int targetHeight = (int)(targetWidth * ratio);
        var resized = new Bitmap(targetWidth, targetHeight);
        using var g = Graphics.FromImage(resized);
        Services.ImageProcessingService.SetHighQualityGraphics(g);
        g.DrawImage(image, 0, 0, targetWidth, targetHeight);
        image.Dispose();
        return resized;
    }
}
