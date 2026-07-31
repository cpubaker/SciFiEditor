using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SciFiEditor.App.Converters;

public sealed class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool isEditing && isEditing ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        !(value is Visibility visibility && visibility == Visibility.Visible);
}
