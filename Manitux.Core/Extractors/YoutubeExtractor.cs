using System;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Web;
using CodeLogic.Core.Logging;
using Manitux.Core.Extractors;
using Manitux.Core.Extractors.Utils;
using Manitux.Core.Models;
using TlsClient.Core.Models.Entities;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace Manitux.Core;

// https://github.com/Tyrrrz/YoutubeExplode/blob/prime/YoutubeExplode.Demo.Gui/ViewModels/MainViewModel.cs
public class YoutubeExtractor : ExtractorBase
{
    public override string Name => "Youtube";
    public override string MainUrl => "https://www.youtube.com";
    public override List<string> SupportedDomains => new()
    {
        "youtube.com",
        "www.youtube.com",
        "m.youtube.com",
        "music.youtube.com",
        "youtube-nocookie.com",
        "www.youtube-nocookie.com",
        "youtu.be"
    };

    public override async Task<VideoSourceModel?> ExtractAsync(VideoSourceModel videoSource, string? referer = null)
    {
        try
        {
            using var youtube = new YoutubeClient();
            var videoId = VideoId.Parse(videoSource.Url);
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoId);

            //var streamInfo = streamManifest.GetMuxedStreams().TryGetWithHighestVideoQuality();

            var streamInfo = streamManifest
                .GetVideoOnlyStreams()
                .Where(s => s.Container == Container.Mp4)
                .GetWithHighestVideoQuality();

            if (streamInfo is null)
            {
                Debug.WriteLine("Youtube streamInfo is null");
                return null;
            }

            //var fileName = $"{videoId}.{streamInfo.Container.Name}";
            //var url = streamInfo.Url;
            //Debug.WriteLine($"File: {fileName} Url: {url}");

            videoSource.Url = streamInfo.Url;

            return videoSource;
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, ex.ToString());
        }

        return null;
        
    }

}

