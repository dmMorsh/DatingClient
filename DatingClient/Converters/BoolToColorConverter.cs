using System.Globalization;

namespace DatingClient.Converters;

public class BoolToColorConverter : IValueConverter
{
    public Color MyMessageColor { get; set; } = Color.FromArgb("#4C6EF5"); // синий
    public Color OtherMessageColor { get; set; } = Color.FromArgb("#444"); // тёмно-серый

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isMine)
            return isMine ? MyMessageColor : OtherMessageColor;

        return OtherMessageColor;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}