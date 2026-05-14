using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using Manitux.Core.Helpers;
using Manitux.Core.Models;
using CodeLogic.Core.Logging;

namespace Manitux.Core.Extractors;

// https://github.com/recloudstream/cloudstream/tree/master/library/src/commonMain/kotlin/com/lagradost/cloudstream3/extractors
// https://github.com/recloudstream/extensions

public abstract class ExtractorBase : HttpHelper, IAsyncDisposable
{
    public virtual string Name { get; set; } = "Extractor";
    public virtual string MainUrl { get; set; } = "";
    public virtual List<string> SupportedDomains { get; set; } = new();

    public ILogger? Logger { get; set; }

    public bool CanHandleUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return false;

        if (!string.IsNullOrEmpty(MainUrl) && url.Contains(MainUrl))
            return true;

        var host = GetDomain(url);
        if (host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
        {
            host = host[4..];
        }

        return SupportedDomains.Any(domain =>
        {
            var supportedDomain = domain.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
                ? domain[4..]
                : domain;

            return string.Equals(supportedDomain, host, StringComparison.OrdinalIgnoreCase);
        });
    }

    protected string GetBaseUrl(string url)
    {
        var uri = new Uri(url);
        return $"{uri.Scheme}://{uri.Host}";
    }

    protected string GetDomain(string url)
    {
        var uri = new Uri(url);
        return uri.Host;
    }

    public void Log(LogLevel logLevel, string message)
    {
        if (Logger is not null)
        {
            switch (logLevel)
            {
                case LogLevel.Info:
                    Logger.Info($"{Name} {message}");
                    break;
                case LogLevel.Warning:
                    Logger.Warning($"{Name} {message}");
                    break;
                case LogLevel.Error:
                    Logger.Error($"{Name} {message}");
                    break;
                case LogLevel.Debug:
                    Logger.Debug($"{Name} {message}");
                    break;
            }
        }
        else
        {
            LogHelper.Extractor.Log(logLevel, Name, message);
        }
    }

    public abstract Task<VideoSourceModel?> ExtractAsync(VideoSourceModel videoSource, string? referer = null);

    
    public async ValueTask DisposeAsync()
    {
        // try
        // {
            
        // }catch (Exception ex)
        // {
        //     Log(LogLevel.Error, "[Extractor] " + ex.ToString());
        // }

        await Task.CompletedTask;
    }
}
