using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace SQLiteExplorer.Converters;

public class HasErrorToColorConverter : IValueConverter
{
    public static HasErrorToColorConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool hasError)
        {
            return hasError ? Brushes.Red : Brushes.Green;
        }
        return Brushes.Green;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
