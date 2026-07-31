using SciFiEditor.Core.Projects;
using SciFiEditor.Data;
using SciFiEditor.Domain;

namespace SciFiEditor.Core.Manuscript;

public sealed class NodeService
{
    private readonly ProjectService _projectService;
    private readonly ManuscriptFileService _fileService;

    public NodeService(ProjectService projectService, ManuscriptFileService fileService)
    {
        _projectService = projectService;
        _fileService = fileService;
    }

    private OpenProject Project =>
        _projectService.Current ?? throw new InvalidOperationException("No project is open.");

    public IReadOnlyList<ManuscriptNode> GetAll() => Project.Nodes.GetAll();

    public ManuscriptNode AddNode(NodeType nodeType, string title, Guid? parentId)
    {
        var now = DateTime.UtcNow;
        var node = new ManuscriptNode
        {
            Id = Guid.NewGuid(),
            ParentId = parentId,
            NodeType = nodeType,
            Title = title,
            SortOrder = Project.Nodes.GetNextSortOrder(parentId),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        Project.Nodes.Insert(node);

        if (nodeType == NodeType.Scene)
        {
            _fileService.CreateSceneFile(Project.RootPath, node.Id);
        }

        return node;
    }

    public void Rename(Guid id, string title)
    {
        if (id == WellKnownNodeIds.Trash)
        {
            throw new InvalidOperationException("The Trash node cannot be renamed.");
        }

        Project.Nodes.UpdateTitle(id, title, DateTime.UtcNow);
    }

    public void UpdateInspector(Guid id, string synopsis, string notes, string label, NodeStatus status, int? targetWordCount)
    {
        if (id == WellKnownNodeIds.Trash)
        {
            throw new InvalidOperationException("The Trash node cannot be edited.");
        }

        Project.Nodes.UpdateInspector(id, synopsis, notes, label, status, targetWordCount, DateTime.UtcNow);
    }

    public void UpdateWordCounts(Guid id, int wordCount, int charCount) =>
        Project.Nodes.UpdateWordCounts(id, wordCount, charCount);

    public int GetProjectWordCount() => Project.Nodes.GetProjectWordCount();

    public ManuscriptNode Duplicate(Guid id)
    {
        if (id == WellKnownNodeIds.Trash)
        {
            throw new InvalidOperationException("The Trash node cannot be duplicated.");
        }

        var all = Project.Nodes.GetAll();
        var source = all.FirstOrDefault(n => n.Id == id)
            ?? throw new InvalidOperationException($"Node {id} not found.");

        return DuplicateSubtree(source, all, source.ParentId, $"{source.Title} copy");
    }

    private ManuscriptNode DuplicateSubtree(ManuscriptNode source, IReadOnlyList<ManuscriptNode> all, Guid? parentId, string title)
    {
        var now = DateTime.UtcNow;
        var copy = new ManuscriptNode
        {
            Id = Guid.NewGuid(),
            ParentId = parentId,
            NodeType = source.NodeType,
            Title = title,
            SortOrder = Project.Nodes.GetNextSortOrder(parentId),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        Project.Nodes.Insert(copy);

        if (source.NodeType == NodeType.Scene)
        {
            _fileService.CopySceneFile(Project.RootPath, source.Id, copy.Id);
        }

        foreach (var child in all.Where(n => n.ParentId == source.Id).OrderBy(n => n.SortOrder))
        {
            DuplicateSubtree(child, all, copy.Id, child.Title);
        }

        return copy;
    }

    public void MoveToTrash(Guid id)
    {
        if (id == WellKnownNodeIds.Trash)
        {
            throw new InvalidOperationException("The Trash node cannot be trashed.");
        }

        Project.Nodes.MoveToTrash(id, DateTime.UtcNow);
    }

    public void Reorder(IReadOnlyList<NodeSortUpdate> updates)
    {
        if (updates.Any(u => u.Id == WellKnownNodeIds.Trash))
        {
            throw new InvalidOperationException("The Trash node cannot be moved.");
        }

        Project.Nodes.UpdateSortOrders(updates);
    }
}
