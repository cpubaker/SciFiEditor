using FluentAssertions;
using SciFiEditor.Domain;

namespace SciFiEditor.Data.Tests;

public class EntityRepositoryTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly ProjectDatabase _database;
    private readonly EntityRepository _repository;

    public EntityRepositoryTests()
    {
        _database = new ProjectDatabase(_temp.Path);
        _repository = new EntityRepository(_database);
    }

    public void Dispose()
    {
        _database.Dispose();
        _temp.Dispose();
    }

    [Fact]
    public void Insert_ThenGetAll_ReturnsTheEntity()
    {
        var entity = _repository.Insert(EntityType.Character, "Ada", "A brilliant engineer.");

        _repository.GetAll().Should().ContainSingle(e => e.Id == entity.Id && e.Name == "Ada");
    }

    [Fact]
    public void GetAll_FiltersByType()
    {
        _repository.Insert(EntityType.Character, "Ada", "");
        _repository.Insert(EntityType.Location, "Mars Base", "");

        var characters = _repository.GetAll(EntityType.Character);

        characters.Should().ContainSingle(e => e.Name == "Ada");
        characters.Should().NotContain(e => e.Name == "Mars Base");
    }

    [Fact]
    public void Update_PersistsNewNameAndDescription()
    {
        var entity = _repository.Insert(EntityType.Character, "Ada", "Old description.");

        _repository.Update(entity.Id, "Ada Lovelace", "New description.", DateTime.UtcNow);

        var updated = _repository.GetAll().Single(e => e.Id == entity.Id);
        updated.Name.Should().Be("Ada Lovelace");
        updated.Description.Should().Be("New description.");
    }

    [Fact]
    public void Delete_RemovesEntityAndItsLinks()
    {
        var entity = _repository.Insert(EntityType.Character, "Ada", "");
        var nodeId = Guid.NewGuid();
        _repository.SetLinksForNode(nodeId, [entity.Id]);

        _repository.Delete(entity.Id);

        _repository.GetAll().Should().BeEmpty();
        _repository.GetForNode(nodeId).Should().BeEmpty();
    }

    [Fact]
    public void SetLinksForNode_ThenGetForNode_ReturnsLinkedEntities()
    {
        var ada = _repository.Insert(EntityType.Character, "Ada", "");
        var mars = _repository.Insert(EntityType.Location, "Mars Base", "");
        var nodeId = Guid.NewGuid();

        _repository.SetLinksForNode(nodeId, [ada.Id, mars.Id]);

        var linked = _repository.GetForNode(nodeId);
        linked.Select(e => e.Name).Should().BeEquivalentTo("Ada", "Mars Base");
    }

    [Fact]
    public void SetLinksForNode_CalledAgainWithSmallerSet_RemovesDroppedLinks()
    {
        var ada = _repository.Insert(EntityType.Character, "Ada", "");
        var mars = _repository.Insert(EntityType.Location, "Mars Base", "");
        var nodeId = Guid.NewGuid();
        _repository.SetLinksForNode(nodeId, [ada.Id, mars.Id]);

        _repository.SetLinksForNode(nodeId, [ada.Id]);

        var linked = _repository.GetForNode(nodeId);
        linked.Should().ContainSingle(e => e.Name == "Ada");
    }

    [Fact]
    public void SetLinksForNode_WithEmptySet_ClearsAllLinks()
    {
        var ada = _repository.Insert(EntityType.Character, "Ada", "");
        var nodeId = Guid.NewGuid();
        _repository.SetLinksForNode(nodeId, [ada.Id]);

        _repository.SetLinksForNode(nodeId, []);

        _repository.GetForNode(nodeId).Should().BeEmpty();
    }
}
