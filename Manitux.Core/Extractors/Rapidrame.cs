using System;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CodeLogic.Core.Logging;
using Manitux.Core.Models;

namespace Manitux.Core.Extractors;

public class Rapidrame : ExtractorBase
{
    public override string Name => "Rapidrame";
    public override string MainUrl => "https://www.hdfilmcehennemi.nl";
    public override List<string> SupportedDomains => new() { "hdfilmcehennemi.com", "hdfilmcehennemi.nl", "rapid.filmmakinesi.to", "rapidvid.net" };

    // https://www.hdfilmcehennemi.nl/rplayer/iln9u9ark7oe

    private string DecryptTest()
    {
        // 25.04.2026
        string[] parts = new string[] {
            "==QP9EFUIh","lczcGbzs0V","zsGcxo1cQN","lTKhlQ3ADM","yBFZQJ1TsV",
            "XcGNmZptUc","zEFUvQzV1d","jaCpnYidlN","Ll1VXRjcqF","mMqJ3QyAXM",
            "J1UUFFXYW1","0UMRFV5REV","jdjRtFkZYN","DO5BXexJTS","zNjQZJDThh",
            "EczZDSalFa","vZWew9mWTV","XVPB1UJFGN","4NTVY9ibqJ","TVKZWTCRnW",
            "sR3NXJlRxZ","1MOdUaJlWS","uhnQwBFRHB","XaCJDWuhXU","6l2Q5kFUZt",
            "EcuNjSv9GV","kV0bz4kSt1","0SWF1LwU1U","UFDRWBTSWF","1SlNTYNp0M",
            "plXOqplQ5J","Da2Z0MmhnT","J5kenpXTwA","neM9yczMWM","shUY"
        };

        string combined = string.Join("", parts);
        return Decrypt2(combined) ?? "Err";
    }

    private string? Decrypt1(string input)
    {
        try
        {
            // 1. atob karşılığı: Mutlaka Latin1 (ISO-8859-1) kullanılmalı. 
            // UTF8 kullanıldığında 'ì', 'æ' gibi bozulmalar başlar.
            byte[] data = Convert.FromBase64String(input);
            string step1 = Encoding.GetEncoding("ISO-8859-1").GetString(data);

            // 2. ROT13 (JavaScript Birebir Mantığı)
            StringBuilder step2 = new StringBuilder();
            foreach (char c in step1)
            {
                if (c >= 'A' && c <= 'Z')
                {
                    int res = c + 13;
                    step2.Append((char)(res <= 90 ? res : res - 26));
                }
                else if (c >= 'a' && c <= 'z')
                {
                    int res = c + 13;
                    step2.Append((char)(res <= 122 ? res : res - 26));
                }
                else
                {
                    step2.Append(c);
                }
            }

            // 3. Reverse
            char[] arr = step2.ToString().ToCharArray();
            Array.Reverse(arr);
            string step3 = new string(arr);

            // 4. Unmix (Matematiksel Döngü)
            StringBuilder unmix = new StringBuilder();
            long junk = 399756995;
            for (int i = 0; i < step3.Length; i++)
            {
                int charCode = (int)step3[i];
                // JS'deki (charCode - (399756995 % (i + 5)) + 256) % 256 mantığı
                int offset = (int)(junk % (i + 5));
                int decoded = (charCode - offset + 256) % 256;
                unmix.Append((char)decoded);
            }

            string url = unmix.ToString();
            Log(LogLevel.Debug, "unmix1: " + unmix);
            if (url.StartsWith("http://") && url.StartsWith("https://")) return url;
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, ex.ToString());
        }

        return null;
    }

    private string? Decrypt2(string input)
    {
        try
        {
            // 1. Join: Parçaları birleştir
            //string joined = string.Concat(valueParts);

            // 2. Reverse: String'i ters çevir
            char[] charArray = input.ToCharArray();
            Array.Reverse(charArray);
            string reversed = new string(charArray);

            // 3. Double Base64 Decode: İki kez atob() işlemi
            // JS'deki atob'un tam karşılığı ISO-8859-1 encoding kullanmaktır.
            string firstPass = Base64DecodeJS(reversed);
            string secondPass = Base64DecodeJS(firstPass);

            // 4. Unmix: Karakter kaydırma döngüsü
            StringBuilder unmix = new StringBuilder();
            long salt = 399756995;

            for (int i = 0; i < secondPass.Length; i++)
            {
                int charCode = (int)secondPass[i];

                // JS Algoritması: (charCode - (salt % (i + 5)) + 256) % 256
                int offset = (int)(salt % (i + 5));
                int decodedCharCode = (charCode - offset + 256) % 256;

                unmix.Append((char)decodedCharCode);
            }

            string url = unmix.ToString();
            Log(LogLevel.Debug, "unmix2: " + unmix);
            if (url.StartsWith("http://") && url.StartsWith("https://")) return url;
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, ex.ToString());
        }

        return null;
    }

    private string? Decrypt3(string input)
    {
        try
        {
            string rot13Result = ApplyRot13(input);

            // 3. Reverse: String'i ters çevir
            char[] charArray = rot13Result.ToCharArray();
            Array.Reverse(charArray);
            string reversed = new string(charArray);

            // 4. Base64 Decode: (atob)
            string base64Decoded = Base64Decode(reversed);

            long seed = 399756995;

            // 5. Unmix Loop: Karakter bazlı matematiksel deşifre
            StringBuilder unmix = new StringBuilder();
            for (int i = 0; i < base64Decoded.Length; i++)
            {
                int charCode = (int)base64Decoded[i];
                // JS: charCode = (charCode - (399756995 % (i + 5)) + 256) % 256;
                int offset = (int)(seed % (i + 5));
                int decodedChar = (charCode - offset + 256) % 256;
                unmix.Append((char)decodedChar);
            }

            string finalResult = unmix.ToString();

            // Eğer sonuç içinde hala "SarahsOil" gibi metadata varsa sadece URL'yi çekelim
            var urlMatch = Regex.Match(finalResult, @"https?://[^\s""|]+");
            return urlMatch.Success ? urlMatch.Value : finalResult;
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, ex.ToString());
        }

        return null;
    }

    private string ApplyRot13(string input)
    {
        return string.Join("", input.Select(c =>
        {
            if (c >= 'a' && c <= 'z')
                return (char)((c - 'a' + 13) % 26 + 'a');
            if (c >= 'A' && c <= 'Z')
                return (char)((c - 'A' + 13) % 26 + 'A');
            return c;
        }));
    }

    private string GetBase64FromHtml(string html)
    {
        // 1. eval bloğunu bul: eval(...) yapısını en dıştan yakala
        // Desen: eval ile başla, split('|'), bir sayı ve kapalı parantezlerle bitir
        var evalMatch = Regex.Match(html, @"eval\(function\(p,a,c,k,e,d\).*?\.split\('\|'\),\d+,\{\}\)\)", RegexOptions.Singleline);

        if (!evalMatch.Success) return "Hata: HTML içinde uygun eval bloğu bulunamadı.";

        string evalBody = evalMatch.Value;

        // 2. Sözlük (Dictionary) kısmını çekelim: split('|') parantezi içindeki son string
        var dictionaryMatch = Regex.Match(evalBody, @"'([^']*)'\.split", RegexOptions.Singleline);
        if (!dictionaryMatch.Success) dictionaryMatch = Regex.Match(evalBody, @"""([^""]*)""\.split", RegexOptions.Singleline);

        if (!dictionaryMatch.Success) return "Hata: Sözlük (pipe-separated list) ayıklanamadı.";

        string[] dictionary = dictionaryMatch.Groups[1].Value.Split('|');

        // 3. dc_bQZWKVgyCcO fonksiyonuna gönderilen diziyi yakalayalım
        // Bu dizi ["parça1", "parça2"...] formatındadır
        var arrayMatch = Regex.Match(evalBody, @"\[\s*([""'].*?[""']\s*,?\s*)*\s*\]", RegexOptions.Singleline);
        if (!arrayMatch.Success) return "Hata: Şifreli parça listesi ([...]) bulunamadı.";

        var matches = Regex.Matches(arrayMatch.Value, @"[""'](?<val>.*?)[""']");

        string base62Lookup = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        StringBuilder fullBase64 = new StringBuilder();

        foreach (Match m in matches)
        {
            string code = m.Groups["val"].Value;

            // Her bir parçayı (Örn: "O+N") sözlükten çöz
            // Kelime karakterlerini bulup Base62 indexine göre sözlükten karşılığını getir
            string decodedPart = Regex.Replace(code, @"\w+", match =>
            {
                int index = ConvertBase62ToIndex(match.Value, base62Lookup);
                return (index < dictionary.Length && !string.IsNullOrEmpty(dictionary[index]))
                        ? dictionary[index]
                        : match.Value;
            });

            fullBase64.Append(decodedPart);
        }

        string fullString = fullBase64.ToString();
        fullString = fullString.Replace("\\/", "/");
        return fullString;
    }

    private int ConvertBase62ToIndex(string value, string lookup)
    {
        int result = 0;
        int p = 1;
        for (int i = value.Length - 1; i >= 0; i--)
        {
            int charIndex = lookup.IndexOf(value[i]);
            if (charIndex == -1) return 0; // Geçersiz karakter
            result += charIndex * p;
            p *= 62;
        }
        return result;
    }

    private string Base64DecodeJS(string input)
    {
        try
        {
            // Base64 padding hatalarını önlemek için (Eksik '=' karakterlerini tamamla)
            string paddedInput = input.PadRight(input.Length + (4 - input.Length % 4) % 4, '=');
            byte[] bytes = Convert.FromBase64String(paddedInput);

            // Binary string yapısını korumak için Latin1 (ISO-8859-1) kullanılır
            return Encoding.GetEncoding("ISO-8859-1").GetString(bytes);
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    private static string Base64Decode(string input)
    {
        try
        {
            // C# için eksik padding (=) tamamlama
            string padded = input.PadRight(input.Length + (4 - input.Length % 4) % 4, '=');
            byte[] bytes = Convert.FromBase64String(padded);
            return Encoding.GetEncoding("ISO-8859-1").GetString(bytes);
        }
        catch
        {
            return "";
        }
    }

    public List<SubtitleModel>? GetTracks(string html)
    {
        try
        {
            // Regex: 'tracks:' ile başlayan ve '],' ile biten JSON dizisini yakalar
            var regex = new Regex(@"tracks:\s*(\[.*?\])\s*,", RegexOptions.Singleline);
            var match = regex.Match(html);

            if (match.Success)
            {
                // JSON içindeki kaçış karakterlerini ( \/ ) temizlemek için:
                string json = match.Groups[1].Value.Replace("\\/", "/");
                var tracks = JsonSerializer.Deserialize<List<RapidrameTrack>>(json);
                if (tracks is not null)
                {
                    var subtitles = new List<SubtitleModel>();

                    // (?i) -> Büyük/küçük harf duyarsız (Case-insensitive)
                    // \.(srt|vtt|ass|ssa|sub)$ -> Belirtilen uzantılarla bitenler
                    string pattern = @"(?i)\.(srt|vtt|ass|ssa|sub)$";

                    foreach (var track in tracks)
                    {
                        if (Regex.IsMatch(track.file, pattern))
                        {
                            subtitles.Add(new() { Name = track.label, Url = FixUrl(track.file, MainUrl) });
                        }

                    }

                    if(subtitles.Any()) return subtitles;
                }
            }
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, ex.ToString());
        }

        return null;
    }

    public override async Task<VideoSourceModel?> ExtractAsync(VideoSourceModel videoSource, string? referer = null)
    {
        try
        {
            //referer = "https://www.hdfilmcehennemi.nl/the-super-mario-galaxy-movie-2026/";
            string userAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 15.7; rv:135.0) Gecko/20100101 Firefox/135.0";

            var headers = new Dictionary<string, string>();
            headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            headers.Add("Content-Type", "application/json");
            headers.Add("User-Agent", userAgent);
            headers.Add("X-Requested-With", "XMLHttpRequest");
            headers.Add("Referer", referer ?? "https://hdfilmcehennemi.nl/");

            if (!videoSource.Url.EndsWith("/"))
            {
                videoSource.Url = videoSource.Url + "/";
            }

            if (referer is not null)
            {
                if (!referer.EndsWith("/")) referer = referer + "/";
                videoSource.Referer = referer;
            }

            videoSource.Headers = new()
            {
                new(){ Name = "User-Agent", Value = userAgent}
            };

            string? html = await HttpGet(url: videoSource.Url, referer: referer, headers: headers, identifier: TlsClient.Core.Models.Entities.TlsClientIdentifier.Cloudscraper);
            //Log(LogLevel.Debug, "html: " + html);
            if (html is null) return null;

            string base64 = GetBase64FromHtml(html);
            Log(LogLevel.Debug, "base64: " + base64);

            string? videoLink = Decrypt1(base64);
            Log(LogLevel.Debug, "videoLink1: " + videoLink);

            if (videoLink is null)
            {
                videoLink = Decrypt2(base64);
                Log(LogLevel.Debug, "videoLink2: " + videoLink);
            }

            if (videoLink is null)
            {
                videoLink = Decrypt3(base64);
                Log(LogLevel.Debug, "videoLink3: " + videoLink);
            }

            if (videoLink is not null)
            {
                videoSource.Url = videoLink;

                var tracks = GetTracks(html);
                videoSource.Subtitles = tracks;

                return videoSource;
            }
            else
            {
                return null;
            }
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, ex.ToString());
        }

        return null;
    }
}

public class RapidrameTrack
{
    public string file { get; set; } = string.Empty;
    public string kind { get; set; } = string.Empty;
    public string label { get; set; } = string.Empty;

    //public bool @default { get; set; } // 'default' C# içinde anahtar kelime olduğu için @ eklenir

    /* string json = TrackExtractor.GetTracksJson(htmlSource);
    var tracks = JsonSerializer.Deserialize<List<Track>>(json);

    foreach (var track in tracks)
    {
        Console.WriteLine($"Dil: {track.label} - Link: {track.file}");
    } */
}
