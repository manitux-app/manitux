using System;

namespace Manitux.Core;

public static class StringExtensions
{
    public static string SubstringBefore(this string source, string delimiter)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(delimiter)) 
            return source;
            
        int index = source.IndexOf(delimiter);
        return index == -1 ? source : source.Substring(0, index);
    }

    public static string? ExtractAttributeValue(this string text, string attributePrefix)
    {
        if (string.IsNullOrEmpty(text)) return null;

        return text.Split(',')
            .Select(parts => parts.Trim())
            .FirstOrDefault(parts => parts.StartsWith(attributePrefix))
            ?.Split('=')
            .ElementAtOrDefault(1)
            ?.Trim('"');
    }
}

