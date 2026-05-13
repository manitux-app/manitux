using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using CodeLogic.Core.Events;
using CodeLogic.Core.Logging;
using Manitux.Core.Application;
using TlsClient.Core.Builders;
using TlsClient.Core.Models.Entities;
using TlsClient.Core.Models.Requests;
using TlsClient.Native.Extensions;
using TlsClient.Api;
using TlsClient.Api.Extensions;

namespace Manitux.Core.Helpers;

public class HttpHelper : HtmlHelper
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

    public async Task<string?> HttpGet(string url, string? referer = null, string? proxyUrl = null, Dictionary<string, string>? headers = null, TlsClientIdentifier? identifier = null, bool useCookie = false, bool followRedirects = true, Dictionary<string, string>? cookieOutput = null)
    {
        if (string.IsNullOrEmpty(url)) return null;

         LogHelper.Http.Log(LogLevel.Debug, $"[HttpGet] Url: {url} Referer: {referer}");

        if (identifier != null)
        {
            if (!OperatingSystem.IsLinux())
            {
                return await HttpGetWithNativeTLS(url, referer, proxyUrl, headers, identifier, useCookie, followRedirects, cookieOutput);
            }
            else
            {
                return await HttpGetWithApiTLS(url, referer, proxyUrl, headers, identifier, useCookie, followRedirects, cookieOutput);
            }
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
                        //LogHelper.Http.Log(LogLevel.Debug, $"[HttpGet] Html: {html}");
                        //Debug.WriteLine(html);
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

    private async Task<string?> HttpGetWithNativeTLS(string url, string? referer = null, string? proxyUrl = null, Dictionary<string, string>? headers = null, TlsClientIdentifier? identifier = null, bool useCookie = false, bool followRedirects = false, Dictionary<string, string>? cookieOutput = null)
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
                _ => "tlsclient.so" // android test? - ok
            };

            string filePath = fileName;

            if (!OperatingSystem.IsAndroid())
            {
                filePath = Path.Combine(Environment.CurrentDirectory, fileName);
            }
           
            var clientBuilder = new TlsClientBuilder()
                //.WithIdentifier(TlsClientIdentifier.Cloudscraper)
                .WithIdentifier(identifier ?? TlsClientIdentifier.Chrome144)
                .WithUserAgent(userAgent)
                .WithFollowRedirects(followRedirects)
                .WithNative(filePath);

            if (useCookie)
            {
                clientBuilder.WithCustomCookieJar();
            }

            using var builder = clientBuilder.Build();

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

            if (useCookie && cookieOutput is not null && response.Cookies is not null)
            {
                cookieOutput.Clear();

                foreach (var cookie in response.Cookies)
                {
                    cookieOutput[cookie.Key] = cookie.Value;
                }
            }

            if (response.IsSuccessStatus)
            {
                //LogHelper.Http.Log(LogLevel.Debug, "[HttpGetWithTLS] " + response.Body);
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

    private async Task<string?> HttpGetWithApiTLS(string url, string? referer = null, string? proxyUrl = null, Dictionary<string, string>? headers = null, TlsClientIdentifier? identifier = null, bool useCookie = false, bool followRedirects = false, Dictionary<string, string>? cookieOutput = null)
    {
        try
        {
            string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/94.0.4606.81 Safari/537.36";

            var baseUri = new Uri("http://127.0.0.1:8080/");

            var clientBuilder = new TlsClientBuilder()
                .WithIdentifier(identifier ?? TlsClientIdentifier.Chrome144)
                .WithUserAgent(userAgent)
                .WithFollowRedirects(followRedirects)
                .WithInsecureSkipVerify(true)
                .WithApi(baseUri, "my-auth-key-1");

            if (useCookie)
            {
                clientBuilder.WithCustomCookieJar();
            }

            using var builder = clientBuilder.ApiBuild();

            var request = new Request()
            {
                RequestUrl = url,
                RequestMethod = HttpMethod.Get
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
            LogHelper.Http.Log(LogLevel.Debug, $"[HttpGetWithApiTLS] Status: {response.Status}");

            if (useCookie && cookieOutput is not null && response.Cookies is not null)
            {
                cookieOutput.Clear();

                foreach (var cookie in response.Cookies)
                {
                    cookieOutput[cookie.Key] = cookie.Value;
                }
            }

            if (response.IsSuccessStatus)
            {
                //LogHelper.Http.Log(LogLevel.Debug, "[HttpGetWithApiTLS] " + response.Body);
                return response.Body;
            }
            else
            {
                LogHelper.Http.Log(LogLevel.Debug, $"[HttpGetWithApiTLS] Status: {response.Status} Body: {response.Body}");
            }
        }
        catch (Exception ex)
        {
            LogHelper.Http.Log(LogLevel.Error, ex.ToString());
        }

        return null;
    }

    public override void Dispose()
    {
        base.Dispose();
        LogHelper.Http.Log(LogLevel.Debug, "HttpHelper Disposed");
    }
}
