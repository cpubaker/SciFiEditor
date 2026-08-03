using SciFiEditor.Core.Manuscript;
using SciFiEditor.Core.Projects;
using SciFiEditor.Domain;

namespace SciFiEditor.Core.Search;

public sealed class SearchService
{
    private readonly ProjectService _projectService;
    private readonly NodeService _nodeService;
    private readonly ManuscriptFileService _fileService;

    public SearchService(ProjectService projectService, NodeService nodeService, ManuscriptFileService fileService)
    {
        _projectService = projectService;
        _nodeService = nodeService;
        _fileService = fileService;
    }

    private OpenProject Project =>
        _projectService.Current ?? throw new InvalidOperationException("No project is open.");

    public async Task IndexNodeAsync(Guid nodeId)
    {
        var node = _nodeService.GetAll().FirstOrDefault(n => n.Id == nodeId);
        if (node is null)
        {
            return;
        }

        var body = node.NodeType == NodeType.Scene
            ? await _fileService.ReadSceneAsync(Project.RootPath, nodeId)
            : string.Empty;

        Project.Search.IndexNode(nodeId, node.Title, node.Synopsis, node.Notes, node.Label, body);
    }

    public IReadOnlyList<SearchResult> Search(string query) =>
        string.IsNullOrWhiteSpace(query) ? [] : Project.Search.Search(query);

    public async Task EnsureIndexPopulatedAsync()
    {
        if (Project.Search.GetIndexedCount() > 0)
        {
            return;
        }

        foreach (var node in _nodeService.GetAll())
        {
            await IndexNodeAsync(node.Id);
        }
    }
}
