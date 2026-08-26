using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using JsonFileComparer.Core.Models;

namespace JsonFileComparer.App.Converters;

public sealed class DiffTypeToBrushConverter : IValueConverter
{
    public static readonly DiffTypeToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            DiffType.Added => Brushes.LimeGreen,
            DiffType.Removed => Brushes.OrangeRed,
            DiffType.Changed => Brushes.Gold,
            DiffType.TypeChanged => Brushes.MediumOrchid,
            DiffType.Unchanged => Brushes.Gray,
            _ => Brushes.White
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
