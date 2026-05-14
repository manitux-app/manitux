using System.Globalization;
using System.Text.RegularExpressions;

namespace Manitux.Core.Extractors.Utils;

public static class SubtitleHelper
{
    private static readonly Regex GarbageRegex = new(
        @"\([^)]*(?:dub|sub|original|audio|code)[^)]*\)|[\u064B-\u065B]|\d|[^\p{L}\p{Mn}\p{Mc}\p{Me} ()-]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex IetfTagRegex = new(
        @"(^[xX](-[A-Za-z0-9]{1,8})*$)|((^[A-Za-z]{2,8}(?=-|$)){1}((-[A-Za-z]{3})(?=-|$)){0,3}(-[A-Za-z]{4}(?=-|$))?(-([A-Za-z]{2}|\d{3})(?=-|$))?(-(\d[A-Za-z0-9]{3}|[A-Za-z0-9]{5,8})(?=-|$))*((-([a-wyzA-WYZ](?=-))(-([A-Za-z0-9]{2,8})+)*))*(-[xX](-[A-Za-z0-9]{1,8})*)?)$",
        RegexOptions.Compiled);

    private static readonly Regex CountryRegex = new(@"[-_](\p{Alnum}{2,3})$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex UnicodeFlagRegex = new(@"[\uD83C\uDDE6-\uD83C\uDDFF]{2}", RegexOptions.Compiled);

    private static readonly Dictionary<string, string> LangToCountry = new(StringComparer.OrdinalIgnoreCase)
    {
        ["aa"] = "ET", ["af"] = "ZA", ["ak"] = "GH", ["am"] = "ET", ["ar"] = "AE",
        ["as"] = "IN", ["az"] = "AZ", ["be"] = "BY", ["bg"] = "BG", ["bn"] = "BD",
        ["bo"] = "CN", ["br"] = "FR", ["bs"] = "BA", ["ca"] = "ES", ["cs"] = "CZ",
        ["cy"] = "GB", ["da"] = "DK", ["de"] = "DE", ["dz"] = "BT", ["el"] = "GR",
        ["en"] = "GB", ["es"] = "ES", ["et"] = "EE", ["eu"] = "ES", ["fa"] = "IR",
        ["fi"] = "FI", ["fil"] = "PH", ["fr"] = "FR", ["ga"] = "IE", ["gd"] = "GB",
        ["gl"] = "ES", ["gu"] = "IN", ["ha"] = "NG", ["haw"] = "US", ["he"] = "IL",
        ["hi"] = "IN", ["hr"] = "HR", ["hu"] = "HU", ["hy"] = "AM", ["id"] = "ID",
        ["ig"] = "NG", ["is"] = "IS", ["it"] = "IT", ["ja"] = "JP", ["jv"] = "ID",
        ["ka"] = "GE", ["kk"] = "KZ", ["km"] = "KH", ["kn"] = "IN", ["ko"] = "KR",
        ["ku"] = "IQ", ["ky"] = "KG", ["lo"] = "LA", ["lt"] = "LT", ["lv"] = "LV",
        ["mk"] = "MK", ["ml"] = "IN", ["mn"] = "MN", ["mr"] = "IN", ["ms"] = "MY",
        ["my"] = "MM", ["nb"] = "NO", ["ne"] = "NP", ["nl"] = "NL", ["nn"] = "NO",
        ["no"] = "NO", ["or"] = "IN", ["pa"] = "IN", ["pl"] = "PL", ["pt"] = "PT",
        ["pt-br"] = "BR", ["ro"] = "RO", ["ru"] = "RU", ["sd"] = "PK", ["si"] = "LK",
        ["sk"] = "SK", ["sl"] = "SI", ["so"] = "SO", ["sq"] = "AL", ["sr"] = "RS",
        ["sv"] = "SE", ["sw"] = "TZ", ["ta"] = "IN", ["te"] = "IN", ["th"] = "TH",
        ["tl"] = "PH", ["tr"] = "TR", ["uk"] = "UA", ["ur"] = "PK", ["uz"] = "UZ",
        ["vi"] = "VN", ["zh"] = "CN", ["zh-hans"] = "CN", ["zh-hant"] = "TW", ["zu"] = "ZA"
    };

    public static string GetCurrentLocale()
    {
        return CultureInfo.CurrentCulture.Name;
    }

    public static string? FromLanguageToTwoLetters(string input, bool looseCheck)
    {
        return GetLanguageDataFromName(input, looseCheck)?.Iso6391;
    }

    public static string? FromLanguageToThreeLetters(string input)
    {
        return GetLanguageDataFromName(input)?.Iso6393;
    }

    public static string? FromLanguageToTagIetf(string? languageName, bool halfMatch = false)
    {
        return GetLanguageDataFromName(languageName, halfMatch)?.IetfTag;
    }

    public static string? FromTwoLettersToLanguage(string input)
    {
        return GetLanguageDataFromCode(input)?.LanguageName;
    }

    public static string? FromThreeLettersToLanguage(string input)
    {
        return GetLanguageDataFromCode(input)?.LanguageName;
    }

    public static string? FromTagToLanguageName(string? languageCode, string? localizedTo = null)
    {
        return GetLanguageDataFromCode(languageCode)?.LocalizedName(localizedTo);
    }

    public static string? FromTagToEnglishLanguageName(string? languageCode)
    {
        return GetLanguageDataFromCode(languageCode)?.LanguageName;
    }

    public static string? FromCodeToOpenSubtitlesTag(string? languageCode)
    {
        return GetLanguageDataFromCode(languageCode)?.OpenSubtitles;
    }

    public static string? FromCodeToLangTagIetf(string? languageCode)
    {
        return GetLanguageDataFromCode(languageCode)?.IetfTag;
    }

    public static bool IsWellFormedTagIetf(string? langTagIetf)
    {
        return !string.IsNullOrWhiteSpace(langTagIetf)
               && langTagIetf.Length >= 2
               && IetfTagRegex.IsMatch(langTagIetf);
    }

    public static string? GetFlagFromIso(string? input)
    {
        if (string.IsNullOrWhiteSpace(input) || input.Length < 2) return null;

        var country = CountryRegex.Match(input).Success ? CountryRegex.Match(input).Groups[1].Value : null;
        var flagEmoji =
            GetFlagFromCountry2Letters(LangToCountry.GetValueOrDefault(input.ToLowerInvariant()))
            ?? GetFlagFromCountry2Letters(country?.ToUpperInvariant())
            ?? GetFlagFromCountry2Letters(input.ToUpperInvariant())
            ?? GetFlagFromCountry2Letters(country is null ? null : LangToCountry.GetValueOrDefault(country.ToLowerInvariant()))
            ?? string.Empty;

        return UnicodeFlagRegex.IsMatch(flagEmoji) ? flagEmoji : null;
    }

    public static string? GetNameNextToFlagEmoji(string? languageCode, string? localizedTo = null)
    {
        return GetLanguageDataFromCode(languageCode)?.NameNextToFlagEmoji(localizedTo);
    }

    private static LanguageMetadata? GetLanguageDataFromName(string? languageName, bool halfMatch = false)
    {
        if (string.IsNullOrWhiteSpace(languageName) || languageName.Length < 2) return null;

        var normalized = GarbageRegex.Replace(languageName.ToLowerInvariant(), string.Empty).Trim();
        var direct = Languages.FirstOrDefault(x =>
            string.Equals(x.LanguageName, normalized, StringComparison.OrdinalIgnoreCase)
            || string.Equals(x.NativeName, normalized, StringComparison.OrdinalIgnoreCase)
            || string.Equals(x.IetfTag, normalized, StringComparison.OrdinalIgnoreCase));

        if (direct is not null || !halfMatch) return direct;

        return Languages
            .Select(lang =>
            {
                var score = Math.Max(
                    SimilarityRatio(normalized, lang.LanguageName.ToLowerInvariant()),
                    SimilarityRatio(normalized, lang.NativeName.ToLowerInvariant()));

                if (normalized.Contains(lang.LanguageName, StringComparison.OrdinalIgnoreCase)
                    || normalized.Contains(lang.NativeName, StringComparison.OrdinalIgnoreCase))
                {
                    score = Math.Max(score, 90);
                }

                return (Language: lang, Score: score);
            })
            .Where(x => x.Score > 80)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Language)
            .FirstOrDefault();
    }

    private static LanguageMetadata? GetLanguageDataFromCode(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode) || languageCode.Length < 2) return null;

        var code = languageCode.ToLowerInvariant().Trim();
        return Languages.FirstOrDefault(x =>
            string.Equals(x.IetfTag, code, StringComparison.OrdinalIgnoreCase)
            || string.Equals(x.Iso6391, code, StringComparison.OrdinalIgnoreCase)
            || string.Equals(x.Iso6392B, code, StringComparison.OrdinalIgnoreCase)
            || string.Equals(x.Iso6393, code, StringComparison.OrdinalIgnoreCase)
            || string.Equals(x.OpenSubtitles, code, StringComparison.OrdinalIgnoreCase));
    }

    private static string? GetFlagFromCountry2Letters(string? countryLetters)
    {
        if (string.IsNullOrWhiteSpace(countryLetters) || countryLetters.Length != 2) return null;

        const int asciiOffset = 0x41;
        const int flagOffset = 0x1F1E6;
        const int offset = flagOffset - asciiOffset;

        var firstChar = char.ConvertFromUtf32(char.ToUpperInvariant(countryLetters[0]) + offset);
        var secondChar = char.ConvertFromUtf32(char.ToUpperInvariant(countryLetters[1]) + offset);
        return firstChar + secondChar;
    }

    private static int SimilarityRatio(string first, string second)
    {
        if (first.Length == 0 && second.Length == 0) return 100;
        if (first.Length == 0 || second.Length == 0) return 0;

        var distance = LevenshteinDistance(first, second);
        var max = Math.Max(first.Length, second.Length);
        return (int)Math.Round((1.0 - (double)distance / max) * 100);
    }

    private static int LevenshteinDistance(string first, string second)
    {
        var distances = new int[first.Length + 1, second.Length + 1];

        for (var i = 0; i <= first.Length; i++) distances[i, 0] = i;
        for (var j = 0; j <= second.Length; j++) distances[0, j] = j;

        for (var i = 1; i <= first.Length; i++)
        {
            for (var j = 1; j <= second.Length; j++)
            {
                var cost = first[i - 1] == second[j - 1] ? 0 : 1;
                distances[i, j] = Math.Min(
                    Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                    distances[i - 1, j - 1] + cost);
            }
        }

        return distances[first.Length, second.Length];
    }

    public sealed record LanguageMetadata(
        string LanguageName,
        string NativeName,
        string IetfTag,
        string Iso6391,
        string Iso6392B,
        string Iso6393,
        string OpenSubtitles)
    {
        public string LocalizedName(string? localizedTo = null)
        {
            try
            {
                var localeOfLanguage = CultureInfo.GetCultureInfo(IetfTag);
                var localizedName = localeOfLanguage.DisplayName;
                if (!string.IsNullOrWhiteSpace(localizedTo))
                {
                    localizedName = localeOfLanguage.GetNativeNameFallback();
                }

                return string.IsNullOrWhiteSpace(localizedName) || localizedName.Equals(IetfTag, StringComparison.OrdinalIgnoreCase)
                    ? NativeName
                    : localizedName;
            }
            catch
            {
                return NativeName;
            }
        }

        public string NameNextToFlagEmoji(string? localizedTo = null)
        {
            return $"{GetFlagFromIso(IetfTag) ?? "??"} {LocalizedName(localizedTo)}";
        }
    }

    private static string GetNativeNameFallback(this CultureInfo cultureInfo)
    {
        return cultureInfo.NativeName;
    }

    public static readonly List<LanguageMetadata> Languages = new()
    {
        new("Afrikaans", "Afrikaans", "af", "af", "afr", "afr", "af"),
        new("Albanian", "Shqip", "sq", "sq", "", "sqi", "sq"),
        new("Amharic", "Amharic", "am", "am", "amh", "amh", "am"),
        new("Arabic", "Arabic", "ar", "ar", "ara", "ara", "ar"),
        new("Armenian", "Armenian", "hy", "hy", "", "hye", "hy"),
        new("Azerbaijani", "Azarbaycan", "az", "az", "aze", "aze", "az-az"),
        new("Basque", "Euskara", "eu", "eu", "", "eus", "eu"),
        new("Belarusian", "Belarusian", "be", "be", "bel", "bel", "be"),
        new("Bengali", "Bangla", "bn", "bn", "ben", "ben", "bn"),
        new("Bosnian", "Bosanski", "bs", "bs", "bos", "bos", "bs"),
        new("Bulgarian", "Bulgarian", "bg", "bg", "bul", "bul", "bg"),
        new("Burmese", "Burmese", "my", "my", "", "mya", "my"),
        new("Catalan", "Catala", "ca", "ca", "cat", "cat", "ca"),
        new("Chinese", "Chinese", "zh", "zh", "chi", "zho", "ze"),
        new("Chinese (Cantonese)", "Cantonese", "yue", "", "", "yue", "zh-ca"),
        new("Chinese (simplified)", "Chinese Simplified", "zh-hans", "", "", "", "zh-cn"),
        new("Chinese (traditional)", "Chinese Traditional", "zh-hant", "", "", "", "zh-tw"),
        new("Croatian", "Hrvatski", "hr", "hr", "hrv", "hrv", "hr"),
        new("Czech", "Cestina", "cs", "cs", "", "ces", "cs"),
        new("Danish", "Dansk", "da", "da", "dan", "dan", "da"),
        new("Dutch", "Nederlands", "nl", "nl", "", "nld", "nl"),
        new("English", "English", "en", "en", "eng", "eng", "en"),
        new("Esperanto", "Esperanto", "eo", "eo", "epo", "epo", "eo"),
        new("Estonian", "Eesti", "et", "et", "est", "est", "et"),
        new("Finnish", "Suomi", "fi", "fi", "fin", "fin", "fi"),
        new("French", "Francais", "fr", "fr", "", "fra", "fr"),
        new("Galician", "Galego", "gl", "gl", "glg", "glg", "gl"),
        new("Georgian", "Georgian", "ka", "ka", "", "kat", "ka"),
        new("German", "Deutsch", "de", "de", "", "deu", "de"),
        new("Greek", "Greek", "el", "el", "", "ell", "el"),
        new("Hebrew", "Hebrew", "he", "iw", "heb", "heb", "he"),
        new("Hindi", "Hindi", "hi", "hi", "hin", "hin", "hi"),
        new("Hungarian", "Magyar", "hu", "hu", "hun", "hun", "hu"),
        new("Icelandic", "Islenska", "is", "is", "", "isl", "is"),
        new("Indonesian", "Bahasa Indonesia", "id", "in", "ind", "ind", "id"),
        new("Irish", "Gaeilge", "ga", "ga", "gle", "gle", "ga"),
        new("Italian", "Italiano", "it", "it", "ita", "ita", "it"),
        new("Japanese", "Japanese", "ja", "ja", "jpn", "jpn", "ja"),
        new("Kannada", "Kannada", "kn", "kn", "kan", "kan", "kn"),
        new("Kazakh", "Kazakh", "kk", "kk", "kaz", "kaz", "kk"),
        new("Khmer", "Khmer", "km", "km", "khm", "khm", "km"),
        new("Korean", "Korean", "ko", "ko", "kor", "kor", "ko"),
        new("Kurdish", "Kurdish", "ku", "ku", "kur", "kur", "ku"),
        new("Latvian", "Latviesu", "lv", "lv", "lav", "lav", "lv"),
        new("Lithuanian", "Lietuviu", "lt", "lt", "lit", "lit", "lt"),
        new("Macedonian", "Macedonian", "mk", "mk", "", "mkd", "mk"),
        new("Malay", "Bahasa Melayu", "ms", "ms", "", "msa", "ms"),
        new("Malayalam", "Malayalam", "ml", "ml", "mal", "mal", "ml"),
        new("Marathi", "Marathi", "mr", "mr", "mar", "mar", "mr"),
        new("Norwegian", "Norsk", "no", "no", "nor", "nor", "no"),
        new("Persian (Farsi)", "Farsi", "fa", "fa", "", "fas", "fa"),
        new("Polish", "Polski", "pl", "pl", "pol", "pol", "pl"),
        new("Portuguese", "Portugues", "pt", "pt", "por", "por", "pt-pt"),
        new("Portuguese (Brazil)", "Portugues Brasil", "pt-br", "", "", "", "pt-br"),
        new("Romanian", "Romana", "ro", "ro", "", "ron", "ro"),
        new("Russian", "Russian", "ru", "ru", "rus", "rus", "ru"),
        new("Serbian", "Srpski", "sr", "sr", "srp", "srp", "sr"),
        new("Slovak", "Slovencina", "sk", "sk", "", "slk", "sk"),
        new("Slovenian", "Slovenscina", "sl", "sl", "slv", "slv", "sl"),
        new("Spanish", "Espanol", "es", "es", "spa", "spa", "es"),
        new("Spanish (Europe)", "Espanol Europa", "es-es", "", "", "", "sp"),
        new("Spanish (Latin America)", "Espanol Latinoamerica", "es-419", "", "", "", "ea"),
        new("Swedish", "Svenska", "sv", "sv", "swe", "swe", "sv"),
        new("Tagalog", "Tagalog", "tl", "tl", "", "tgl", "tl"),
        new("Tamil", "Tamil", "ta", "ta", "tam", "tam", "ta"),
        new("Telugu", "Telugu", "te", "te", "tel", "tel", "te"),
        new("Thai", "Thai", "th", "th", "tha", "tha", "th"),
        new("Turkish", "Turkce", "tr", "tr", "tur", "tur", "tr"),
        new("Ukrainian", "Ukrainian", "uk", "uk", "ukr", "ukr", "uk"),
        new("Urdu", "Urdu", "ur", "ur", "urd", "urd", "ur"),
        new("Vietnamese", "Tieng Viet", "vi", "vi", "vie", "vie", "vi"),
        new("Welsh", "Cymraeg", "cy", "cy", "", "cym", "cy"),
        new("Zulu", "Zulu", "zu", "zu", "zul", "zul", "")
    };
}
