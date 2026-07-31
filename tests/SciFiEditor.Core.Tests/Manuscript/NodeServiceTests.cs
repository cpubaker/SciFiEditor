using FluentAssertions;
using SciFiEditor.Core.Manuscript;
using SciFiEditor.Core.Projects;
using SciFiEditor.Data;
using SciFiEditor.Domain;

namespace SciFiEditor.Core.Tests.Manuscript;

public class NodeServiceTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly ProjectService _projectService;
    private readonly NodeService _nodeService;
    private readonly ManuscriptFileService _fileService;
    private readonly string _projectRoot;

    public NodeServiceTests()
    {
        var recentProjects = new RecentProjectsService(Path.Combine(_temp.Path, "recent.json"));
        _projectService = new ProjectService(recentProjects, new ProjectBackupService());
        _fileService = new ManuscriptFileService();
        _nodeService = new NodeService(_projectService, _fileService);

        _projectRoot = _projectService.CreateProject(_temp.Path, "MyNovel");
    }

    public void Dispose()
    {
        _projectService.Current?.Dispose();
        _temp.Dispose();
    }

    [Fact]
    public void AddNode_Scene_CreatesNodeAndSceneFile()
    {
        var node = _nodeService.AddNode(NodeType.Scene, "Chapter 1 - Scene 1", null);

        _nodeService.GetAll().Should().Contain(n => n.Id == node.Id);
        File.Exists(_fileService.GetScenePath(_projectRoot, node.Id)).Should().BeTrue();
    }

    [Fact]
    public void AddNode_Folder_DoesNotCreateSceneFile()
    {
        var node = _nodeService.AddNode(NodeType.Folder, "Part One", null);

        File.Exists(_fileService.GetScenePath(_projectRoot, node.Id)).Should().BeFalse();
    }

    [Fact]
    public void AddNode_UnderParent_SetsParentId()
    {
        var parent = _nodeService.AddNode(NodeType.Folder, "Part One", null);

        var child = _nodeService.AddNode(NodeType.Chapter, "Chapter 1", parent.Id);

        child.ParentId.Should().Be(parent.Id);
    }

    [Fact]
    public void Rename_UpdatesTitle()
    {
        var node = _nodeService.AddNode(NodeType.Scene, "Draft Title", null);

        _nodeService.Rename(node.Id, "Final Title");

        _nodeService.GetAll().Single(n => n.Id == node.Id).Title.Should().Be("Final Title");
    }

    [Fact]
    public void Rename_TrashNode_Throws()
    {
        var act = () => _nodeService.Rename(WellKnownNodeIds.Trash, "Not Allowed");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task Duplicate_Scene_CopiesFileContentAndCreatesNewNode()
    {
        var scene = _nodeService.AddNode(NodeType.Scene, "Original", null);
        await _fileService.WriteSceneAsync(_projectRoot, scene.Id, "Once upon a time...");

        var copy = _nodeService.Duplicate(scene.Id);

        copy.Id.Should().NotBe(scene.Id);
        copy.Title.Should().Be("Original copy");
        var copiedContent = await _fileService.ReadSceneAsync(_projectRoot, copy.Id);
        copiedContent.Should().Be("Once upon a time...");
    }

    [Fact]
    public void Duplicate_Folder_RecursivelyDuplicatesChildren()
    {
        var folder = _nodeService.AddNode(NodeType.Folder, "Part One", null);
        _nodeService.AddNode(NodeType.Scene, "Scene A", folder.Id);
        _nodeService.AddNode(NodeType.Scene, "Scene B", folder.Id);

        var copy = _nodeService.Duplicate(folder.Id);

        var copiedChildren = _nodeService.GetAll().Where(n => n.ParentId == copy.Id).ToList();
        copiedChildren.Should().HaveCount(2);
        copiedChildren.Select(n => n.Title).Should().BeEquivalentTo("Scene A", "Scene B");
    }

    [Fact]
    public void Duplicate_TrashNode_Throws()
    {
        var act = () => _nodeService.Duplicate(WellKnownNodeIds.Trash);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MoveToTrash_ReparentsNodeUnderTrash()
    {
        var node = _nodeService.AddNode(NodeType.Scene, "Cut Scene", null);

        _nodeService.MoveToTrash(node.Id);

        var updated = _nodeService.GetAll().Single(n => n.Id == node.Id);
        updated.ParentId.Should().Be(WellKnownNodeIds.Trash);
        updated.IsTrashed.Should().BeTrue();
    }

    [Fact]
    public void MoveToTrash_TrashNodeItself_Throws()
    {
        var act = () => _nodeService.MoveToTrash(WellKnownNodeIds.Trash);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Reorder_UpdatesSortOrderForSiblings()
    {
        var a = _nodeService.AddNode(NodeType.Scene, "A", null);
        var b = _nodeService.AddNode(NodeType.Scene, "B", null);

        _nodeService.Reorder(
        [
            new NodeSortUpdate(b.Id, null, 1000),
            new NodeSortUpdate(a.Id, null, 2000)
        ]);

        var all = _nodeService.GetAll().Where(n => n.Id == a.Id || n.Id == b.Id).OrderBy(n => n.SortOrder).ToList();
        all.Select(n => n.Id).Should().ContainInOrder(b.Id, a.Id);
    }

    [Fact]
    public void Reorder_IncludingTrashNode_Throws()
    {
        var act = () => _nodeService.Reorder([new NodeSortUpdate(WellKnownNodeIds.Trash, null, 1000)]);

        act.Should().Throw<InvalidOperationException>();
    }
}

public class NodeServiceNoProjectTests
{
    [Fact]
    public void GetAll_ThrowsWhenNoProjectOpen()
    {
        using var temp = new TempDirectory();
        var recentProjects = new RecentProjectsService(Path.Combine(temp.Path, "recent.json"));
        var projectService = new ProjectService(recentProjects, new ProjectBackupService());
        var nodeService = new NodeService(projectService, new ManuscriptFileService());

        var act = () => nodeService.GetAll();

        act.Should().Throw<InvalidOperationException>();
    }
}
