using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Manitux.Core.Helpers;
using Manitux.Core.Models;
using TlsClient.Core.Models.Entities;

namespace Manitux.Core.Extractors.Helpers;

public static class GogoHelper
{
    public static async Task<List<VideoSourceModel>> ExtractVidstream(
        string iframeUrl,
        string mainApiName,
        string? iv,
        string? secretKey,
        string? secretDecryptKey,
        bool isUsingAdaptiveKeys,
        bool isUsingAdaptiveData,
        string? iframeHtml = null)
    {
        var results = new List<VideoSourceModel>();

        if ((iv is null || secretKey is null || secretDecryptKey is null) && !isUsingAdaptiveKeys)
        {
            return results;
        }

        var id = GetQueryValue(iframeUrl, "id");
        if (string.IsNullOrWhiteSpace(id)) return results;

        using var http = new HttpHelper();
        var documentHtml = iframeHtml;
        if ((iv is null || isUsingAdaptiveData) && string.IsNullOrWhiteSpace(documentHtml))
        {
            documentHtml = await http.HttpGet(
                iframeUrl,
                headers: GetPageHeaders(),
                identifier: TlsClientIdentifier.Cloudscraper);
        }

        var foundIv = iv ?? await GetAdaptiveIv(http, documentHtml);
        if (string.IsNullOrWhiteSpace(foundIv)) return results;

        var foundKey = secretKey ?? GetKey(Base64Decode(id) + foundIv);
        if (string.IsNullOrWhiteSpace(foundKey)) return results;

        var foundDecryptKey = secretDecryptKey ?? foundKey;
        var mainUrl = GetBaseUrl(iframeUrl);
        var encryptedId = CryptoHandler(id, foundIv, foundKey);
        if (string.IsNullOrWhiteSpace(encryptedId)) return results;

        var requestData = $"id={Uri.EscapeDataString(encryptedId)}&alias={Uri.EscapeDataString(id)}";
        if (isUsingAdaptiveData)
        {
            var headers = await GetAdaptiveRequestData(http, documentHtml, iframeUrl, foundIv, foundKey);
            if (!string.IsNullOrWhiteSpace(headers))
            {
                requestData += "&" + headers.Substring(headers.IndexOf('&') + 1);
            }
        }

        var response = await http.HttpGet(
            $"{mainUrl}/encrypt-ajax.php?{requestData}",
            referer: iframeUrl,
            headers: GetAjaxHeaders(),
            identifier: TlsClientIdentifier.Cloudscraper);

        if (string.IsNullOrWhiteSpace(response)) return results;

        var json = JsonSerializer.Deserialize<GogoJsonData>(response);
        if (string.IsNullOrWhiteSpace(json?.Data)) return results;

        var decrypted = CryptoHandler(json.Data, foundIv, foundDecryptKey, encrypt: false);
        if (string.IsNullOrWhiteSpace(decrypted)) return results;

        var sources = JsonSerializer.Deserialize<GogoSources>(decrypted);
        AddSources(results, sources?.Source, mainApiName, mainUrl);
        AddSources(results, sources?.SourceBk, mainApiName, mainUrl);

        return results;
    }

    private static string? GetKey(string id)
    {
        try
        {
            var hex = string.Concat(id.Select(x => ((int)x).ToString("x")));
            return hex.Length >= 32 ? hex[..32] : null;
        }
        catch
        {
            return null;
        }
    }

    private static string? CryptoHandler(string value, string iv, string secretKey, bool encrypt = true)
    {
        try
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(secretKey);
            aes.IV = Encoding.UTF8.GetBytes(iv);
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            if (!encrypt)
            {
                var encrypted = Convert.FromBase64String(value);
                using var decryptor = aes.CreateDecryptor();
                var decrypted = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
                return Encoding.UTF8.GetString(decrypted);
            }

            var plain = Encoding.UTF8.GetBytes(value);
            using var encryptor = aes.CreateEncryptor();
            return Convert.ToBase64String(encryptor.TransformFinalBlock(plain, 0, plain.Length));
        }
        catch
        {
            return null;
        }
    }

    private static async Task<string?> GetAdaptiveIv(HttpHelper http, string? iframeHtml)
    {
        if (string.IsNullOrWhiteSpace(iframeHtml)) return null;

        using var document = await http.HtmlParse(iframeHtml);
        var className = document?
            .QuerySelector("div.wrapper[class*=container]")
            ?.GetAttribute("class");

        return className?
            .Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .LastOrDefault();
    }

    private static async Task<string?> GetAdaptiveRequestData(
        HttpHelper http,
        string? iframeHtml,
        string iframeUrl,
        string iv,
        string secretKey)
    {
        var html = iframeHtml;
        if (string.IsNullOrWhiteSpace(html))
        {
            html = await http.HttpGet(
                iframeUrl,
                headers: GetPageHeaders(),
                identifier: TlsClientIdentifier.Cloudscraper);
        }

        if (string.IsNullOrWhiteSpace(html)) return null;

        using var document = await http.HtmlParse(html);
        var encryptedData = document?
            .QuerySelector("script[data-name='episode']")
            ?.GetAttribute("data-value");

        return string.IsNullOrWhiteSpace(encryptedData)
            ? null
            : CryptoHandler(encryptedData, iv, secretKey, encrypt: false);
    }

    private static void AddSources(
        List<VideoSourceModel> results,
        List<GogoSource>? sources,
        string mainApiName,
        string mainUrl)
    {
        if (sources is null) return;

        foreach (var source in sources)
        {
            if (string.IsNullOrWhiteSpace(source.File)) continue;

            var name = string.IsNullOrWhiteSpace(source.Label)
                ? mainApiName
                : $"{mainApiName} - {source.Label}";

            var headers = source.File.Contains(".m3u8", StringComparison.OrdinalIgnoreCase)
                ? new List<HeaderModel>
                {
                    new() { Name = "Origin", Value = "https://plyr.link" }
                }
                : null;

            results.Add(new VideoSourceModel
            {
                Name = name,
                Url = source.File,
                Referer = mainUrl,
                Headers = headers
            });
        }
    }

    private static string? GetQueryValue(string url, string key)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;

        foreach (var part in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split('=', 2);
            if (pair.Length == 2 && string.Equals(Uri.UnescapeDataString(pair[0]), key, StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(pair[1]);
            }
        }

        var match = Regex.Match(url, $@"[?&]{Regex.Escape(key)}=(?<value>[^&]+)", RegexOptions.IgnoreCase);
        return match.Success ? Uri.UnescapeDataString(match.Groups["value"].Value) : null;
    }

    private static string GetBaseUrl(string url)
    {
        var uri = new Uri(url);
        return $"{uri.Scheme}://{uri.Host}";
    }

    private static string Base64Decode(string value)
    {
        var padded = value.PadRight(value.Length + (4 - value.Length % 4) % 4, '=');
        return Encoding.UTF8.GetString(Convert.FromBase64String(padded));
    }

    private static Dictionary<string, string> GetPageHeaders()
    {
        return new Dictionary<string, string>
        {
            ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
            ["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36"
        };
    }

    private static Dictionary<string, string> GetAjaxHeaders()
    {
        var headers = GetPageHeaders();
        headers["Accept"] = "application/json, text/javascript, */*; q=0.01";
        headers["X-Requested-With"] = "XMLHttpRequest";
        return headers;
    }

    public sealed class GogoSources
    {
        [JsonPropertyName("source")]
        public List<GogoSource>? Source { get; set; }

        [JsonPropertyName("sourceBk")]
        public List<GogoSource>? SourceBk { get; set; }
    }

    public sealed class GogoSource
    {
        [JsonPropertyName("file")]
        public string File { get; set; } = string.Empty;

        [JsonPropertyName("label")]
        public string? Label { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("default")]
        public string? Default { get; set; }
    }

    public sealed class GogoJsonData
    {
        [JsonPropertyName("data")]
        public string? Data { get; set; }
    }
}
