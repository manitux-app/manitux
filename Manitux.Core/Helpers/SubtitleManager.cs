using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using CodeLogic.Core.Logging;
using TlsClient.Core.Builders;
using TlsClient.Core.Models.Entities;
using TlsClient.Core.Models.Requests;
using TlsClient.Native.Extensions;

namespace Manitux.Core.Helpers;

public class SubtitleManager
{
    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36";
    private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(10);
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".srt",
        ".vtt",
        ".ass",
        ".ssa",
        ".sub",
        ".ttml",
        ".dfxp"
    };

    public async Task<string> ResolveAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return url;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return url;
        }

        if (await IsOkAsync(uri, cancellationToken))
        {
            return url;
        }

        return await DownloadWithTlsAsync(uri, cancellationToken) ?? url;
    }

    public Task<string> ResolveUrlAsync(string url, CancellationToken cancellationToken = default)
        => ResolveAsync(url, cancellationToken);

    private static async Task<bool> IsOkAsync(Uri uri, CancellationToken cancellationToken)
    {
        try
        {
            using var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                UseProxy = false,
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };

            using var client = new HttpClient(handler)
            {
                Timeout = CheckTimeout
            };

            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/vtt"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-subrip"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

            using var head = new HttpRequestMessage(HttpMethod.Head, uri);
            using var headResponse = await client.SendAsync(head, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (headResponse.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }

            if (headResponse.StatusCode != HttpStatusCode.MethodNotAllowed
                && headResponse.StatusCode != HttpStatusCode.NotImplemented)
            {
                return false;
            }

            using var get = new HttpRequestMessage(HttpMethod.Get, uri);
            using var getResponse = await client.SendAsync(get, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return getResponse.StatusCode == HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            LogHelper.Http.Log(LogLevel.Debug, $"[SubtitleManager] HTTP status check failed. Url: {uri} Error: {ex.Message}");
            return false;
        }
    }

    private static async Task<string?> DownloadWithTlsAsync(Uri uri, CancellationToken cancellationToken)
    {
        var filePath = CreateTempFilePath(uri);

        try
        {
            var clientBuilder = new TlsClientBuilder()
                .WithIdentifier(TlsClientIdentifier.Chrome144)
                .WithUserAgent(UserAgent)
                .WithFollowRedirects(true)
                .WithNative(GetNativeTlsPath());

            using var client = clientBuilder.Build();

            var request = new Request
            {
                RequestUrl = uri.ToString(),
                RequestMethod = HttpMethod.Get,
                StreamOutputPath = filePath,
                TimeoutSeconds = 30,
                Headers = new Dictionary<string, string>
                {
                    ["User-Agent"] = UserAgent,
                    ["Accept"] = "text/vtt,application/x-subrip,text/plain,*/*"
                }
            };

            var response = await client.RequestAsync(request, cancellationToken);
            if (response.Status == HttpStatusCode.OK && File.Exists(filePath) && new FileInfo(filePath).Length > 0)
            {
                LogHelper.Http.Log(LogLevel.Debug, $"[SubtitleManager] Subtitle downloaded with TLS. Url: {uri} Path: {filePath}");
                return filePath;
            }

            TryDelete(filePath);
            LogHelper.Http.Log(LogLevel.Debug, $"[SubtitleManager] TLS subtitle download failed. Url: {uri} Status: {response.Status}");
        }
        catch (Exception ex)
        {
            TryDelete(filePath);
            LogHelper.Http.Log(LogLevel.Error, $"[SubtitleManager] TLS subtitle download error. Url: {uri} Error: {ex}");
        }

        return null;
    }

    private static string CreateTempFilePath(Uri uri)
    {
        var directory = Path.Combine(Path.GetTempPath(), "Manitux", "Subtitles");
        Directory.CreateDirectory(directory);

        var extension = Path.GetExtension(uri.AbsolutePath);
        if (string.IsNullOrWhiteSpace(extension) || !SupportedExtensions.Contains(extension))
        {
            extension = ".vtt";
        }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(uri.ToString()))).ToLowerInvariant();
        return Path.Combine(directory, $"{hash[..24]}{extension.ToLowerInvariant()}");
    }

    private static string GetNativeTlsPath()
    {
        var fileName = true switch
        {
            _ when OperatingSystem.IsWindows() => "tls-client.dll",
            _ when OperatingSystem.IsLinux() => "tls-client.so",
            _ when OperatingSystem.IsMacOS() => "tls-client.dylib",
            _ => "tlsclient.so"
        };

        return OperatingSystem.IsAndroid()
            ? fileName
            : Path.Combine(Environment.CurrentDirectory, fileName);
    }

    private static void TryDelete(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
        }
    }
}
