using FluentAssertions;
using SciFiEditor.Core.Entities;
using SciFiEditor.Core.Projects;
using SciFiEditor.Data;
using SciFiEditor.Domain;

namespace SciFiEditor.Core.Tests.Entities;

public class EntityServiceTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly ProjectService _projectService;
    private readonly EntityService _entityService;

    public EntityServiceTests()
    {
        var recentProjects = new RecentProjectsService(Path.Combine(_temp.Path, "recent.json"));
        _projectService = new ProjectService(recentProjects, new ProjectBackupService());
        _entityService = new EntityService(_projectService);

        _projectService.CreateProject(_temp.Path, "EntityNovel");
    }

    public void Dispose()
    {
        _projectService.Current?.Dispose();
        _temp.Dispose();
    }

    [Fact]
    public void AddEntity_ThenGetAll_ReturnsIt()
    {
        var entity = _entityService.AddEntity(EntityType.Character, "Ada", "Engineer");

        _entityService.GetAll().Should().ContainSingle(e => e.Id == entity.Id);
    }

    [Fact]
    public void UpdateEntity_PersistsChanges()
    {
        var entity = _entityService.AddEntity(EntityType.Character, "Ada", "Old");

        _entityService.UpdateEntity(entity.Id, "Ada Lovelace", "New");

        _entityService.GetAll().Single(e => e.Id == entity.Id).Name.Should().Be("Ada Lovelace");
    }

    [Fact]
    public void DeleteEntity_RemovesIt()
    {
        var entity = _entityService.AddEntity(EntityType.Character, "Ada", "");

        _entityService.DeleteEntity(entity.Id);

        _entityService.GetAll().Should().BeEmpty();
    }

    [Fact]
    public void SetLinksForNode_ThenGetForNode_RoundTrips()
    {
        var ada = _entityService.AddEntity(EntityType.Character, "Ada", "");
        var mars = _entityService.AddEntity(EntityType.Location, "Mars Base", "");
        var nodeId = Guid.NewGuid();

        _entityService.SetLinksForNode(nodeId, [ada.Id, mars.Id]);

        _entityService.GetForNode(nodeId).Select(e => e.Name).Should().BeEquivalentTo("Ada", "Mars Base");
    }

    [Fact]
    public void SetLinksForNode_ReplacesPreviousLinks()
    {
        var ada = _entityService.AddEntity(EntityType.Character, "Ada", "");
        var mars = _entityService.AddEntity(EntityType.Location, "Mars Base", "");
        var nodeId = Guid.NewGuid();
        _entityService.SetLinksForNode(nodeId, [ada.Id, mars.Id]);

        _entityService.SetLinksForNode(nodeId, [mars.Id]);

        _entityService.GetForNode(nodeId).Should().ContainSingle(e => e.Name == "Mars Base");
    }

    [Fact]
    public void GetAll_FiltersByType()
    {
        _entityService.AddEntity(EntityType.Character, "Ada", "");
        _entityService.AddEntity(EntityType.Location, "Mars Base", "");

        _entityService.GetAll(EntityType.Location).Should().ContainSingle(e => e.Name == "Mars Base");
    }
}
