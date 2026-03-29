using System.Text.Json.Serialization;

namespace FamilyPhotos.Models;

public class OneDriveItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("lastModifiedDateTime")]
    public DateTime? LastModified { get; set; }

    [JsonPropertyName("folder")]
    public OneDriveFolder? Folder { get; set; }

    [JsonPropertyName("file")]
    public OneDriveFile? File { get; set; }

    [JsonPropertyName("@microsoft.graph.downloadUrl")]
    public string? DownloadUrl { get; set; }

    [JsonPropertyName("parentReference")]
    public OneDriveParentReference? ParentReference { get; set; }

    [JsonIgnore]
    public bool IsFolder => Folder != null;

    [JsonIgnore]
    public bool IsImage => File?.MimeType?.StartsWith("image/") == true;

    [JsonIgnore]
    public string SizeDisplay => Size switch
    {
        < 1024 => $"{Size} B",
        < 1024 * 1024 => $"{Size / 1024.0:F1} KB",
        _ => $"{Size / (1024.0 * 1024.0):F1} MB"
    };
}

public class OneDriveFolder
{
    [JsonPropertyName("childCount")]
    public int ChildCount { get; set; }
}

public class OneDriveFile
{
    [JsonPropertyName("mimeType")]
    public string? MimeType { get; set; }
}

public class OneDriveParentReference
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }
}

public class OneDriveItemCollection
{
    [JsonPropertyName("value")]
    public List<OneDriveItem> Value { get; set; } = [];

    [JsonPropertyName("@odata.nextLink")]
    public string? NextLink { get; set; }
}

public class OneDriveThumbnailSet
{
    [JsonPropertyName("value")]
    public List<ThumbnailSetItem> Value { get; set; } = [];
}

public class ThumbnailSetItem
{
    [JsonPropertyName("large")]
    public ThumbnailInfo? Large { get; set; }

    [JsonPropertyName("medium")]
    public ThumbnailInfo? Medium { get; set; }

    [JsonPropertyName("small")]
    public ThumbnailInfo? Small { get; set; }
}

public class ThumbnailInfo
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }
}
