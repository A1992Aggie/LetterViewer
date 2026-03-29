using System.Text;
using System.Text.Json;
using FamilyPhotos.Models;

namespace FamilyPhotos.Services;

public class MetadataService
{
    private readonly OneDriveService _oneDrive;
    private readonly DateParsingService _dateParser;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public MetadataService(OneDriveService oneDrive, DateParsingService dateParser)
    {
        _oneDrive = oneDrive;
        _dateParser = dateParser;
    }

    public string GetMetadataFileName(string photoFileName) => $"{photoFileName}.meta.json";

    public async Task<PhotoMetadata?> LoadMetadataAsync(string parentFolderId, string photoFileName)
    {
        var metaFileName = GetMetadataFileName(photoFileName);
        var metaFile = await _oneDrive.FindFileByNameAsync(parentFolderId, metaFileName);

        if (metaFile == null) return null;

        var data = await _oneDrive.DownloadFileAsync(metaFile.Id);
        if (data == null) return null;

        try
        {
            var json = Encoding.UTF8.GetString(data);
            return JsonSerializer.Deserialize<PhotoMetadata>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveMetadataAsync(string parentFolderId, string photoFileName, PhotoMetadata metadata)
    {
        metadata.PhotoFileName = photoFileName;
        metadata.LastModifiedUtc = DateTime.UtcNow;

        // Auto-extract date if not set
        if (string.IsNullOrEmpty(metadata.ExtractedDate))
        {
            metadata.ExtractedDate = _dateParser.ExtractDateFromFileName(photoFileName);
        }

        var metaFileName = GetMetadataFileName(photoFileName);
        var json = JsonSerializer.Serialize(metadata, JsonOptions);
        var data = Encoding.UTF8.GetBytes(json);

        await _oneDrive.UploadSmallFileAsync(parentFolderId, metaFileName, data, "application/json");
    }

    public async Task<PhotoMetadata> GetOrCreateMetadataAsync(string parentFolderId, string photoFileName, string photoItemId)
    {
        var existing = await LoadMetadataAsync(parentFolderId, photoFileName);
        if (existing != null) return existing;

        return new PhotoMetadata
        {
            PhotoItemId = photoItemId,
            PhotoFileName = photoFileName,
            ExtractedDate = _dateParser.ExtractDateFromFileName(photoFileName)
        };
    }
}
