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

    public static readonly DependencyProperty DaysProperty = DependencyProperty.Register(
        nameof(Days), typeof(int), typeof(WordCountBarChartControl),
        new PropertyMetadata(30, OnDataChanged));

    public WordCountBarChartControl()
    {
        InitializeComponent();
    }

    public IReadOnlyList<DailyStat>? Data
    {
        get => (IReadOnlyList<DailyStat>?)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    public int Days
    {
        get => (int)GetValue(DaysProperty);
        set => SetValue(DaysProperty, value);
    }

    private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((WordCountBarChartControl)d).Rebuild();

    private void Rebuild()
    {
        var byDate = (Data ?? Array.Empty<DailyStat>()).ToDictionary(s => s.Date);
        var max = Math.Max(1, byDate.Values.Select(s => s.WordsWritten).DefaultIfEmpty(0).Max());
        var lastDate = DateOnly.FromDateTime(DateTime.Now);
        var firstDate = lastDate.AddDays(-(Days - 1));

        var bars = new List<BarCell>();
        for (var date = firstDate; date <= lastDate; date = date.AddDays(1))
        {
            var stat = byDate.GetValueOrDefault(date);
            var wordsWritten = stat?.WordsWritten ?? 0;
            var height = wordsWritten <= 0 ? MinBarHeight : Math.Max(MinBarHeight, (double)wordsWritten / max * MaxBarHeight);
            var tooltip = stat is null
                ? string.Empty
                : $"{date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}: {wordsWritten} слів";
            bars.Add(new BarCell(height, tooltip));
        }

        BarsItemsControl.ItemsSource = bars;
    }

    private sealed record BarCell(double Height, string Tooltip);
}
