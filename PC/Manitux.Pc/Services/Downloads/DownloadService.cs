using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Avalonia.Threading;
using Manitux.Core.Extractors.Utils;
using Manitux.Core.Helpers;
using Manitux.Core.Models;
using Manitux.Models;
using Manitux.Services.Storage;

namespace Manitux.Services.Downloads;

public class DownloadService : IDownloadService
{
    private const int BufferSize = 128 * 1024;

    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly HttpClient _httpClient = new();
    private readonly SubtitleManager _subtitleManager = new();
    private readonly Dictionary<string, CancellationTokenSource> _activeDownloads = new();
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private List<DownloadItemModel> _downloads = [];
    private bool _isInitialized;

    public event EventHandler? DownloadsChanged;

    public IReadOnlyList<DownloadItemModel> Downloads => _downloads;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            return;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_isInitialized)
            {
                return;
            }

            _downloads = await ReadDownloadsAsync(cancellationToken);
            foreach (var download in _downloads.Where(x => x.Status == DownloadStatus.Downloading))
            {
                download.Status = DownloadStatus.Paused;
            }

            _isInitialized = true;
            await SaveDownloadsAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }

        NotifyDownloadsChanged();
    }

    public async Task<DownloadItemModel> AddAsync(VideoSourceModel source, string fileName, string filePath, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        var cleanFileName = SanitizeFileName(fileName);

        var download = new DownloadItemModel
        {
            Id = Guid.NewGuid().ToString("N"),
            FileName = cleanFileName,
            Url = source.Url,
            Referer = source.Referer,
            Headers = source.Headers,
            Subtitles = source.Subtitles,
            FilePath = filePath,
            Status = DownloadStatus.Queued,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _lock.WaitAsync(cancellationToken);
        try
        {
            _downloads.Insert(0, download);
            await SaveDownloadsAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }

        NotifyDownloadsChanged();
        _ = StartAsync(download.Id, cancellationToken);
        return download;
    }

    public async Task StartAsync(string id, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);

        DownloadItemModel? download;
        CancellationTokenSource linkedCts;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            download = _downloads.FirstOrDefault(x => x.Id == id);
            if (download is null || download.Status == DownloadStatus.Downloading)
            {
                return;
            }

            if (_activeDownloads.Remove(id, out var existingCts))
            {
                existingCts.Cancel();
                existingCts.Dispose();
            }

            linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _activeDownloads[id] = linkedCts;
            download.Status = DownloadStatus.Downloading;
            download.ErrorMessage = null;
            download.CompletedAt = null;
            await SaveDownloadsAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }

        NotifyDownloadsChanged();

        try
        {
            await DownloadAsync(download, linkedCts.Token);
            await DownloadSubtitlesAsync(download, linkedCts.Token);
            download.Status = DownloadStatus.Completed;
            download.CompletedAt = DateTimeOffset.UtcNow;
            download.ErrorMessage = null;
        }
        catch (OperationCanceledException) when (linkedCts.IsCancellationRequested)
        {
            download.Status = DownloadStatus.Paused;
        }
        catch (OperationCanceledException ex)
        {
            download.Status = DownloadStatus.Failed;
            download.ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            download.Status = DownloadStatus.Failed;
            download.ErrorMessage = ex.Message;
        }
        finally
        {
            await _lock.WaitAsync(CancellationToken.None);
            try
            {
                if (_activeDownloads.Remove(id, out var cts))
                {
                    cts.Dispose();
                }

                await SaveDownloadsAsync(CancellationToken.None);
            }
            finally
            {
                _lock.Release();
            }

            NotifyDownloadsChanged();
        }
    }

    public async Task PauseAsync(string id, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_activeDownloads.TryGetValue(id, out var cts))
            {
                cts.Cancel();
            }
            else if (_downloads.FirstOrDefault(x => x.Id == id) is { } download)
            {
                download.Status = DownloadStatus.Paused;
                await SaveDownloadsAsync(cancellationToken);
            }
        }
        finally
        {
            _lock.Release();
        }

        NotifyDownloadsChanged();
    }

    public async Task RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_activeDownloads.TryGetValue(id, out var cts))
            {
                cts.Cancel();
                _activeDownloads.Remove(id);
                cts.Dispose();
            }

            _downloads.RemoveAll(x => x.Id == id);
            await SaveDownloadsAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }

        NotifyDownloadsChanged();
    }

    private async Task DownloadAsync(DownloadItemModel download, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(download.FilePath)!);

        if (IsPotentialHlsUrl(download.Url))
        {
            try
            {
                await DownloadHlsAsync(download, cancellationToken);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception) when (download.HlsSegmentIndex > 0 && File.Exists(download.FilePath))
            {
                throw;
            }
            catch
            {
                download.BytesDownloaded = 0;
                download.TotalBytes = null;
                download.HlsSegmentIndex = 0;
                if (File.Exists(download.FilePath))
                {
                    File.Delete(download.FilePath);
                }
            }
        }

        var existingBytes = File.Exists(download.FilePath)
            ? new FileInfo(download.FilePath).Length
            : 0;

        using var request = CreateRequest(HttpMethod.Get, download);
        if (existingBytes > 0)
        {
            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(existingBytes, null);
        }

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        if (await TryDownloadHlsFromResponseAsync(download, response, cancellationToken))
        {
            return;
        }

        var canResume = existingBytes > 0 && response.StatusCode == HttpStatusCode.PartialContent;
        var mode = canResume ? FileMode.Append : FileMode.Create;
        if (!canResume)
        {
            existingBytes = 0;
        }

        download.BytesDownloaded = existingBytes;
        download.TotalBytes = response.Content.Headers.ContentLength is { } contentLength
            ? existingBytes + contentLength
            : null;

        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = new FileStream(download.FilePath, mode, FileAccess.Write, FileShare.Read, BufferSize, true);
        var buffer = new byte[BufferSize];
        var lastSavedAt = DateTimeOffset.UtcNow;

        while (true)
        {
            var read = await input.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                break;
            }

            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            download.BytesDownloaded += read;

            if (DateTimeOffset.UtcNow - lastSavedAt > TimeSpan.FromSeconds(1))
            {
                lastSavedAt = DateTimeOffset.UtcNow;
                await SaveProgressSnapshotAsync();
            }
        }

        await output.FlushAsync(cancellationToken);
    }

    private async Task<bool> TryDownloadHlsFromResponseAsync(
        DownloadItemModel download,
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (!IsHlsResponse(download.Url, response))
        {
            return false;
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!IsHlsPlaylist(content))
        {
            await File.WriteAllTextAsync(download.FilePath, content, cancellationToken);
            download.BytesDownloaded = new FileInfo(download.FilePath).Length;
            download.TotalBytes = download.BytesDownloaded;
            return true;
        }

        await DownloadHlsAsync(download, cancellationToken);
        return true;
    }

    private async Task DownloadHlsAsync(DownloadItemModel download, CancellationToken cancellationToken)
    {
        var headers = BuildHeaderDictionary(download);
        var hls = await M3u8Helper.HlsLazy(
            new M3u8Helper.M3u8Stream(download.Url, Headers: headers),
            cancellationToken: cancellationToken);

        var startIndex = File.Exists(download.FilePath)
            ? Math.Clamp(download.HlsSegmentIndex, 0, hls.Size)
            : 0;

        if (startIndex == 0)
        {
            download.BytesDownloaded = 0;
        }
        else
        {
            download.BytesDownloaded = new FileInfo(download.FilePath).Length;
        }

        download.TotalBytes = null;
        download.HlsSegmentIndex = startIndex;

        var fileMode = startIndex > 0 ? FileMode.Append : FileMode.Create;
        await using var output = new FileStream(download.FilePath, fileMode, FileAccess.Write, FileShare.Read, BufferSize, true);
        var lastSavedAt = DateTimeOffset.UtcNow;

        for (var index = startIndex; index < hls.Size; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var data = await hls.ResolveLink(index, cancellationToken);
            await output.WriteAsync(data, CancellationToken.None);
            download.BytesDownloaded += data.Length;
            download.HlsSegmentIndex = index + 1;

            if (DateTimeOffset.UtcNow - lastSavedAt > TimeSpan.FromSeconds(1))
            {
                lastSavedAt = DateTimeOffset.UtcNow;
                await SaveProgressSnapshotAsync();
            }
        }

        await output.FlushAsync(cancellationToken);
        download.HlsSegmentIndex = hls.Size;
    }

    private async Task DownloadSubtitlesAsync(DownloadItemModel download, CancellationToken cancellationToken)
    {
        var subtitles = download.Subtitles?
            .Where(x => !string.IsNullOrWhiteSpace(x.Url))
            .ToList();

        if (subtitles is null || subtitles.Count == 0)
        {
            return;
        }

        for (var index = 0; index < subtitles.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var subtitle = subtitles[index];
            var targetPath = CreateSubtitlePath(download.FilePath, subtitle, subtitles.Count, index);

            try
            {
                await DownloadSubtitleAsync(subtitle.Url, targetPath, download, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Subtitle download failed: {subtitle.Url} -> {targetPath}. {ex}");
            }
        }
    }

    private async Task DownloadSubtitleAsync(
        string subtitleUrl,
        string targetPath,
        DownloadItemModel download,
        CancellationToken cancellationToken)
    {
        var resolvedUrl = await _subtitleManager.ResolveAsync(subtitleUrl, cancellationToken);
        if (TryGetLocalPath(resolvedUrl, out var localPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            File.Copy(localPath, targetPath, overwrite: true);
            return;
        }

        using var request = CreateSubtitleRequest(resolvedUrl, download);
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.Read, BufferSize, true);
        await input.CopyToAsync(output, cancellationToken);
    }

    private static HttpRequestMessage CreateSubtitleRequest(string subtitleUrl, DownloadItemModel download)
    {
        var request = CreateRequest(HttpMethod.Get, new DownloadItemModel
        {
            Url = subtitleUrl,
            Referer = download.Referer,
            Headers = download.Headers,
            FileName = download.FileName,
            FilePath = download.FilePath
        });

        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/vtt"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-subrip"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

        return request;
    }

    private static string CreateSubtitlePath(
        string videoPath,
        SubtitleModel subtitle,
        int subtitleCount,
        int subtitleIndex)
    {
        var directory = Path.GetDirectoryName(videoPath) ?? string.Empty;
        var baseName = Path.GetFileNameWithoutExtension(videoPath);
        var extension = GetSubtitleExtension(subtitle.Url);
        var suffix = subtitleCount == 1
            ? string.Empty
            : $".{SanitizeFileName(subtitle.Name ?? subtitle.Id ?? (subtitleIndex + 1).ToString())}";

        return Path.Combine(directory, $"{baseName}{suffix}{extension}");
    }

    private static string GetSubtitleExtension(string subtitleUrl)
    {
        if (Uri.TryCreate(subtitleUrl, UriKind.Absolute, out var uri))
        {
            var extension = Path.GetExtension(uri.AbsolutePath);
            if (IsSupportedSubtitleExtension(extension))
            {
                return extension.ToLowerInvariant();
            }
        }

        var localExtension = Path.GetExtension(subtitleUrl);
        return IsSupportedSubtitleExtension(localExtension)
            ? localExtension.ToLowerInvariant()
            : ".srt";
    }

    private static bool IsSupportedSubtitleExtension(string? extension)
    {
        return extension is not null
               && (extension.Equals(".srt", StringComparison.OrdinalIgnoreCase)
                   || extension.Equals(".vtt", StringComparison.OrdinalIgnoreCase)
                   || extension.Equals(".ass", StringComparison.OrdinalIgnoreCase)
                   || extension.Equals(".ssa", StringComparison.OrdinalIgnoreCase)
                   || extension.Equals(".sub", StringComparison.OrdinalIgnoreCase)
                   || extension.Equals(".ttml", StringComparison.OrdinalIgnoreCase)
                   || extension.Equals(".dfxp", StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryGetLocalPath(string value, out string localPath)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            localPath = uri.IsFile ? uri.LocalPath : string.Empty;
            return uri.IsFile;
        }

        localPath = value;
        return true;
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, DownloadItemModel download)
    {
        var request = new HttpRequestMessage(method, download.Url);

        if (!string.IsNullOrWhiteSpace(download.Referer) && Uri.TryCreate(download.Referer, UriKind.Absolute, out var referer))
        {
            request.Headers.Referrer = referer;
        }

        if (download.Headers is not null)
        {
            foreach (var header in download.Headers)
            {
                if (string.IsNullOrWhiteSpace(header.Name) || string.IsNullOrWhiteSpace(header.Value))
                {
                    continue;
                }

                request.Headers.TryAddWithoutValidation(header.Name, header.Value);
            }
        }

        return request;
    }

    private static Dictionary<string, string> BuildHeaderDictionary(DownloadItemModel download)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (download.Headers is not null)
        {
            foreach (var header in download.Headers)
            {
                if (!string.IsNullOrWhiteSpace(header.Name) && !string.IsNullOrWhiteSpace(header.Value))
                {
                    headers[header.Name] = header.Value;
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(download.Referer))
        {
            headers.TryAdd("Referer", download.Referer);
        }

        return headers;
    }

    private static bool IsPotentialHlsUrl(string url)
    {
        return url.Contains(".m3u8", StringComparison.OrdinalIgnoreCase)
               || url.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)
               || url.Contains("/hls/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHlsResponse(string url, HttpResponseMessage response)
    {
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        return IsPotentialHlsUrl(url)
               || string.Equals(mediaType, "application/vnd.apple.mpegurl", StringComparison.OrdinalIgnoreCase)
               || string.Equals(mediaType, "application/x-mpegurl", StringComparison.OrdinalIgnoreCase)
               || string.Equals(mediaType, "audio/mpegurl", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHlsPlaylist(string content)
    {
        return content.TrimStart().StartsWith("#EXTM3U", StringComparison.OrdinalIgnoreCase);
    }

    private async Task SaveProgressSnapshotAsync()
    {
        await _lock.WaitAsync(CancellationToken.None);
        try
        {
            await SaveDownloadsAsync(CancellationToken.None);
        }
        finally
        {
            _lock.Release();
        }

        NotifyDownloadsChanged();
    }

    private async Task<List<DownloadItemModel>> ReadDownloadsAsync(CancellationToken cancellationToken)
    {
        var path = GetDownloadsFilePath();
        if (!File.Exists(path))
        {
            return [];
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<DownloadItemModel>>(stream, _jsonOptions, cancellationToken) ?? [];
    }

    private async Task SaveDownloadsAsync(CancellationToken cancellationToken)
    {
        var path = GetDownloadsFilePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, _downloads, _jsonOptions, cancellationToken);
    }

    private static string GetDownloadsFilePath()
    {
        return AppDataPath.GetDataPath("app", "downloads.json");
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var cleaned = new string(value.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "video.mp4" : cleaned;
    }

    private void NotifyDownloadsChanged()
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            DownloadsChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        Dispatcher.UIThread.Post(() => DownloadsChanged?.Invoke(this, EventArgs.Empty));
    }
}
