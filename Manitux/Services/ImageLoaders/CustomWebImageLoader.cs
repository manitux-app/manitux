using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncImageLoader.Loaders;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Manitux.Services.Storage;

namespace Manitux.Services.ImageLoaders;

public class CustomWebImageLoader : RamCachedWebImageLoader
{
    public static CustomWebImageLoader Instance { get; } = new CustomWebImageLoader();

    private const int MaxAttempts = 4;
    private const int MaxConcurrentRequestsPerHost = 3;
    private const int MaxImageSize = 10 * 1024 * 1024;
    private const int AndroidDecodeWidth = 360;
    private const long DiskCacheTargetSize = 200L * 1024 * 1024;
    private const long DiskCacheMaximumSize = 256L * 1024 * 1024;
    private static readonly TimeSpan DiskCacheLifetime = TimeSpan.FromDays(30);

    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _hostLimiters = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Task<byte[]?>> _inflightRequests = new(StringComparer.Ordinal);
    private readonly string _diskCachePath = AppDataPath.GetAppPath("cache", "images");
    private int _cacheCleanupStarted;

    public CustomWebImageLoader()
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            MaxConnectionsPerServer = MaxConcurrentRequestsPerHost,
            AutomaticDecompression = DecompressionMethods.All
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(20)
        };

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Accept.ParseAdd("image/avif,image/webp,image/apng,image/svg+xml,image/*,*/*;q=0.8");
        _httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7");
    }

    protected override Task<byte[]?> LoadDataFromExternalAsync(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return Task.FromResult<byte[]?>(null);
        }

        return _inflightRequests.GetOrAdd(url, _ => LoadAndReleaseAsync(uri, url));
    }

    protected override async Task<Bitmap?> LoadAsync(string url, IStorageProvider? storageProvider)
    {
        if (!OperatingSystem.IsAndroid() || !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https"))
        {
            return await base.LoadAsync(url, storageProvider).ConfigureAwait(false);
        }

        var bytes = await LoadDataFromExternalAsync(url).ConfigureAwait(false);
        if (bytes is null || bytes.Length == 0)
        {
            return null;
        }

        await using var stream = new MemoryStream(bytes, writable: false);
        return Bitmap.DecodeToWidth(stream, AndroidDecodeWidth);
    }

    private async Task<byte[]?> LoadAndReleaseAsync(Uri uri, string url)
    {
        try
        {
            return await LoadFromCacheOrWebAsync(uri, url).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"CustomWebImageLoader Error: {url} - {ex.Message}");
            return null;
        }
        finally
        {
            _inflightRequests.TryRemove(url, out _);
        }
    }

    private async Task<byte[]?> LoadFromCacheOrWebAsync(Uri uri, string url)
    {
        var cacheFile = GetCacheFilePath(url);
        try
        {
            if (File.Exists(cacheFile) &&
                DateTime.UtcNow - File.GetLastWriteTimeUtc(cacheFile) <= DiskCacheLifetime)
            {
                return await File.ReadAllBytesAsync(cacheFile).ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"CustomWebImageLoader cache read failed: {exception.Message}");
        }

        var bytes = await LoadWithRetryAsync(uri).ConfigureAwait(false);
        if (bytes is null)
        {
            return null;
        }

        try
        {
            Directory.CreateDirectory(_diskCachePath);
            await File.WriteAllBytesAsync(cacheFile, bytes).ConfigureAwait(false);
            StartCacheCleanupIfNeeded();
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"CustomWebImageLoader cache write failed: {exception.Message}");
        }

        return bytes;
    }

    private string GetCacheFilePath(string url)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(url)));
        return Path.Combine(_diskCachePath, hash + ".img");
    }

    private void StartCacheCleanupIfNeeded()
    {
        if (Interlocked.Exchange(ref _cacheCleanupStarted, 1) == 0)
        {
            _ = Task.Run(CleanupDiskCache);
        }
    }

    private void CleanupDiskCache()
    {
        try
        {
            var files = new DirectoryInfo(_diskCachePath)
                .EnumerateFiles("*.img")
                .OrderBy(file => file.LastWriteTimeUtc)
                .ToList();
            var totalSize = files.Sum(file => file.Length);

            foreach (var file in files)
            {
                var expired = DateTime.UtcNow - file.LastWriteTimeUtc > DiskCacheLifetime;
                if (!expired && totalSize <= DiskCacheMaximumSize)
                {
                    break;
                }

                totalSize -= file.Length;
                file.Delete();
                if (!expired && totalSize <= DiskCacheTargetSize)
                {
                    break;
                }
            }
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"CustomWebImageLoader cache cleanup failed: {exception.Message}");
        }
    }

    private async Task<byte[]?> LoadWithRetryAsync(Uri uri)
    {
        var limiter = _hostLimiters.GetOrAdd(
            uri.IdnHost,
            _ => new SemaphoreSlim(MaxConcurrentRequestsPerHost, MaxConcurrentRequestsPerHost));

        await limiter.WaitAsync().ConfigureAwait(false);
        try
        {
            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                using var request = CreateRequest(uri);
                using var response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    if (response.Content.Headers.ContentLength > MaxImageSize)
                    {
                        return null;
                    }

                    return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                }

                if (response.StatusCode != HttpStatusCode.TooManyRequests || attempt == MaxAttempts)
                {
                    Debug.WriteLine(
                        $"CustomWebImageLoader HTTP {(int)response.StatusCode}: {uri}");
                    return null;
                }

                var delay = GetRetryDelay(response.Headers.RetryAfter, attempt);
                Debug.WriteLine(
                    $"CustomWebImageLoader 429: {uri} - retry {attempt}/{MaxAttempts - 1} after {delay.TotalSeconds:0.##}s");
                await Task.Delay(delay).ConfigureAwait(false);
            }

            return null;
        }
        finally
        {
            limiter.Release();
        }
    }

    private static HttpRequestMessage CreateRequest(Uri uri)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Referrer = new Uri(uri.GetLeftPart(UriPartial.Authority) + "/");
        request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = false };
        return request;
    }

    private static TimeSpan GetRetryDelay(RetryConditionHeaderValue? retryAfter, int attempt)
    {
        var serverDelay = retryAfter?.Delta;
        if (serverDelay is null && retryAfter?.Date is not null)
        {
            serverDelay = retryAfter.Date.Value - DateTimeOffset.UtcNow;
        }

        var delay = serverDelay is { } value && value > TimeSpan.Zero
            ? value
            : TimeSpan.FromMilliseconds(1500 * Math.Pow(2, attempt - 1));

        delay += TimeSpan.FromMilliseconds(Random.Shared.Next(100, 401));

        return delay > TimeSpan.FromSeconds(15)
            ? TimeSpan.FromSeconds(15)
            : delay;
    }
}

public class CustomWebImageLoaderLinuxTest : RamCachedWebImageLoader
{
    public static CustomWebImageLoaderLinuxTest Instance { get; } = new CustomWebImageLoaderLinuxTest();

    private readonly HttpClient _httpClient;

    public CustomWebImageLoaderLinuxTest()
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            MaxConnectionsPerServer = 10
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Referrer = new System.Uri("https://www.google.com/");
    }

    protected override async Task<Bitmap?> LoadAsync(string url, IStorageProvider? storageProvider)
    {
        //Debug.WriteLine("LoadAsync url: " + url);
        if (OperatingSystem.IsLinux() & url.Contains(".webp", StringComparison.OrdinalIgnoreCase))
        {
            Debug.WriteLine("bypass webp (for linux)");
            return null;
        }

        return await base.LoadAsync(url, storageProvider).ConfigureAwait(false);
    }

    protected override async Task<byte[]?> LoadDataFromExternalAsync(string url)
    {
        if (string.IsNullOrEmpty(url)) return null;
        //Debug.WriteLine("LoadDataFromExternalAsync url: " + url);

        try
        {
            // if (url.Contains(".webp", StringComparison.OrdinalIgnoreCase))
            // {
            //      Debug.WriteLine("WebP atlanıyor (Linux kararlılığı için)");
            //      byte[] smallImage = new byte[]
            //         {
            //             0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
            //             0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x00, 0x00, 0x00, 0x00, 0x3A, 0x7E, 0x9B,
            //             0x55, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41, 0x54, 0x08, 0xD7, 0x63, 0x60, 0x00, 0x00, 0x00,
            //             0x02, 0x00, 0x01, 0xE2, 0x21, 0xBC, 0x33, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE,
            //             0x42, 0x60, 0x82
            //         };
            //      return smallImage; 
            // }

            if (OperatingSystem.IsLinux() & url.Contains(".webp", StringComparison.OrdinalIgnoreCase))
            {
                Debug.WriteLine("bypass webp (for linux)");
                return null;
            }

            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode) return null;

            var contentLength = response.Content.Headers.ContentLength;
            if (contentLength > 10 * 1024 * 1024) return null;

            return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Image Load Failed: {url} | Error: {ex.Message}");
            return null;
        }
    }

    public override async Task<Bitmap?> ProvideImageAsync(string url)
    {
        //Debug.WriteLine("ProvideImageAsync url: " + url);
        if (OperatingSystem.IsLinux() & url.Contains(".webp", StringComparison.OrdinalIgnoreCase))
        {
            Debug.WriteLine("bypass webp (for linux)");
            return null;
        }

        return await LoadAsync(url);
    }
}
