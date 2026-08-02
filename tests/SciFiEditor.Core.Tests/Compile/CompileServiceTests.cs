using FluentAssertions;
using SciFiEditor.Core.Compile;
using SciFiEditor.Core.Manuscript;
using SciFiEditor.Core.Projects;
using SciFiEditor.Data;
using SciFiEditor.Domain;

namespace SciFiEditor.Core.Tests.Compile;

public class CompileServiceTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly ProjectService _projectService;
    private readonly NodeService _nodeService;
    private readonly ManuscriptFileService _fileService;
    private readonly CompileService _compileService;

    public CompileServiceTests()
    {
        var recentProjects = new RecentProjectsService(Path.Combine(_temp.Path, "recent.json"));
        _projectService = new ProjectService(recentProjects, new ProjectBackupService());
        _fileService = new ManuscriptFileService();
        _nodeService = new NodeService(_projectService, _fileService);
        _compileService = new CompileService(_projectService, _nodeService, _fileService);

        _projectService.CreateProject(_temp.Path, "CompileNovel");
    }

    public void Dispose()
    {
        _projectService.Current?.Dispose();
        _temp.Dispose();
    }

    [Fact]
    public async Task BuildManuscriptMarkdownAsync_IncludesSceneContentVerbatim()
    {
        var scene = _nodeService.AddNode(NodeType.Scene, "Scene One", null);
        await _fileService.WriteSceneAsync(_projectService.Current!.RootPath, scene.Id, "Once upon a time.");

        var markdown = await _compileService.BuildManuscriptMarkdownAsync();

        markdown.Should().Contain("Once upon a time.");
    }

    [Fact]
    public async Task BuildManuscriptMarkdownAsync_EmitsHeadingsForFoldersAndChaptersAtCorrectDepth()
    {
        var part = _nodeService.AddNode(NodeType.Folder, "Part One", null);
        var chapter = _nodeService.AddNode(NodeType.Chapter, "Chapter 1", part.Id);
        _nodeService.AddNode(NodeType.Scene, "Scene", chapter.Id);

        var markdown = await _compileService.BuildManuscriptMarkdownAsync();

        markdown.Should().Contain("# Part One");
        markdown.Should().Contain("## Chapter 1");
    }

    [Fact]
    public async Task BuildManuscriptMarkdownAsync_SkipsExcludedNodeAndItsSubtree()
    {
        var folder = _nodeService.AddNode(NodeType.Folder, "Excluded Part", null);
        var scene = _nodeService.AddNode(NodeType.Scene, "Hidden Scene", folder.Id);
        await _fileService.WriteSceneAsync(_projectService.Current!.RootPath, scene.Id, "Secret content.");
        _nodeService.SetIncludeInCompile(folder.Id, false);

        var markdown = await _compileService.BuildManuscriptMarkdownAsync();

        markdown.Should().NotContain("Excluded Part");
        markdown.Should().NotContain("Secret content.");
    }

    [Fact]
    public async Task BuildManuscriptMarkdownAsync_ExcludingSceneOnly_LeavesSiblingsIntact()
    {
        var sceneA = _nodeService.AddNode(NodeType.Scene, "A", null);
        var sceneB = _nodeService.AddNode(NodeType.Scene, "B", null);
        await _fileService.WriteSceneAsync(_projectService.Current!.RootPath, sceneA.Id, "Content A");
        await _fileService.WriteSceneAsync(_projectService.Current!.RootPath, sceneB.Id, "Content B");
        _nodeService.SetIncludeInCompile(sceneA.Id, false);

        var markdown = await _compileService.BuildManuscriptMarkdownAsync();

        markdown.Should().NotContain("Content A");
        markdown.Should().Contain("Content B");
    }

    [Fact]
    public async Task BuildManuscriptMarkdownAsync_AlwaysSkipsTrashedSubtree()
    {
        var scene = _nodeService.AddNode(NodeType.Scene, "Cut Scene", null);
        await _fileService.WriteSceneAsync(_projectService.Current!.RootPath, scene.Id, "Deleted content.");
        _nodeService.MoveToTrash(scene.Id);

        var markdown = await _compileService.BuildManuscriptMarkdownAsync();

        markdown.Should().NotContain("Deleted content.");
    }

    [Fact]
    public async Task BuildManuscriptMarkdownAsync_RespectsSortOrder()
    {
        var second = _nodeService.AddNode(NodeType.Scene, "Second", null);
        var first = _nodeService.AddNode(NodeType.Scene, "First", null);
        await _fileService.WriteSceneAsync(_projectService.Current!.RootPath, second.Id, "SECOND-CONTENT");
        await _fileService.WriteSceneAsync(_projectService.Current!.RootPath, first.Id, "FIRST-CONTENT");
        _nodeService.Reorder(
        [
            new NodeSortUpdate(first.Id, null, 1000),
            new NodeSortUpdate(second.Id, null, 2000)
        ]);

        var markdown = await _compileService.BuildManuscriptMarkdownAsync();

        markdown.IndexOf("FIRST-CONTENT", StringComparison.Ordinal)
            .Should().BeLessThan(markdown.IndexOf("SECOND-CONTENT", StringComparison.Ordinal));
    }
}
