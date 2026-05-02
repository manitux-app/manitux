using System;
using System.Net;
using System.Net.Http.Headers;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using CodeLogic.Core.Events;
using CodeLogic.Core.Logging;
using Manitux.Core.Application;
using TlsClient.Core.Builders;
using TlsClient.Core.Models.Entities;
using TlsClient.Core.Models.Requests;
using TlsClient.Native.Extensions;

namespace Manitux.Core.Helpers;

public class HttpHelper: HtmlHelper, IDisposable
{
    // private IEventBus _eventBus = CodeLogic.CodeLogic.GetEventBus();

    // public async void HttpHelperLog(LogLevel level, string message)
    // {
    //     await _eventBus.PublishAsync(new LogEvent(level, "HttpHelper", message));
    // }
    
    public async Task<string?> HttpPost(string url, Dictionary<string, string> body, string? referer = null, Dictionary<string, string>? headers = null)
    {
        if (string.IsNullOrEmpty(url)) return null;

        //HttpHelperLog(LogLevel.Info, url);

        try
        {
            Uri uri = new Uri(url);

            //var q = new Dictionary<string, string>();
            //q.Add("query", query);

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                if (referer is null)
                {
                    client.DefaultRequestHeaders.Add("Referer", url);
                }
                else
                {
                    client.DefaultRequestHeaders.Add("Referer", referer);
                }

                if (headers is null)
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/94.0.4606.81 Safari/537.36");
                    //client.DefaultRequestHeaders.Add("x-requested-with", "XMLHttpRequest");
                    client.DefaultRequestHeaders.Add("authority", uri.Authority);
                    client.DefaultRequestHeaders.Add("origin", url);
                }
                else
                {
                    foreach (var header in headers)
                    {
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }

                using (HttpResponseMessage response = await client.PostAsync(url, new FormUrlEncodedContent(body)))
                {
                    using (HttpContent content = response.Content)
                    {

                        string json = await content.ReadAsStringAsync();
                        return json;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogHelper.Http.Log(LogLevel.Error, ex.ToString());
        }

        return null;
    }

    public async Task<string?> HttpGet(string url, string? referer = null, string? proxyUrl = null, Dictionary<string, string>? headers = null, TlsClientIdentifier? identifier = null)
    {
        if (string.IsNullOrEmpty(url)) return null;

         LogHelper.Http.Log(LogLevel.Debug, $"[HttpGet] Url: {url} Referer: {referer}");

        if (identifier != null)
        {
            return await HttpGetWithTLS(url, referer, proxyUrl, headers, identifier);
        }

        try
        {
            Uri uri = new Uri(url);

            using var handler = new HttpClientHandler { UseProxy = false, ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true };

            if (proxyUrl != null)
            {
                var proxy = new WebProxy(proxyUrl)
                {
                    UseDefaultCredentials = false
                };

                handler.Proxy = proxy;
                handler.UseProxy = true;
            }

            using (handler)
            using (HttpClient client = new HttpClient(handler))
            {
                client.Timeout = TimeSpan.FromSeconds(10);

                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                if (referer is null)
                {
                    client.DefaultRequestHeaders.Add("Referer", url);
                }
                else
                {
                    client.DefaultRequestHeaders.Add("Referer", referer);
                }

                if (headers is null)
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/94.0.4606.81 Safari/537.36");
                    //client.DefaultRequestHeaders.Add("x-requested-with", "fetch");
                    client.DefaultRequestHeaders.Add("authority", uri.Authority);
                    client.DefaultRequestHeaders.Add("origin", url);
                }
                else
                {
                    foreach (var header in headers)
                    {
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }

                using (HttpResponseMessage response = await client.GetAsync(url))
                {
                    using (HttpContent content = response.Content)
                    {

                        string html = await content.ReadAsStringAsync();
                         LogHelper.Http.Log(LogLevel.Debug, $"[HttpGet] Html: {html}");
                        return html;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogHelper.Http.Log(LogLevel.Error, ex.ToString());
        }

        return null;
    }

    private async Task<string?> HttpGetWithTLS(string url, string? referer = null, string? proxyUrl = null, Dictionary<string, string>? headers = null, TlsClientIdentifier? identifier = null)
    {
        try
        {
            // NativeTlsClient.Initialize(Path.Combine(Environment.CurrentDirectory, "tls-client.so"));
            // using var client = new NativeTlsClient();
            // var res = client.Request(new Request { RequestUrl = url });
            // Console.WriteLine(res.Status);

            string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/94.0.4606.81 Safari/537.36";

            string fileName = true switch
            {
                _ when OperatingSystem.IsWindows() => "tls-client.dll",
                _ when OperatingSystem.IsLinux() => "tls-client.so",
                _ when OperatingSystem.IsMacOS() => "tls-client.dylib",
                _ => "tls-client.dll"
            };


            using var builder = new TlsClientBuilder()
             //.WithIdentifier(TlsClientIdentifier.Cloudscraper)
             .WithIdentifier(identifier ?? TlsClientIdentifier.Chrome144)
             .WithUserAgent(userAgent)
             .WithNative(Path.Combine(Environment.CurrentDirectory, fileName))
             .Build();

            var request = new Request()
            {
                RequestUrl = url,
                RequestMethod = HttpMethod.Get,
                // Headers = new Dictionary<string, string>()
                // {
                //     { "User-Agent", "TlsClient-Example" }
                // }
            };

            if (headers != null)
            {
                request.Headers = headers;
            }

            if (proxyUrl != null)
            {
                request.ProxyUrl = proxyUrl;
            }

            var response = await builder.RequestAsync(request);
             LogHelper.Http.Log(LogLevel.Debug, $"[HttpGetWithTLS] Status: {response.Status}");

            if (response.IsSuccessStatus)
            {
                 LogHelper.Http.Log(LogLevel.Debug, "[HttpGetWithTLS] " + response.Body);
                return response.Body;
            }
            else
            {
                 LogHelper.Http.Log(LogLevel.Debug, $"[HttpGetWithTLS] Status: {response.Status} Body: {response.Body}");
            }
        }
        catch (Exception ex)
        {
            LogHelper.Http.Log(LogLevel.Error, ex.ToString());
        }

        return null;
    }

    public void Dispose()
    {
        LogHelper.Http.Log(LogLevel.Debug, "HttpHelper Disposed");
    }
}
