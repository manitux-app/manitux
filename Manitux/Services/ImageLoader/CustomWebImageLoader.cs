using System.Diagnostics;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AsyncImageLoader.Loaders;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace Manitux.Services.ImageLoaders;

public class CustomWebImageLoader : RamCachedWebImageLoader
{
    public static CustomWebImageLoader Instance { get; } = new CustomWebImageLoader();

    private readonly HttpClient _httpClient;

    public CustomWebImageLoader()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Referrer = new System.Uri("https://google.com/");
    }

    protected override async Task<byte[]?> LoadDataFromExternalAsync(string url)
    {
        try
        {
            return await _httpClient.GetByteArrayAsync(url).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"CustomWebImageLoader Error: {url} - {ex.Message}");
            return null;
        }
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

