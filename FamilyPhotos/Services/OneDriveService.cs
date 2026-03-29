using System.Net.Http.Json;
using System.Text.Json;
using FamilyPhotos.Models;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace FamilyPhotos.Services;

public class OneDriveService
{
    private readonly HttpClient _http;
    private const string GraphBaseUrl = "https://graph.microsoft.com/v1.0";
    private static readonly string[] ImageMimeTypes = ["image/jpeg", "image/png", "image/gif", "image/bmp", "image/tiff", "image/webp"];

    public OneDriveService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<OneDriveItem>> GetChildrenAsync(string folderId, bool imagesOnly = false)
    {
        var items = new List<OneDriveItem>();
        var url = $"{GraphBaseUrl}/me/drive/items/{folderId}/children?$top=200&$select=id,name,size,file,folder,lastModifiedDateTime,parentReference,@microsoft.graph.downloadUrl&$expand=thumbnails";

        while (!string.IsNullOrEmpty(url))
        {
            var collection = await GetAsync<OneDriveItemCollection>(url);
            if (collection == null) break;

            items.AddRange(collection.Value);
            url = collection.NextLink;
        }

        if (imagesOnly)
            return items.Where(i => i.IsImage).OrderBy(i => i.Name).ToList();

        return items
            .OrderByDescending(i => i.IsFolder)
            .ThenBy(i => i.Name)
            .ToList();
    }

    public async Task<OneDriveItem?> GetItemAsync(string itemId)
    {
        return await GetAsync<OneDriveItem>(
            $"{GraphBaseUrl}/me/drive/items/{itemId}");
    }

    public async Task<OneDriveItem?> GetRootAsync()
    {
        return await GetAsync<OneDriveItem>($"{GraphBaseUrl}/me/drive/root");
    }

    public async Task<string?> GetThumbnailUrlAsync(string itemId, string size = "medium")
    {
        try
        {
            var result = await GetAsync<OneDriveThumbnailSet>(
                $"{GraphBaseUrl}/me/drive/items/{itemId}/thumbnails");
            if (result?.Value.Count > 0)
            {
                var set = result.Value[0];
                return size switch
                {
                    "small" => set.Small?.Url,
                    "large" => set.Large?.Url,
                    _ => set.Medium?.Url
                };
            }
        }
        catch { }
        return null;
    }

    public async Task<OneDriveItem?> UploadSmallFileAsync(string parentId, string fileName, byte[] content, string contentType)
    {
        var url = $"{GraphBaseUrl}/me/drive/items/{parentId}:/{Uri.EscapeDataString(fileName)}:/content";
        var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = new ByteArrayContent(content)
        };
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

        var response = await SendAsync(request);
        if (response?.IsSuccessStatusCode == true)
        {
            return await response.Content.ReadFromJsonAsync<OneDriveItem>();
        }
        return null;
    }

    public async Task<OneDriveItem?> UploadLargeFileAsync(string parentId, string fileName, byte[] content, string contentType)
    {
        // For files > 4MB, use upload session
        if (content.Length <= 4 * 1024 * 1024)
            return await UploadSmallFileAsync(parentId, fileName, content, contentType);

        // Create upload session
        var sessionUrl = $"{GraphBaseUrl}/me/drive/items/{parentId}:/{Uri.EscapeDataString(fileName)}:/createUploadSession";
        var sessionRequest = new HttpRequestMessage(HttpMethod.Post, sessionUrl)
        {
            Content = JsonContent.Create(new { item = new { name = fileName } })
        };
        var sessionResponse = await SendAsync(sessionRequest);
        if (sessionResponse?.IsSuccessStatusCode != true) return null;

        var session = await sessionResponse.Content.ReadFromJsonAsync<JsonElement>();
        var uploadUrl = session.GetProperty("uploadUrl").GetString();
        if (uploadUrl == null) return null;

        // Upload in 5MB chunks
        const int chunkSize = 5 * 1024 * 1024;
        OneDriveItem? result = null;

        for (int offset = 0; offset < content.Length; offset += chunkSize)
        {
            var length = Math.Min(chunkSize, content.Length - offset);
            var chunk = new byte[length];
            Array.Copy(content, offset, chunk, 0, length);

            var chunkRequest = new HttpRequestMessage(HttpMethod.Put, uploadUrl)
            {
                Content = new ByteArrayContent(chunk)
            };
            chunkRequest.Content.Headers.ContentRange =
                new System.Net.Http.Headers.ContentRangeHeaderValue(offset, offset + length - 1, content.Length);

            // Upload session uses its own auth, no need for Graph token
            using var httpClient = new HttpClient();
            var chunkResponse = await httpClient.SendAsync(chunkRequest);

            if (offset + length >= content.Length && chunkResponse.IsSuccessStatusCode)
            {
                result = await chunkResponse.Content.ReadFromJsonAsync<OneDriveItem>();
            }
        }

        return result;
    }

    public async Task<bool> DeleteFileAsync(string itemId)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete,
            $"{GraphBaseUrl}/me/drive/items/{itemId}");
        var response = await SendAsync(request);
        return response?.IsSuccessStatusCode == true;
    }

    public async Task<OneDriveItem?> FindFileByNameAsync(string parentId, string fileName)
    {
        var escapedName = fileName.Replace("'", "''");
        var url = $"{GraphBaseUrl}/me/drive/items/{parentId}/children?$filter=name eq '{escapedName}'";

        var result = await GetAsync<OneDriveItemCollection>(url);
        return result?.Value.FirstOrDefault();
    }

    public async Task<byte[]?> DownloadFileAsync(string itemId)
    {
        try
        {
            var response = await _http.GetAsync($"{GraphBaseUrl}/me/drive/items/{itemId}/content");
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadAsByteArrayAsync();
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
        }
        return null;
    }

    public async Task<List<BreadcrumbItem>> GetBreadcrumbAsync(string itemId)
    {
        var breadcrumbs = new List<BreadcrumbItem>();
        var currentId = itemId;

        while (!string.IsNullOrEmpty(currentId))
        {
            var item = await GetItemAsync(currentId);
            if (item == null) break;

            breadcrumbs.Insert(0, new BreadcrumbItem
            {
                Name = item.Name,
                ItemId = item.Id
            });

            currentId = item.ParentReference?.Id;
            // Stop at root
            if (item.ParentReference?.Path == null) break;
        }

        return breadcrumbs;
    }

    private async Task<T?> GetAsync<T>(string url)
    {
        try
        {
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Graph API error {response.StatusCode}: {url}");
                return default;
            }
            return await response.Content.ReadFromJsonAsync<T>();
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return default;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Graph API exception: {ex.Message}");
            return default;
        }
    }

    private async Task<HttpResponseMessage?> SendAsync(HttpRequestMessage request)
    {
        try
        {
            return await _http.SendAsync(request);
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Graph API exception: {ex.Message}");
            return null;
        }
    }
}
