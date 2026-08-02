using SciFiEditor.Core.Manuscript;
using SciFiEditor.Core.Projects;
using SciFiEditor.Domain;

namespace SciFiEditor.Core.Stats;

public sealed class WritingStatsService
{
    private const int HeatmapDays = 182;
    private const int ChartDays = 30;

    private readonly ProjectService _projectService;
    private readonly NodeService _nodeService;

    public WritingStatsService(ProjectService projectService, NodeService nodeService)
    {
        _projectService = projectService;
        _nodeService = nodeService;
    }

    private OpenProject Project =>
        _projectService.Current ?? throw new InvalidOperationException("No project is open.");

    public void RecordSnapshot()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var currentTotal = _nodeService.GetProjectWordCount();
        var baseline = Project.Stats.GetMostRecentBefore(today)?.WordCountTotal ?? 0;
        var wordsWritten = currentTotal - baseline;
        Project.Stats.UpsertDailyStat(today, currentTotal, wordsWritten);
    }

    public IReadOnlyList<DailyStat> GetHeatmapData()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        return Project.Stats.GetRange(today.AddDays(-(HeatmapDays - 1)), today);
    }

    public IReadOnlyList<DailyStat> GetChartData()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        return Project.Stats.GetRange(today.AddDays(-(ChartDays - 1)), today);
    }

    public int GetStreak()
    {
        var cursor = DateOnly.FromDateTime(DateTime.Now);
        if (Project.Stats.GetByDate(cursor) is not { WordsWritten: > 0 })
        {
            cursor = cursor.AddDays(-1);
        }

        var streak = 0;
        while (Project.Stats.GetByDate(cursor) is { WordsWritten: > 0 })
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }

        return streak;
    }

    public ProjectGoals GetGoals() => Project.Stats.GetGoals();

    public void SetGoals(int? dailyGoal, int? sessionGoal) => Project.Stats.SetGoals(dailyGoal, sessionGoal);
}
