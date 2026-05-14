using System.Text.RegularExpressions;

namespace Manitux.Core.Extractors.Utils;

public static class HlsPlaylistParser
{
    private const string TagPrefix = "#EXT";
    private const string TagStreamInf = "#EXT-X-STREAM-INF";
    private const string TagIFrameStreamInf = "#EXT-X-I-FRAME-STREAM-INF";
    private const string TagMedia = "#EXT-X-MEDIA";
    private const string TagDefine = "#EXT-X-DEFINE";
    private const string TagIndependentSegments = "#EXT-X-INDEPENDENT-SEGMENTS";
    private const string TypeAudio = "AUDIO";
    private const string TypeVideo = "VIDEO";
    private const string TypeSubtitles = "SUBTITLES";
    private const string TypeClosedCaptions = "CLOSED-CAPTIONS";

    private static readonly Regex AttributeRegex = new(@"(?<key>[A-Z0-9-]+)=(?:""(?<quoted>[^""]*)""|(?<plain>[^,]*))", RegexOptions.IgnoreCase);
    private static readonly Regex VariableReferenceRegex = new(@"\{\$(?<name>[a-zA-Z0-9\-_]+)\}", RegexOptions.IgnoreCase);

    public static HlsMultivariantPlaylist? Parse(string baseUri, string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var lines = text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n')
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        return lines.Any(x => x.StartsWith(TagStreamInf, StringComparison.OrdinalIgnoreCase)
                              || x.StartsWith(TagIFrameStreamInf, StringComparison.OrdinalIgnoreCase))
            ? ParseMultivariantPlaylist(lines, baseUri)
            : null;
    }

    private static HlsMultivariantPlaylist ParseMultivariantPlaylist(List<string> lines, string baseUri)
    {
        var variableDefinitions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var variants = new List<Variant>();
        var mediaTags = new List<string>();
        var tags = new List<string>();
        var videos = new List<Rendition>();
        var audios = new List<Rendition>();
        var subtitles = new List<Rendition>();
        var closedCaptions = new List<Rendition>();
        var hasIndependentSegments = false;
        Format? muxedAudioFormat = null;
        var muxedCaptionFormats = new List<Format>();

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (line.StartsWith(TagPrefix, StringComparison.OrdinalIgnoreCase))
            {
                tags.Add(line);
            }

            if (line.StartsWith(TagDefine, StringComparison.OrdinalIgnoreCase))
            {
                var defineAttrs = ParseAttributes(line);
                if (defineAttrs.TryGetValue("NAME", out var name) && defineAttrs.TryGetValue("VALUE", out var value))
                {
                    variableDefinitions[name] = value;
                }

                continue;
            }

            if (line.Equals(TagIndependentSegments, StringComparison.OrdinalIgnoreCase))
            {
                hasIndependentSegments = true;
                continue;
            }

            if (line.StartsWith(TagMedia + ":", StringComparison.OrdinalIgnoreCase))
            {
                mediaTags.Add(line);
                continue;
            }

            var isIFrameOnlyVariant = line.StartsWith(TagIFrameStreamInf, StringComparison.OrdinalIgnoreCase);
            if (!line.StartsWith(TagStreamInf, StringComparison.OrdinalIgnoreCase) && !isIFrameOnlyVariant)
            {
                continue;
            }

            var attrs = ParseAttributes(ReplaceVariableReferences(line, variableDefinitions));
            var uriValue = isIFrameOnlyVariant
                ? attrs.GetValueOrDefault("URI")
                : GetNextUriLine(lines, ref i, variableDefinitions);

            if (string.IsNullOrWhiteSpace(uriValue))
            {
                continue;
            }

            var (width, height) = ParseResolution(attrs.GetValueOrDefault("RESOLUTION"));
            var codecs = attrs.GetValueOrDefault("CODECS");
            var roleFlags = isIFrameOnlyVariant ? C.RoleFlagTrickPlay : 0;
            var variant = new Variant(
                Url: ResolveUrl(baseUri, uriValue),
                Format: new Format(
                    Id: variants.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ContainerMimeType: MimeTypes.ApplicationM3u8,
                    Codecs: codecs,
                    AverageBitrate: ParseInt(attrs.GetValueOrDefault("AVERAGE-BANDWIDTH"), Format.NoValue),
                    PeakBitrate: ParseInt(attrs.GetValueOrDefault("BANDWIDTH"), Format.NoValue),
                    Width: width,
                    Height: height,
                    FrameRate: ParseFloat(attrs.GetValueOrDefault("FRAME-RATE"), Format.NoValue),
                    RoleFlags: roleFlags,
                    VideoRange: attrs.GetValueOrDefault("VIDEO-RANGE")),
                VideoGroupId: attrs.GetValueOrDefault("VIDEO"),
                AudioGroupId: attrs.GetValueOrDefault("AUDIO"),
                SubtitleGroupId: attrs.GetValueOrDefault("SUBTITLES"),
                CaptionGroupId: attrs.GetValueOrDefault("CLOSED-CAPTIONS"));

            if (!variants.Any(x => string.Equals(x.Url, variant.Url, StringComparison.OrdinalIgnoreCase)))
            {
                variants.Add(variant);
            }
        }

        foreach (var mediaTag in mediaTags)
        {
            ParseMediaTag(
                mediaTag,
                baseUri,
                variableDefinitions,
                variants,
                videos,
                audios,
                subtitles,
                closedCaptions,
                ref muxedAudioFormat,
                muxedCaptionFormats);
        }

        return new HlsMultivariantPlaylist(
            BaseUri: baseUri,
            Tags: tags,
            Variants: variants,
            Videos: videos,
            Audios: audios,
            Subtitles: subtitles,
            ClosedCaptions: closedCaptions,
            MuxedAudioFormat: muxedAudioFormat,
            MuxedCaptionFormats: muxedCaptionFormats,
            HasIndependentSegments: hasIndependentSegments,
            VariableDefinitions: variableDefinitions);
    }

    private static void ParseMediaTag(
        string line,
        string baseUri,
        Dictionary<string, string> variableDefinitions,
        List<Variant> variants,
        List<Rendition> videos,
        List<Rendition> audios,
        List<Rendition> subtitles,
        List<Rendition> closedCaptions,
        ref Format? muxedAudioFormat,
        List<Format> muxedCaptionFormats)
    {
        var attrs = ParseAttributes(ReplaceVariableReferences(line, variableDefinitions));
        var type = attrs.GetValueOrDefault("TYPE");
        var groupId = attrs.GetValueOrDefault("GROUP-ID") ?? string.Empty;
        var name = attrs.GetValueOrDefault("NAME") ?? groupId;
        var uri = attrs.GetValueOrDefault("URI");

        var format = new Format(
            Id: $"{groupId}:{name}",
            Label: name,
            Language: attrs.GetValueOrDefault("LANGUAGE"),
            ContainerMimeType: MimeTypes.ApplicationM3u8,
            RoleFlags: ParseRoleFlags(attrs),
            SelectionFlags: ParseSelectionFlags(attrs));

        switch (type)
        {
            case TypeVideo:
                var videoVariant = variants.FirstOrDefault(x => string.Equals(x.VideoGroupId, groupId, StringComparison.OrdinalIgnoreCase));
                if (videoVariant is not null)
                {
                    format = format with
                    {
                        Width = videoVariant.Format.Width,
                        Height = videoVariant.Format.Height,
                        FrameRate = videoVariant.Format.FrameRate,
                        Codecs = MimeTypes.GetCodecsOfType(videoVariant.Format.Codecs, C.TrackTypeVideo)
                    };
                }

                if (!string.IsNullOrWhiteSpace(uri))
                {
                    videos.Add(new Rendition(ResolveUrl(baseUri, uri), format, groupId, name));
                }
                break;

            case TypeAudio:
                var audioVariant = variants.FirstOrDefault(x => string.Equals(x.AudioGroupId, groupId, StringComparison.OrdinalIgnoreCase));
                if (audioVariant is not null)
                {
                    var codecs = MimeTypes.GetCodecsOfType(audioVariant.Format.Codecs, C.TrackTypeAudio);
                    format = format with
                    {
                        Codecs = codecs,
                        SampleMimeType = MimeTypes.GetMediaMimeType(codecs)
                    };
                }

                if (attrs.TryGetValue("CHANNELS", out var channels))
                {
                    format = format with { ChannelCount = ParseInt(channels.Split('/')[0], Format.NoValue) };
                }

                if (!string.IsNullOrWhiteSpace(uri))
                {
                    audios.Add(new Rendition(ResolveUrl(baseUri, uri), format, groupId, name));
                }
                else if (audioVariant is not null)
                {
                    muxedAudioFormat = format;
                }
                break;

            case TypeSubtitles:
                var subtitleVariant = variants.FirstOrDefault(x => string.Equals(x.SubtitleGroupId, groupId, StringComparison.OrdinalIgnoreCase));
                if (subtitleVariant is not null)
                {
                    format = format with { Codecs = MimeTypes.GetCodecsOfType(subtitleVariant.Format.Codecs, C.TrackTypeText) };
                }

                if (!string.IsNullOrWhiteSpace(uri))
                {
                    subtitles.Add(new Rendition(ResolveUrl(baseUri, uri), format with
                    {
                        SampleMimeType = format.SampleMimeType ?? MimeTypes.TextVtt
                    }, groupId, name));
                }
                break;

            case TypeClosedCaptions:
                var instreamId = attrs.GetValueOrDefault("INSTREAM-ID") ?? string.Empty;
                var sampleMimeType = instreamId.StartsWith("SERVICE", StringComparison.OrdinalIgnoreCase)
                    ? MimeTypes.ApplicationCea708
                    : MimeTypes.ApplicationCea608;
                var accessibilityChannel = ParseInt(Regex.Match(instreamId, @"\d+").Value, Format.NoValue);
                var captionFormat = format with
                {
                    SampleMimeType = sampleMimeType,
                    AccessibilityChannel = accessibilityChannel
                };

                muxedCaptionFormats.Add(captionFormat);
                closedCaptions.Add(new Rendition(null, captionFormat, groupId, name));
                break;
        }
    }

    private static string? GetNextUriLine(List<string> lines, ref int index, Dictionary<string, string> variableDefinitions)
    {
        while (index + 1 < lines.Count)
        {
            index++;
            var next = lines[index];
            if (next.StartsWith("#", StringComparison.Ordinal)) continue;
            return ReplaceVariableReferences(next, variableDefinitions);
        }

        return null;
    }

    private static Dictionary<string, string> ParseAttributes(string value)
    {
        return AttributeRegex
            .Matches(value)
            .ToDictionary(
                x => x.Groups["key"].Value.ToUpperInvariant(),
                x => x.Groups["quoted"].Success ? x.Groups["quoted"].Value : x.Groups["plain"].Value,
                StringComparer.OrdinalIgnoreCase);
    }

    private static string ReplaceVariableReferences(string value, Dictionary<string, string> variableDefinitions)
    {
        return VariableReferenceRegex.Replace(value, match =>
            variableDefinitions.TryGetValue(match.Groups["name"].Value, out var replacement)
                ? replacement
                : match.Value);
    }

    private static (int Width, int Height) ParseResolution(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return (Format.NoValue, Format.NoValue);

        var parts = value.Split('x', 'X');
        return parts.Length == 2
               && int.TryParse(parts[0], out var width)
               && int.TryParse(parts[1], out var height)
               && width > 0
               && height > 0
            ? (width, height)
            : (Format.NoValue, Format.NoValue);
    }

    private static int ParseSelectionFlags(Dictionary<string, string> attrs)
    {
        var flags = 0;
        if (IsYes(attrs.GetValueOrDefault("DEFAULT"))) flags |= C.SelectionFlagDefault;
        if (IsYes(attrs.GetValueOrDefault("FORCED"))) flags |= C.SelectionFlagForced;
        if (IsYes(attrs.GetValueOrDefault("AUTOSELECT"))) flags |= C.SelectionFlagAutoselect;
        return flags;
    }

    private static int ParseRoleFlags(Dictionary<string, string> attrs)
    {
        var characteristics = attrs.GetValueOrDefault("CHARACTERISTICS");
        if (string.IsNullOrWhiteSpace(characteristics)) return 0;

        var flags = 0;
        if (characteristics.Contains("public.accessibility.describes-video", StringComparison.OrdinalIgnoreCase)) flags |= C.RoleFlagDescribesVideo;
        if (characteristics.Contains("public.accessibility.transcribes-spoken-dialog", StringComparison.OrdinalIgnoreCase)) flags |= C.RoleFlagTranscribesDialog;
        if (characteristics.Contains("public.easy-to-read", StringComparison.OrdinalIgnoreCase)) flags |= C.RoleFlagEasyToRead;
        return flags;
    }

    private static bool IsYes(string? value)
    {
        return string.Equals(value, "YES", StringComparison.OrdinalIgnoreCase);
    }

    private static int ParseInt(string? value, int defaultValue)
    {
        return int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : defaultValue;
    }

    private static float ParseFloat(string? value, float defaultValue)
    {
        return float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : defaultValue;
    }

    private static string ResolveUrl(string baseUri, string referenceUri)
    {
        if (Uri.TryCreate(referenceUri, UriKind.Absolute, out var absolute)) return absolute.ToString();
        if (referenceUri.StartsWith("//", StringComparison.Ordinal)) return "https:" + referenceUri;
        return Uri.TryCreate(baseUri, UriKind.Absolute, out var baseUrl)
               && Uri.TryCreate(baseUrl, referenceUri, out var resolved)
            ? resolved.ToString()
            : $"{baseUri.TrimEnd('/')}/{referenceUri.TrimStart('/')}";
    }

    public static class C
    {
        public const int TrackTypeVideo = 2;
        public const int TrackTypeAudio = 1;
        public const int TrackTypeText = 3;
        public const int RoleFlagTrickPlay = 1 << 14;
        public const int RoleFlagDescribesVideo = 1 << 8;
        public const int RoleFlagTranscribesDialog = 1 << 9;
        public const int RoleFlagEasyToRead = 1 << 13;
        public const int SelectionFlagDefault = 1;
        public const int SelectionFlagForced = 1 << 1;
        public const int SelectionFlagAutoselect = 1 << 2;
    }

    public static class MimeTypes
    {
        public const string ApplicationM3u8 = "application/x-mpegURL";
        public const string ApplicationCea608 = "application/cea-608";
        public const string ApplicationCea708 = "application/cea-708";
        public const string TextVtt = "text/vtt";
        public const string AudioEac3 = "audio/eac3";
        public const string AudioEac3Joc = "audio/eac3-joc";
        public const string CodecEac3Joc = "ec+3";

        public static string? GetCodecsOfType(string? codecs, int trackType)
        {
            if (string.IsNullOrWhiteSpace(codecs)) return null;

            var selected = codecs
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Where(codec => GetTrackTypeOfCodec(codec) == trackType)
                .ToList();

            return selected.Count == 0 ? null : string.Join(",", selected);
        }

        public static string? GetMediaMimeType(string? codecs)
        {
            if (string.IsNullOrWhiteSpace(codecs)) return null;

            var codec = codecs.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (codec is null) return null;

            if (codec.StartsWith("mp4a", StringComparison.OrdinalIgnoreCase)) return "audio/mp4a-latm";
            if (codec.StartsWith("ac-3", StringComparison.OrdinalIgnoreCase)) return "audio/ac3";
            if (codec.StartsWith("ec-3", StringComparison.OrdinalIgnoreCase)) return AudioEac3;
            if (codec.StartsWith("avc", StringComparison.OrdinalIgnoreCase)) return "video/avc";
            if (codec.StartsWith("hvc", StringComparison.OrdinalIgnoreCase) || codec.StartsWith("hev", StringComparison.OrdinalIgnoreCase)) return "video/hevc";
            if (codec.StartsWith("vp9", StringComparison.OrdinalIgnoreCase)) return "video/x-vnd.on2.vp9";
            if (codec.StartsWith("av01", StringComparison.OrdinalIgnoreCase)) return "video/av01";
            if (codec.StartsWith("stpp", StringComparison.OrdinalIgnoreCase)) return "application/ttml+xml";
            if (codec.StartsWith("wvtt", StringComparison.OrdinalIgnoreCase)) return TextVtt;
            return null;
        }

        public static int GetTrackTypeOfCodec(string codec)
        {
            if (codec.StartsWith("avc", StringComparison.OrdinalIgnoreCase)
                || codec.StartsWith("hvc", StringComparison.OrdinalIgnoreCase)
                || codec.StartsWith("hev", StringComparison.OrdinalIgnoreCase)
                || codec.StartsWith("vp", StringComparison.OrdinalIgnoreCase)
                || codec.StartsWith("av01", StringComparison.OrdinalIgnoreCase)
                || codec.StartsWith("dvh", StringComparison.OrdinalIgnoreCase)
                || codec.StartsWith("dvhe", StringComparison.OrdinalIgnoreCase))
            {
                return C.TrackTypeVideo;
            }

            if (codec.StartsWith("mp4a", StringComparison.OrdinalIgnoreCase)
                || codec.StartsWith("ac-3", StringComparison.OrdinalIgnoreCase)
                || codec.StartsWith("ec-3", StringComparison.OrdinalIgnoreCase)
                || codec.StartsWith("opus", StringComparison.OrdinalIgnoreCase))
            {
                return C.TrackTypeAudio;
            }

            if (codec.StartsWith("stpp", StringComparison.OrdinalIgnoreCase)
                || codec.StartsWith("wvtt", StringComparison.OrdinalIgnoreCase))
            {
                return C.TrackTypeText;
            }

            return 0;
        }
    }
}

public sealed record HlsMultivariantPlaylist(
    string BaseUri,
    List<string> Tags,
    List<Variant> Variants,
    List<Rendition> Videos,
    List<Rendition> Audios,
    List<Rendition> Subtitles,
    List<Rendition> ClosedCaptions,
    Format? MuxedAudioFormat,
    List<Format> MuxedCaptionFormats,
    bool HasIndependentSegments,
    Dictionary<string, string> VariableDefinitions);

public sealed record Variant(
    string Url,
    Format Format,
    string? VideoGroupId,
    string? AudioGroupId,
    string? SubtitleGroupId,
    string? CaptionGroupId)
{
    public bool IsTrickPlay()
    {
        return (Format.RoleFlags & HlsPlaylistParser.C.RoleFlagTrickPlay) != 0
               || (Format.VideoRange?.Contains("trick", StringComparison.OrdinalIgnoreCase) == true)
               || (Format.Codecs?.Contains("jpeg", StringComparison.OrdinalIgnoreCase) == true);
    }

    public bool ContainsAudio()
    {
        return string.IsNullOrWhiteSpace(AudioGroupId)
               && HlsPlaylistParser.MimeTypes.GetCodecsOfType(Format.Codecs, HlsPlaylistParser.C.TrackTypeAudio) is not null;
    }

    public bool IsPlayableStandalone(HlsMultivariantPlaylist playlist)
    {
        if (IsTrickPlay()) return false;
        if (string.IsNullOrWhiteSpace(AudioGroupId)) return true;

        return playlist.Audios.Any(x =>
            string.Equals(x.GroupId, AudioGroupId, StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(x.Url));
    }
}

public sealed record Rendition(
    string? Url,
    Format Format,
    string GroupId,
    string Name);

public sealed record Format(
    string? Id = null,
    string? Label = null,
    string? Language = null,
    string? ContainerMimeType = null,
    string? SampleMimeType = null,
    string? Codecs = null,
    int AverageBitrate = Format.NoValue,
    int PeakBitrate = Format.NoValue,
    int Width = Format.NoValue,
    int Height = Format.NoValue,
    float FrameRate = Format.NoValue,
    int RoleFlags = 0,
    int SelectionFlags = 0,
    int ChannelCount = Format.NoValue,
    int AccessibilityChannel = Format.NoValue,
    string? VideoRange = null)
{
    public const int NoValue = -1;
}
