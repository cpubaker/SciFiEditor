using FluentAssertions;
using SciFiEditor.Domain;

namespace SciFiEditor.Data.Tests;

public class SearchRepositoryTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly ProjectDatabase _database;
    private readonly SearchRepository _searchRepository;
    private readonly NodeRepository _nodeRepository;

    public SearchRepositoryTests()
    {
        _database = new ProjectDatabase(_temp.Path);
        _searchRepository = new SearchRepository(_database);
        _nodeRepository = new NodeRepository(_database);
    }

    public void Dispose()
    {
        _database.Dispose();
        _temp.Dispose();
    }

    private Guid InsertNode(string title, bool isTrashed = false)
    {
        var node = new ManuscriptNode
        {
            Id = Guid.NewGuid(),
            NodeType = NodeType.Scene,
            Title = title,
            SortOrder = 1000,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        _nodeRepository.Insert(node);
        if (isTrashed)
        {
            _nodeRepository.MoveToTrash(node.Id, DateTime.UtcNow);
        }

        return node.Id;
    }

    [Fact]
    public void IndexNode_ThenSearch_FindsByTitle()
    {
        var nodeId = InsertNode("Chapter One");
        _searchRepository.IndexNode(nodeId, "Chapter One", "", "", "", "");

        var results = _searchRepository.Search("Chapter One");

        results.Should().ContainSingle(r => r.NodeId == nodeId);
    }

    [Fact]
    public void IndexNode_ThenSearch_FindsByBodyText()
    {
        var nodeId = InsertNode("Scene");
        _searchRepository.IndexNode(nodeId, "Scene", "", "", "", "The spaceship drifted silently through the void.");

        var results = _searchRepository.Search("spaceship drifted");

        results.Should().ContainSingle(r => r.NodeId == nodeId);
    }

    [Fact]
    public void Search_ReturnsNoResults_ForNonMatchingQuery()
    {
        var nodeId = InsertNode("Scene");
        _searchRepository.IndexNode(nodeId, "Scene", "", "", "", "Some prose.");

        _searchRepository.Search("nonexistent phrase").Should().BeEmpty();
    }

    [Fact]
    public void IndexNode_CalledAgainForSameNode_ReplacesRatherThanDuplicates()
    {
        var nodeId = InsertNode("Scene");
        _searchRepository.IndexNode(nodeId, "Scene", "", "", "", "Old content.");

        _searchRepository.IndexNode(nodeId, "Scene", "", "", "", "New content.");

        _searchRepository.GetIndexedCount().Should().Be(1);
        _searchRepository.Search("New content").Should().ContainSingle(r => r.NodeId == nodeId);
        _searchRepository.Search("Old content").Should().BeEmpty();
    }

    [Fact]
    public void Search_ExcludesTrashedNodes()
    {
        var nodeId = InsertNode("Trashed Scene", isTrashed: true);
        _searchRepository.IndexNode(nodeId, "Trashed Scene", "", "", "", "Content that should be hidden.");

        _searchRepository.Search("hidden").Should().BeEmpty();
    }

    [Fact]
    public void Search_HandlesSpecialCharacters_WithoutThrowing()
    {
        var nodeId = InsertNode("Scene");
        _searchRepository.IndexNode(nodeId, "Scene", "", "", "", "It's a \"test\" - with punctuation.");

        var act = () => _searchRepository.Search("it's a \"weird\" - query!");

        act.Should().NotThrow();
    }

    [Fact]
    public void GetIndexedCount_ReflectsNumberOfIndexedRows()
    {
        _searchRepository.GetIndexedCount().Should().Be(0);

        _searchRepository.IndexNode(InsertNode("A"), "A", "", "", "", "");
        _searchRepository.IndexNode(InsertNode("B"), "B", "", "", "", "");

        _searchRepository.GetIndexedCount().Should().Be(2);
    }
}
