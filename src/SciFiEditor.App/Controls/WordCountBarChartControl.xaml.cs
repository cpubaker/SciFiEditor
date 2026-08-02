using System.Globalization;
using System.Windows;
using SciFiEditor.Domain;

namespace SciFiEditor.App.Controls;

public partial class WordCountBarChartControl : System.Windows.Controls.UserControl
{
    private const double MaxBarHeight = 100;
    private const double MinBarHeight = 1;

    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(IReadOnlyList<DailyStat>), typeof(WordCountBarChartControl),
        new PropertyMetadata(null, OnDataChanged));

    public WordCountBarChartControl()
    {
        InitializeComponent();
    }

    public IReadOnlyList<DailyStat>? Data
    {
        get => (IReadOnlyList<DailyStat>?)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((WordCountBarChartControl)d).Rebuild();

    private void Rebuild()
    {
        var data = Data;
        if (data is null || data.Count == 0)
        {
            BarsItemsControl.ItemsSource = null;
            return;
        }

        var max = Math.Max(1, data.Max(s => s.WordsWritten));
        var bars = data
            .Select(stat => new BarCell(
                Math.Max(MinBarHeight, stat.WordsWritten <= 0 ? MinBarHeight : (double)stat.WordsWritten / max * MaxBarHeight),
                $"{stat.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}: {stat.WordsWritten} слів"))
            .ToList();

        BarsItemsControl.ItemsSource = bars;
    }

    private sealed record BarCell(double Height, string Tooltip);
}
