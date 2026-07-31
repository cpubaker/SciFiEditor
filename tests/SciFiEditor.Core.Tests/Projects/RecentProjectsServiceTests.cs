using FluentAssertions;
using SciFiEditor.Core.Projects;
using SciFiEditor.Domain;

namespace SciFiEditor.Core.Tests.Projects;

public class RecentProjectsServiceTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly string _filePath;

    public RecentProjectsServiceTests()
    {
        _filePath = Path.Combine(_temp.Path, "recent-projects.json");
    }

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void GetAll_ReturnsEmpty_WhenFileDoesNotExist()
    {
        var service = new RecentProjectsService(_filePath);

        service.GetAll().Should().BeEmpty();
    }

    [Fact]
    public void AddOrUpdate_PersistsEntry_RoundTrip()
    {
        var service = new RecentProjectsService(_filePath);
        var entry = new RecentProjectEntry { Path = @"C:\Projects\Novel", DisplayName = "Novel", LastOpenedUtc = DateTime.UtcNow };

        service.AddOrUpdate(entry);
        var reloaded = new RecentProjectsService(_filePath);

        reloaded.GetAll().Should().ContainSingle(e => e.Path == entry.Path && e.DisplayName == "Novel");
    }

    [Fact]
    public void AddOrUpdate_MovesExistingEntryToFront_WhenPathAlreadyKnown()
    {
        var service = new RecentProjectsService(_filePath);
        service.AddOrUpdate(new RecentProjectEntry { Path = "A", DisplayName = "A" });
        service.AddOrUpdate(new RecentProjectEntry { Path = "B", DisplayName = "B" });

        service.AddOrUpdate(new RecentProjectEntry { Path = "A", DisplayName = "A-updated" });

        var all = service.GetAll();
        all.Should().HaveCount(2);
        all[0].DisplayName.Should().Be("A-updated");
        all[1].Path.Should().Be("B");
    }

    [Fact]
    public void AddOrUpdate_CapsAtTenEntries()
    {
        var service = new RecentProjectsService(_filePath);

        for (var i = 0; i < 12; i++)
        {
            service.AddOrUpdate(new RecentProjectEntry { Path = $"Project{i}", DisplayName = $"Project{i}" });
        }

        var all = service.GetAll();
        all.Should().HaveCount(10);
        all[0].Path.Should().Be("Project11");
    }

    [Fact]
    public void Remove_DeletesMatchingEntry()
    {
        var service = new RecentProjectsService(_filePath);
        service.AddOrUpdate(new RecentProjectEntry { Path = "A", DisplayName = "A" });
        service.AddOrUpdate(new RecentProjectEntry { Path = "B", DisplayName = "B" });

        service.Remove("A");

        service.GetAll().Should().ContainSingle(e => e.Path == "B");
    }
}
