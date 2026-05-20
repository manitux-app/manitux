using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Manitux.Core.Helpers;
using Manitux.Core.Models;

namespace Manitux.Seyirturk;

internal sealed class SeyirturkParser : HttpHelper
{
    private const string Ua = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36";
    private const string VidName = "seyirTURK";

    private readonly Dictionary<string, Func<string, Task<ParserResult?>>> _options;

    public SeyirturkParser()
    {
        _options = new(StringComparer.OrdinalIgnoreCase)
        {
            ["sinemaizle"] = mavifilm,
            ["#mavifilm3"] = filmizlesene,
            ["#mavifilm"] = mavifilm,
            ["filese.me"] = filese,
            ["diziwatch"] = diziwatch,
            ["filmmax"] = filmmax,
            ["//four"] = contentx,
            ["contentx.me"] = contentx,
            ["pichive"] = contentx,
            ["hotlinger"] = contentx,
            ["fullhdfilmizlesene"] = fullhdfilmizlesene,
            ["fullhdfilm"] = fullhdfilm,
            ["hdfilmizle"] = hdfilmizle,
            ["playru.net"] = contentx,
            ["ok.ru/videoembed"] = okru,
            ["odnoklassniki.ru"] = okru,
            ["canlidizi"] = canlidizi,
            ["diziplus"] = diziplus,
            ["vidmoly"] = vidmoly,
            ["flmplayer"] = vidmoly,
            ["closeload"] = closeload,
            ["diziyou"] = diziyou,
            ["dizifin"] = dizifin,
            ["canlitvulusal"] = canlitvulusal,
            ["diziberlin"] = diziplus,
            ["watchomovies"] = watchomovies,
            ["streamoupload"] = streamoupload,
            ["xhamster"] = xhamster,
            ["xnxx"] = xnxx,
            ["cnbce"] = cnbce,
            ["vidmody"] = vidmody,
            ["filmmakinesi"] = filmmakinesi,
            ["canlitvlive"] = canlitvlive,
            ["uptostream"] = uptostream,
            ["youtube"] = youtube,
            ["filmmodu"] = filmmodu,
            ["mail.ru"] = mailru,
            ["streamimdb"] = streamimdb,
            ["fembed"] = fembed,
            ["feurl"] = fembed,
            ["femax"] = fembed,
            ["fplay"] = fembed,
            ["dutrag"] = fembed,
            ["vanfem"] = fembed,
            ["imdb"] = imdb,
            ["s1cdn"] = s1cdn,
            ["showtv.com"] = show,
            ["showturk.com.tr"] = show,
            ["showmax.com.tr"] = show,
            ["yjco.xyz"] = yjco,
            ["nowtv.com.tr"] = nowtv,
            ["startv.com"] = startv,
            ["ahaber.com.tr"] = ahaber,
            ["apara"] = ahaber,
            ["anews.com.tr"] = ahaber,
            ["wikiflix"] = wikiflix,
            ["filmizlemek"] = filmizlemek,
            ["aspor.com.tr"] = ahaber,
            ["a2tv"] = ahaber,
            ["atv.com.tr/canli-yayin"] = atvcanli,
            ["atv.com.tr"] = atv,
            ["atv.json"] = atv,
            ["dailymotion"] = dailymotion,
            ["fileru"] = fileru,
            ["kanald.com"] = kanald,
            ["kanald.json"] = kanald,
            ["dizibox"] = dizibox,
            ["dizilab"] = dizilab,
            ["chefkoch24.eu"] = chefkoch24,
            ["haberturk.com"] = haberturk,
            ["bloomberght.com"] = haberturk,
            ["dha.com"] = dha,
            ["tlctv.com"] = tlc,
            ["dmax.com"] = tlc,
            ["milyontv.com"] = milyontv,
            ["dizitime"] = dizitime,
            ["videobin.co"] = videobin,
            ["gounlimited.to"] = gounlimited,
            ["saruch.co"] = saruch,
            ["yabancidizi"] = yabancidizi,
            ["streamruby"] = streamruby,
            ["streamtheworld.com"] = streamtheworld,
            ["chaturbate.com"] = chaturbate,
            ["volo.com"] = volotv,
            ["cnnturk.com"] = cnnteve2,
            ["4kfilmizle"] = k4filmizle,
            ["canlitv.ws"] = canlitvws,
            ["teve2.com.tr/canli-yayin"] = cnnteve2,
            ["tv2.com.tr/diziler"] = teve2,
            ["kanal7.com/dizi"] = kanal7dizi,
            ["kanal7.com/ozel-haber"] = kanal7dizi,
            ["kanal7.com/canli-yayin"] = kanal7,
            ["halktv.com.tr"] = halktv,
            ["tv100.com"] = halktv,
            ["kralmuzik.com.tr"] = kralmuzik,
            ["numberone.com.tr"] = numberone,
            ["filmatek"] = filmatek,
            ["dizipal"] = dizipal,
            ["tv360.com.tr"] = tv360,
            ["m.star.com.tr/video/canli.asp"] = tv360,
            ["ucankus.com"] = ucankus,
            ["dreamturk.com.tr"] = dreamturk,
            ["trt1"] = trtparser,
            ["setfilmizle"] = setfilmizle,
            ["tvem.com.tr"] = tvem,
            ["ulusal.com.tr"] = ulusal,
            ["kanalb.com.tr"] = kanalb,
            ["ekoturk.com"] = ekoturk,
            ["dood"] = dood,
            ["vidply"] = dood,
            ["vk.com"] = vkcom,
            ["pokitv"] = pokitv,
            ["womantv.com.tr"] = womantv,
            ["tjk.org"] = tjk,
            ["yabantv.com"] = yaban,
            ["koytv.tv"] = yaban,
            ["canliradyolar.org"] = canliradyolar,
            ["ashemaletube.com"] = ashemale,
            ["oneupload"] = oneupload,
            ["pornhub.com"] = pornhub,
            ["clipwatching.com"] = mixdrop,
            ["xvideos.com"] = xvideos,
            ["puhutv.com"] = puh,
            ["mixdrop"] = mixdrop,
            ["streamhub"] = mixdrop,
            ["yoltv"] = yoltv,
            ["vudeo"] = vudeo,
            ["radyohome.com"] = radyohome,
            ["onlineradiobox.com"] = onlineradiobox,
            ["tv8.com.tr"] = tv8,
            ["streamtape.com"] = streamtape,
            ["7dak"] = dak7,
            ["filmcidayi"] = filmcidayi,
            ["dizilla"] = dizilla_last,
            ["dizipub"] = dizilla_last,
            ["dizigom"] = dizigom,
            ["voe."] = voe,
            ["brookethoughi"] = voe,
            ["ntv.com.tr"] = startv,
            ["koreanturk"] = koreanturk,
            ["unutulmazfilmler"] = dizilla_last,
            ["canlitv"] = canlitvcenter,
            ["luluvdo"] = mixdrop,
            ["figeterpiazine"] = voe,
            ["maxfinishseveral"] = voe,
            ["hdabla"] = hdabla,
            ["dizist"] = dizilla_last,
            ["ugurfilm"] = ugurfilm,
            ["hdfilmcehennemisyrtrk"] = hdfilmcehennemisyrtrk,
            ["hdfilmcehennemi"] = hdfilmcehennemi,
            ["vidlop"] = vidlop,
            ["streamplayer"] = streamplayer,
            ["webteizle"] = webteizle,
            ["yilmaztv.com"] = yilmaztv,
            ["k2s.cc"] = k2s,
            ["vcdn.io"] = vcdn,
            ["hdmom"] = hdmom,
            ["govids"] = govids,
            ["sibnet"] = sibnet,
            ["vectorx"] = vectorx,
            ["#sinefil"] = dizilla_last,
            ["cloudvideo"] = cloudvideo,
            ["jetfilmizle"] = jetfilmizle,
            ["plus4.asp"] = plus4,
            ["sezonlukdizi"] = sezonlukdizi,
            ["setplayyyy"] = govids,
            ["upstream.to"] = upstream,
            ["streamsb"] = sbembed,
            ["filemoon"] = upstream,
            ["vtube"] = upstream,
            ["videoseyred"] = videoseyred,
            ["tele1"] = tele1,
            ["ulketv"] = ulketv,
            ["tvnet"] = tvnet,
            ["sinefy"] = sinefy,
            ["freeomovie"] = pandamovie_freeomovie,
            ["pandamovie"] = pandamovie_freeomovie,
            ["diziroll"] = dizilla_last,
            ["fullfilmizlede"] = filmizlesene,
            ["streamz"] = streamz,
            ["diziyo"] = diziyo,
            ["yabanci-dizi"] = dizilla_last,
            ["meteorolojitv"] = meteor,
            ["onlinedizi"] = onlinedizi,
            ["sbembed"] = sbembed,
            ["liderfilm"] = liderfilm,
            ["dizirella"] = dizirella,
            ["hdtoday"] = hdtoday,
            ["streamlare"] = streamlare,
            ["slwatch"] = streamlare,
            ["sinemafilmizle"] = sinemafilmizle,
            ["filmon"] = filmon,
            ["dizibal"] = dizibal,
            ["dizimom"] = dizimom,
            ["suhiaza"] = fembed,
            ["beyaztv"] = beyaztv,
            ["yirmidort.tv"] = yirmidort,
            ["siyahfilmizle"] = siyahfilmizle,
            ["filmekseni"] = filmekseni,
            ["watch-free"] = hdtoday,
            ["radyodelisi"] = radyodelisi,
            ["vidoza.net/embed"] = vidoza,
            ["videzz.net"] = vidoza,
            ["https://s.to"] = sto,
            ["aniworld"] = sto,
            ["filelions"] = mixdrop,
            ["dooood.com"] = dood,
            ["streamwish"] = streamwish,
            ["cinemathek"] = cinemathek,
            ["supervideo"] = mixdrop,
            ["movie4k"] = movie4k,
            ["youporn"] = youporn,
            ["sobreatsesuyp"] = trstx,
            ["goodstream"] = goodstream,
            ["dropload"] = mixdrop,
            ["vimeo"] = vimeo,
            ["trstx"] = trstx,
            ["teleontv.at"] = teleontv,
            ["lookmovie2"] = lookmovie2,
            ["sozcu"] = sozcu,
            ["roketdizi"] = dizilla_last,
            ["themoviearchive"] = themoviearchive,
            ["thehun.net"] = thehun,
            ["istanbuluseyret"] = istanbuluseyret,
            ["dizimia"] = dizilla_last,
            ["sinema"] = sinemacx,
            ["vumoo"] = hdtoday,
            ["149.255.152.218/channels"] = myvideoaz,
            ["parsatv"] = parsatv,
            ["ddizi"] = ddizi,
            ["allclassic"] = allclassic,
            ["dizipod"] = dizipod,
            ["www.pornpics"] = pornpics
        };
    }

    public async Task<VideoSourceModel?> ResolveAsync(VideoSourceModel source)
    {
        var result = await parse(source.Url);
        if (result is null)
        {
            return null;
        }

        source.Url = result.Url;
        source.Referer = result.Referer ?? source.Referer;
        source.Headers = BuildHeaders(result.Referer ?? source.Referer, result.Headers);
        source.Subtitles = result.Subtitles
            .Select((url, index) => new SubtitleModel { Id = (index + 1).ToString(), Name = $"Subtitle {index + 1}", Url = url })
            .ToList();

        if (source.Subtitles.Count == 0)
        {
            source.Subtitles = null;
        }

        return source;
    }

    public VideoSourceModel CreateSource(string name, string url, string? referer = null)
    {
        return new VideoSourceModel
        {
            Name = CleanKodiText(name),
            Url = url,
            Referer = referer,
            Headers = BuildHeaders(referer)
        };
    }

    private async Task<ParserResult?> parse(string url)
    {
        if (string.IsNullOrWhiteSpace(url) || url.Contains("selection cancelled", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        url = url.Replace("|", "#", StringComparison.Ordinal).Trim();
        if (IsDirectMediaUrl(url))
        {
            return Direct(url);
        }

        foreach (var option in _options)
        {
            if (!url.Contains(option.Key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var result = await option.Value(url);
            if (result is null)
            {
                return null;
            }

            if (ShouldReparse(result.Url) && !string.Equals(result.Url, url, StringComparison.OrdinalIgnoreCase))
            {
                return await parse(result.Url);
            }

            return result;
        }

        return await GenericPageParserAsync(url);
    }

    private static string encrypt_message(string message, string key)
    {
        var chars = message.Select((c, i) => (char)(c ^ key[i % key.Length])).ToArray();
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(chars));
    }

    private static string decrypt_message(string encryptedMessage)
    {
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encryptedMessage));
        var chars = decoded.Select((c, i) => (char)(c ^ VidName[i % VidName.Length])).ToArray();
        return new string(chars);
    }

    private async Task<string?> fixsub(string sub, string? site = null)
    {
        if (site == "contentx")
        {
            return sub;
        }

        var content = await HttpGet(sub, referer: site, headers: BrowserHeaders(site));
        if (string.IsNullOrWhiteSpace(content))
        {
            return sub;
        }

        return Regex.Replace(content, @"(\d+:\d+\.\d*) --> (\d+:\d+\.\d*)", "00:$1 --> 00:$2");
    }

    private static string contentx_local_m3u8(string page, string? site = null) => page;

    private static ParserResult? select(IReadOnlyList<string> kaynaklar, IReadOnlyList<string> linkler, int tip = 2)
    {
        return auto_select(kaynaklar, linkler);
    }

    private async Task<bool> check_response_code(string link, Dictionary<string, string>? head = null)
    {
        if (link.Contains("contentx", StringComparison.OrdinalIgnoreCase))
        {
            head = new() { ["Referer"] = "https://contentx.me/" };
        }
        else if (link.Contains("vidmoly", StringComparison.OrdinalIgnoreCase))
        {
            link = link.Split('#')[0];
            head = new() { ["Referer"] = "https://vidmoly.me/" };
        }

        try
        {
            using var client = new HttpClient();
            foreach (var header in BrowserHeaders(head?.GetValueOrDefault("Referer")))
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
            }

            if (head is not null)
            {
                foreach (var header in head)
                {
                    client.DefaultRequestHeaders.Remove(header.Key);
                    client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            using var request = new HttpRequestMessage(HttpMethod.Head, link);
            using var response = await client.SendAsync(request);
            return response.StatusCode is HttpStatusCode.OK
                or HttpStatusCode.PartialContent
                or HttpStatusCode.MovedPermanently
                or HttpStatusCode.TemporaryRedirect
                or HttpStatusCode.PermanentRedirect
                or HttpStatusCode.NotModified;
        }
        catch
        {
            return false;
        }
    }

    private static ParserResult? auto_select(IReadOnlyList<string> kaynaklar, IReadOnlyList<string> linkler, IReadOnlyList<string>? sources = null)
    {
        if (linkler.Count == 0)
        {
            return null;
        }

        sources ??= ["2160p", "1080p", "720p", "480p", "360p", "240p", "144p", "m3u8", "mp4"];
        foreach (var source in sources)
        {
            var index = kaynaklar.ToList().FindIndex(x => x.Contains(source, StringComparison.OrdinalIgnoreCase));
            if (index >= 0 && index < linkler.Count)
            {
                return Direct(linkler[index]);
            }
        }

        return Direct(linkler[0]);
    }

    private static char findRealChar(char c)
    {
        if (!char.IsLetter(c))
        {
            return c;
        }

        var offset = char.ToLowerInvariant(c) < 'n' ? 13 : -13;
        return (char)(c + offset);
    }

    private static string myUnpacker(string s, string e)
    {
        var symbols = s.Split('|');
        var alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".Select(x => x.ToString()).ToList();
        var output = new StringBuilder();

        foreach (var token in e.Split(@"\\").Skip(1))
        {
            var index = alphabet.IndexOf(token);
            if (index >= 0 && index < symbols.Length && symbols[index].Length > 1)
            {
                output.Append(symbols[index][1..].Trim());
            }
        }

        return output.ToString();
    }

    private static bool detect(string source)
    {
        return Regex.IsMatch(source, @"eval[ ]*\([ ]*function[ ]*\([ ]*p[ ]*,[ ]*a[ ]*,[ ]*c[ ]*,[ ]*k[ ]*,[ ]*e[ ]*,", RegexOptions.Singleline);
    }

    private static string unpack(string source)
    {
        return JsUnpack(source);
    }

    private static (string Payload, string[] Symtab, int Radix, int Count)? _filterargs(string source)
    {
        var patterns = new[]
        {
            @"}\('(.*)', *(\d+|\[\]), *(\d+), *'(.*)'\.split\('\|'\), *(\d+), *(.*)\)\)",
            @"}\('(.*)', *(\d+|\[\]), *(\d+), *'(.*)'\.split\('\|'\)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(source, pattern, RegexOptions.Singleline);
            if (!match.Success)
            {
                continue;
            }

            var radix = match.Groups[2].Value == "[]" ? 62 : int.Parse(match.Groups[2].Value);
            return (match.Groups[1].Value, match.Groups[4].Value.Split('|'), radix, int.Parse(match.Groups[3].Value));
        }

        return null;
    }

    private static string _replacestrings(string source)
    {
        var match = Regex.Match(source, "var *(_\\w+)\\=\\[\"(.*?)\"\\];", RegexOptions.Singleline);
        if (!match.Success)
        {
            return source;
        }

        var varName = match.Groups[1].Value;
        var lookup = match.Groups[2].Value.Split("\",\"");
        var replaced = source[match.Length..];
        for (var i = 0; i < lookup.Length; i++)
        {
            replaced = replaced.Replace($"{varName}[{i}]", $"\"{lookup[i]}\"", StringComparison.Ordinal);
        }

        return replaced;
    }

    private sealed class Unbaser
    {
        private const string Alphabet62 = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private readonly string _alphabet;
        private readonly int _base;

        public Unbaser(int numberBase)
        {
            _base = numberBase;
            _alphabet = numberBase <= 62 ? Alphabet62[..numberBase] : Alphabet62;
        }

        public int Decode(string value)
        {
            if (_base <= 36)
            {
                return Convert.ToInt32(value, _base);
            }

            var result = 0;
            foreach (var c in value)
            {
                result = result * _base + Math.Max(0, _alphabet.IndexOf(c, StringComparison.Ordinal));
            }

            return result;
        }
    }

    private static Task send_report(string url) => Task.CompletedTask;

    private static string normalize_url(string url)
    {
        if (url.StartsWith("//", StringComparison.Ordinal))
        {
            url = "http:" + url;
        }
        else if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            url = "http://" + url;
        }

        var uri = new Uri(url);
        return new UriBuilder(uri) { Scheme = Uri.UriSchemeHttps, Port = -1 }.Uri.ToString();
    }

    private Task<ParserResult?> play_youtube(string url) => youtube(url);

    private Task<ParserResult?> youtube(string url)
    {
        var id = Regex.Match(url, @"(?:v=|embed/)(.*?)(?:&|$)").Groups[1].Value;
        return Task.FromResult<ParserResult?>(string.IsNullOrWhiteSpace(id) ? null : Direct(url));
    }

    private async Task<ParserResult?> vkcom(string url)
    {
        var idPart = url.Split("video", StringSplitOptions.None).LastOrDefault();
        if (string.IsNullOrWhiteSpace(idPart))
        {
            return await GenericPageParserAsync(url);
        }

        var idPrefix = idPart.Split('_')[0];
        var body = new Dictionary<string, string>
        {
            ["act"] = "show",
            ["al"] = "1",
            ["playlist_id"] = $"{idPrefix}_-2",
            ["video"] = idPart
        };

        var json = await HttpPost("https://vk.com/al_video.php?act=show", body, referer: url, headers: BrowserHeaders(url));
        var link = FirstMatch(json, "\"hls\"\\s*:\\s*\"(?<url>[^\"]+)\"");
        return string.IsNullOrWhiteSpace(link) ? await GenericPageParserAsync(url) : Direct(CleanEscapedUrl(link), url);
    }

    private async Task<ParserResult?> okru(string url)
    {
        var id = FirstMatch(url, @"https?://(?:www\.)?(?:odnoklassniki|ok)\.ru/(?:videoembed/|dk\?cmd=videoPlayerMetadata&mid=)(?<url>\d+)");
        if (string.IsNullOrWhiteSpace(id))
        {
            return await GenericPageParserAsync(url);
        }

        var page = await HttpPost("https://www.ok.ru/dk", new Dictionary<string, string>
        {
            ["cmd"] = "videoPlayerMetadata",
            ["mid"] = id
        }, referer: url, headers: BrowserHeaders(url));

        var link = FirstMatch(page, "(?:ultra|quad|full|hd|sd|low|lowest)\",\"url\":\"(?<url>.*?)\"");
        return string.IsNullOrWhiteSpace(link) ? null : Direct(CleanEscapedUrl(link), "https://ok.ru/");
    }

    private async Task<ParserResult?> vidmoly(string url)
    {
        url = "https:" + url.Replace("http:", "", StringComparison.OrdinalIgnoreCase)
            .Replace("https:", "", StringComparison.OrdinalIgnoreCase)
            .Replace(".top", ".to", StringComparison.OrdinalIgnoreCase);
        var html = await HttpGet(url, referer: url, headers: BrowserHeaders(url), followRedirects: true);
        var redirect = FirstMatch(html, "window.location\\s*=\\s*'(?<url>.*?)'");
        if (!string.IsNullOrWhiteSpace(redirect))
        {
            html = await HttpGet(url.Replace("embed-", redirect, StringComparison.Ordinal), referer: url, headers: BrowserHeaders(url));
        }

        return PickFromHtml(html, url, "https://vidmoly.to/");
    }

    private Task<ParserResult?> vidmoly_old(string url) => vidmoly(url);

    private async Task<ParserResult?> mailru(string url)
    {
        var code = await HttpGet(url, headers: BrowserHeaders(url));
        var meta = FirstMatch(code, "(?:metadataUrl|metaUrl)\"\\s*:\\s*\"(?<url>//my[^\"]+)");
        if (string.IsNullOrWhiteSpace(meta))
        {
            return await GenericPageParserAsync(url);
        }

        var page = await HttpGet($"https:{meta}?ver=0.2.123", referer: url, headers: BrowserHeaders(url));
        var cookie = FirstMatch(page, "video_key[^;]+");
        var links = Regex.Matches(page ?? string.Empty, "url\":\"(?<url>//cdn[^\"]+).+?(?<quality>\\d+p)");
        var best = PickBest(links.Select(x => ($"http:{x.Groups["url"].Value}", x.Groups["quality"].Value)).ToList());
        return best is null ? null : Direct(best.Value.Url, url, extraHeaders: string.IsNullOrWhiteSpace(cookie) ? null : new() { ["Cookie"] = cookie });
    }

    private async Task<ParserResult?> fembed(string url)
    {
        var redirected = await HttpGet(url, referer: url, headers: BrowserHeaders(url), followRedirects: true);
        var apiUrl = (redirected is not null && redirected.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? redirected : url).Replace("/v/", "/api/source/", StringComparison.OrdinalIgnoreCase);
        var domain = FirstMatch(apiUrl, @"(?:http://|//)(?<url>.*?)/");
        var json = await HttpPost(apiUrl, new Dictionary<string, string> { ["r"] = "", ["d"] = domain }, referer: url, headers: BrowserHeaders(url));
        var links = Regex.Matches(json?.Replace("\\", "", StringComparison.Ordinal) ?? string.Empty, "\"file\":\"(?<url>[^\"]+)\",\"label\":\"(?<quality>[^\"]+)\"");
        var best = PickBest(links.Select(x => (x.Groups["url"].Value, x.Groups["quality"].Value)).ToList());
        return best is null ? null : Direct(best.Value.Url, BaseUrl(url));
    }

    private async Task<ParserResult?> streamtape(string url)
    {
        var html = await HttpGet(url, referer: url, headers: BrowserHeaders(url));
        var token = FirstMatch(html, "get_video\\?id=[^'\"&]+&expires=[^'\"&]+&ip=[^'\"&]+&token=[^'\"]+");
        if (string.IsNullOrWhiteSpace(token))
        {
            token = FirstMatch(html, "document.getElementById\\('videolink'\\).*?innerHTML\\s*=\\s*'(?<url>[^']+)'");
        }

        return string.IsNullOrWhiteSpace(token) ? await GenericPageParserAsync(url) : Direct(FixUrl(token, "https://streamtape.com"), url);
    }

    private async Task<ParserResult?> dood(string url)
    {
        var html = await HttpGet(url, referer: url, headers: BrowserHeaders(url));
        var pass = FirstMatch(html, @"/pass_md5/[^'""\s]+");
        if (string.IsNullOrWhiteSpace(pass))
        {
            return await GenericPageParserAsync(url);
        }

        var baseUrl = BaseUrl(url);
        var partial = await HttpGet(FixUrl(pass, baseUrl), referer: url, headers: BrowserHeaders(url));
        return string.IsNullOrWhiteSpace(partial) ? null : Direct(partial + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), url);
    }

    private async Task<ParserResult?> mixdrop(string url)
    {
        var html = await HttpGet(url, referer: url, headers: BrowserHeaders(url));
        if (detect(html ?? string.Empty))
        {
            html = unpack(html!);
        }

        return PickFromHtml(html, url, BaseUrl(url));
    }

    private async Task<ParserResult?> streamwish(string url) => await GenericPageParserAsync(url, "https://streamwish.to/");

    private async Task<ParserResult?> upstream(string url) => await GenericPageParserAsync(url, BaseUrl(url));

    private async Task<ParserResult?> sbembed(string url) => await GenericPageParserAsync(url, BaseUrl(url));

    private async Task<ParserResult?> voe(string url) => await GenericPageParserAsync(url, BaseUrl(url));

    private async Task<ParserResult?> dailymotion(string url) => await GenericPageParserAsync(url, "https://www.dailymotion.com/");

    private async Task<ParserResult?> canlitvlive(string url)
    {
        var html = await HttpGet(url, headers: BrowserHeaders(url));
        var link = FirstMatch(html, "player\\.src\\('(?<url>.*?)'");
        return string.IsNullOrWhiteSpace(link) ? await GenericPageParserAsync(url) : Direct(link, url);
    }

    private async Task<ParserResult?> uptostream(string url) => await GenericPageParserAsync(url);

    private async Task<ParserResult?> closeload(string url)
    {
        var html = await HttpGet(url, referer: url, headers: BrowserHeaders(url));
        var link = FirstMatch(html, "\"contentUrl\"\\s*:\\s*\"(?<url>[^\"]+)\"");
        var subtitles = Regex.Matches(html ?? string.Empty, "track src=\"(?<url>/vtt.*?)\"")
            .Select(x => "https://closeload.com" + x.Groups["url"].Value)
            .ToList();
        return string.IsNullOrWhiteSpace(link) ? await GenericPageParserAsync(url) : Direct(link, url, subtitles);
    }

    private async Task<ParserResult?> filmmodu(string url)
    {
        var html = await HttpGet(url, referer: url, headers: BrowserHeaders(url));
        var sources = Regex.Matches(html ?? string.Empty, "\"src\"\\s*:\\s*\"(?<url>.*?)\"")
            .Select(x => (CleanEscapedUrl(x.Groups["url"].Value), x.Groups["url"].Value))
            .ToList();
        var best = PickBest(sources);
        var subtitle = FirstMatch(html, "\"subtitle\"\\s*:\\s*\"(?<url>.*?)\"");
        var subtitles = string.IsNullOrWhiteSpace(subtitle) ? [] : new List<string> { FixUrl(CleanEscapedUrl(subtitle), BaseUrl(url)) };
        return best is null ? await GenericPageParserAsync(url) : Direct(best.Value.Url, url, subtitles);
    }

    private async Task<ParserResult?> imdb(string url) => await GenericPageParserAsync(url, "https://www.imdb.com/");
    private async Task<ParserResult?> s1cdn(string url)
    {
        var page = await HttpGet(url, headers: BrowserHeaders(url));
        var encoded = FirstMatch(page, "\"#2(?<url>.*?)\"");
        return string.IsNullOrWhiteSpace(encoded) ? null : Direct(DecodeBase64(Regex.Replace(encoded, "(//.*?=)", string.Empty)), url);
    }

    private async Task<ParserResult?> show(string url)
    {
        var page = await HttpGet(url, headers: BrowserHeaders(url));
        var hope = FirstMatch(page, "data-hope-video='(?<url>.*?)'");
        if (!string.IsNullOrWhiteSpace(hope))
        {
            var m3u8 = FirstMatch(WebUtility.HtmlDecode(hope), "\"src\"\\s*:\\s*\"(?<url>[^\"]+\\.m3u8[^\"]*)\"");
            if (!string.IsNullOrWhiteSpace(m3u8))
            {
                return Direct(CleanEscapedUrl(m3u8), url);
            }
        }

        var link = FirstMatch(page, "var\\s*videoUrl\\s*=\\s*\"(?<url>.*?)\"");
        return string.IsNullOrWhiteSpace(link) ? await GenericPageParserAsync(url) : Direct(link, url);
    }

    private async Task<ParserResult?> yjco(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> nowtv(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> startv(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> atv(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> atvcanli(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> fileru(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> kanald(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> dizibox(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> chefkoch24(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> haberturk(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> dha(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> tv8(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> cnnteve2(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> tlc(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> kanal7(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> kanal7dizi(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> halktv(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> kralmuzik(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> numberone(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> ahaber(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> tv360(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> ucankus(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> dreamturk(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> tvem(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> ulusal(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> kanalb(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> ekoturk(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> womantv(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> tjk(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> yaban(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> videobin(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> gounlimited(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> saruch(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> streamtheworld(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> chaturbate(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> volotv(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> canliradyolar(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> ashemale(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> pornhub(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> xvideos(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> puh(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> teve2(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> milyontv(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> dizilab(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> radyohome(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> onlineradiobox(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> dak7(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> dizigom(string url) => await GenericPageParserAsync(url);
    private static string dizilla_securedata(string v1) => DecodeBase64(v1);
    private async Task<ParserResult?> dizilla_last(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> filese(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> contentx(string url) => await GenericPageParserAsync(url, BaseUrl(url));
    private async Task<ParserResult?> yabancidizi1(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> fullhdfilmizlesene(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> koreanturk(string url) => await GenericPageParserAsync(url);
    private static string? extract_function(string page, string fname)
    {
        var match = Regex.Match(page, $@"function\s+{Regex.Escape(fname)}\s*\(.*?\)\s*\{{(?<url>.*?)\}}", RegexOptions.Singleline);
        return match.Success ? match.Groups["url"].Value : null;
    }

    private async Task<ParserResult?> filmmakinesi(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> ugurfilm(string url) => await GenericPageParserAsync(url);
    private async Task<string?> get_embed_url_hdfilmcehennemi(string url)
    {
        var html = await HttpGet(url, headers: BrowserHeaders(url));
        return FirstMatch(html, @"<iframe[^>]+src=[""'](?<url>[^""']+)[""']");
    }

    private async Task<ParserResult?> hdfilmcehennemi(string url) => await GenericPageParserAsync(await get_embed_url_hdfilmcehennemi(url) ?? url);
    private async Task<ParserResult?> webteizle(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> yilmaztv(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> k2s(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> vcdn(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> cloudvideo(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> jetfilmizle(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> plus4(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> videoseyred(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> sezonlukdizi(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> tele1(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> ulketv(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> tvnet(string url) => await GenericPageParserAsync(url);
    private static string decryptFor4KIzle(string input_str) => new(input_str.Select(findRealChar).ToArray());
    private async Task<ParserResult?> k4filmizle(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> dizitime(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> pandamovie_freeomovie(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> filmizlesene(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> sinefy(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> diziyo(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> meteor(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> onlinedizi(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> hdfilmcehennemisyrtrk(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> streamz(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> trtparser(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> hdtoday(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> streamlare(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> sinemafilmizle(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> canlitvcenter(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> dizimom(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> filmon(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> videoone(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> beyaztv(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> yirmidort(string url) => await GenericPageParserAsync(url);
    private Task<ParserResult?> hugo(string site = "dizi") => GenericPageParserAsync(site);
    private async Task<ParserResult?> siyahfilmizle(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> radyodelisi(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> vidoza(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> sto(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> cinemathek(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> movie4k(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> goodstream(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> vimeo(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> trstx(string url) => await GenericPageParserAsync(url);
    private static string ydd(string text, string referer) => text;
    private async Task<ParserResult?> yabancidizi(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> youporn(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> teleontv(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> lookmovie2(string url) => await GenericPageParserAsync(url);
    private static Task upload_to_server(string text) => Task.CompletedTask;
    private async Task<ParserResult?> themoviearchive(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> thehun(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> istanbuluseyret(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> diziyou(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> pokitv(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> wikiflix(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> filmatek(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> filmkovasi(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> filmekseni(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> setfilmizle(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> diziwatch(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> filmmax(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> diziplus(string url) => await GenericPageParserAsync(url);
    private Task<ParserResult?> hdmom(string url) => GenericPageParserAsync(url);
    private static string vidmoxy(string s) => s;
    private async Task<ParserResult?> dizifin(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> canlitvulusal(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> sinemacx(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> hdfilmizle(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> watchomovies(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> streamoupload(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> xhamster(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> xnxx(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> cnbce(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> govids(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> sibnet(string url) => await GenericPageParserAsync(url);
    private static string baseUrl(string url) => BaseUrl(url);
    private async Task<ParserResult?> vectorx(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> streamruby(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> yoltv(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> canlitvws(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> filmcidayi(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> myvideoaz(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> parsatv(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> ddizi(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> hdabla(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> allclassic(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> filmizlemek(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> oneupload(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> vudeo(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> canlidizi(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> vidlop(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> streamplayer(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> fullhdfilm(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> mavifilm1(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> mavifilm(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> dizipod(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> pornpics(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> sozcu(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> streamimdb(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> dizipal(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> liderfilm(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> dizibal(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> dizirella(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> altiyuz(string url) => await GenericPageParserAsync(url);
    private async Task<ParserResult?> vidmody(string url) => await GenericPageParserAsync(url);

    private async Task<ParserResult?> GenericPageParserAsync(string url, string? forcedReferer = null)
    {
        if (IsDirectMediaUrl(url))
        {
            return Direct(url, forcedReferer);
        }

        var referer = forcedReferer ?? url;
        var html = await HttpGet(url, referer: referer, headers: BrowserHeaders(referer), followRedirects: true);
        if (string.IsNullOrWhiteSpace(html))
        {
            return Direct(url, referer);
        }

        if (detect(html))
        {
            html = unpack(html);
        }

        return PickFromHtml(html, url, referer) ?? Direct(url, referer);
    }

    private ParserResult? PickFromHtml(string? html, string pageUrl, string? referer = null)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        var subtitles = FindSubtitles(html, pageUrl);
        var candidates = new List<(string Url, string Quality)>();

        foreach (var pattern in MediaPatterns())
        {
            candidates.AddRange(Regex.Matches(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline)
                .Select(match =>
                {
                    var mediaUrl = match.Groups["url"].Value;
                    var quality = match.Groups["quality"].Success ? match.Groups["quality"].Value : mediaUrl;
                    return (FixUrl(CleanEscapedUrl(mediaUrl), pageUrl), quality);
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Item1)));
        }

        var best = PickBest(candidates);
        return best is null ? null : Direct(best.Value.Url, referer ?? pageUrl, subtitles);
    }

    private static IReadOnlyList<string> MediaPatterns()
    {
        return
        [
            @"(?<url>https?:\\?/\\?/[^'""\s<>]+?\.m3u8[^'""\s<>]*)",
            @"(?<url>https?:\\?/\\?/[^'""\s<>]+?\.mpd[^'""\s<>]*)",
            @"(?<url>https?:\\?/\\?/[^'""\s<>]+?\.mp4[^'""\s<>]*)",
            @"""file""\s*:\s*""(?<url>[^""]+)""(?:\s*,\s*""label""\s*:\s*""(?<quality>[^""]+)"")?",
            @"file\s*:\s*['""](?<url>[^'""]+)['""](?:.*?label\s*:\s*['""](?<quality>[^'""]+)['""])?",
            @"source\s+src\s*=\s*['""](?<url>[^'""]+)['""](?:.*?(?:data-res|label)\s*=\s*['""](?<quality>[^'""]+)['""])?",
            @"""src""\s*:\s*""(?<url>[^""]+)""(?:\s*,\s*""type""\s*:\s*""[^""]+""\s*,\s*""label""\s*:\s*""(?<quality>[^""]+)"")?"
        ];
    }

    private static List<string> FindSubtitles(string html, string pageUrl)
    {
        var results = new List<string>();
        var patterns = new[]
        {
            @"track\s+src\s*=\s*['""](?<url>[^'""]+)['""]",
            @"""file""\s*:\s*""(?<url>[^""]+\.(?:srt|vtt)[^""]*)""",
            @"file\s*:\s*['""](?<url>[^'""]+\.(?:srt|vtt)[^'""]*)['""]"
        };

        foreach (var pattern in patterns)
        {
            results.AddRange(Regex.Matches(html, pattern, RegexOptions.IgnoreCase)
                .Select(x => FixUrlStatic(CleanEscapedUrl(x.Groups["url"].Value), pageUrl)));
        }

        return results.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static (string Url, string Quality)? PickBest(IReadOnlyList<(string Url, string Quality)> candidates)
    {
        if (candidates.Count == 0)
        {
            return null;
        }

        var order = new[] { "2160", "1080", "720", "480", "360", "m3u8", "mp4" };
        foreach (var quality in order)
        {
            var match = candidates.FirstOrDefault(x => x.Quality.Contains(quality, StringComparison.OrdinalIgnoreCase) || x.Url.Contains(quality, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(match.Url))
            {
                return match;
            }
        }

        return candidates[0];
    }

    private static bool ShouldReparse(string url)
    {
        if (IsDirectMediaUrl(url))
        {
            return false;
        }

        return url.Contains("dailymotion.com/video", StringComparison.OrdinalIgnoreCase)
            || url.Contains("streamlare.com/e", StringComparison.OrdinalIgnoreCase)
            || url.Contains("sbembed", StringComparison.OrdinalIgnoreCase)
            || url.Contains("vidlop", StringComparison.OrdinalIgnoreCase)
            || url.Contains("streamplayer", StringComparison.OrdinalIgnoreCase)
            || url.Contains("mixdrop", StringComparison.OrdinalIgnoreCase)
            || url.Contains("dood", StringComparison.OrdinalIgnoreCase)
            || url.Contains("voe.", StringComparison.OrdinalIgnoreCase)
            || url.Contains("vk.com", StringComparison.OrdinalIgnoreCase)
            || url.Contains("streamtape", StringComparison.OrdinalIgnoreCase)
            || url.Contains("videoseyred", StringComparison.OrdinalIgnoreCase)
            || url.Contains("vidmoly", StringComparison.OrdinalIgnoreCase)
            || url.Contains("closeload", StringComparison.OrdinalIgnoreCase)
            || url.Contains("ok.ru", StringComparison.OrdinalIgnoreCase)
            || url.Contains("mail.ru", StringComparison.OrdinalIgnoreCase)
            || url.Contains("fembed", StringComparison.OrdinalIgnoreCase)
            || url.Contains("streamwish", StringComparison.OrdinalIgnoreCase)
            || url.Contains("filemoon", StringComparison.OrdinalIgnoreCase);
    }

    private static ParserResult Direct(string url, string? referer = null, IReadOnlyList<string>? subtitles = null, Dictionary<string, string>? extraHeaders = null)
    {
        var clean = CleanEscapedUrl(url);
        var result = new ParserResult(clean.Split('#')[0])
        {
            Referer = ExtractFragmentValue(clean, "Referer") ?? referer
        };

        foreach (var subtitle in subtitles ?? [])
        {
            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                result.Subtitles.Add(subtitle);
            }
        }

        if (extraHeaders is not null)
        {
            foreach (var header in extraHeaders)
            {
                result.Headers[header.Key] = header.Value;
            }
        }

        foreach (var key in new[] { "User-Agent", "UserAgent", "Origin", "Cookie" })
        {
            var value = ExtractFragmentValue(clean, key);
            if (!string.IsNullOrWhiteSpace(value))
            {
                result.Headers[key == "UserAgent" ? "User-Agent" : key] = value;
            }
        }

        return result;
    }

    private static string? ExtractFragmentValue(string url, string key)
    {
        var index = url.IndexOf('#');
        if (index < 0)
        {
            return null;
        }

        var fragment = url[(index + 1)..];
        foreach (var part in fragment.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = part.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            if (part[..separator].Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                return WebUtility.UrlDecode(part[(separator + 1)..]);
            }
        }

        return null;
    }

    private static bool IsDirectMediaUrl(string url)
    {
        var clean = url.Split('#')[0];
        return clean.Contains(".m3u8", StringComparison.OrdinalIgnoreCase)
            || clean.Contains(".mpd", StringComparison.OrdinalIgnoreCase)
            || clean.Contains(".mp4", StringComparison.OrdinalIgnoreCase)
            || clean.Contains(".mkv", StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<string, string> BrowserHeaders(string? referer)
    {
        var headers = new Dictionary<string, string>
        {
            ["User-Agent"] = Ua,
            ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8"
        };

        if (!string.IsNullOrWhiteSpace(referer))
        {
            headers["Referer"] = referer;
        }

        return headers;
    }

    private static List<HeaderModel> BuildHeaders(string? referer, Dictionary<string, string>? extra = null)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["User-Agent"] = Ua
        };

        if (!string.IsNullOrWhiteSpace(referer))
        {
            values["Referer"] = referer;
        }

        if (extra is not null)
        {
            foreach (var header in extra)
            {
                values[header.Key] = header.Value;
            }
        }

        return values.Select(x => new HeaderModel { Name = x.Key, Value = x.Value }).ToList();
    }

    public static string CleanKodiText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var clean = Regex.Replace(text, @"\[(?:/?COLOR|/?B).*?\]", string.Empty, RegexOptions.IgnoreCase);
        clean = clean.Replace("[/COLOR]", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("[/B]", string.Empty, StringComparison.OrdinalIgnoreCase);
        return Regex.Replace(clean, @"\s+", " ").Trim();
    }

    private static string CleanEscapedUrl(string value)
    {
        return WebUtility.HtmlDecode(value)
            .Replace("\\/", "/", StringComparison.Ordinal)
            .Replace("\\u0026", "&", StringComparison.OrdinalIgnoreCase)
            .Replace("\\", string.Empty, StringComparison.Ordinal)
            .Trim();
    }

    private static string FirstMatch(string? text, string pattern)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!match.Success)
        {
            return string.Empty;
        }

        return match.Groups["url"].Success ? match.Groups["url"].Value : match.Groups[1].Value;
    }

    private static string DecodeBase64(string value)
    {
        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(value));
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string BaseUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) ? $"{uri.Scheme}://{uri.Host}/" : url;
    }

    private static string FixUrlStatic(string url, string mainUrl)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        if (url.StartsWith("//", StringComparison.Ordinal))
        {
            return "https:" + url;
        }

        return Uri.TryCreate(mainUrl, UriKind.Absolute, out var baseUri)
            ? new Uri(baseUri, url).ToString()
            : url;
    }

    private static string JsUnpack(string source)
    {
        var args = _filterargs(source);
        if (args is null)
        {
            return source;
        }

        var (payload, symtab, radix, _) = args.Value;
        var unbaser = new Unbaser(radix);
        payload = payload.Replace(@"\\", @"\", StringComparison.Ordinal).Replace("\\'", "'", StringComparison.Ordinal);

        return Regex.Replace(payload, @"\b\w+\b", match =>
        {
            try
            {
                var index = unbaser.Decode(match.Value);
                return index >= 0 && index < symtab.Length && !string.IsNullOrWhiteSpace(symtab[index])
                    ? symtab[index]
                    : match.Value;
            }
            catch
            {
                return match.Value;
            }
        }, RegexOptions.CultureInvariant);
    }

    private sealed class ParserResult(string url)
    {
        public string Url { get; set; } = url;
        public string? Referer { get; set; }
        public List<string> Subtitles { get; } = [];
        public Dictionary<string, string> Headers { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
