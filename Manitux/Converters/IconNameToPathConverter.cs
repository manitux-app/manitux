using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Manitux.Converters;

public class IconNameToPathConverter : IValueConverter
{
    // private readonly string[] _paths =
    // [
    //     "M2,21V3H7L12,12L17,3H22V21H18V8L12,18L6,8V21H2Z", // M
    //     "M12,2L3,21H7.5L9,17H15L16.5,21H21L12,2M12,8L14,13H10L12,8Z", // A
    //     "M4,21V3H9L15,15V3H19V21H14L8,9V21H4Z", // N
    //     "M9,3H15V21H9V3Z", // I
    //     "M5,3H19V7H15V21H9V7H5V3Z", // T
    //     "M4,3V15A6,6 0 0,0 10,21H14A6,6 0 0,0 20,15V3H16V15A2,2 0 0,1 14,17H10A2,2 0 0,1 8,15V3H4Z", // U
    //     "M3,3H7.5L12,10.5L16.5,3H21L14,12L21,21H16.5L12,13.5L7.5,21H3L10,12L3,3Z" // X
    // ];

    // private readonly string[] _paths =
    // [
    //     "M12,4A4,4 0 0,1 16,8A4,4 0 0,1 12,12A4,4 0 0,1 8,8A4,4 0 0,1 12,4M12,14C16.42,14 20,15.79 20,18V20H4V18C4,15.79 7.58,14 12,14Z",
    //     "M16 12L9 2L2 12H3.86L0 18H7V22H11V18H18L14.14 12H16M20.14 12H22L15 2L12.61 5.41L17.92 13H15.97L19.19 18H24L20.14 12M13 19H17V22H13V19Z",
    //     "M15,9H5V5H15M12,19A3,3 0 0,1 9,16A3,3 0 0,1 12,13A3,3 0 0,1 15,16A3,3 0 0,1 12,19M17,3H5C3.89,3 3,3.9 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V7L17,3Z",
    //     "M5 21C3.9 21 3 20.1 3 19V5C3 3.9 3.9 3 5 3H19C20.1 3 21 3.9 21 5V19C21 20.1 20.1 21 19 21H5M15.3 16L13.2 13.9L17 10L14.2 7.2L10.4 11.1L8.2 8.9V16H15.3Z",
    //     "M16,9V7H12V12.5C11.58,12.19 11.07,12 10.5,12A2.5,2.5 0 0,0 8,14.5A2.5,2.5 0 0,0 10.5,17A2.5,2.5 0 0,0 13,14.5V9H16M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2Z",
    //     "M16.67,4H15V2H9V4H7.33A1.33,1.33 0 0,0 6,5.33V20.67C6,21.4 6.6,22 7.33,22H16.67A1.33,1.33 0 0,0 18,20.67V5.33C18,4.6 17.4,4 16.67,4Z",
    //     "M12 2C6.5 2 2 6.5 2 12C2 17.5 6.5 22 12 22C17.5 22 22 17.5 22 12S17.5 2 12 2M12.5 13H11V7H12.5V11.3L16.2 9.2L17 10.5L12.5 13Z"
    // ];

    private readonly string[] _paths =
    [
        // https://www.svgrepo.com/collection/cornered-basic-ui-icons/
        // 0 - Settings
        "M32.26,52a2.92,2.92,0,0,1-2.37-1.21l-3.48-4.73h-.82L22.11,50.8a2.93,2.93,0,0,1-3.5,1L13,49.45a2.93,2.93,0,0,1-1.78-3.17l.89-5.8-.59-.59-5.8.89A2.93,2.93,0,0,1,2.55,39L.23,33.39a2.92,2.92,0,0,1,1-3.5l4.73-3.48v-.82L1.21,22.11a2.92,2.92,0,0,1-1-3.5L2.55,13a2.93,2.93,0,0,1,3.17-1.78l5.8.89.59-.59-.89-5.8A2.93,2.93,0,0,1,13,2.55L18.61.23a2.93,2.93,0,0,1,3.5,1l3.48,4.74h.82l3.48-4.73a2.93,2.93,0,0,1,3.5-1L39,2.55a2.93,2.93,0,0,1,1.78,3.17l-.89,5.8.59.59,5.8-.89A2.93,2.93,0,0,1,49.45,13l2.32,5.61a2.92,2.92,0,0,1-1,3.5l-4.73,3.48v.82l4.73,3.48a2.92,2.92,0,0,1,1,3.5L49.45,39a2.93,2.93,0,0,1-3.17,1.78l-5.8-.89-.59.59.89,5.8A2.93,2.93,0,0,1,39,49.45l-5.61,2.32A2.82,2.82,0,0,1,32.26,52Zm-17-5.93,4.09,1.69,3.3-4.49a2.94,2.94,0,0,1,2.37-1.21H27a3,3,0,0,1,2.37,1.2l3.3,4.5,4.09-1.69-.85-5.51A3,3,0,0,1,36.68,38L38,36.69a3,3,0,0,1,2.53-.83l5.51.85,1.69-4.09-4.49-3.3A2.94,2.94,0,0,1,42.06,27v-1.9a2.94,2.94,0,0,1,1.21-2.37l4.49-3.3L46.07,15.3l-5.51.84A3,3,0,0,1,38,15.31L36.69,14a3,3,0,0,1-.83-2.53l.85-5.51L32.62,4.24l-3.3,4.5A3,3,0,0,1,27,9.94h-1.9a2.94,2.94,0,0,1-2.37-1.21l-3.3-4.49L15.29,5.93l.85,5.51A3,3,0,0,1,15.31,14L14,15.31a3,3,0,0,1-2.53.83L5.93,15.3,4.24,19.38l4.49,3.3a2.94,2.94,0,0,1,1.21,2.37V27a2.94,2.94,0,0,1-1.21,2.37l-4.49,3.3,1.69,4.09,5.51-.85a2.94,2.94,0,0,1,2.53.83L15.31,38a2.94,2.94,0,0,1,.83,2.53Zm31.6-30.9Zm-.31-2h0ZM26,38A12,12,0,1,1,38,26,12,12,0,0,1,26,38Zm0-20a8,8,0,1,0,8,8A8,8,0,0,0,26,18Z",
        
        // 1 - Plus (+)
        "M19,11H13V5H11V11H5V13H11V19H13V13H19V11Z",

        // 2 - Play
        "M8,5V19L19,12L8,5Z"
    ];

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || _paths.Length == 0)
            return AvaloniaProperty.UnsetValue;

        int index;

        switch (value)
        {
            case int i:
                index = NormalizeIndex(i);
                break;

            case string str:
                index = MapStringToIndex(str);
                break;

            default:
                return AvaloniaProperty.UnsetValue;
        }

        return StreamGeometry.Parse(_paths[index]);
    }

    private int MapStringToIndex(string key)
    {
        return key.Trim().ToLowerInvariant() switch
        {
            "settings" => 0,
            "plus" => 1,
            "play" => 2,
            _ => 0
        };
    }

    private int NormalizeIndex(int i)
    {
        var mod = i % _paths.Length;
        return mod < 0 ? mod + _paths.Length : mod;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return AvaloniaProperty.UnsetValue;
    }
}