using System.Text;
using System.Text.RegularExpressions;

namespace Manitux.Core.Extractors.Utils;

public sealed class JsUnpacker
{
    private static readonly Regex DetectRegex = new(@"eval\(function\(p,a,c,k,e,[rd]", RegexOptions.Compiled);
    private static readonly Regex PackedRegex = new(@"\}\s*\('(.*)',\s*(.*?),\s*(\d+),\s*'(.*?)'\.split\('\|'\)", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex WordRegex = new(@"\b[a-zA-Z0-9_]+\b", RegexOptions.Compiled);

    private readonly string? _packedJs;

    public JsUnpacker(string? packedJs)
    {
        _packedJs = packedJs;
    }

    public bool Detect()
    {
        if (string.IsNullOrWhiteSpace(_packedJs)) return false;

        var js = _packedJs.Replace(" ", string.Empty, StringComparison.Ordinal);
        return DetectRegex.IsMatch(js);
    }

    public string? Unpack()
    {
        if (string.IsNullOrWhiteSpace(_packedJs)) return null;

        try
        {
            var match = PackedRegex.Match(_packedJs);
            if (!match.Success || match.Groups.Count != 5) return null;

            var payload = match.Groups[1].Value.Replace("\\'", "'", StringComparison.Ordinal);
            var radix = int.TryParse(match.Groups[2].Value, out var parsedRadix) ? parsedRadix : 36;
            var count = int.TryParse(match.Groups[3].Value, out var parsedCount) ? parsedCount : 0;
            var symbols = match.Groups[4].Value.Split('|');

            if (symbols.Length != count)
            {
                return null;
            }

            var unbase = new Unbase(radix);
            var decoded = new StringBuilder(payload);
            var replaceOffset = 0;

            foreach (Match wordMatch in WordRegex.Matches(payload))
            {
                var word = wordMatch.Value;
                var symbolIndex = unbase.Decode(word);
                var value = symbolIndex >= 0 && symbolIndex < symbols.Length ? symbols[symbolIndex] : null;

                if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(word))
                {
                    decoded.Replace(
                        word,
                        value,
                        wordMatch.Index + replaceOffset,
                        wordMatch.Length);

                    replaceOffset += value.Length - word.Length;
                }
            }

            return decoded.ToString();
        }
        catch
        {
            return null;
        }
    }

    public static bool IsPacked(string? script)
    {
        return new JsUnpacker(script).Detect();
    }

    public static string? Unpack(string? script)
    {
        return new JsUnpacker(script).Unpack();
    }

    private sealed class Unbase
    {
        private const string Alphabet62 = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string Alphabet95 = " !\"#$%&\\'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";

        private readonly int _radix;
        private readonly Dictionary<char, int>? _dictionary;

        public Unbase(int radix)
        {
            _radix = radix;

            if (radix <= 36) return;

            string? alphabet = radix switch
            {
                < 62 => Alphabet62[..radix],
                62 => Alphabet62,
                >= 63 and <= 94 => Alphabet95[..radix],
                95 => Alphabet95,
                _ => null
            };

            if (alphabet is null) return;

            _dictionary = new Dictionary<char, int>(alphabet.Length);
            for (var i = 0; i < alphabet.Length; i++)
            {
                _dictionary[alphabet[i]] = i;
            }
        }

        public int Decode(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0;

            if (_dictionary is null)
            {
                return Convert.ToInt32(value, _radix);
            }

            var result = 0;
            var reversed = value.Reverse().ToArray();
            for (var i = 0; i < reversed.Length; i++)
            {
                if (!_dictionary.TryGetValue(reversed[i], out var digit)) continue;
                result += (int)(Math.Pow(_radix, i) * digit);
            }

            return result;
        }
    }
}
