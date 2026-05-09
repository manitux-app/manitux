using System.Text;
using System.Text.RegularExpressions;

namespace Manitux.Core.Extractors.Utils;

public sealed class JsHunter
{
    private static readonly Regex DetectRegex = new(@"eval\(function\(h,u,n,t,e,r\)", RegexOptions.Compiled);
    private static readonly Regex HunterRegex = new(@"\}\(""([^""]+)"",[^,]+,\s*""([^""]+)"",\s*(\d+),\s*(\d+)", RegexOptions.Compiled | RegexOptions.Singleline);
    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ+/";

    private readonly string _hunterJs;

    public JsHunter(string hunterJs)
    {
        _hunterJs = hunterJs;
    }

    public bool Detect()
    {
        return !string.IsNullOrWhiteSpace(_hunterJs) && DetectRegex.IsMatch(_hunterJs);
    }

    public string? Dehunt()
    {
        if (string.IsNullOrWhiteSpace(_hunterJs)) return null;

        try
        {
            var match = HunterRegex.Match(_hunterJs);
            if (!match.Success || match.Groups.Count != 5) return null;

            var h = match.Groups[1].Value;
            var n = match.Groups[2].Value;
            var t = int.Parse(match.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture);
            var e = int.Parse(match.Groups[4].Value, System.Globalization.CultureInfo.InvariantCulture);

            return Hunter(h, n, t, e);
        }
        catch
        {
            return null;
        }
    }

    public static bool IsHunterPacked(string script)
    {
        return new JsHunter(script).Detect();
    }

    public static string? Dehunt(string script)
    {
        return new JsHunter(script).Dehunt();
    }

    private static int Duf(string value, int sourceBase, int targetBase = 10)
    {
        var sourceAlphabet = Alphabet.Take(sourceBase).ToArray();
        var targetAlphabet = Alphabet.Take(targetBase).ToArray();
        var total = 0d;
        var reversed = value.Reverse().ToArray();

        for (var index = 0; index < reversed.Length; index++)
        {
            var digit = Array.IndexOf(sourceAlphabet, reversed[index]);
            if (digit >= 0)
            {
                total += digit * Math.Pow(sourceBase, index);
            }
        }

        var converted = new StringBuilder();
        while (total > 0)
        {
            var remainder = (int)(total % targetBase);
            converted.Insert(0, targetAlphabet[remainder]);
            total = (total - remainder) / targetBase;
        }

        return int.TryParse(converted.ToString(), out var parsed) ? parsed : 0;
    }

    private static string Hunter(string h, string n, int t, int e)
    {
        var result = new StringBuilder();
        var i = 0;

        while (i < h.Length)
        {
            var s = new StringBuilder();
            while (i < h.Length && h[i] != n[e])
            {
                s.Append(h[i]);
                i++;
            }

            var value = s.ToString();
            for (var j = 0; j < n.Length; j++)
            {
                value = value.Replace(n[j], (char)('0' + j));
            }

            result.Append((char)(Duf(value, e) - t));
            i++;
        }

        return result.ToString();
    }
}
