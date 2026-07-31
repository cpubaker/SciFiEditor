using SciFiEditor.Data;
using SciFiEditor.Domain;

namespace SciFiEditor.Core.Projects;

public sealed class ProjectService
{
    private const string ManuscriptFolderName = "manuscript";

    private readonly RecentProjectsService _recentProjects;
    private readonly ProjectBackupService _backupService;
    private OpenProject? _current;

    public ProjectService(RecentProjectsService recentProjects, ProjectBackupService backupService)
    {
        _recentProjects = recentProjects;
        _backupService = backupService;
    }

    public OpenProject? Current => _current;

    public event EventHandler? ProjectOpened;

    public string CreateProject(string parentFolder, string projectName)
    {
        var rootPath = Path.Combine(parentFolder, projectName);
        if (Directory.Exists(rootPath) && Directory.EnumerateFileSystemEntries(rootPath).Any())
        {
            throw new InvalidOperationException($"Folder '{rootPath}' already exists and is not empty.");
        }

        Directory.CreateDirectory(rootPath);
        Directory.CreateDirectory(Path.Combine(rootPath, ManuscriptFolderName));

        Open(rootPath, seedTrashNode: true);
        return rootPath;
    }

    public void OpenProject(string rootPath)
    {
        var dbPath = Path.Combine(rootPath, ProjectDatabase.DatabaseFileName);
        if (!File.Exists(dbPath))
        {
            throw new FileNotFoundException($"'{rootPath}' is not a SciFiEditor project (missing project.db).", dbPath);
        }

        _backupService.RotateBackup(rootPath);
        Open(rootPath, seedTrashNode: false);
    }

    private void Open(string rootPath, bool seedTrashNode)
    {
        _current?.Dispose();
        var project = new OpenProject(rootPath);

        if (seedTrashNode && project.Nodes.GetById(WellKnownNodeIds.Trash) is null)
        {
            var now = DateTime.UtcNow;
            project.Nodes.Insert(new ManuscriptNode
            {
                Id = WellKnownNodeIds.Trash,
                ParentId = null,
                NodeType = NodeType.Trash,
                Title = "Trash",
                SortOrder = int.MaxValue,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        _current = project;
        _recentProjects.AddOrUpdate(new RecentProjectEntry
        {
            Path = rootPath,
            DisplayName = Path.GetFileName(rootPath),
            LastOpenedUtc = DateTime.UtcNow
        });

        ProjectOpened?.Invoke(this, EventArgs.Empty);
    }
}
