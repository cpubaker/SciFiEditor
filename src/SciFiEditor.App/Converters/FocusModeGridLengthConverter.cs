using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SciFiEditor.App.Converters;

public sealed class FocusModeGridLengthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isFocusMode && isFocusMode)
        {
            return new GridLength(0);
        }

        var text = parameter as string ?? "*";
        return (GridLength)new GridLengthConverter().ConvertFromString(text)!;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
