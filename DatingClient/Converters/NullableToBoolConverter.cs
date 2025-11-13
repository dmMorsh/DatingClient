using System.Collections;
using System.Globalization;

namespace DatingClient.Converters;

public class NullableToBoolConverter: IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool result;

        if (value is IEnumerable enumerable)
            result = enumerable.Cast<object>().Any();
        else
            result = value is not null;

        var mode = parameter?.ToString()?.ToLowerInvariant();
        if (mode == "reverse") 
            result = !result;
        
        return result;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}