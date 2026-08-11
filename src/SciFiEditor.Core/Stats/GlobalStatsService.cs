using SciFiEditor.Data;
using SciFiEditor.Domain;

namespace SciFiEditor.Core.Stats;

public sealed class GlobalStatsService
{
    private const int HeatmapDays = 182;

    private readonly GlobalActivityRepository _repository;

    public GlobalStatsService(GlobalActivityRepository repository)
    {
        _repository = repository;
    }

    public void RecordProjectDay(string projectPath, DateOnly date, int wordsWritten) =>
        _repository.UpsertProjectDay(projectPath, date, wordsWritten);

    public IReadOnlyList<DailyStat> GetHeatmapData()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        return _repository.GetRange(today.AddDays(-(HeatmapDays - 1)), today);
    }

    public int GetStreak()
    {
        var cursor = DateOnly.FromDateTime(DateTime.Now);
        if (_repository.GetWordsWrittenOn(cursor) <= 0)
        {
            cursor = cursor.AddDays(-1);
        }

        var streak = 0;
        while (_repository.GetWordsWrittenOn(cursor) > 0)
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }

        return streak;
    }

    public int GetTotalDaysWritten() => _repository.GetTotalDaysWithActivity();
}
