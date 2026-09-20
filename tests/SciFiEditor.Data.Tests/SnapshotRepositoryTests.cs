using FluentAssertions;

namespace SciFiEditor.Data.Tests;

public class SnapshotRepositoryTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly ProjectDatabase _database;
    private readonly SnapshotRepository _repository;

    public SnapshotRepositoryTests()
    {
        _database = new ProjectDatabase(_temp.Path);
        _repository = new SnapshotRepository(_database);
    }

    public void Dispose()
    {
        _database.Dispose();
        _temp.Dispose();
    }

    [Fact]
    public void Insert_ThenGetContent_RoundTripsThroughGzipIntact()
    {
        var nodeId = Guid.NewGuid();
        var content = "Once upon a time, there was a spaceship. " + new string('x', 5000);

        _repository.Insert(nodeId, DateTime.UtcNow, "Manual", content);
        var snapshot = _repository.GetByNode(nodeId).Single();

        _repository.GetContent(snapshot.Id).Should().Be(content);
    }

    [Fact]
    public void GetByNode_ReturnsOnlySnapshotsForThatNode_NewestFirst()
    {
        var nodeA = Guid.NewGuid();
        var nodeB = Guid.NewGuid();
        _repository.Insert(nodeA, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), "Manual", "old");
        _repository.Insert(nodeA, new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), "Manual", "new");
        _repository.Insert(nodeB, DateTime.UtcNow, "Manual", "other node");

        var history = _repository.GetByNode(nodeA);

        history.Should().HaveCount(2);
        history[0].CreatedAtUtc.Should().BeAfter(history[1].CreatedAtUtc);
    }

    [Fact]
    public void HasSnapshotToday_ReturnsFalse_WhenNoneExists()
    {
        var nodeId = Guid.NewGuid();

        _repository.HasSnapshotToday(nodeId, "Auto", DateOnly.FromDateTime(DateTime.Now)).Should().BeFalse();
    }

    [Fact]
    public void HasSnapshotToday_ReturnsTrue_AfterInsertingTodaysSnapshot()
    {
        var nodeId = Guid.NewGuid();
        _repository.Insert(nodeId, DateTime.UtcNow, "Auto", "content");

        _repository.HasSnapshotToday(nodeId, "Auto", DateOnly.FromDateTime(DateTime.Now)).Should().BeTrue();
    }

    [Fact]
    public void HasSnapshotToday_ReturnsFalse_ForDifferentLabel()
    {
        var nodeId = Guid.NewGuid();
        _repository.Insert(nodeId, DateTime.UtcNow, "Manual", "content");

        _repository.HasSnapshotToday(nodeId, "Auto", DateOnly.FromDateTime(DateTime.Now)).Should().BeFalse();
    }

    [Fact]
    public void HasSnapshotToday_ReturnsFalse_ForOlderDate()
    {
        var nodeId = Guid.NewGuid();
        _repository.Insert(nodeId, DateTime.UtcNow.AddDays(-3), "Auto", "content");

        _repository.HasSnapshotToday(nodeId, "Auto", DateOnly.FromDateTime(DateTime.Now)).Should().BeFalse();
    }

    // The real caller (SnapshotService.RecordAutoSnapshotIfNeeded) computes "today" from local time,
    // not UTC, while created_at_utc is stored in UTC. Near local midnight these two calendar dates
    // can differ depending on the machine's UTC offset. These two boundary instants -- local
    // day-start and local day-end -- reproduce that mismatch regardless of which direction (east or
    // west of UTC) the test machine's offset happens to be.
    [Fact]
    public void HasSnapshotToday_SnapshotJustAfterLocalMidnight_MatchesLocalToday()
    {
        var nodeId = Guid.NewGuid();
        var localToday = DateOnly.FromDateTime(DateTime.Now);
        var justAfterLocalMidnight = new DateTime(localToday.Year, localToday.Month, localToday.Day, 0, 0, 1, DateTimeKind.Local);
        _repository.Insert(nodeId, justAfterLocalMidnight.ToUniversalTime(), "Auto", "content");

        _repository.HasSnapshotToday(nodeId, "Auto", localToday).Should().BeTrue();
    }

    [Fact]
    public void HasSnapshotToday_SnapshotJustBeforeLocalMidnight_MatchesLocalToday()
    {
        var nodeId = Guid.NewGuid();
        var localToday = DateOnly.FromDateTime(DateTime.Now);
        var justBeforeNextLocalMidnight = new DateTime(localToday.Year, localToday.Month, localToday.Day, 23, 59, 59, DateTimeKind.Local);
        _repository.Insert(nodeId, justBeforeNextLocalMidnight.ToUniversalTime(), "Auto", "content");

        _repository.HasSnapshotToday(nodeId, "Auto", localToday).Should().BeTrue();
    }
}
