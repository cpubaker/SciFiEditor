using FluentAssertions;
using SciFiEditor.Core.Manuscript;
using SciFiEditor.Core.Projects;
using SciFiEditor.Core.Snapshots;
using SciFiEditor.Data;
using SciFiEditor.Domain;

namespace SciFiEditor.Core.Tests.Snapshots;

public class SnapshotServiceTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly ProjectService _projectService;
    private readonly NodeService _nodeService;
    private readonly ManuscriptFileService _fileService;
    private readonly SnapshotService _snapshotService;

    public SnapshotServiceTests()
    {
        var recentProjects = new RecentProjectsService(Path.Combine(_temp.Path, "recent.json"));
        _projectService = new ProjectService(recentProjects, new ProjectBackupService());
        _fileService = new ManuscriptFileService();
        _nodeService = new NodeService(_projectService, _fileService);
        _snapshotService = new SnapshotService(_projectService, _fileService);

        _projectService.CreateProject(_temp.Path, "SnapshotNovel");
    }

    public void Dispose()
    {
        _projectService.Current?.Dispose();
        _temp.Dispose();
    }

    [Fact]
    public async Task TakeSnapshotAsync_ReadsCurrentFileContentAndInserts()
    {
        var scene = _nodeService.AddNode(NodeType.Scene, "Scene", null);
        await _fileService.WriteSceneAsync(_projectService.Current!.RootPath, scene.Id, "Draft text.");

        await _snapshotService.TakeSnapshotAsync(scene.Id);

        var history = _snapshotService.GetHistory(scene.Id);
        history.Should().ContainSingle(s => s.Label == SnapshotService.ManualLabel);
    }

    [Fact]
    public async Task TakeSnapshotAsync_CalledTwice_InsertsTwoSeparateSnapshots()
    {
        var scene = _nodeService.AddNode(NodeType.Scene, "Scene", null);
        await _fileService.WriteSceneAsync(_projectService.Current!.RootPath, scene.Id, "v1");
        await _snapshotService.TakeSnapshotAsync(scene.Id);

        await _fileService.WriteSceneAsync(_projectService.Current!.RootPath, scene.Id, "v2");
        await _snapshotService.TakeSnapshotAsync(scene.Id);

        _snapshotService.GetHistory(scene.Id).Should().HaveCount(2);
    }

    [Fact]
    public void RecordAutoSnapshotIfNeeded_FirstCallToday_Inserts()
    {
        var scene = _nodeService.AddNode(NodeType.Scene, "Scene", null);

        _snapshotService.RecordAutoSnapshotIfNeeded(scene.Id, "Auto content");

        _snapshotService.GetHistory(scene.Id).Should().ContainSingle(s => s.Label == SnapshotService.AutoLabel);
    }

    [Fact]
    public void RecordAutoSnapshotIfNeeded_SecondCallSameDay_IsNoOp()
    {
        var scene = _nodeService.AddNode(NodeType.Scene, "Scene", null);
        _snapshotService.RecordAutoSnapshotIfNeeded(scene.Id, "First");

        _snapshotService.RecordAutoSnapshotIfNeeded(scene.Id, "Second");

        _snapshotService.GetHistory(scene.Id).Should().ContainSingle();
    }

    [Fact]
    public async Task RestoreSnapshotAsync_WritesFileAndReturnsContent()
    {
        var scene = _nodeService.AddNode(NodeType.Scene, "Scene", null);
        await _fileService.WriteSceneAsync(_projectService.Current!.RootPath, scene.Id, "Original text.");
        await _snapshotService.TakeSnapshotAsync(scene.Id);
        var snapshotId = _snapshotService.GetHistory(scene.Id).Single().Id;
        await _fileService.WriteSceneAsync(_projectService.Current!.RootPath, scene.Id, "Edited text.");

        var restored = await _snapshotService.RestoreSnapshotAsync(scene.Id, snapshotId);

        restored.Should().Be("Original text.");
        (await _fileService.ReadSceneAsync(_projectService.Current!.RootPath, scene.Id)).Should().Be("Original text.");
    }
}
