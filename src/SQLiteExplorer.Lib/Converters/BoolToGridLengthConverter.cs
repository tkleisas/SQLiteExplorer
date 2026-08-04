using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace SQLiteExplorer.Lib.Converters;

/// <summary>
/// Maps a bool to a GridLength: true → the given star weight (default 1*), false → 0.
/// Used to collapse grid rows/columns whose panel is hidden (star-sized tracks
/// would otherwise keep consuming space).
/// </summary>
public sealed class BoolToGridLengthConverter : IValueConverter
{
    public static readonly BoolToGridLengthConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var weight = 1.0;
        if (parameter is string s && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var w))
        {
            weight = w;
        }

        return value is true
            ? new GridLength(weight, GridUnitType.Star)
            : new GridLength(0);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
