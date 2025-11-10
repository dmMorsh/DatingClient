using System.Net.Http.Headers;

namespace DatingClient.Services;

public class AvatarCacheService
{
    private readonly HttpClient _http = new();
    private readonly string _cacheDir;

    public AvatarCacheService()
    {
        _cacheDir = Path.Combine(FileSystem.CacheDirectory, "avatars");
        Directory.CreateDirectory(_cacheDir);
    }

    private string GetCachePath(string url)
    {
        var fileName = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(url))
            .Replace("/", "_");
        return Path.Combine(_cacheDir, fileName + ".jpg");
    }

    private string GetMetaPath(string cachePath) => cachePath + ".meta";

    public async Task<string?> GetAvatarPathAsync(string url)
    {
        if (string.IsNullOrEmpty(url))
            return null;

        var cachePath = GetCachePath(url);
        var metaPath = GetMetaPath(cachePath);

        // If the file exists and the metadata is present, let's try checking for updates.
        if (File.Exists(cachePath) && File.Exists(metaPath))
        {
            var meta = await File.ReadAllLinesAsync(metaPath);
            var request = new HttpRequestMessage(HttpMethod.Head, url);
            
            if (meta.Length > 0 && !string.IsNullOrWhiteSpace(meta[0]))
            {
                try
                {
                    request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(meta[0]));
                }
                catch (FormatException)
                {
                    // ETag is corrupted - ignore
                }
            }
            if (meta.Length > 1 && DateTime.TryParse(meta[1], out var date))
                request.Headers.IfModifiedSince = date;
            
            try
            {
                var response = await _http.SendAsync(request);
                if (response.StatusCode == System.Net.HttpStatusCode.NotModified)
                    return cachePath; // evrth ok
            }
            catch
            {
                // offline - just return the old copy
                return cachePath;
            }
        }

        // download and update the cache
        try
        {
            var response = await _http.GetAsync(url);
            response.EnsureSuccessStatusCode();

            await using var fs = File.Create(cachePath);
            await response.Content.CopyToAsync(fs);

            var eTag = response.Headers.ETag?.ToString() ?? "";

            var lastModified = response.Content.Headers.LastModified?.ToString() ?? "";
            await File.WriteAllLinesAsync(metaPath, [eTag, lastModified]);

            return cachePath;
        }
        catch
        {
            // offline or error - return the old one, if there is one
            if (File.Exists(cachePath))
                return cachePath;
            throw;
        }
    }

    public void ClearCache()
    {
        if (Directory.Exists(_cacheDir))
            Directory.Delete(_cacheDir, true);
        Directory.CreateDirectory(_cacheDir);
    }
}
