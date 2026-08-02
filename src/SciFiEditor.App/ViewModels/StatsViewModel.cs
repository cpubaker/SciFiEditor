using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SciFiEditor.Core.Stats;
using SciFiEditor.Domain;

namespace SciFiEditor.App.ViewModels;

public sealed partial class StatsViewModel : ObservableObject
{
    private readonly WritingStatsService _statsService;

    public StatsViewModel(WritingStatsService statsService)
    {
        _statsService = statsService;
        Load();
    }

    [ObservableProperty]
    private IReadOnlyList<DailyStat> _heatmapData = [];

    [ObservableProperty]
    private IReadOnlyList<DailyStat> _chartData = [];

    [ObservableProperty]
    private int _streak;

    [ObservableProperty]
    private int? _dailyGoal;

    [ObservableProperty]
    private int? _sessionGoal;

    private void Load()
    {
        HeatmapData = _statsService.GetHeatmapData();
        ChartData = _statsService.GetChartData();
        Streak = _statsService.GetStreak();

        var goals = _statsService.GetGoals();
        DailyGoal = goals.DailyGoal;
        SessionGoal = goals.SessionGoal;
    }

    [RelayCommand]
    private void SaveGoals() => _statsService.SetGoals(DailyGoal, SessionGoal);
}
