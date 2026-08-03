using FluentAssertions;
using SciFiEditor.Core.Manuscript;
using SciFiEditor.Core.Projects;
using SciFiEditor.Core.Search;
using SciFiEditor.Data;
using SciFiEditor.Domain;

namespace SciFiEditor.Core.Tests.Search;

public class SearchServiceTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly ProjectService _projectService;
    private readonly NodeService _nodeService;
    private readonly ManuscriptFileService _fileService;
    private readonly SearchService _searchService;

    public SearchServiceTests()
    {
        var recentProjects = new RecentProjectsService(Path.Combine(_temp.Path, "recent.json"));
        _projectService = new ProjectService(recentProjects, new ProjectBackupService());
        _fileService = new ManuscriptFileService();
        _nodeService = new NodeService(_projectService, _fileService);
        _searchService = new SearchService(_projectService, _nodeService, _fileService);

        _projectService.CreateProject(_temp.Path, "SearchNovel");
    }

    public void Dispose()
    {
        _projectService.Current?.Dispose();
        _temp.Dispose();
    }

    [Fact]
    public async Task IndexNodeAsync_IndexesSceneContent_FindableBySearch()
    {
        var scene = _nodeService.AddNode(NodeType.Scene, "Scene One", null);
        await _fileService.WriteSceneAsync(_projectService.Current!.RootPath, scene.Id, "The rocket launched at dawn.");

        await _searchService.IndexNodeAsync(scene.Id);

        _searchService.Search("rocket launched").Should().ContainSingle(r => r.NodeId == scene.Id);
    }

    [Fact]
    public async Task IndexNodeAsync_NonExistentNode_DoesNotThrow()
    {
        var act = async () => await _searchService.IndexNodeAsync(Guid.NewGuid());

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void Search_EmptyOrWhitespaceQuery_ReturnsEmpty()
    {
        _searchService.Search("").Should().BeEmpty();
        _searchService.Search("   ").Should().BeEmpty();
    }

    [Fact]
    public async Task EnsureIndexPopulatedAsync_IndexesAllExistingNodes_WhenIndexIsEmpty()
    {
        var sceneA = _nodeService.AddNode(NodeType.Scene, "Scene A", null);
        await _fileService.WriteSceneAsync(_projectService.Current!.RootPath, sceneA.Id, "Alpha content.");
        var sceneB = _nodeService.AddNode(NodeType.Scene, "Scene B", null);
        await _fileService.WriteSceneAsync(_projectService.Current!.RootPath, sceneB.Id, "Beta content.");

        await _searchService.EnsureIndexPopulatedAsync();

        _searchService.Search("Alpha").Should().ContainSingle(r => r.NodeId == sceneA.Id);
        _searchService.Search("Beta").Should().ContainSingle(r => r.NodeId == sceneB.Id);
    }

    [Fact]
    public async Task EnsureIndexPopulatedAsync_IsNoOp_WhenIndexAlreadyPopulated()
    {
        var sceneA = _nodeService.AddNode(NodeType.Scene, "Scene A", null);
        await _fileService.WriteSceneAsync(_projectService.Current!.RootPath, sceneA.Id, "Alpha content.");
        await _searchService.EnsureIndexPopulatedAsync();

        var sceneB = _nodeService.AddNode(NodeType.Scene, "Scene B", null);
        await _fileService.WriteSceneAsync(_projectService.Current!.RootPath, sceneB.Id, "Beta content.");
        await _searchService.EnsureIndexPopulatedAsync();

        _searchService.Search("Beta").Should().BeEmpty();
    }
}
