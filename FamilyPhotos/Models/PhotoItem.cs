namespace FamilyPhotos.Models;

public class PhotoItem
{
    public OneDriveItem DriveItem { get; set; } = new();
    public PhotoMetadata? Metadata { get; set; }
    public string? ThumbnailUrl { get; set; }

    public string DisplayName => DriveItem.Name;
    public string? ExtractedDate => Metadata?.ExtractedDate;
    public int AudioCount => Metadata?.AudioRecordings.Count ?? 0;
    public int TagCount => Metadata?.FaceTags.Count ?? 0;
    public bool HasNotes => !string.IsNullOrWhiteSpace(Metadata?.Notes);
}
