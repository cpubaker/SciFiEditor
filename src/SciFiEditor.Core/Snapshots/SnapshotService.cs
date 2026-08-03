using SciFiEditor.Core.Manuscript;
using SciFiEditor.Core.Projects;
using SciFiEditor.Domain;

namespace SciFiEditor.Core.Snapshots;

public sealed class SnapshotService
{
    public const string ManualLabel = "Manual";
    public const string AutoLabel = "Auto";

    private readonly ProjectService _projectService;
    private readonly ManuscriptFileService _fileService;

    public SnapshotService(ProjectService projectService, ManuscriptFileService fileService)
    {
        _projectService = projectService;
        _fileService = fileService;
    }

    private OpenProject Project =>
        _projectService.Current ?? throw new InvalidOperationException("No project is open.");

    public async Task TakeSnapshotAsync(Guid nodeId, string label = ManualLabel)
    {
        var content = await _fileService.ReadSceneAsync(Project.RootPath, nodeId);
        Project.Snapshots.Insert(nodeId, DateTime.UtcNow, label, content);
    }

    public void RecordAutoSnapshotIfNeeded(Guid nodeId, string content)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        if (Project.Snapshots.HasSnapshotToday(nodeId, AutoLabel, today))
        {
            return;
        }

        Project.Snapshots.Insert(nodeId, DateTime.UtcNow, AutoLabel, content);
    }

    public async Task<string> RestoreSnapshotAsync(Guid nodeId, Guid snapshotId)
    {
        var content = Project.Snapshots.GetContent(snapshotId);
        await _fileService.WriteSceneAsync(Project.RootPath, nodeId, content);
        return content;
    }

    public IReadOnlyList<SceneSnapshotInfo> GetHistory(Guid nodeId) => Project.Snapshots.GetByNode(nodeId);
}
