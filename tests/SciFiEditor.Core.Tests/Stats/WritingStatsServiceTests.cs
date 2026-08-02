using FluentAssertions;
using SciFiEditor.Core.Manuscript;
using SciFiEditor.Core.Projects;
using SciFiEditor.Core.Stats;
using SciFiEditor.Data;
using SciFiEditor.Domain;

namespace SciFiEditor.Core.Tests.Stats;

public class WritingStatsServiceTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly ProjectService _projectService;
    private readonly NodeService _nodeService;
    private readonly WritingStatsService _statsService;

    public WritingStatsServiceTests()
    {
        var recentProjects = new RecentProjectsService(Path.Combine(_temp.Path, "recent.json"));
        _projectService = new ProjectService(recentProjects, new ProjectBackupService());
        var fileService = new ManuscriptFileService();
        _nodeService = new NodeService(_projectService, fileService);
        _statsService = new WritingStatsService(_projectService, _nodeService);

        _projectService.CreateProject(_temp.Path, "StatsNovel");
    }

    public void Dispose()
    {
        _projectService.Current?.Dispose();
        _temp.Dispose();
    }

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Now);

    [Fact]
    public void RecordSnapshot_FirstEverDay_WordsWrittenEqualsTotal()
    {
        var scene = _nodeService.AddNode(NodeType.Scene, "Scene", null);
        _nodeService.UpdateWordCounts(scene.Id, 400, 2000);

        _statsService.RecordSnapshot();

        var stat = _projectService.Current!.Stats.GetByDate(Today)!;
        stat.WordCountTotal.Should().Be(400);
        stat.WordsWritten.Should().Be(400);
    }

    [Fact]
    public void RecordSnapshot_ComputesDeltaAgainstMostRecentPriorSnapshot()
    {
        _projectService.Current!.Stats.UpsertDailyStat(Today.AddDays(-5), wordCountTotal: 1000, wordsWritten: 1000);
        var scene = _nodeService.AddNode(NodeType.Scene, "Scene", null);
        _nodeService.UpdateWordCounts(scene.Id, 1300, 6000);

        _statsService.RecordSnapshot();

        var stat = _projectService.Current!.Stats.GetByDate(Today)!;
        stat.WordCountTotal.Should().Be(1300);
        stat.WordsWritten.Should().Be(300);
    }

    [Fact]
    public void RecordSnapshot_CalledTwiceSameDay_StaysIdempotentAgainstSameBaseline()
    {
        _projectService.Current!.Stats.UpsertDailyStat(Today.AddDays(-1), wordCountTotal: 500, wordsWritten: 500);
        var scene = _nodeService.AddNode(NodeType.Scene, "Scene", null);
        _nodeService.UpdateWordCounts(scene.Id, 600, 3000);
        _statsService.RecordSnapshot();

        _nodeService.UpdateWordCounts(scene.Id, 800, 4000);
        _statsService.RecordSnapshot();

        var range = _projectService.Current!.Stats.GetRange(Today, Today);
        range.Should().ContainSingle();
        range[0].WordCountTotal.Should().Be(800);
        range[0].WordsWritten.Should().Be(300);
    }

    [Fact]
    public void GetStreak_ConsecutiveDaysWithWriting_CountsCorrectly()
    {
        var stats = _projectService.Current!.Stats;
        stats.UpsertDailyStat(Today, 300, 100);
        stats.UpsertDailyStat(Today.AddDays(-1), 200, 100);
        stats.UpsertDailyStat(Today.AddDays(-2), 100, 100);

        _statsService.GetStreak().Should().Be(3);
    }

    [Fact]
    public void GetStreak_GapBreaksStreak()
    {
        var stats = _projectService.Current!.Stats;
        stats.UpsertDailyStat(Today, 300, 100);
        stats.UpsertDailyStat(Today.AddDays(-1), 200, 100);
        stats.UpsertDailyStat(Today.AddDays(-3), 100, 100);

        _statsService.GetStreak().Should().Be(2);
    }

    [Fact]
    public void GetStreak_TodayNotWrittenYet_DoesNotBreakExistingStreak()
    {
        var stats = _projectService.Current!.Stats;
        stats.UpsertDailyStat(Today.AddDays(-1), 200, 100);
        stats.UpsertDailyStat(Today.AddDays(-2), 100, 100);

        _statsService.GetStreak().Should().Be(2);
    }

    [Fact]
    public void GetStreak_TodayWithZeroWordsWritten_TreatedAsNotWrittenYet()
    {
        var stats = _projectService.Current!.Stats;
        stats.UpsertDailyStat(Today, 500, 0);
        stats.UpsertDailyStat(Today.AddDays(-1), 500, 200);

        _statsService.GetStreak().Should().Be(1);
    }

    [Fact]
    public void GetStreak_NoData_ReturnsZero()
    {
        _statsService.GetStreak().Should().Be(0);
    }

    [Fact]
    public void GetGoals_And_SetGoals_RoundTripThroughService()
    {
        _statsService.SetGoals(500, 250);

        var goals = _statsService.GetGoals();
        goals.DailyGoal.Should().Be(500);
        goals.SessionGoal.Should().Be(250);
    }

    [Fact]
    public void GetHeatmapData_ExcludesEntriesOutsideWindow()
    {
        var stats = _projectService.Current!.Stats;
        stats.UpsertDailyStat(Today, 100, 100);
        stats.UpsertDailyStat(Today.AddDays(-200), 50, 50);

        var heatmap = _statsService.GetHeatmapData();

        heatmap.Should().Contain(s => s.Date == Today);
        heatmap.Should().NotContain(s => s.Date == Today.AddDays(-200));
    }

    [Fact]
    public void GetChartData_ExcludesEntriesOlderThan30Days()
    {
        var stats = _projectService.Current!.Stats;
        stats.UpsertDailyStat(Today, 100, 100);
        stats.UpsertDailyStat(Today.AddDays(-40), 50, 50);

        var chart = _statsService.GetChartData();

        chart.Should().Contain(s => s.Date == Today);
        chart.Should().NotContain(s => s.Date == Today.AddDays(-40));
    }
}
