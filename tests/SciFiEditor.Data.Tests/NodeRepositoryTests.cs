using FluentAssertions;
using SciFiEditor.Domain;

namespace SciFiEditor.Data.Tests;

public class NodeRepositoryTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly ProjectDatabase _database;
    private readonly NodeRepository _repository;

    public NodeRepositoryTests()
    {
        _database = new ProjectDatabase(_temp.Path);
        _repository = new NodeRepository(_database);
    }

    public void Dispose()
    {
        _database.Dispose();
        _temp.Dispose();
    }

    private static ManuscriptNode CreateNode(NodeType type = NodeType.Scene, Guid? parentId = null, int sortOrder = 1000, string title = "Node") =>
        new()
        {
            Id = Guid.NewGuid(),
            ParentId = parentId,
            NodeType = type,
            Title = title,
            SortOrder = sortOrder,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

    [Fact]
    public void Insert_ThenGetById_ReturnsEquivalentNode()
    {
        var node = CreateNode(title: "Chapter One");

        _repository.Insert(node);
        var fetched = _repository.GetById(node.Id);

        fetched.Should().NotBeNull();
        fetched!.Title.Should().Be("Chapter One");
        fetched.NodeType.Should().Be(NodeType.Scene);
        fetched.ParentId.Should().BeNull();
    }

    [Fact]
    public void GetAll_ReturnsNodesOrderedBySortOrder()
    {
        _repository.Insert(CreateNode(sortOrder: 3000, title: "Third"));
        _repository.Insert(CreateNode(sortOrder: 1000, title: "First"));
        _repository.Insert(CreateNode(sortOrder: 2000, title: "Second"));

        var all = _repository.GetAll();

        all.Select(n => n.Title).Should().ContainInOrder("First", "Second", "Third");
    }

    [Fact]
    public void UpdateTitle_PersistsNewTitle()
    {
        var node = CreateNode(title: "Old Title");
        _repository.Insert(node);

        _repository.UpdateTitle(node.Id, "New Title", DateTime.UtcNow);

        _repository.GetById(node.Id)!.Title.Should().Be("New Title");
    }

    [Fact]
    public void MoveToTrash_ReparentsUnderTrashAndRecordsOriginalParent()
    {
        var parent = CreateNode(NodeType.Folder, title: "Parent");
        var child = CreateNode(NodeType.Scene, parentId: parent.Id, title: "Child");
        _repository.Insert(parent);
        _repository.Insert(child);

        _repository.MoveToTrash(child.Id, DateTime.UtcNow);

        var updated = _repository.GetById(child.Id)!;
        updated.IsTrashed.Should().BeTrue();
        updated.ParentId.Should().Be(WellKnownNodeIds.Trash);
        updated.OriginalParentId.Should().Be(parent.Id);
    }

    [Fact]
    public void UpdateSortOrders_AppliesAllUpdatesTransactionally()
    {
        var a = CreateNode(sortOrder: 1000, title: "A");
        var b = CreateNode(sortOrder: 2000, title: "B");
        _repository.Insert(a);
        _repository.Insert(b);

        _repository.UpdateSortOrders(
        [
            new NodeSortUpdate(a.Id, null, 5000),
            new NodeSortUpdate(b.Id, null, 6000)
        ]);

        _repository.GetById(a.Id)!.SortOrder.Should().Be(5000);
        _repository.GetById(b.Id)!.SortOrder.Should().Be(6000);
    }

    [Fact]
    public void GetNextSortOrder_ReturnsStep_WhenNoSiblingsExist()
    {
        _repository.GetNextSortOrder(null).Should().Be(1000);
    }

    [Fact]
    public void GetNextSortOrder_ReturnsIncrementedValue_WhenSiblingsExist()
    {
        _repository.Insert(CreateNode(sortOrder: 3000));

        _repository.GetNextSortOrder(null).Should().Be(4000);
    }
}
