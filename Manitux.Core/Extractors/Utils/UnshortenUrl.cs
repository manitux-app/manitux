using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Manitux.Core.Extractors.Utils;

public static class ShortLink
{
    private static readonly List<ShortUrl> ShortList = new()
    {
        new(@"adf\.ly|j\.gs|q\.gs|u\.bb|ay\.gy|atominik\.com|tinyium\.com|microify\.com|threadsphere\.bid|clearload\.bid|activetect\.net|swiftviz\.net|briskgram\.net|stoodsef\.com|baymaleti\.net|thouth\.net|uclaut\.net|gloyah\.net|larati\.net|scuseami\.net", "adfly", UnshortenAdfly),
        new(@"linkup\.pro|buckler.link", "linkup", UnshortenLinkup),
        new(@"linksafe\.cc", "linksafe", uri => Task.FromResult(UnshortenLinksafe(uri))),
        new(@"mixdrop\.nuovoindirizzo\.com", "nuovoindirizzo", UnshortenNuovoIndirizzo),
        new(@"nuovolink\.com", "nuovolink", UnshortenNuovoLink),
        new(@"uprot\.net", "uprot", UnshortenUprot),
        new(@"davisonbarker\.pro|lowrihouston\.pro", "uprot", uri => Task.FromResult(UnshortenDavisonbarker(uri))),
        new(@"isecure\.link", "isecure", UnshortenIsecure)
    };

    public static bool IsShortLink(string url)
    {
        return !string.IsNullOrWhiteSpace(url)
               && ShortList.Any(x => x.Regex.IsMatch(url));
    }

    public static async Task<string> Unshorten(string uri, string? type = null, CancellationToken cancellationToken = default)
    {
        var currentUrl = uri;
        var visitedUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var count = 10;

        while (!visitedUrls.Contains(currentUrl) && count > 0)
        {
            visitedUrls.Add(currentUrl);
            count--;

            if (!Uri.TryCreate(currentUrl.Trim(), UriKind.Absolute, out var parsedUri) || string.IsNullOrWhiteSpace(parsedUri.Host))
            {
                throw new ArgumentException("No domain found in URI", nameof(uri));
            }

            var shortener = ShortList.FirstOrDefault(x =>
                x.Regex.IsMatch(parsedUri.Host) || string.Equals(type, x.Type, StringComparison.OrdinalIgnoreCase));

            if (shortener is null) break;
            currentUrl = await shortener.Function(currentUrl);
        }

        return currentUrl.Trim();
    }

    public static async Task<string> FollowRedirects(string uri, int maxRedirects = 10, CancellationToken cancellationToken = default)
    {
        var current = uri;
        for (var i = 0; i < maxRedirects; i++)
        {
            using var response = await SendAsync(current, allowRedirects: false, cancellationToken);
            if (!IsRedirect(response.StatusCode) || response.Headers.Location is null) break;
            current = ResolveUrl(current, response.Headers.Location.ToString());
        }

        return current;
    }

    public static async Task<string> UnshortenAdfly(string uri)
    {
        var html = await GetStringAsync(uri);
        var match = Regex.Match(html, @"var ysmm\s*=\s*['""](?<value>[^'""]+)['""];?", RegexOptions.IgnoreCase);
        if (!match.Success) return uri;

        var ysmm = match.Groups["value"].Value;
        var left = new StringBuilder();
        var right = new StringBuilder();

        foreach (var chunk in Chunk(ysmm, 2).Where(x => x.Length == 2))
        {
            left.Append(chunk[0]);
            right.Insert(0, chunk[1]);
        }

        var encodedUri = (left.ToString() + right).ToCharArray();
        var numbers = encodedUri
            .Select((value, index) => (Index: index, Value: value))
            .Where(x => char.IsDigit(x.Value))
            .ToList();

        foreach (var pair in numbers.Chunk(2).Where(x => x.Length == 2))
        {
            var xor = pair[0].Value ^ pair[1].Value;
            if (xor < 10)
            {
                encodedUri[pair[0].Index] = (char)('0' + xor);
            }
        }

        var decodedUri = Base64Decode(new string(encodedUri));
        if (decodedUri.Length > 32)
        {
            decodedUri = decodedUri[16..^16];
        }

        if (decodedUri.Contains("go.php?u=", StringComparison.OrdinalIgnoreCase))
        {
            decodedUri = Base64Decode(Regex.Replace(decodedUri, @"(.*?)u=", string.Empty, RegexOptions.IgnoreCase));
        }

        return string.IsNullOrWhiteSpace(decodedUri) ? uri : decodedUri;
    }

    public static async Task<string> UnshortenLinkup(string uri)
    {
        HttpResponseMessage? response = null;

        if (uri.Contains("/tv/", StringComparison.OrdinalIgnoreCase))
        {
            uri = uri.Replace("/tv/", "/tva/", StringComparison.OrdinalIgnoreCase);
        }
        else if (uri.Contains("delta", StringComparison.OrdinalIgnoreCase))
        {
            uri = uri.Replace("/delta/", "/adelta/", StringComparison.OrdinalIgnoreCase);
        }
        else if (uri.Contains("/ga/", StringComparison.OrdinalIgnoreCase) || uri.Contains("/ga2/", StringComparison.OrdinalIgnoreCase))
        {
            uri = Base64Decode(uri.Split('/').Last()).Trim();
        }
        else if (uri.Contains("/speedx/", StringComparison.OrdinalIgnoreCase))
        {
            uri = uri.Replace("http://linkup.pro/speedx", "http://speedvideo.net", StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            response = await SendAsync(uri, allowRedirects: true);
            uri = response.RequestMessage?.RequestUri?.ToString() ?? uri;
            var html = await response.Content.ReadAsStringAsync();
            var link =
                Regex.Match(html, @"<iframe[^<>]*src=['""](?<url>[^'"">]*)['""][^<>]*>", RegexOptions.IgnoreCase).Groups["url"].Value;

            if (string.IsNullOrWhiteSpace(link))
            {
                link = Regex.Match(html, @"action=""(?:[^/]+.*?/[^/]+/(?<id>[a-zA-Z0-9_]+))"">", RegexOptions.IgnoreCase).Groups["id"].Value;
            }

            if (string.IsNullOrWhiteSpace(link))
            {
                link = Regex.Matches(html, @"""href"",""(?<url>(.|\n)*?)""", RegexOptions.IgnoreCase)
                    .ElementAtOrDefault(1)
                    ?.Groups["url"].Value;
            }

            if (!string.IsNullOrWhiteSpace(link))
            {
                uri = link;
            }
        }

        var nested = Regex.Match(uri, @"^https?://.*?(https?://.*)", RegexOptions.IgnoreCase).Groups[1].Value;
        if (!string.IsNullOrWhiteSpace(nested))
        {
            uri = nested;
        }

        if (response is null)
        {
            using var redirectResponse = await SendAsync(uri, allowRedirects: false);
            if (redirectResponse.Headers.Location is not null)
            {
                uri = ResolveUrl(uri, redirectResponse.Headers.Location.ToString());
            }
        }

        if (uri.Contains("snip.", StringComparison.OrdinalIgnoreCase))
        {
            if (uri.Contains("out_generator", StringComparison.OrdinalIgnoreCase))
            {
                uri = Regex.Match(uri, @"url=(.*)$", RegexOptions.IgnoreCase).Groups[1].Value;
            }
            else if (uri.Contains("/decode/", StringComparison.OrdinalIgnoreCase))
            {
                uri = await FollowRedirects(uri);
            }
        }

        return uri;
    }

    public static string UnshortenLinksafe(string uri)
    {
        return Base64Decode(uri.Split("?url=").Last());
    }

    public static async Task<string> UnshortenNuovoIndirizzo(string uri)
    {
        using var response = await SendAsync(uri, allowRedirects: true);
        return response.Headers.TryGetValues("refresh", out var values)
            ? values.FirstOrDefault()?.Split('=').LastOrDefault() ?? uri
            : uri;
    }

    public static async Task<string> UnshortenNuovoLink(string uri)
    {
        var html = await GetStringAsync(uri);
        var href = Regex.Match(html, @"<a[^>]+href=['""](?<url>[^'""]+)['""]", RegexOptions.IgnoreCase).Groups["url"].Value;
        return string.IsNullOrWhiteSpace(href) ? uri : ResolveUrl(uri, href);
    }

    public static async Task<string> UnshortenUprot(string uri)
    {
        var html = await GetStringAsync(uri);
        foreach (Match match in Regex.Matches(html, @"<a[^>]+href=""(?<url>[^""]+)""[^>]*>.*?Continue", RegexOptions.IgnoreCase | RegexOptions.Singleline))
        {
            var link = match.Groups["url"].Value;
            if (link.Contains("https://maxstream.video", StringComparison.OrdinalIgnoreCase)
                || (link.Contains("https://uprot.net", StringComparison.OrdinalIgnoreCase) && !string.Equals(link, uri, StringComparison.OrdinalIgnoreCase)))
            {
                return link;
            }
        }

        return uri;
    }

    public static string UnshortenDavisonbarker(string uri)
    {
        return WebUtility.UrlDecode(uri.Split("dest=").Last());
    }

    public static async Task<string> UnshortenIsecure(string uri)
    {
        var html = await GetStringAsync(uri);
        var iframe = Regex.Match(html, @"<iframe[^>]+src=['""](?<url>[^'""]+)['""]", RegexOptions.IgnoreCase).Groups["url"].Value;
        return string.IsNullOrWhiteSpace(iframe) ? uri : iframe.Trim();
    }

    private static async Task<string> GetStringAsync(string uri)
    {
        using var response = await SendAsync(uri, allowRedirects: true);
        return await response.Content.ReadAsStringAsync();
    }

    private static async Task<HttpResponseMessage> SendAsync(string uri, bool allowRedirects, CancellationToken cancellationToken = default)
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = allowRedirects,
            UseProxy = false,
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };

        using var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(20)
        };

        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36");
        return await client.GetAsync(uri, cancellationToken);
    }

    private static bool IsRedirect(HttpStatusCode statusCode)
    {
        var code = (int)statusCode;
        return code is >= 300 and < 400;
    }

    private static string ResolveUrl(string baseUrl, string reference)
    {
        if (Uri.TryCreate(reference, UriKind.Absolute, out var absolute)) return absolute.ToString();
        return Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri)
               && Uri.TryCreate(baseUri, reference, out var resolved)
            ? resolved.ToString()
            : reference;
    }

    private static string Base64Decode(string value)
    {
        try
        {
            var normalized = value.Trim();
            var padding = normalized.Length % 4;
            if (padding > 0) normalized = normalized.PadRight(normalized.Length + 4 - padding, '=');
            return Encoding.UTF8.GetString(Convert.FromBase64String(normalized));
        }
        catch
        {
            return string.Empty;
        }
    }

    private static IEnumerable<string> Chunk(string value, int size)
    {
        for (var i = 0; i < value.Length; i += size)
        {
            yield return value.Substring(i, Math.Min(size, value.Length - i));
        }
    }

    public sealed record ShortUrl(string RegexPattern, string Type, Func<string, Task<string>> Function)
    {
        public Regex Regex { get; } = new(RegexPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }
}
