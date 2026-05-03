using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Manitux.Core.Models
{
    
    /// <summary>
    /// Page results model
    /// </summary>
    public class PageItemModel
    {
        //public required string PluginName { get; set; }
        public required string Title { get; set; }
        public required string Url { get; set; }
        public string? CategoryName { get; set; }
        public string? Poster { get; set; }
        public string? Rating { get; set; }
        public string? Year { get; set; }
        public IpTvChannelModel? IpTvChannel { get; set; }
    }

    /// <summary>
    /// Category model for movies, tv series, iptv etc
    /// </summary>
    public class CategoryModel
    {
        public required string Title { get; set; }
        public required string Url { get; set; }
        public string? Poster { get; set; }
        public List<IpTvChannelModel>? IpTvChannels { get; set; }
    }


    /// <summary>
    /// Media info model for movies,tv series, iptv etc
    /// </summary>
    public class MediaInfoModel
    {
        public string? ImdbId { get; set; }
        public required string Title { get; set; }
        public required string Url { get; set; }
        public string? Description { get; set; }
        public string? Poster { get; set; }
        public string? Backdrop { get; set; }
        public string? Trailer { get; set; }
        public string? Tags { get; set; }
        public string? Rating { get; set; }
        public string? Year { get; set; }
        public string? Duration { get; set; }
        public string? Actors { get; set; }
        public string? Country { get; set; }
        public List<VideoSourceModel>? VideoSources { get; set; }
        public List<EpisodeModel>? Episodes { get; set; }
        //public List<SeasonModel>? Seasons { get; set; }
        public List<RelatedVideoModel>? RelatedVideos { get; set; }
        public List<CommentModel>? Comments { get; set; }

        public void SetTags(object value)
        {
            Tags = ConvertLists(value);
        }

        public void SetActors(object value)
        {
            Actors = ConvertLists(value);
        }

        public void SetRating(object value)
        {
            Rating = EnsureString(value);
        }

        public void SetYear(object value)
        {
            Year = EnsureString(value);
        }

        private string? ConvertLists(object value)
        {
            if (value is List<string> list)
            {
                return string.Join(", ", list);
            }

            return value as string;
        }

        private string? EnsureString(object value)
        {
            return value?.ToString();
        }
    }


    /// <summary>
    /// Season model
    /// </summary>
    public class SeasonModel
    {
        public int SeasonNumber { get; set; }
        public required string Title { get; set; }
        public List<EpisodeModel>? Episodes { get; set; }
    }

    /// <summary>
    /// Episode info model
    /// </summary>
    public class EpisodeModel
    {
        public int SeasonNumber { get; set; }
        public int EpisodeNumber { get; set; }
        public required string Url { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Trailer { get; set; }
     }


    /// <summary>
    /// Related video model for movies, tv series
    /// </summary>
    public class RelatedVideoModel
    {
        public required string Title { get; set; }
        public required string Url { get; set; }
        public string? Poster { get; set; }
    }

    /// <summary>
    /// User comment model
    /// </summary>
    public class CommentModel
    {
        public string? Name { get; set; }
        public required string Comment { get; set; }
        public string? Date { get; set; }
    }

    public class IpTvChannelModel
    {
        public string? TvgId { get; set; } = null;

        public string? TvgName { get; set; } = null;

        public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        public int? TvgChno { get; set; } = null;

        public string? TvgLogo { get; set; } = null;

        public bool Radio { get; set; } = false;

        public int? TvgShift { get; set; } = null;

        public string? Catchup { get; set; } = null;

        public string? CatchupSource { get; set; } = null;

        public int? CatchupDays { get; set; } = null;

        public int? CatchupCorrection { get; set; } = null;

        public string ChannelName { get; set; } = String.Empty;

        public string URL { get; set; } = String.Empty;

        public string? Country { get; set; }
    }

    /// <summary>
    /// Video link model
    /// </summary>
    public class VideoLinkModel
    {
        public string? Poster { get; set; }
        public List<VideoSourceModel>? VideoSources { get; set; }
    }

    /// <summary>
    /// Extracted video source model
    /// </summary>
    public class VideoSourceModel
    {
        public required string Name { get; set; }
        public required string Url { get; set; }
        public string? Referer { get; set; }
        public List<HeaderModel>? Headers { get; set; }
        public List<SubtitleModel>? Subtitles { get; set; }
    }

    /// <summary>
    /// Subtitle model
    /// </summary>
    public class SubtitleModel
    {
        public required string Name { get; set; }
        public required string Url { get; set; }
    }

    /// <summary>
    /// Http header model
    /// </summary>
    public class HeaderModel
    {
        public required string Name { get; set; }
        public required string Value { get; set; }
    }

    /// <summary>
    /// Generic item model
    /// </summary>
    public class ItemModel
    {
        public required string Item { get; set; }
    }

    /// <summary>
    /// Generic title url model
    /// </summary>
    public class TitleUrlModel
    {
        public required string Title { get; set; }
        public required string Url { get; set; }
    }
}