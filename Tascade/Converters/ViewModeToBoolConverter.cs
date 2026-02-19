using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Tascade.Models;

namespace Tascade.Converters
{
    public class ViewModeToBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not ViewMode current || parameter is not string modeText)
            {
                return false;
            }

            return Enum.TryParse<ViewMode>(modeText, true, out var mode) && current == mode;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
