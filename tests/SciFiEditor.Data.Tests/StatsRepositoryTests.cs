using FluentAssertions;

namespace SciFiEditor.Data.Tests;

public class StatsRepositoryTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly ProjectDatabase _database;
    private readonly StatsRepository _repository;

    public StatsRepositoryTests()
    {
        _database = new ProjectDatabase(_temp.Path);
        _repository = new StatsRepository(_database);
    }

    public void Dispose()
    {
        _database.Dispose();
        _temp.Dispose();
    }

    [Fact]
    public void UpsertDailyStat_ThenGetByDate_ReturnsSameValues()
    {
        var date = new DateOnly(2026, 7, 1);

        _repository.UpsertDailyStat(date, wordCountTotal: 1000, wordsWritten: 250);

        var stat = _repository.GetByDate(date);
        stat.Should().NotBeNull();
        stat!.WordCountTotal.Should().Be(1000);
        stat.WordsWritten.Should().Be(250);
    }

    [Fact]
    public void UpsertDailyStat_IsIdempotent_OverwritesSameDate()
    {
        var date = new DateOnly(2026, 7, 1);
        _repository.UpsertDailyStat(date, 1000, 250);

        _repository.UpsertDailyStat(date, 1200, 450);

        var stat = _repository.GetByDate(date)!;
        stat.WordCountTotal.Should().Be(1200);
        stat.WordsWritten.Should().Be(450);
    }

    [Fact]
    public void GetRange_ReturnsStatsInChronologicalOrder()
    {
        _repository.UpsertDailyStat(new DateOnly(2026, 7, 3), 3000, 100);
        _repository.UpsertDailyStat(new DateOnly(2026, 7, 1), 1000, 100);
        _repository.UpsertDailyStat(new DateOnly(2026, 7, 2), 2000, 100);

        var range = _repository.GetRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 3));

        range.Select(s => s.Date).Should().ContainInOrder(
            new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 2), new DateOnly(2026, 7, 3));
    }

    [Fact]
    public void GetRange_ExcludesDatesOutsideBounds()
    {
        _repository.UpsertDailyStat(new DateOnly(2026, 6, 30), 500, 500);
        _repository.UpsertDailyStat(new DateOnly(2026, 7, 1), 1000, 500);
        _repository.UpsertDailyStat(new DateOnly(2026, 7, 2), 1500, 500);

        var range = _repository.GetRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 2));

        range.Should().HaveCount(2);
        range.Should().NotContain(s => s.Date == new DateOnly(2026, 6, 30));
    }

    [Fact]
    public void GetMostRecentBefore_ReturnsLatestPriorDate_SkippingGaps()
    {
        _repository.UpsertDailyStat(new DateOnly(2026, 7, 1), 1000, 1000);
        // Gap on 2026-07-02 and 2026-07-03 (no entry)

        var mostRecent = _repository.GetMostRecentBefore(new DateOnly(2026, 7, 4));

        mostRecent.Should().NotBeNull();
        mostRecent!.Date.Should().Be(new DateOnly(2026, 7, 1));
        mostRecent.WordCountTotal.Should().Be(1000);
    }

    [Fact]
    public void GetMostRecentBefore_ReturnsNull_WhenNoEarlierEntryExists()
    {
        _repository.UpsertDailyStat(new DateOnly(2026, 7, 1), 1000, 1000);

        _repository.GetMostRecentBefore(new DateOnly(2026, 7, 1)).Should().BeNull();
    }

    [Fact]
    public void GetGoals_ReturnsBothNull_WhenNeverSet()
    {
        var goals = _repository.GetGoals();

        goals.DailyGoal.Should().BeNull();
        goals.SessionGoal.Should().BeNull();
    }

    [Fact]
    public void SetGoals_ThenGetGoals_RoundTrips()
    {
        _repository.SetGoals(500, 250);

        var goals = _repository.GetGoals();
        goals.DailyGoal.Should().Be(500);
        goals.SessionGoal.Should().Be(250);
    }

    [Fact]
    public void SetGoals_Twice_OverwritesPreviousValues()
    {
        _repository.SetGoals(500, 250);

        _repository.SetGoals(800, null);

        var goals = _repository.GetGoals();
        goals.DailyGoal.Should().Be(800);
        goals.SessionGoal.Should().BeNull();
    }
}
