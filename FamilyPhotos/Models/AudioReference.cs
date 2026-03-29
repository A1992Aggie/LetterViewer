namespace FamilyPhotos.Models;

public class AudioReference
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FileName { get; set; } = "";
    public string OneDriveItemId { get; set; } = "";
    public string MimeType { get; set; } = "audio/webm";
    public DateTime RecordedUtc { get; set; } = DateTime.UtcNow;
    public string RecordedBy { get; set; } = "";
    public double DurationSeconds { get; set; }
}
