using System.Globalization;
using System.Windows.Data;

namespace SciFiEditor.App.Converters;

public sealed class FocusModeMinWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool isFocusMode && isFocusMode
            ? 0.0
            : double.Parse((string)parameter, CultureInfo.InvariantCulture);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
