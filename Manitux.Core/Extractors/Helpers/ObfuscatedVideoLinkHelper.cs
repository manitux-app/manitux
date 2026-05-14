using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Manitux.Core.Extractors.Utils;

namespace Manitux.Core.Extractors.Helpers;

public static class ObfuscatedVideoLinkHelper
{
    private const long DefaultSalt = 399756995;
    private static readonly Regex AnyUrlRegex = new(@"https?://[^\s""'|<>]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string? TryDecrypt(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        foreach (var strategy in GetStrategies())
        {
            try
            {
                var decoded = strategy(input);
                var url = GetUrl(decoded);
                if (!string.IsNullOrWhiteSpace(url)) return url;
            }
            catch
            {
            }
        }

        return null;
    }

    public static List<string> GetBase64CandidatesFromHtml(string html)
    {
        var candidates = new List<string>();

        AddDirectArrayValues(html, candidates);
        AddQuotedBase64Values(html, candidates);

        var unpacked = JsUnpacker.Unpack(html);
        if (!string.IsNullOrWhiteSpace(unpacked))
        {
            AddDirectArrayValues(unpacked, candidates);
            AddQuotedBase64Values(unpacked, candidates);
        }

        foreach (Match evalMatch in Regex.Matches(html, @"eval\(function\(p,a,c,k,e,[rd]\).*?\.split\(['""]\|['""]\),\d+,\{\}\)\)", RegexOptions.Singleline))
        {
            AddPackedArrayValues(evalMatch.Value, candidates);
        }

        return candidates
            .Select(x => x.Replace("\\/", "/", StringComparison.Ordinal).Trim())
            .Where(IsLikelyEncryptedPayload)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static IEnumerable<Func<string, string?>> GetStrategies()
    {
        yield return input => Unmix(ApplyRot13(Base64DecodeJs(Reverse(input))));
        yield return input => Unmix(Reverse(Base64DecodeJs(ApplyRot13(input))));
        yield return input => Unmix(Reverse(ApplyRot13(Base64DecodeJs(input))));
        yield return input => Unmix(Base64DecodeJs(Base64DecodeJs(Reverse(input))));
        yield return input => Unmix(Reverse(ApplyRot13(Base64DecodeJs(input))));
        yield return input => Unmix(Base64DecodeJs(Reverse(ApplyRot13(input))));
    }

    private static string? GetUrl(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        var match = AnyUrlRegex.Match(CleanUrl(input));
        return match.Success ? match.Value : null;
    }

    private static void AddDirectArrayValues(string html, List<string> candidates)
    {
        var regex = new Regex(@"\b\w+\s*\(\s*(\[\s*(?:""[^""]*""|'[^']*')\s*(?:,\s*(?:""[^""]*""|'[^']*')\s*)*\])\s*\)", RegexOptions.Singleline);

        foreach (Match match in regex.Matches(html))
        {
            var arrayText = match.Groups[1].Value;
            var parsed = TryParseStringArray(arrayText);
            if (!string.IsNullOrWhiteSpace(parsed))
            {
                candidates.Add(parsed);
                continue;
            }

            var fallbackParts = Regex.Matches(arrayText, @"[""']([^""']+)[""']")
                .Select(x => x.Groups[1].Value);
            candidates.Add(string.Concat(fallbackParts));
        }
    }

    private static void AddQuotedBase64Values(string html, List<string> candidates)
    {
        foreach (Match match in Regex.Matches(html, @"[""'](?<value>(?:={0,2})[A-Za-z0-9+/]{80,}={0,2})[""']", RegexOptions.Singleline))
        {
            candidates.Add(match.Groups["value"].Value);
        }
    }

    private static void AddPackedArrayValues(string evalBody, List<string> candidates)
    {
        var dictionaryMatch = Regex.Match(evalBody, @"'([^']*)'\.split", RegexOptions.Singleline);
        if (!dictionaryMatch.Success) dictionaryMatch = Regex.Match(evalBody, @"""([^""]*)""\.split", RegexOptions.Singleline);
        if (!dictionaryMatch.Success) return;

        var dictionary = dictionaryMatch.Groups[1].Value.Split('|');
        const string base62Lookup = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

        foreach (Match arrayMatch in Regex.Matches(evalBody, @"\[\s*([""'].*?[""']\s*,?\s*)+\s*\]", RegexOptions.Singleline))
        {
            var parts = Regex.Matches(arrayMatch.Value, @"[""'](?<val>.*?)[""']");
            var fullBase64 = new StringBuilder();

            foreach (Match part in parts)
            {
                var decodedPart = Regex.Replace(part.Groups["val"].Value, @"\w+", match =>
                {
                    var index = ConvertBase62ToIndex(match.Value, base62Lookup);
                    return index < dictionary.Length && !string.IsNullOrEmpty(dictionary[index])
                        ? dictionary[index]
                        : match.Value;
                });

                fullBase64.Append(decodedPart);
            }

            candidates.Add(fullBase64.ToString());
        }
    }

    private static string? TryParseStringArray(string arrayText)
    {
        try
        {
            var normalizedArrayText = Regex.Replace(arrayText, @"'([^'\\]*(?:\\.[^'\\]*)*)'", match =>
            {
                return JsonSerializer.Serialize(match.Groups[1].Value);
            });

            var parts = JsonSerializer.Deserialize<List<string>>(normalizedArrayText);
            return parts is null ? null : string.Concat(parts);
        }
        catch
        {
            return null;
        }
    }

    private static bool IsLikelyEncryptedPayload(string input)
    {
        if (input.Length < 40) return false;

        var base64Chars = input.Count(c => char.IsLetterOrDigit(c) || c is '+' or '/' or '=');
        return (double)base64Chars / input.Length > 0.85;
    }

    private static string Base64DecodeJs(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        try
        {
            var normalized = input.Trim();
            var paddedInput = normalized.PadRight(normalized.Length + (4 - normalized.Length % 4) % 4, '=');
            var bytes = Convert.FromBase64String(paddedInput);
            return Encoding.GetEncoding("ISO-8859-1").GetString(bytes);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string Reverse(string input)
    {
        var charArray = input.ToCharArray();
        Array.Reverse(charArray);
        return new string(charArray);
    }

    private static string ApplyRot13(string input)
    {
        return string.Concat(input.Select(c =>
        {
            if (c is >= 'a' and <= 'z') return (char)((c - 'a' + 13) % 26 + 'a');
            if (c is >= 'A' and <= 'Z') return (char)((c - 'A' + 13) % 26 + 'A');
            return c;
        }));
    }

    private static string Unmix(string input)
    {
        var unmix = new StringBuilder();

        for (var i = 0; i < input.Length; i++)
        {
            var offset = (int)(DefaultSalt % (i + 5));
            var decodedCharCode = (input[i] - offset + 256) % 256;
            unmix.Append((char)decodedCharCode);
        }

        return unmix.ToString();
    }

    private static string CleanUrl(string value)
    {
        return value
            .Replace("\\/", "/", StringComparison.Ordinal)
            .Replace("\\u0026", "&", StringComparison.Ordinal)
            .Replace("u0026", "&", StringComparison.Ordinal);
    }

    private static int ConvertBase62ToIndex(string value, string lookup)
    {
        var result = 0;
        var multiplier = 1;

        for (var i = value.Length - 1; i >= 0; i--)
        {
            var charIndex = lookup.IndexOf(value[i]);
            if (charIndex < 0) return 0;

            result += charIndex * multiplier;
            multiplier *= 62;
        }

        return result;
    }
}
