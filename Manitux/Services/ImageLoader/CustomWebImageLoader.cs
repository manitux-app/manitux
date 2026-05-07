using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using AsyncImageLoader.Loaders;

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
