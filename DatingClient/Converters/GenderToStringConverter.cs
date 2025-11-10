using System.Globalization;
using DatingClient.Models;

namespace DatingClient.Converters;

public class GenderToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Gender gender)
            return "";
        var mode = parameter?.ToString()?.ToLowerInvariant();
        return mode switch
        {
            "accusative" => gender switch
            {
                Gender.Male => "Мужчину",
                Gender.Female => "Женщину",
                _ => "Не указано"
            },
            _ => gender switch
            {
                Gender.Male => "Мужской",
                Gender.Female => "Женский",
                _ => "Не указан"
            }
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            "Мужской" or "Мужчину" => Gender.Male,
            "Женский" or "Женщину" => Gender.Female,
            _ => Gender.Unknown
        };
    }
}
