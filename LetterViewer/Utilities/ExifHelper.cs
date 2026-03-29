using System.Drawing.Imaging;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Text;

namespace LetterViewer.Utilities;

[SupportedOSPlatform("windows")]
public static class ExifHelper
{
    private const int DateTimeTagId = 0x0132;
    private const int DateTimeOriginalTagId = 0x9003;
    private const int OrientationTagId = 0x0112;

    public static DateTime? GetDateTaken(string imagePath)
    {
        try
        {
            byte[] imageData = File.ReadAllBytes(imagePath);
            using var ms = new MemoryStream(imageData);
            using var image = Image.FromStream(ms, false, false);

            foreach (var item in image.PropertyItems)
            {
                if (item.Id == DateTimeOriginalTagId || item.Id == DateTimeTagId)
                {
                    string dateValue = Encoding.ASCII.GetString(item.Value!).TrimEnd('\0');
                    if (DateTime.TryParseExact(dateValue, "yyyy:MM:dd HH:mm:ss",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var date))
                    {
                        return date;
                    }
                }
            }
        }
        catch { }
        return null;
    }

    public static void SetDateTaken(string imagePath, DateTime date)
    {
        byte[] imageData = File.ReadAllBytes(imagePath);
        using var ms = new MemoryStream(imageData);
        using var image = Image.FromStream(ms);

        string exifDate = date.ToString("yyyy:MM:dd HH:mm:ss");

        var dateTimeProp = (PropertyItem)FormatterServices.GetUninitializedObject(typeof(PropertyItem));
        dateTimeProp.Id = DateTimeTagId;
        dateTimeProp.Type = 2;
        dateTimeProp.Value = Encoding.ASCII.GetBytes(exifDate + "\0");
        dateTimeProp.Len = dateTimeProp.Value.Length;
        image.SetPropertyItem(dateTimeProp);

        var dateTimeOrigProp = (PropertyItem)FormatterServices.GetUninitializedObject(typeof(PropertyItem));
        dateTimeOrigProp.Id = DateTimeOriginalTagId;
        dateTimeOrigProp.Type = 2;
        dateTimeOrigProp.Value = Encoding.ASCII.GetBytes(exifDate + "\0");
        dateTimeOrigProp.Len = dateTimeOrigProp.Value.Length;
        image.SetPropertyItem(dateTimeOrigProp);

        string tempPath = imagePath + ".tmp";
        Services.ImageProcessingService.SaveWithMaxQuality(image, tempPath);
        image.Dispose();
        ms.Dispose();

        File.Delete(imagePath);
        File.Move(tempPath, imagePath);
    }

    public static Dictionary<string, string> GetImageInfo(string imagePath)
    {
        var info = new Dictionary<string, string>();
        var fileInfo = new FileInfo(imagePath);

        info["File Name"] = fileInfo.Name;
        info["File Size"] = FormatFileSize(fileInfo.Length);
        info["Date Modified"] = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm");

        try
        {
            byte[] imageData = File.ReadAllBytes(imagePath);
            using var ms = new MemoryStream(imageData);
            using var image = Image.FromStream(ms, false, false);

            info["Dimensions"] = $"{image.Width} x {image.Height}";
            info["Resolution"] = $"{image.HorizontalResolution:F0} x {image.VerticalResolution:F0} DPI";

            var dateTaken = GetDateTaken(imagePath);
            if (dateTaken.HasValue)
                info["Date Taken"] = dateTaken.Value.ToString("yyyy-MM-dd HH:mm");
        }
        catch
        {
            info["Dimensions"] = "Unknown";
        }

        return info;
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }
}
