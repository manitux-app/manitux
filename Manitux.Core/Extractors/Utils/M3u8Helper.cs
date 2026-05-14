using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Manitux.Core.Models;

namespace Manitux.Core.Extractors.Utils;

public static class M3u8Helper
{
    private static readonly Regex ExtInfRegex = new(@"#EXTINF:(?<time>(?:[0-9]*[.])?[0-9]+|)[^\r\n]*\r?\n(?<url>[^\r\n]+)", RegexOptions.IgnoreCase);
    private static readonly Regex EncryptionRegex = new(@"#EXT-X-KEY:METHOD=(?<method>[^,]+),URI=""(?<url>[^""]+)""(?:,IV=(?<iv>[^,\r\n]+))?", RegexOptions.IgnoreCase);

    public static async Task<List<VideoSourceModel>> GenerateM3u8(
        string source,
        string streamUrl,
        string referer,
        int? quality = null,
        Dictionary<string, string>? headers = null,
        string? name = null,
        bool returnThis = true,
        CancellationToken cancellationToken = default)
    {
        var streams = await M3u8Generation(
            new M3u8Stream(streamUrl, quality, headers ?? new Dictionary<string, string>()),
            returnThis,
            cancellationToken);

        return streams
            .Select(stream => new VideoSourceModel
            {
                Name = name ?? source,
                Url = stream.StreamUrl,
                Referer = referer,
                Headers = ToHeaderModels(stream.Headers),
            })
            .ToList();
    }

    public static async Task<List<M3u8Stream>> M3u8Generation(
        M3u8Stream m3u8,
        bool returnThis = true,
        CancellationToken cancellationToken = default)
    {
        var response = await GetStringAsync(m3u8.StreamUrl, m3u8.Headers, cancellationToken);
        if (string.IsNullOrWhiteSpace(response)) return new List<M3u8Stream>();

        var variants = ParseVariants(m3u8.StreamUrl, response)
            .Where(x => !x.IsTrickPlay)
            .ToList();

        var list = variants
            .Select(video => new M3u8Stream(video.Url, video.Height > 0 ? video.Height : null, m3u8.Headers))
            .ToList();

        if (variants.Count == 0 || returnThis)
        {
            if (variants.Count > 0 || IsMediaPlaylist(response))
            {
                list.Add(m3u8);
            }
        }

        return list
            .GroupBy(x => x.StreamUrl, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .ToList();
    }

    public static async Task<LazyHlsDownloadData> HlsLazy(
        M3u8Stream playlistStream,
        bool selectBest = true,
        bool requireAudio = false,
        int depth = 3,
        CancellationToken cancellationToken = default)
    {
        if (depth < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(depth));
        }

        var playlistResponse = await GetStringAsync(playlistStream.StreamUrl, playlistStream.Headers, cancellationToken);
        if (string.IsNullOrWhiteSpace(playlistResponse))
        {
            throw new InvalidOperationException("M3u8 playlist is empty");
        }

        var variants = ParseVariants(playlistStream.StreamUrl, playlistResponse)
            .Where(x => requireAudio ? x.IsPlayableStandalone : !x.IsTrickPlay)
            .ToList();

        if (variants.Count > 0)
        {
            var bestVideo = selectBest
                ? variants.MaxBy(x => (long)Math.Max(1, x.Width) * Math.Max(1, x.Height) * 1000L + Math.Max(0, x.AverageBitrate))
                : variants.MinBy(x => (long)Math.Max(1, x.Width) * Math.Max(1, x.Height) * 1000L + Math.Max(0, x.AverageBitrate));

            if (bestVideo is null)
            {
                throw new InvalidOperationException(requireAudio ? "M3u8 contains no video with audio" : "M3u8 contains no video");
            }

            return await HlsLazy(
                new M3u8Stream(bestVideo.Url, bestVideo.Height > 0 ? bestVideo.Height : null, playlistStream.Headers),
                selectBest,
                requireAudio,
                depth - 1,
                cancellationToken);
        }

        var relativeUrl = GetParentLink(playlistStream.StreamUrl);
        var encryptionData = Array.Empty<byte>();
        var encryptionIv = Array.Empty<byte>();
        var encryptionState = false;

        var encryptionMatch = EncryptionRegex.Match(playlistResponse);
        if (encryptionMatch.Success)
        {
            var method = encryptionMatch.Groups["method"].Value;
            if (!method.Equals("NONE", StringComparison.OrdinalIgnoreCase))
            {
                encryptionState = true;
                var encryptionUri = MakeAbsoluteUrl(encryptionMatch.Groups["url"].Value, relativeUrl);
                encryptionIv = ParseIv(encryptionMatch.Groups["iv"].Value);
                encryptionData = await GetBytesAsync(encryptionUri, playlistStream.Headers, cancellationToken);
            }
        }

        var tsLinks = ExtInfRegex
            .Matches(playlistResponse + Environment.NewLine)
            .Select(match =>
            {
                var value = match.Groups["url"].Value.Trim();
                var time = double.TryParse(match.Groups["time"].Value, System.Globalization.CultureInfo.InvariantCulture, out var seconds)
                    ? seconds
                    : (double?)null;

                return new TsLink(MakeAbsoluteUrl(value, relativeUrl), time);
            })
            .ToList();

        if (tsLinks.Count == 0)
        {
            throw new InvalidOperationException("M3u8 must contain media segment files");
        }

        return new LazyHlsDownloadData(
            encryptionData,
            encryptionIv,
            encryptionState,
            tsLinks,
            relativeUrl,
            playlistStream.Headers);
    }

    public static byte[] GetDecrypted(byte[] secretKey, byte[] data, byte[]? iv, int index)
    {
        var ivKey = iv is null || iv.Length == 0 ? DefaultIv(index) : iv;

        using var aes = Aes.Create();
        aes.Key = secretKey;
        aes.IV = ivKey;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(data, 0, data.Length);
    }

    private static List<VariantStream> ParseVariants(string playlistUrl, string playlist)
    {
        var parsed = HlsPlaylistParser.Parse(playlistUrl, playlist);
        return parsed?.Variants
                   .Select(variant => new VariantStream(
                       variant.Url,
                       Math.Max(0, variant.Format.Width),
                       Math.Max(0, variant.Format.Height),
                       Math.Max(0, variant.Format.AverageBitrate > 0 ? variant.Format.AverageBitrate : variant.Format.PeakBitrate),
                       variant.IsTrickPlay(),
                       variant.IsPlayableStandalone(parsed)))
                   .ToList()
               ?? new List<VariantStream>();
    }

    private static bool IsMediaPlaylist(string playlist)
    {
        return ExtInfRegex.IsMatch(playlist);
    }

    private static string GetParentLink(string uri)
    {
        var index = uri.LastIndexOf('/');
        return index <= 0 ? uri : uri[..index];
    }

    private static string MakeAbsoluteUrl(string url, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;
        if (Uri.TryCreate(url, UriKind.Absolute, out var absolute)) return absolute.ToString();
        if (url.StartsWith("//", StringComparison.Ordinal)) return "https:" + url;

        if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri)
            && Uri.TryCreate(baseUri, url, out var resolved))
        {
            return resolved.ToString();
        }

        return $"{baseUrl.TrimEnd('/')}/{url.TrimStart('/')}";
    }

    private static byte[] DefaultIv(int index)
    {
        var value = index + 1;
        var bytes = new byte[16];
        bytes[12] = (byte)((value >> 24) & 0xff);
        bytes[13] = (byte)((value >> 16) & 0xff);
        bytes[14] = (byte)((value >> 8) & 0xff);
        bytes[15] = (byte)(value & 0xff);
        return bytes;
    }

    private static byte[] ParseIv(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return Array.Empty<byte>();

        var normalized = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? value[2..]
            : value;

        if (normalized.Length % 2 != 0) normalized = "0" + normalized;

        try
        {
            var bytes = Convert.FromHexString(normalized);
            if (bytes.Length == 16) return bytes;
            if (bytes.Length > 16) return bytes[^16..];

            var padded = new byte[16];
            Buffer.BlockCopy(bytes, 0, padded, 16 - bytes.Length, bytes.Length);
            return padded;
        }
        catch
        {
            return System.Text.Encoding.UTF8.GetBytes(value);
        }
    }

    private static async Task<string> GetStringAsync(string url, Dictionary<string, string> headers, CancellationToken cancellationToken)
    {
        using var client = CreateClient(headers);
        return await client.GetStringAsync(url, cancellationToken);
    }

    private static async Task<byte[]> GetBytesAsync(string url, Dictionary<string, string> headers, CancellationToken cancellationToken)
    {
        using var client = CreateClient(headers);
        return await client.GetByteArrayAsync(url, cancellationToken);
    }

    private static HttpClient CreateClient(Dictionary<string, string> headers)
    {
        var handler = new HttpClientHandler
        {
            UseProxy = false,
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };

        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(20)
        };

        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

        foreach (var header in headers)
        {
            if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
        }

        return client;
    }

    private static List<HeaderModel>? ToHeaderModels(Dictionary<string, string> headers)
    {
        return headers.Count == 0
            ? null
            : headers.Select(x => new HeaderModel { Name = x.Key, Value = x.Value }).ToList();
    }

    public sealed record M3u8Stream(string StreamUrl, int? Quality = null, Dictionary<string, string>? Headers = null)
    {
        public Dictionary<string, string> Headers { get; } = Headers ?? new Dictionary<string, string>();
    }

    public sealed record TsLink(string Url, double? Time);

    public sealed class LazyHlsDownloadData
    {
        private readonly byte[] _encryptionData;
        private readonly byte[] _encryptionIv;

        public LazyHlsDownloadData(
            byte[] encryptionData,
            byte[] encryptionIv,
            bool isEncrypted,
            List<TsLink> allTsLinks,
            string relativeUrl,
            Dictionary<string, string> headers)
        {
            _encryptionData = encryptionData;
            _encryptionIv = encryptionIv;
            IsEncrypted = isEncrypted;
            AllTsLinks = allTsLinks;
            RelativeUrl = relativeUrl;
            Headers = headers;
        }

        public bool IsEncrypted { get; }
        public List<TsLink> AllTsLinks { get; }
        public string RelativeUrl { get; }
        public Dictionary<string, string> Headers { get; }
        public int Size => AllTsLinks.Count;

        public async Task<byte[]?> ResolveLinkWhileSafe(
            int index,
            Func<bool> condition,
            int tries = 3,
            int failDelayMs = 3000,
            CancellationToken cancellationToken = default)
        {
            for (var i = 0; i < tries; i++)
            {
                if (!condition()) return null;

                try
                {
                    var output = await ResolveLink(index, cancellationToken);
                    return condition() ? output : null;
                }
                catch (OperationCanceledException)
                {
                    return null;
                }
                catch (ArgumentException)
                {
                    return null;
                }
                catch
                {
                    await Task.Delay(failDelayMs, cancellationToken);
                }
            }

            return null;
        }

        public async Task<byte[]?> ResolveLinkSafe(
            int index,
            int tries = 3,
            int failDelayMs = 3000,
            CancellationToken cancellationToken = default)
        {
            for (var i = 0; i < tries; i++)
            {
                try
                {
                    return await ResolveLink(index, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return null;
                }
                catch (ArgumentException)
                {
                    return null;
                }
                catch
                {
                    await Task.Delay(failDelayMs, cancellationToken);
                }
            }

            return null;
        }

        public async Task<byte[]> ResolveLink(int index, CancellationToken cancellationToken = default)
        {
            if (index < 0 || index >= Size)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            var ts = AllTsLinks[index];
            var data = await GetBytesAsync(ts.Url, Headers, cancellationToken);
            if (data.Length == 0)
            {
                throw new InvalidOperationException("no data");
            }

            return IsEncrypted
                ? GetDecrypted(_encryptionData, data, _encryptionIv, index)
                : data;
        }
    }

    private sealed record VariantStream(
        string Url,
        int Width,
        int Height,
        int AverageBitrate,
        bool IsTrickPlay,
        bool IsPlayableStandalone);
}
