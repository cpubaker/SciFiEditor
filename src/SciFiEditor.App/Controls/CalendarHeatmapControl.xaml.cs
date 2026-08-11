using System.Globalization;
using System.Windows;
using System.Windows.Media;
using SciFiEditor.Domain;

namespace SciFiEditor.App.Controls;

public partial class CalendarHeatmapControl : System.Windows.Controls.UserControl
{
    /// <summary>Rendered width of one week-column: cell Width (12) plus its 1px margin on each side.</summary>
    public const double ColumnWidth = 14;

    private static readonly Brush EmptyBrush = FreezeBrush(Color.FromRgb(0xB8, 0xA7, 0x88));
    private static readonly Brush LowBrush = FreezeBrush(Color.FromRgb(0x9B, 0xE9, 0xA8));
    private static readonly Brush HighBrush = FreezeBrush(Color.FromRgb(0x40, 0xC4, 0x63));
    private static readonly Brush FullBrush = FreezeBrush(Color.FromRgb(0x21, 0x6E, 0x39));

    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(IReadOnlyList<DailyStat>), typeof(CalendarHeatmapControl),
        new PropertyMetadata(null, OnDataOrGoalChanged));

    public static readonly DependencyProperty DailyGoalProperty = DependencyProperty.Register(
        nameof(DailyGoal), typeof(int?), typeof(CalendarHeatmapControl),
        new PropertyMetadata(null, OnDataOrGoalChanged));

    public static readonly DependencyProperty DaysProperty = DependencyProperty.Register(
        nameof(Days), typeof(int), typeof(CalendarHeatmapControl),
        new PropertyMetadata(182, OnDataOrGoalChanged));

    public CalendarHeatmapControl()
    {
        InitializeComponent();
    }

    public IReadOnlyList<DailyStat>? Data
    {
        get => (IReadOnlyList<DailyStat>?)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    public int? DailyGoal
    {
        get => (int?)GetValue(DailyGoalProperty);
        set => SetValue(DailyGoalProperty, value);
    }

    public int Days
    {
        get => (int)GetValue(DaysProperty);
        set => SetValue(DaysProperty, value);
    }

    private static void OnDataOrGoalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((CalendarHeatmapControl)d).Rebuild();

    private void Rebuild()
    {
        var byDate = (Data ?? Array.Empty<DailyStat>()).ToDictionary(s => s.Date);
        var lastDate = DateOnly.FromDateTime(DateTime.Now);
        var firstDate = lastDate.AddDays(-(Days - 1));
        var gridStart = firstDate.AddDays(-DayOfWeekMondayFirst(firstDate.DayOfWeek));

        var weeks = new List<List<HeatmapCell>>();
        List<HeatmapCell>? currentWeek = null;

        for (var cursor = gridStart; cursor <= lastDate; cursor = cursor.AddDays(1))
        {
            if (DayOfWeekMondayFirst(cursor.DayOfWeek) == 0)
            {
                currentWeek = [];
                weeks.Add(currentWeek);
            }

            currentWeek!.Add(CreateCell(cursor, cursor < firstDate ? null : byDate.GetValueOrDefault(cursor)));
        }

        WeeksItemsControl.ItemsSource = weeks;
    }

    private HeatmapCell CreateCell(DateOnly date, DailyStat? stat)
    {
        if (stat is null)
        {
            return new HeatmapCell(EmptyBrush, string.Empty);
        }

        var tooltip = $"{date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}: {stat.WordsWritten} слів";
        return new HeatmapCell(BrushFor(stat.WordsWritten, DailyGoal), tooltip);
    }

    private static Brush BrushFor(int wordsWritten, int? goal)
    {
        if (wordsWritten <= 0)
        {
            return EmptyBrush;
        }

        if (goal is > 0)
        {
            var ratio = (double)wordsWritten / goal.Value;
            return ratio switch
            {
                >= 1.0 => FullBrush,
                >= 0.5 => HighBrush,
                _ => LowBrush
            };
        }

        return wordsWritten switch
        {
            >= 1000 => FullBrush,
            >= 300 => HighBrush,
            _ => LowBrush
        };
    }

    private static int DayOfWeekMondayFirst(DayOfWeek day) => ((int)day + 6) % 7;

    private static Brush FreezeBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private sealed record HeatmapCell(Brush Brush, string Tooltip);
}
