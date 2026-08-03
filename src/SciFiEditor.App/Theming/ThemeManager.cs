using System.Windows;
using SciFiEditor.Domain;

namespace SciFiEditor.App.Theming;

public static class ThemeManager
{
    public static void Apply(AppTheme theme)
    {
        var uri = theme == AppTheme.Dark
            ? new Uri("Themes/DarkTheme.xaml", UriKind.Relative)
            : new Uri("Themes/LightTheme.xaml", UriKind.Relative);

        var dictionary = new ResourceDictionary { Source = uri };
        var mergedDictionaries = Application.Current.Resources.MergedDictionaries;

        for (var i = mergedDictionaries.Count - 1; i >= 0; i--)
        {
            var source = mergedDictionaries[i].Source?.OriginalString;
            if (source is not null && (source.EndsWith("LightTheme.xaml") || source.EndsWith("DarkTheme.xaml")))
            {
                mergedDictionaries.RemoveAt(i);
            }
        }

        mergedDictionaries.Add(dictionary);
    }
}
