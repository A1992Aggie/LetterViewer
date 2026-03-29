using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.Versioning;

namespace LetterViewer.Services;

[SupportedOSPlatform("windows")]
public static class ImageProcessingService
{
    public static Bitmap Rotate90CW(Bitmap source)
    {
        var rotated = new Bitmap(source);
        rotated.RotateFlip(RotateFlipType.Rotate90FlipNone);
        return rotated;
    }

    public static Bitmap Rotate90CCW(Bitmap source)
    {
        var rotated = new Bitmap(source);
        rotated.RotateFlip(RotateFlipType.Rotate270FlipNone);
        return rotated;
    }

    public static Bitmap Rotate180(Bitmap source)
    {
        var rotated = new Bitmap(source);
        rotated.RotateFlip(RotateFlipType.Rotate180FlipNone);
        return rotated;
    }

    public static Bitmap Crop(Bitmap source, Rectangle cropRect)
    {
        // Clamp to image bounds
        cropRect.Intersect(new Rectangle(0, 0, source.Width, source.Height));
        if (cropRect.Width <= 0 || cropRect.Height <= 0)
            return new Bitmap(source);

        return source.Clone(cropRect, source.PixelFormat);
    }

    public static Bitmap AdjustBrightnessContrast(Bitmap source, float brightness, float contrast)
    {
        var result = new Bitmap(source.Width, source.Height, source.PixelFormat);

        float adjustedBrightness = brightness - 1.0f;
        float[][] matrixItems = {
            new float[] { contrast, 0, 0, 0, 0 },
            new float[] { 0, contrast, 0, 0, 0 },
            new float[] { 0, 0, contrast, 0, 0 },
            new float[] { 0, 0, 0, 1, 0 },
            new float[] { adjustedBrightness, adjustedBrightness, adjustedBrightness, 0, 1 }
        };

        var colorMatrix = new ColorMatrix(matrixItems);
        using var attributes = new ImageAttributes();
        attributes.SetColorMatrix(colorMatrix);

        using var g = Graphics.FromImage(result);
        g.DrawImage(source,
            new Rectangle(0, 0, source.Width, source.Height),
            0, 0, source.Width, source.Height,
            GraphicsUnit.Pixel, attributes);

        return result;
    }

    public static void SaveWithMaxQuality(Image image, string path)
    {
        var encoderParameters = new EncoderParameters(1);
        encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, 100L);

        string ext = Path.GetExtension(path).ToLowerInvariant();
        ImageCodecInfo codec;

        if (ext == ".png")
        {
            codec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Png.Guid);
        }
        else if (ext == ".bmp")
        {
            codec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Bmp.Guid);
        }
        else
        {
            // Default to JPEG
            codec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
        }

        image.Save(path, codec, encoderParameters);
    }

    public static Image ApplyExifOrientation(Image image)
    {
        const int orientationPropertyId = 0x0112;

        if (!image.PropertyIdList.Contains(orientationPropertyId))
            return image;

        var orientationProperty = image.GetPropertyItem(orientationPropertyId);
        if (orientationProperty?.Value == null) return image;

        int orientation = BitConverter.ToUInt16(orientationProperty.Value, 0);

        RotateFlipType rotateFlipType = orientation switch
        {
            2 => RotateFlipType.RotateNoneFlipX,
            3 => RotateFlipType.Rotate180FlipNone,
            4 => RotateFlipType.RotateNoneFlipY,
            5 => RotateFlipType.Rotate90FlipX,
            6 => RotateFlipType.Rotate90FlipNone,
            7 => RotateFlipType.Rotate270FlipX,
            8 => RotateFlipType.Rotate270FlipNone,
            _ => RotateFlipType.RotateNoneFlipNone
        };

        if (rotateFlipType != RotateFlipType.RotateNoneFlipNone)
        {
            var rotated = new Bitmap(image);
            rotated.RotateFlip(rotateFlipType);
            if (rotated.PropertyIdList.Contains(orientationPropertyId))
                rotated.RemovePropertyItem(orientationPropertyId);
            image.Dispose();
            return rotated;
        }

        return image;
    }

    public static void SetHighQualityGraphics(Graphics g)
    {
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
    }
}
