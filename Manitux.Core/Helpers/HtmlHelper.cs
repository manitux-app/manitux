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

public class HtmlHelper: IDisposable
{
    public virtual async Task<IHtmlDocument?> HtmlParse(string html)
    {
        if (string.IsNullOrEmpty(html))
        {
            LogHelper.Html.Log(LogLevel.Warning, "[HtmlParse] html is null!");
            return null;
        }

        LogHelper.Html.Log(LogLevel.Debug, $"[HtmlParse] Html: {html}");

        try
        {
            var parser = new HtmlParser();
            var document = await parser.ParseDocumentAsync(html);
            return document;
        }
        catch (Exception ex)
        {
            LogHelper.Html.Log(LogLevel.Error, ex.ToString());
        }

        return null;
    }

    public virtual string FixUrl(string url, string mainUrl)
    {
        if (string.IsNullOrEmpty(url)) return "";

        if (url.StartsWith("http") || url.StartsWith("{\""))
            return url.Replace("\\", "");

        if (url.StartsWith("//"))
            return $"https:{url}".Replace("\\", "");

        if (!url.StartsWith("http"))
        {
            var baseUri = new Uri(mainUrl);
            var combinedUri = new Uri(baseUri, url);
            return combinedUri.ToString().Replace("\\", "");
        }

        LogHelper.Html.Log(LogLevel.Debug, $"[FixUrl] Url: {url}");

        return url.Replace("\\", "");
    }
    
    public void Dispose()
    {
        LogHelper.Html.Log(LogLevel.Debug, "HtmlHelper Disposed");
    }
}
