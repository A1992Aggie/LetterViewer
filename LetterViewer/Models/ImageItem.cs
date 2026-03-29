namespace LetterViewer.Models;

public class ImageItem
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName => Path.GetFileName(FilePath);
    public long FileSize { get; set; }
    public DateTime DateModified { get; set; }
    public DateTime? DateTaken { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public string FileSizeDisplay
    {
        get
        {
            if (FileSize < 1024) return $"{FileSize} B";
            if (FileSize < 1024 * 1024) return $"{FileSize / 1024.0:F1} KB";
            return $"{FileSize / (1024.0 * 1024.0):F1} MB";
        }
    }

    public string DimensionsDisplay => $"{Width} x {Height}";

    public static ImageItem FromFile(string filePath)
    {
        var info = new FileInfo(filePath);
        var item = new ImageItem
        {
            FilePath = filePath,
            FileSize = info.Length,
            DateModified = info.LastWriteTime
        };
        return item;
    }

    public void LoadDimensions()
    {
        try
        {
            using var stream = File.OpenRead(FilePath);
            using var img = Image.FromStream(stream, false, false);
            Width = img.Width;
            Height = img.Height;
        }
        catch { }
    }
}
