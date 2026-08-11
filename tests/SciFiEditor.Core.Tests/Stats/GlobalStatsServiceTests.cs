using FluentAssertions;
using SciFiEditor.Core.Stats;
using SciFiEditor.Data;

namespace SciFiEditor.Core.Tests.Stats;

public class GlobalStatsServiceTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly GlobalActivityDatabase _database;
    private readonly GlobalStatsService _service;

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Now);

    public GlobalStatsServiceTests()
    {
        _database = new GlobalActivityDatabase(Path.Combine(_temp.Path, "global-activity.db"));
        _service = new GlobalStatsService(new GlobalActivityRepository(_database));
    }

    public void Dispose()
    {
        _database.Dispose();
        _temp.Dispose();
    }

    [Fact]
    public void RecordProjectDay_MultipleProjects_SumsIntoHeatmapData()
    {
        _service.RecordProjectDay(@"C:\Projects\Novel", Today, 250);
        _service.RecordProjectDay(@"C:\Projects\Shorts", Today, 100);

        var heatmap = _service.GetHeatmapData();

        heatmap.Should().Contain(s => s.Date == Today && s.WordsWritten == 350);
    }

    [Fact]
    public void GetHeatmapData_ExcludesEntriesOutsideWindow()
    {
        _service.RecordProjectDay(@"C:\Projects\Novel", Today, 100);
        _service.RecordProjectDay(@"C:\Projects\Novel", Today.AddDays(-200), 50);

        var heatmap = _service.GetHeatmapData();

        heatmap.Should().Contain(s => s.Date == Today);
        heatmap.Should().NotContain(s => s.Date == Today.AddDays(-200));
    }

    [Fact]
    public void GetStreak_ConsecutiveDaysAcrossDifferentProjects_CountsCorrectly()
    {
        _service.RecordProjectDay(@"C:\Projects\Novel", Today, 100);
        _service.RecordProjectDay(@"C:\Projects\Shorts", Today.AddDays(-1), 100);
        _service.RecordProjectDay(@"C:\Projects\Novel", Today.AddDays(-2), 100);

        _service.GetStreak().Should().Be(3);
    }

    [Fact]
    public void GetStreak_GapBreaksStreak()
    {
        _service.RecordProjectDay(@"C:\Projects\Novel", Today, 100);
        _service.RecordProjectDay(@"C:\Projects\Novel", Today.AddDays(-1), 100);
        _service.RecordProjectDay(@"C:\Projects\Novel", Today.AddDays(-3), 100);

        _service.GetStreak().Should().Be(2);
    }

    [Fact]
    public void GetStreak_TodayNotWrittenYet_DoesNotBreakExistingStreak()
    {
        _service.RecordProjectDay(@"C:\Projects\Novel", Today.AddDays(-1), 100);
        _service.RecordProjectDay(@"C:\Projects\Novel", Today.AddDays(-2), 100);

        _service.GetStreak().Should().Be(2);
    }

    [Fact]
    public void GetStreak_NoData_ReturnsZero()
    {
        _service.GetStreak().Should().Be(0);
    }

    [Fact]
    public void GetTotalDaysWritten_CountsDistinctDaysAcrossProjects()
    {
        _service.RecordProjectDay(@"C:\Projects\Novel", Today, 100);
        _service.RecordProjectDay(@"C:\Projects\Shorts", Today, 50);
        _service.RecordProjectDay(@"C:\Projects\Novel", Today.AddDays(-1), 100);

        _service.GetTotalDaysWritten().Should().Be(2);
    }
}
