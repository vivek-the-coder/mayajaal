using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MayaJaal.SecurityCenter.Converters;

public sealed class StringToGeometryConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string data && !string.IsNullOrWhiteSpace(data))
        {
            try
            {
                return Geometry.Parse(data);
            }
            catch
            {
                // fall through
            }
        }

        return Geometry.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
