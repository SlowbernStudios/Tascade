using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Tascade.Services;

namespace Tascade.Converters
{
    public class SuggestionTypeToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var color = value is SuggestionType type
                ? type switch
                {
                    SuggestionType.Command => Color.Parse("#4E9A06"),
                    SuggestionType.FilePath => Color.Parse("#204A87"),
                    SuggestionType.Snippet => Color.Parse("#F57900"),
                    _ => Color.Parse("#555753")
                }
                : Color.Parse("#555753");

            return new SolidColorBrush(color);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
