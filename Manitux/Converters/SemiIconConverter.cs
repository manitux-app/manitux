using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Metadata;
using Ursa.Controls;

namespace Manitux.Converters;

public class SemiIconConverter: IValueConverter
{
    [Content]
    public Dictionary<string, Geometry> Paths { get; set; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return null;
        if (value is string s)
        {
            return Paths.TryGetValue(s, out var path)? path: AvaloniaProperty.UnsetValue;
        }
        return AvaloniaProperty.UnsetValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}