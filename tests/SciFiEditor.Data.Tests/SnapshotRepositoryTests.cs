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

        _repository.HasSnapshotToday(nodeId, "Auto", DateOnly.FromDateTime(DateTime.UtcNow)).Should().BeFalse();
    }

    [Fact]
    public void HasSnapshotToday_ReturnsTrue_AfterInsertingTodaysSnapshot()
    {
        var nodeId = Guid.NewGuid();
        _repository.Insert(nodeId, DateTime.UtcNow, "Auto", "content");

        _repository.HasSnapshotToday(nodeId, "Auto", DateOnly.FromDateTime(DateTime.UtcNow)).Should().BeTrue();
    }

    [Fact]
    public void HasSnapshotToday_ReturnsFalse_ForDifferentLabel()
    {
        var nodeId = Guid.NewGuid();
        _repository.Insert(nodeId, DateTime.UtcNow, "Manual", "content");

        _repository.HasSnapshotToday(nodeId, "Auto", DateOnly.FromDateTime(DateTime.UtcNow)).Should().BeFalse();
    }

    [Fact]
    public void HasSnapshotToday_ReturnsFalse_ForOlderDate()
    {
        var nodeId = Guid.NewGuid();
        _repository.Insert(nodeId, DateTime.UtcNow.AddDays(-3), "Auto", "content");

        _repository.HasSnapshotToday(nodeId, "Auto", DateOnly.FromDateTime(DateTime.UtcNow)).Should().BeFalse();
    }
}
