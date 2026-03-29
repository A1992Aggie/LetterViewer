namespace FamilyPhotos.Models;

public class FaceTag
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PersonName { get; set; } = "";
    public double XPercent { get; set; }
    public double YPercent { get; set; }
    public string TaggedBy { get; set; } = "";
    public DateTime TaggedUtc { get; set; } = DateTime.UtcNow;
}
