namespace FamilyPhotos.Models;

public class PhotoMetadata
{
    public string PhotoItemId { get; set; } = "";
    public string PhotoFileName { get; set; } = "";
    public List<FaceTag> FaceTags { get; set; } = [];
    public string Notes { get; set; } = "";
    public string? ExtractedDate { get; set; }
    public List<AudioReference> AudioRecordings { get; set; } = [];
    public DateTime LastModifiedUtc { get; set; } = DateTime.UtcNow;
}
