using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Tascade.Converters
{
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is bool flag ? !flag : true;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is bool flag ? !flag : false;
        }
    }
}
