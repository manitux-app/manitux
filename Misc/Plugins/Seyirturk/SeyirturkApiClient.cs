using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Manitux.Core.Helpers;

namespace Manitux.Seyirturk;

internal sealed class SeyirturkApiClient : HttpHelper
{
    private const string RootCheckUrl = "https://seyirturk.net/rootcheck/kodi.php";
    private const string AddonVersion = "3.70.6";
    private const string VidName = "seyirTURK";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private string? _root;
    private string? _root2;

    public async Task<string> GetRootAsync(string? configuredRoot)
    {
        // if (!string.IsNullOrWhiteSpace(configuredRoot))
        // {
        //     _root = EnsureTrailingSlash(configuredRoot);
        //     _root2 = GetRoot2(_root);
        //     return _root;
        // }

        // if (!string.IsNullOrWhiteSpace(_root))
        // {
        //     return _root;
        // }

        // var encoded = await HttpGet(RootCheckUrl, headers: DefaultHeaders());
        // if (string.IsNullOrWhiteSpace(encoded))
        // {
        //     throw new InvalidOperationException("Seyirturk root endpoint returned an empty response.");
        // }

        // var reversed = new string(encoded.Trim().Reverse().ToArray());
        // var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(reversed));

        // _root = EnsureTrailingSlash(decoded) + "sey/back/";
        // _root2 = GetRoot2(_root);

        _root = configuredRoot + "sey/back/";
         _root2 = GetRoot2(_root);
        return _root;
    }

    public async Task<string> GetRoot2Async(string? configuredRoot)
    {
        var root = await GetRootAsync(configuredRoot);
        _root2 ??= GetRoot2(root);
        return _root2;
    }

    public async Task<List<SeyirturkMainItem>> GetMainAsync(string? configuredRoot)
    {
        var root2 = await GetRoot2Async(configuredRoot);
        var url = $"{root2}/kodi/main.php?ct={ClientToken()}&surum={Uri.EscapeDataString(AddonVersion)}";
        var response = await GetJsonAsync<SeyirturkMainResponse>(url);
        return response?.Main ?? [];
    }

    public async Task<SeyirturkMoviesResponse?> GetMoviesAsync(string url)
    {
        return await GetJsonAsync<SeyirturkMoviesResponse>(url);
    }

    public async Task<SeyirturkLinksResponse?> GetLinksAsync(string url)
    {
        return await GetJsonAsync<SeyirturkLinksResponse>(url);
    }

    public async Task<SeyirturkEpisodesResponse?> GetEpisodesAsync(string url)
    {
        return await GetJsonAsync<SeyirturkEpisodesResponse>(url);
    }

    public async Task<string?> GetRawAsync(string url)
    {
        return await HttpGet(AppendClientToken(url), headers: DefaultHeaders());
    }

    public string BuildStreamsUrl(string root, string id)
    {
        return $"{EnsureTrailingSlash(root)}streams.php?id={Uri.EscapeDataString(id)}";
    }

    public string BuildEpisodesUrl(string root, string id)
    {
        return $"{EnsureTrailingSlash(root)}episodes.php?id={Uri.EscapeDataString(id)}";
    }

    public string BuildSearchUrl(string root, string query)
    {
        return $"{EnsureTrailingSlash(root)}search.php?name={Uri.EscapeDataString(query)}&p_type=Movie";
    }

    public string AppendClientToken(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            return url;
        }

        if (url.Contains("ct=", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        return url + (url.Contains('?', StringComparison.Ordinal) ? "&" : "?") + "ct=" + ClientToken();
    }

    private async Task<T?> GetJsonAsync<T>(string url)
    {
        var json = await HttpGet(AppendClientToken(url), headers: DefaultHeaders());
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch
        {
            return default;
        }
    }

    private static Dictionary<string, string> DefaultHeaders()
    {
        return new Dictionary<string, string>
        {
            ["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36",
            ["Accept"] = "application/json,text/html;q=0.9,*/*;q=0.8"
        };
    }

    private static string ClientToken()
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(VidName));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string EnsureTrailingSlash(string value)
    {
        return value.EndsWith("/", StringComparison.Ordinal) ? value : value + "/";
    }

    private static string GetRoot2(string root)
    {
        var trimmed = root.TrimEnd('/');
        var last = trimmed.LastIndexOf('/');
        if (last > 0)
        {
            trimmed = trimmed[..last];
        }

        last = trimmed.LastIndexOf('/');
        return last > 0 ? trimmed[..last] : trimmed;
    }
}
