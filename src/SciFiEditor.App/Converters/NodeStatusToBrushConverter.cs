using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using SciFiEditor.Domain;

namespace SciFiEditor.App.Converters;

public sealed class NodeStatusToBrushConverter : IValueConverter
{
    private static readonly Brush NoneBrush = Freeze(Color.FromRgb(0xB0, 0xB0, 0xB0));
    private static readonly Brush DraftBrush = Freeze(Color.FromRgb(0x40, 0x78, 0xC0));
    private static readonly Brush RevisedBrush = Freeze(Color.FromRgb(0xE0, 0x8E, 0x2A));
    private static readonly Brush FinalBrush = Freeze(Color.FromRgb(0x21, 0x6E, 0x39));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
    {
        NodeStatus.Draft => DraftBrush,
        NodeStatus.Revised => RevisedBrush,
        NodeStatus.Final => FinalBrush,
        _ => NoneBrush
    };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static Brush Freeze(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
