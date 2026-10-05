using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MayaJaal.SecurityCenter.Converters;

public sealed class StringToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrWhiteSpace(hex))
        {
            try
            {
                return (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
            }
            catch
            {
                // fall through
            }
        }

        return new SolidColorBrush(Color.FromRgb(0x88, 0x92, 0xa4));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
