using FluentAssertions;

namespace SciFiEditor.Data.Tests;

public class GlobalActivityRepositoryTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly GlobalActivityDatabase _database;
    private readonly GlobalActivityRepository _repository;

    public GlobalActivityRepositoryTests()
    {
        _database = new GlobalActivityDatabase(Path.Combine(_temp.Path, "global-activity.db"));
        _repository = new GlobalActivityRepository(_database);
    }

    public void Dispose()
    {
        _database.Dispose();
        _temp.Dispose();
    }

    [Fact]
    public void UpsertProjectDay_ThenGetRange_ReturnsSameValue()
    {
        var date = new DateOnly(2026, 7, 1);

        _repository.UpsertProjectDay(@"C:\Projects\Novel", date, 250);

        var range = _repository.GetRange(date, date);
        range.Should().ContainSingle();
        range[0].WordsWritten.Should().Be(250);
    }

    [Fact]
    public void UpsertProjectDay_MultipleProjectsSameDay_SumsAcrossProjects()
    {
        var date = new DateOnly(2026, 7, 1);
        _repository.UpsertProjectDay(@"C:\Projects\Novel", date, 250);
        _repository.UpsertProjectDay(@"C:\Projects\Shorts", date, 100);

        var range = _repository.GetRange(date, date);

        range.Should().ContainSingle();
        range[0].WordsWritten.Should().Be(350);
    }

    [Fact]
    public void UpsertProjectDay_IsIdempotentPerProject_OverwritesRatherThanAccumulates()
    {
        var date = new DateOnly(2026, 7, 1);
        _repository.UpsertProjectDay(@"C:\Projects\Novel", date, 250);

        _repository.UpsertProjectDay(@"C:\Projects\Novel", date, 400);

        var range = _repository.GetRange(date, date);
        range.Should().ContainSingle();
        range[0].WordsWritten.Should().Be(400);
    }

    [Fact]
    public void GetRange_ExcludesDatesOutsideBounds()
    {
        _repository.UpsertProjectDay(@"C:\Projects\Novel", new DateOnly(2026, 6, 30), 500);
        _repository.UpsertProjectDay(@"C:\Projects\Novel", new DateOnly(2026, 7, 1), 500);

        var range = _repository.GetRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 1));

        range.Should().ContainSingle();
        range[0].Date.Should().Be(new DateOnly(2026, 7, 1));
    }

    [Fact]
    public void GetWordsWrittenOn_SumsAcrossProjects()
    {
        var date = new DateOnly(2026, 7, 1);
        _repository.UpsertProjectDay(@"C:\Projects\Novel", date, 250);
        _repository.UpsertProjectDay(@"C:\Projects\Shorts", date, 100);

        _repository.GetWordsWrittenOn(date).Should().Be(350);
    }

    [Fact]
    public void GetWordsWrittenOn_NoData_ReturnsZero()
    {
        _repository.GetWordsWrittenOn(new DateOnly(2026, 7, 1)).Should().Be(0);
    }

    [Fact]
    public void GetTotalDaysWithActivity_CountsDistinctDaysAcrossProjects()
    {
        _repository.UpsertProjectDay(@"C:\Projects\Novel", new DateOnly(2026, 7, 1), 100);
        _repository.UpsertProjectDay(@"C:\Projects\Shorts", new DateOnly(2026, 7, 1), 50);
        _repository.UpsertProjectDay(@"C:\Projects\Novel", new DateOnly(2026, 7, 2), 100);

        _repository.GetTotalDaysWithActivity().Should().Be(2);
    }

    [Fact]
    public void GetTotalDaysWithActivity_ExcludesDaysWithNonPositiveTotal()
    {
        _repository.UpsertProjectDay(@"C:\Projects\Novel", new DateOnly(2026, 7, 1), 0);

        _repository.GetTotalDaysWithActivity().Should().Be(0);
    }
}
