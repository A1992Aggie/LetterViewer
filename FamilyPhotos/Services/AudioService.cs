using FamilyPhotos.Models;

namespace FamilyPhotos.Services;

public class AudioService
{
    private readonly OneDriveService _oneDrive;
    private readonly MetadataService _metadata;

    public AudioService(OneDriveService oneDrive, MetadataService metadata)
    {
        _oneDrive = oneDrive;
        _metadata = metadata;
    }

    public async Task<AudioReference?> SaveRecordingAsync(
        string parentFolderId,
        string photoFileName,
        PhotoMetadata metadata,
        byte[] audioData,
        string mimeType,
        double durationSeconds,
        string recordedBy)
    {
        // Determine file extension from MIME type
        var ext = mimeType switch
        {
            "audio/webm" => "webm",
            "audio/mp4" => "m4a",
            "audio/ogg" => "ogg",
            _ => "webm"
        };

        // Determine next sequence number
        var nextSeq = (metadata.AudioRecordings.Count + 1).ToString("D3");
        var audioFileName = $"{photoFileName}.audio-{nextSeq}.{ext}";

        // Upload audio to OneDrive
        var uploadedItem = await _oneDrive.UploadLargeFileAsync(
            parentFolderId, audioFileName, audioData, mimeType);

        if (uploadedItem == null) return null;

        // Create audio reference
        var audioRef = new AudioReference
        {
            FileName = audioFileName,
            OneDriveItemId = uploadedItem.Id,
            MimeType = mimeType,
            RecordedUtc = DateTime.UtcNow,
            RecordedBy = recordedBy,
            DurationSeconds = durationSeconds
        };

        // Update metadata
        metadata.AudioRecordings.Add(audioRef);
        await _metadata.SaveMetadataAsync(parentFolderId, photoFileName, metadata);

        return audioRef;
    }

    public async Task<bool> DeleteRecordingAsync(
        string parentFolderId,
        string photoFileName,
        PhotoMetadata metadata,
        AudioReference recording)
    {
        // Delete audio file from OneDrive
        if (!string.IsNullOrEmpty(recording.OneDriveItemId))
        {
            await _oneDrive.DeleteFileAsync(recording.OneDriveItemId);
        }

        // Remove from metadata
        metadata.AudioRecordings.RemoveAll(a => a.Id == recording.Id);
        await _metadata.SaveMetadataAsync(parentFolderId, photoFileName, metadata);

        return true;
    }

    public async Task<string?> GetPlaybackUrlAsync(AudioReference recording)
    {
        if (string.IsNullOrEmpty(recording.OneDriveItemId)) return null;

        var item = await _oneDrive.GetItemAsync(recording.OneDriveItemId);
        return item?.DownloadUrl;
    }
}
